using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Interfaces;

public interface IMediaConverter
{
    Task ConvertAsync(
        ConversionJob job,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default);
}
