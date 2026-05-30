using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Interfaces;

public interface IImageConverterService
{
    Task ConvertAsync(
        ConversionJob job,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default);
}
