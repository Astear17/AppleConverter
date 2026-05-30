namespace AppleLegacyMediaConverter.Core.Models;

public sealed record OutputPathResult(
    string Path,
    bool ShouldSkip = false,
    string? SkipReason = null);
