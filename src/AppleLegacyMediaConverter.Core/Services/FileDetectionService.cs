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
            return new FileTypeInfo(path, string.Empty, MediaKind.Unknown, false, "Unknown", "The path is empty.");
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (Images.Contains(extension))
        {
            var displayType = extension is ".heic" or ".heif" ? "Apple HEIC/HEIF image" : $"{extension.TrimStart('.').ToUpperInvariant()} image";
            return new FileTypeInfo(path, extension, MediaKind.Image, true, displayType);
        }

        if (Videos.Contains(extension))
        {
            var displayType = extension is ".mov" ? "Apple QuickTime MOV video" : $"{extension.TrimStart('.').ToUpperInvariant()} video";
            return new FileTypeInfo(path, extension, MediaKind.Video, true, displayType);
        }

        var reason = string.IsNullOrEmpty(extension)
            ? "Files without an extension are not supported."
            : $"The {extension} file type is not supported.";

        return new FileTypeInfo(path, extension, MediaKind.Unknown, false, "Unsupported file", reason);
    }
}
