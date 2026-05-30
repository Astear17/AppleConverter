using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Interfaces;

public interface IVideoConverterService
{
    Task ConvertToMp4Async(
        ConversionJob job,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default);

    Task ExtractFirstFrameAsync(
        ConversionJob job,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default);

    Task ExtractFramesEveryAsync(
        ConversionJob job,
        TimeSpan interval,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default);

    Task ExtractAllFramesAsync(
        ConversionJob job,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default);
}
