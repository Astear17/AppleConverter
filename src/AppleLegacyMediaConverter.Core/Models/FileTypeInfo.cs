namespace AppleLegacyMediaConverter.Core.Models;

public sealed record FileTypeInfo(
    string Path,
    string Extension,
    MediaKind Kind,
    bool IsSupported,
    string DisplayType,
    string? Reason = null);
