using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Services;

public sealed class ConversionQueueService : IConversionQueueService
{
    private readonly IFileDetectionService _fileDetection;
    private readonly IFileSystemService _fileSystem;
    private readonly ILivePhotoDetector _livePhotoDetector;
    private readonly IOutputNamingService _outputNaming;
    private readonly IMediaConverter _mediaConverter;
    private readonly ILoggerService _logger;

    public ConversionQueueService(
        IFileDetectionService fileDetection,
        IFileSystemService fileSystem,
        ILivePhotoDetector livePhotoDetector,
        IOutputNamingService outputNaming,
        IMediaConverter mediaConverter,
        ILoggerService logger)
    {
        _fileDetection = fileDetection;
        _fileSystem = fileSystem;
        _livePhotoDetector = livePhotoDetector;
        _outputNaming = outputNaming;
        _mediaConverter = mediaConverter;
        _logger = logger;
    }

    public Task<IReadOnlyList<MediaFileItem>> AddInputsAsync(
        IEnumerable<string> paths,
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var items = new List<MediaFileItem>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in paths.Where(static value => !string.IsNullOrWhiteSpace(value)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fullPath = Path.GetFullPath(path);

                if (_fileSystem.DirectoryExists(fullPath))
                {
                    AddFolder(fullPath, settings, seen, items, cancellationToken);
                    continue;
                }

                AddFile(fullPath, relativePath: null, seen, items);
            }

            _livePhotoDetector.Detect(items);
            return (IReadOnlyList<MediaFileItem>)items;
        }, cancellationToken);
    }

    public async Task<ConversionSummary> ConvertAsync(
        IReadOnlyList<MediaFileItem> items,
        ConversionBatchOptions options,
        IProgress<ConversionProgressUpdate>? progress,
        CancellationToken cancellationToken = default)
    {
        options.Settings.Normalize();
        var processable = items
            .Where(item => item.IsSupported)
            .Where(item => item.Status is ConversionStatus.Pending or ConversionStatus.Failed or ConversionStatus.Cancelled)
            .ToArray();

        if (processable.Length == 0)
        {
            return CreateSummary(items, options);
        }

        var total = Math.Max(1, processable.Length);
        var finishedInThisRun = 0;

        await Parallel.ForEachAsync(
            processable,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = options.Settings.ParallelConversionLimit,
                CancellationToken = cancellationToken
            },
            async (item, token) =>
            {
                try
                {
                    if (TryApplyLivePhotoPolicy(item, options, out var skipReason))
                    {
                        item.MarkSkipped(skipReason);
                        Report(item, progress, Interlocked.Increment(ref finishedInThisRun), total, 0);
                        return;
                    }

                    var job = CreateJob(item, options);
                    if (job is null)
                    {
                        if (item.Status != ConversionStatus.Skipped)
                        {
                            item.MarkSkipped("The selected conversion mode does not apply to this file.");
                        }

                        Report(item, progress, Interlocked.Increment(ref finishedInThisRun), total, 0);
                        return;
                    }

                    if (job.OutputPath is { Length: > 0 })
                    {
                        item.MarkConverting();
                    }

                    var itemProgress = new Progress<double>(value =>
                    {
                        item.MarkProgress(value);
                        var totalProgress = ((Volatile.Read(ref finishedInThisRun) + value / 100d) / total) * 100d;
                        progress?.Report(new ConversionProgressUpdate(item, value, totalProgress, item.Status));
                    });

                    await _mediaConverter.ConvertAsync(job, itemProgress, token).ConfigureAwait(false);

                    item.MarkCompleted(job.OutputPath);
                    TryPreserveTimestamps(item.SourcePath, job.OutputPath, job.Settings);
                    await _logger.LogInfoAsync($"Converted {item.SourcePath} -> {job.OutputPath}", token).ConfigureAwait(false);
                    Report(item, progress, Interlocked.Increment(ref finishedInThisRun), total, 100);
                }
                catch (OperationCanceledException)
                {
                    item.MarkCancelled();
                    Report(item, progress, Interlocked.Increment(ref finishedInThisRun), total, 0);
                }
                catch (MediaConversionException ex)
                {
                    item.MarkFailed(ex.UserMessage, ex.TechnicalDetails);
                    await _logger.LogErrorAsync($"Failed to convert {item.SourcePath}: {ex.UserMessage}", ex, CancellationToken.None)
                        .ConfigureAwait(false);
                    Report(item, progress, Interlocked.Increment(ref finishedInThisRun), total, 0);
                }
                catch (Exception ex)
                {
                    item.MarkFailed("The file could not be converted. See error details for more information.", ex.ToString());
                    await _logger.LogErrorAsync($"Unexpected conversion failure for {item.SourcePath}", ex, CancellationToken.None)
                        .ConfigureAwait(false);
                    Report(item, progress, Interlocked.Increment(ref finishedInThisRun), total, 0);
                }
            }).ConfigureAwait(false);

        return CreateSummary(items, options);
    }

    private void AddFolder(
        string folderPath,
        AppSettings settings,
        HashSet<string> seen,
        List<MediaFileItem> items,
        CancellationToken cancellationToken)
    {
        foreach (var file in _fileSystem.EnumerateFiles(folderPath, settings.RecursiveFolderScanning))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = settings.KeepFolderStructure
                ? Path.GetRelativePath(folderPath, file)
                : null;
            AddFile(file, relativePath, seen, items);
        }
    }

    private void AddFile(string path, string? relativePath, HashSet<string> seen, List<MediaFileItem> items)
    {
        var fullPath = Path.GetFullPath(path);
        if (!seen.Add(fullPath))
        {
            return;
        }

        var info = _fileDetection.Detect(fullPath);
        items.Add(new MediaFileItem(fullPath, info.Kind, info.DisplayType, info.IsSupported, relativePath, info.Reason));
    }

    private ConversionJob? CreateJob(MediaFileItem item, ConversionBatchOptions options)
    {
        var mode = ResolveMode(item, options);
        if (mode is null)
        {
            return null;
        }

        var outputFormat = ResolveOutputFormat(mode.Value, options);
        var suffix = ResolveSuffix(item, mode.Value);
        var isPattern = mode is ConversionMode.ExtractFramesEveryNSeconds or ConversionMode.ExtractAllFrames;
        var result = _outputNaming.CreateOutputPath(
            item.SourcePath,
            item.RelativePath,
            outputFormat,
            options,
            suffix,
            isPattern);

        if (result.ShouldSkip)
        {
            item.MarkSkipped(result.SkipReason ?? "Skipped because the output file already exists.");
            return null;
        }

        return new ConversionJob(item, result.Path, mode.Value, outputFormat, options.Settings, options.FrameIntervalSeconds);
    }

    private static ConversionMode? ResolveMode(MediaFileItem item, ConversionBatchOptions options)
    {
        if (item.IsLivePhoto && options.LivePhotoAction == LivePhotoAction.ExtractPreviewFrameFromVideo && item.MediaKind == MediaKind.Video)
        {
            return ConversionMode.ExtractFirstFrame;
        }

        if (options.ConversionMode == ConversionMode.Auto)
        {
            return item.MediaKind switch
            {
                MediaKind.Image => ConversionMode.ImageConversion,
                MediaKind.Video => ConversionMode.VideoToMp4,
                _ => null
            };
        }

        return item.MediaKind switch
        {
            MediaKind.Image when options.ConversionMode == ConversionMode.ImageConversion => ConversionMode.ImageConversion,
            MediaKind.Video when options.ConversionMode != ConversionMode.ImageConversion => options.ConversionMode,
            _ => null
        };
    }

    private static OutputFormat ResolveOutputFormat(ConversionMode mode, ConversionBatchOptions options)
    {
        return mode == ConversionMode.VideoToMp4
            ? OutputFormat.Mp4
            : options.ImageOutputFormat;
    }

    private static string? ResolveSuffix(MediaFileItem item, ConversionMode mode)
    {
        return mode switch
        {
            ConversionMode.VideoToMp4 when item.IsLivePhoto => "_video",
            ConversionMode.ExtractFirstFrame => "_frame_0001",
            ConversionMode.ExtractFramesEveryNSeconds => "_frame",
            ConversionMode.ExtractAllFrames => "_frame",
            _ => null
        };
    }

    private static bool TryApplyLivePhotoPolicy(MediaFileItem item, ConversionBatchOptions options, out string reason)
    {
        reason = string.Empty;
        if (!item.IsLivePhoto)
        {
            return false;
        }

        if (options.LivePhotoAction == LivePhotoAction.ConvertStillOnly && item.MediaKind == MediaKind.Video)
        {
            reason = "Skipped because this Live Photo option converts the still image only.";
            return true;
        }

        if (options.LivePhotoAction == LivePhotoAction.ConvertVideoOnly && item.MediaKind == MediaKind.Image)
        {
            reason = "Skipped because this Live Photo option converts the motion video only.";
            return true;
        }

        if (options.LivePhotoAction == LivePhotoAction.ExtractPreviewFrameFromVideo && item.MediaKind == MediaKind.Image)
        {
            reason = "Skipped because this Live Photo option extracts a preview frame from the MOV.";
            return true;
        }

        return false;
    }

    private void TryPreserveTimestamps(string sourcePath, string outputPath, AppSettings settings)
    {
        if (!settings.PreserveTimestamps || outputPath.Contains("%", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            _fileSystem.SetCreationTimeUtc(outputPath, _fileSystem.GetCreationTimeUtc(sourcePath));
            _fileSystem.SetLastWriteTimeUtc(outputPath, _fileSystem.GetLastWriteTimeUtc(sourcePath));
        }
        catch
        {
            // Timestamp preservation is best-effort and should never turn a successful conversion into a failure.
        }
    }

    private static void Report(
        MediaFileItem item,
        IProgress<ConversionProgressUpdate>? progress,
        int finished,
        int total,
        double itemProgress)
    {
        var totalProgress = Math.Clamp(finished / (double)Math.Max(1, total) * 100d, 0, 100);
        progress?.Report(new ConversionProgressUpdate(item, itemProgress, totalProgress, item.Status));
    }

    private static ConversionSummary CreateSummary(IReadOnlyList<MediaFileItem> items, ConversionBatchOptions options)
    {
        return new ConversionSummary(
            items.Count(static item => item.Status == ConversionStatus.Completed),
            items.Count(static item => item.Status == ConversionStatus.Failed),
            items.Count(static item => item.Status == ConversionStatus.Skipped),
            items.Count(static item => item.Status == ConversionStatus.Cancelled),
            options.OutputFolderMode == OutputFolderMode.CustomFolder ? options.CustomOutputFolder : null);
    }
}
