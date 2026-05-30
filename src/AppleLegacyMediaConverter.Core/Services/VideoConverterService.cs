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
            "-nostdin",
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
            job.Settings.VideoEncoderPreset,
            "-crf",
            job.Settings.VideoConstantRateFactor.ToString(CultureInfo.InvariantCulture),
            "-threads:v",
            job.Settings.FFmpegThreadCount.ToString(CultureInfo.InvariantCulture),
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
            "-nostdin",
            "-y",
            "-i",
            job.SourceItem.SourcePath,
            "-frames:v",
            "1",
            "-threads:v",
            job.Settings.FFmpegThreadCount.ToString(CultureInfo.InvariantCulture),
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
            "-nostdin",
            "-y",
            "-i",
            job.SourceItem.SourcePath,
            "-vf",
            $"fps=1/{seconds}",
            "-threads:v",
            job.Settings.FFmpegThreadCount.ToString(CultureInfo.InvariantCulture),
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
            "-nostdin",
            "-y",
            "-i",
            job.SourceItem.SourcePath,
            "-threads:v",
            job.Settings.FFmpegThreadCount.ToString(CultureInfo.InvariantCulture),
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
                "Thiếu FFmpeg. Hãy đặt đường dẫn FFmpeg trong Cài đặt hoặc đặt ffmpeg.exe vào thư mục tools của ứng dụng.",
                "Không tìm thấy đường dẫn FFmpeg.");
        }

        progress?.Report(2);

        using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(ffmpegPath) ?? AppContext.BaseDirectory
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
                "Không khởi động được FFmpeg. Hãy kiểm tra đường dẫn backend trong Cài đặt.",
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
        var stdout = await stdoutTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var stderr = errorBuilder.ToString();
            var details = BuildFFmpegDetails(ffmpegPath, arguments, process.ExitCode, stdout, stderr);
            throw new MediaConversionException(
                CreateFFmpegUserMessage(stderr),
                details);
        }

        progress?.Report(100);
    }

    private static string BuildFFmpegDetails(
        string ffmpegPath,
        IReadOnlyList<string> arguments,
        int exitCode,
        string stdout,
        string stderr)
    {
        return string.Join(
            Environment.NewLine,
            "FFmpeg failed.",
            $"Exit code: {exitCode}",
            $"Executable: {ffmpegPath}",
            $"Arguments: {FormatArguments(arguments)}",
            string.Empty,
            "stdout:",
            string.IsNullOrWhiteSpace(stdout) ? "(empty)" : stdout.Trim(),
            string.Empty,
            "stderr:",
            string.IsNullOrWhiteSpace(stderr) ? "(empty)" : stderr.Trim());
    }

    private static string CreateFFmpegUserMessage(string stderr)
    {
        var lower = stderr.ToLowerInvariant();
        if (lower.Contains("unknown encoder 'libx264'", StringComparison.Ordinal))
        {
            return "Bản FFmpeg này thiếu bộ mã hóa H.264 cần cho MP4 tương thích.";
        }

        if (lower.Contains("permission denied", StringComparison.Ordinal))
        {
            return "FFmpeg không ghi được vào thư mục đầu ra. Hãy kiểm tra quyền thư mục hoặc chọn thư mục khác.";
        }

        if (lower.Contains("no space left", StringComparison.Ordinal) || lower.Contains("not enough space", StringComparison.Ordinal))
        {
            return "Ổ đĩa đầu ra không còn đủ dung lượng trống.";
        }

        if (lower.Contains("invalid data found", StringComparison.Ordinal) || lower.Contains("moov atom not found", StringComparison.Ordinal))
        {
            return "FFmpeg không đọc được video này. Tệp có thể bị hỏng hoặc chưa đầy đủ.";
        }

        var lastUsefulLine = stderr
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => line.Trim())
            .LastOrDefault(static line =>
                !line.StartsWith("frame=", StringComparison.OrdinalIgnoreCase) &&
                !line.StartsWith("size=", StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(lastUsefulLine)
            ? "FFmpeg không chuyển được video này. Hãy sao chép chi tiết lỗi để xem log backend đầy đủ."
            : $"FFmpeg không chuyển được video này: {lastUsefulLine}";
    }

    private static string FormatArguments(IReadOnlyList<string> arguments)
    {
        return string.Join(" ", arguments.Select(static argument =>
            argument.Contains(' ', StringComparison.Ordinal) || argument.Contains('"', StringComparison.Ordinal)
                ? $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
                : argument));
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
