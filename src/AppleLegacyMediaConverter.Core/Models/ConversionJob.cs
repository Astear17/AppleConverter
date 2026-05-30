namespace AppleLegacyMediaConverter.Core.Models;

public sealed record ConversionJob(
    MediaFileItem SourceItem,
    string OutputPath,
    ConversionMode Mode,
    OutputFormat OutputFormat,
    AppSettings Settings,
    int? FrameIntervalSeconds = null);
