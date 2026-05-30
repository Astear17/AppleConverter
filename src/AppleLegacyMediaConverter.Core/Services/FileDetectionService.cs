using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Services;

public sealed class FileDetectionService : IFileDetectionService
{
    private static readonly HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase)
    {
        ".heic",
        ".heif",
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".tiff",
        ".tif",
        ".bmp"
    };

    private static readonly HashSet<string> Videos = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mov",
        ".mp4",
        ".m4v"
    };

    public IReadOnlySet<string> SupportedImageExtensions => Images;

    public IReadOnlySet<string> SupportedVideoExtensions => Videos;

    public FileTypeInfo Detect(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new FileTypeInfo(path, string.Empty, MediaKind.Unknown, false, "Không xác định", "Đường dẫn trống.");
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (Images.Contains(extension))
        {
            var displayType = extension is ".heic" or ".heif"
                ? "Ảnh Apple HEIC/HEIF"
                : $"Ảnh {extension.TrimStart('.').ToUpperInvariant()}";
            return new FileTypeInfo(path, extension, MediaKind.Image, true, displayType);
        }

        if (Videos.Contains(extension))
        {
            var displayType = extension is ".mov"
                ? "Video Apple QuickTime MOV"
                : $"Video {extension.TrimStart('.').ToUpperInvariant()}";
            return new FileTypeInfo(path, extension, MediaKind.Video, true, displayType);
        }

        var reason = string.IsNullOrEmpty(extension)
            ? "Tệp không có phần mở rộng chưa được hỗ trợ."
            : $"Loại tệp {extension} chưa được hỗ trợ.";

        return new FileTypeInfo(path, extension, MediaKind.Unknown, false, "Tệp chưa hỗ trợ", reason);
    }
}
