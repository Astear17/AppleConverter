using System.Diagnostics;
using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Services;

public sealed class BackendStatusService : IBackendStatusService
{
    private readonly IFileSystemService _fileSystem;
    private readonly IFileDetectionService _fileDetection;

    public BackendStatusService(IFileSystemService fileSystem, IFileDetectionService fileDetection)
    {
        _fileSystem = fileSystem;
        _fileDetection = fileDetection;
    }

    public async Task<BackendStatus> GetStatusAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var ffmpegPath = FindFFmpegPath(settings);
        var ffmpegFound = ffmpegPath is not null;
        var message = ffmpegFound
            ? await GetFFmpegVersionAsync(ffmpegPath!, cancellationToken).ConfigureAwait(false)
            : "Không tìm thấy FFmpeg. Hãy đặt đường dẫn trong Cài đặt hoặc đặt ffmpeg.exe vào tools\\ffmpeg cạnh ứng dụng.";

        return new BackendStatus(
            ffmpegFound,
            ffmpegPath,
            message,
            ImageBackendAvailable: true,
            ImageBackendMessage: "Magick.NET đã sẵn sàng để chuyển ảnh. HEIC/HEIF phụ thuộc vào delegate ImageMagick được đóng gói.",
            _fileDetection.SupportedImageExtensions.OrderBy(static value => value).ToArray(),
            _fileDetection.SupportedVideoExtensions.OrderBy(static value => value).ToArray(),
            new[] { ".jpg", ".jpeg", ".png", ".mp4" });
    }

    public string? FindFFmpegPath(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.FFmpegPath) && _fileSystem.FileExists(settings.FFmpegPath))
        {
            return settings.FFmpegPath;
        }

        var appLocal = Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg", "ffmpeg.exe");
        if (_fileSystem.FileExists(appLocal))
        {
            return appLocal;
        }

        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathVariable))
        {
            return null;
        }

        foreach (var path in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(path, "ffmpeg.exe");
            if (_fileSystem.FileExists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static async Task<string> GetFFmpegVersionAsync(string ffmpegPath, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            process.StartInfo.ArgumentList.Add("-version");

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);
            var firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                ?? error.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            return firstLine?.Trim() ?? "Đã tìm thấy FFmpeg nhưng không đọc được thông tin phiên bản.";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"Đã tìm thấy FFmpeg nhưng kiểm tra phiên bản thất bại: {ex.Message}";
        }
    }
}
