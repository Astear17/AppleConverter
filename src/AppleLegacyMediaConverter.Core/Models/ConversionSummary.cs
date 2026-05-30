namespace AppleLegacyMediaConverter.Core.Models;

public sealed record ConversionSummary(
    int Completed,
    int Failed,
    int Skipped,
    int Cancelled,
    string? OutputFolder)
{
    public int Total => Completed + Failed + Skipped + Cancelled;
}
