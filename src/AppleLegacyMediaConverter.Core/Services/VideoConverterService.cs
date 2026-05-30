using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Services;

public sealed partial class VideoConverterService : IVideoConverterService
{
    private readonly IBackendStatusService _backendStatusService;

    public VideoConverterService(IBackendStatusService backendStatusService)
    {
        _backendStatusService = backendStatusService;
    }

    public Task ConvertToMp4Async(
        ConversionJob job,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        var args = new[]
        {
            "-hide_banner",
            "-y",
            "-i",
            job.SourceItem.SourcePath,
            "-map",
            "0:v:0",
            "-map",
            "0:a?",
            "-c:v",
            "libx264",
            "-pix_fmt",
            "yuv420p",
            "-preset",
            "medium",
            "-crf",
            "20",
            "-c:a",
            "aac",
            "-movflags",
            "+faststart",
            job.OutputPath
        };

        return RunFFmpegAsync(job.Settings, args, progress, cancellationToken);
    }

    public Task ExtractFirstFrameAsync(
        ConversionJob job,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        var args = new[]
        {
            "-hide_banner",
            "-y",
            "-i",
            job.SourceItem.SourcePath,
            "-frames:v",
            "1",
            "-q:v",
            "2",
            job.OutputPath
        };

        return RunFFmpegAsync(job.Settings, args, progress, cancellationToken);
    }

    public Task ExtractFramesEveryAsync(
        ConversionJob job,
        TimeSpan interval,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        var seconds = Math.Max(1, (int)Math.Round(interval.TotalSeconds));
        var args = new[]
        {
            "-hide_banner",
            "-y",
            "-i",
            job.SourceItem.SourcePath,
            "-vf",
            $"fps=1/{seconds}",
            "-q:v",
            "2",
            job.OutputPath
        };

        return RunFFmpegAsync(job.Settings, args, progress, cancellationToken);
    }

    public Task ExtractAllFramesAsync(
        ConversionJob job,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        var args = new[]
        {
            "-hide_banner",
            "-y",
            "-i",
            job.SourceItem.SourcePath,
            "-q:v",
            "2",
            job.OutputPath
        };

        return RunFFmpegAsync(job.Settings, args, progress, cancellationToken);
    }

    private async Task RunFFmpegAsync(
        AppSettings settings,
        IReadOnlyList<string> arguments,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        var ffmpegPath = _backendStatusService.FindFFmpegPath(settings);
        if (ffmpegPath is null)
        {
            throw new MediaConversionException(
                "FFmpeg is missing. Set the FFmpeg path in Settings or place ffmpeg.exe in the app tools folder.",
                "FFmpeg path lookup failed.");
        }

        progress?.Report(2);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        var errorBuilder = new StringBuilder();
        TimeSpan? duration = null;

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new MediaConversionException(
                "FFmpeg could not be started. Check the backend path in Settings.",
                ex.ToString(),
                ex);
        }

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // The process may already have exited.
            }
        });

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = Task.Run(async () =>
        {
            while (!process.StandardError.EndOfStream)
            {
                var line = await process.StandardError.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    continue;
                }

                errorBuilder.AppendLine(line);
                duration ??= TryParseDuration(line);
                var currentTime = TryParseProgressTime(line);
                if (duration is { TotalMilliseconds: > 0 } && currentTime is not null)
                {
                    var percent = Math.Clamp(currentTime.Value.TotalMilliseconds / duration.Value.TotalMilliseconds * 100, 2, 99);
                    progress?.Report(percent);
                }
            }
        }, cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new MediaConversionException(
                "FFmpeg could not convert this video. It may be corrupt, unsupported, or blocked by permissions.",
                errorBuilder.ToString());
        }

        progress?.Report(100);
    }

    private static TimeSpan? TryParseDuration(string line)
    {
        var match = DurationRegex().Match(line);
        return match.Success && TimeSpan.TryParse(match.Groups["value"].Value, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static TimeSpan? TryParseProgressTime(string line)
    {
        var match = ProgressRegex().Match(line);
        return match.Success && TimeSpan.TryParse(match.Groups["value"].Value, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    [GeneratedRegex(@"Duration:\s(?<value>\d{2}:\d{2}:\d{2}\.\d{2})")]
    private static partial Regex DurationRegex();

    [GeneratedRegex(@"time=(?<value>\d{2}:\d{2}:\d{2}\.\d{2})")]
    private static partial Regex ProgressRegex();
}
