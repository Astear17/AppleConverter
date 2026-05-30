using System.Collections.Concurrent;
using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;
using AppleLegacyMediaConverter.Core.Services;

namespace AppleLegacyMediaConverter.Tests;

public sealed class ConversionQueueServiceTests
{
    [Fact]
    public async Task AutoModeUsesImageSettingsAndSeparateVideoMode()
    {
        var converter = new RecordingMediaConverter();
        var service = CreateService(converter);
        var image = new MediaFileItem(@"C:\In\IMG_1001.HEIC", MediaKind.Image, "Ảnh Apple HEIC/HEIF", true);
        var video = new MediaFileItem(@"C:\In\CLIP_1001.MOV", MediaKind.Video, "Video Apple QuickTime MOV", true);

        await service.ConvertAsync(
            new[] { image, video },
            new ConversionBatchOptions
            {
                ConversionMode = ConversionMode.Auto,
                VideoConversionMode = ConversionMode.ExtractFirstFrame,
                ImageOutputFormat = OutputFormat.Png,
                CustomOutputFolder = @"C:\Out",
                LivePhotoAction = LivePhotoAction.ConvertBoth
            },
            null);

        Assert.Contains(converter.Jobs, job => job.SourceItem == image && job.Mode == ConversionMode.ImageConversion && job.OutputFormat == OutputFormat.Png);
        Assert.Contains(converter.Jobs, job => job.SourceItem == video && job.Mode == ConversionMode.ExtractFirstFrame);
    }

    [Fact]
    public async Task RemoveLivePhotoMotionKeepsOnlyStillImage()
    {
        var converter = new RecordingMediaConverter();
        var service = CreateService(converter);
        var still = new MediaFileItem(@"C:\In\IMG_1234.HEIC", MediaKind.Image, "Ảnh Apple HEIC/HEIF", true);
        var motion = new MediaFileItem(@"C:\In\IMG_1234.MOV", MediaKind.Video, "Video Apple QuickTime MOV", true);
        new LivePhotoDetector().Detect(new[] { still, motion });

        await service.ConvertAsync(
            new[] { still, motion },
            new ConversionBatchOptions
            {
                ConversionMode = ConversionMode.Auto,
                VideoConversionMode = ConversionMode.VideoToMp4,
                ImageOutputFormat = OutputFormat.Jpg,
                CustomOutputFolder = @"C:\Out",
                LivePhotoAction = LivePhotoAction.RemoveMotionKeepStill
            },
            null);

        Assert.Single(converter.Jobs);
        Assert.Equal(still, converter.Jobs.Single().SourceItem);
        Assert.Equal(ConversionStatus.Completed, still.Status);
        Assert.Equal(ConversionStatus.Skipped, motion.Status);
        Assert.Contains("loại bỏ", motion.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static ConversionQueueService CreateService(IMediaConverter converter)
    {
        var fileSystem = new FakeFileSystem();
        return new ConversionQueueService(
            new FileDetectionService(),
            fileSystem,
            new LivePhotoDetector(),
            new OutputNamingService(fileSystem),
            converter,
            new NoOpLogger());
    }

    private sealed class RecordingMediaConverter : IMediaConverter
    {
        private readonly ConcurrentBag<ConversionJob> _jobs = new();

        public IReadOnlyCollection<ConversionJob> Jobs => _jobs.ToArray();

        public Task ConvertAsync(ConversionJob job, IProgress<double>? progress, CancellationToken cancellationToken = default)
        {
            _jobs.Add(job);
            progress?.Report(100);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileSystem : IFileSystemService
    {
        public bool FileExists(string path) => false;

        public bool DirectoryExists(string path) => true;

        public void CreateDirectory(string path)
        {
        }

        public IEnumerable<string> EnumerateFiles(string folderPath, bool recursive) => Enumerable.Empty<string>();

        public DateTime GetCreationTimeUtc(string path) => DateTime.UnixEpoch;

        public DateTime GetLastWriteTimeUtc(string path) => DateTime.UnixEpoch;

        public void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
        {
        }

        public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        {
        }
    }

    private sealed class NoOpLogger : ILoggerService
    {
        public string LogDirectory => Path.GetTempPath();

        public string CurrentLogPath => Path.Combine(LogDirectory, "apple-converter-test.log");

        public Task LogInfoAsync(string message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LogWarningAsync(string message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LogErrorAsync(string message, Exception? exception = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
