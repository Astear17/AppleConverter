using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Interfaces;

public interface IFileDetectionService
{
    IReadOnlySet<string> SupportedImageExtensions { get; }

    IReadOnlySet<string> SupportedVideoExtensions { get; }

    FileTypeInfo Detect(string path);
}
