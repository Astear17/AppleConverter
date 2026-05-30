using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Services;

public sealed class MediaConverter : IMediaConverter
{
    private readonly IImageConverterService _imageConverter;
    private readonly IVideoConverterService _videoConverter;

    public MediaConverter(IImageConverterService imageConverter, IVideoConverterService videoConverter)
    {
        _imageConverter = imageConverter;
        _videoConverter = videoConverter;
    }

    public Task ConvertAsync(
        ConversionJob job,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        return job.Mode switch
        {
            ConversionMode.ImageConversion => _imageConverter.ConvertAsync(job, progress, cancellationToken),
            ConversionMode.VideoToMp4 => _videoConverter.ConvertToMp4Async(job, progress, cancellationToken),
            ConversionMode.ExtractFirstFrame => _videoConverter.ExtractFirstFrameAsync(job, progress, cancellationToken),
            ConversionMode.ExtractFramesEveryNSeconds => _videoConverter.ExtractFramesEveryAsync(
                job,
                TimeSpan.FromSeconds(job.FrameIntervalSeconds ?? job.Settings.FrameIntervalSeconds),
                progress,
                cancellationToken),
            ConversionMode.ExtractAllFrames => _videoConverter.ExtractAllFramesAsync(job, progress, cancellationToken),
            _ => throw new MediaConversionException("No conversion mode was selected.", $"Unsupported mode: {job.Mode}")
        };
    }
}
