using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Interfaces;

public interface IConversionQueueService
{
    Task<IReadOnlyList<MediaFileItem>> AddInputsAsync(
        IEnumerable<string> paths,
        AppSettings settings,
        CancellationToken cancellationToken = default);

    Task<ConversionSummary> ConvertAsync(
        IReadOnlyList<MediaFileItem> items,
        ConversionBatchOptions options,
        IProgress<ConversionProgressUpdate>? progress,
        CancellationToken cancellationToken = default);
}
