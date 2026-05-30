namespace AppleLegacyMediaConverter.Core.Models;

public sealed record ConversionProgressUpdate(
    MediaFileItem Item,
    double ItemProgress,
    double TotalProgress,
    ConversionStatus Status,
    string? Message = null);
