using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;
using ImageMagick;

namespace AppleLegacyMediaConverter.Core.Services;

public sealed class ImageConverterService : IImageConverterService
{
    public async Task ConvertAsync(
        ConversionJob job,
        IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(5);

        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var image = new MagickImage(job.SourceItem.SourcePath);
                ApplyResize(image, job.Settings);

                if (job.Settings.MetadataBehavior == MetadataBehavior.StripForPrivacy)
                {
                    image.Strip();
                }

                switch (job.OutputFormat)
                {
                    case OutputFormat.Jpg:
                    case OutputFormat.Jpeg:
                        image.Format = MagickFormat.Jpeg;
                        image.Quality = (uint)job.Settings.JpegQuality;
                        break;
                    case OutputFormat.Png:
                        image.Format = MagickFormat.Png;
                        image.Settings.SetDefine(MagickFormat.Png, "compression-level", job.Settings.PngCompressionLevel.ToString());
                        break;
                    default:
                        throw new MediaConversionException(
                            "Định dạng đầu ra này không dùng được cho chuyển đổi ảnh.",
                            $"Định dạng ảnh đầu ra là {job.OutputFormat}.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                image.Write(job.OutputPath);
            }, cancellationToken).ConfigureAwait(false);

            progress?.Report(100);
        }
        catch (MagickException ex)
        {
            throw new MediaConversionException(
                "Không thể đọc ảnh. Tệp có thể bị hỏng hoặc dùng codec mà bản build này chưa đọc được.",
                ex.ToString(),
                ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new MediaConversionException(
                "Apple Converter không có quyền ghi tệp đầu ra.",
                ex.ToString(),
                ex);
        }
        catch (IOException ex)
        {
            throw new MediaConversionException(
                "Không thể ghi ảnh. Hãy kiểm tra thư mục đầu ra, dung lượng ổ đĩa và tên tệp.",
                ex.ToString(),
                ex);
        }
    }

    private static void ApplyResize(IMagickImage<ushort> image, AppSettings settings)
    {
        switch (settings.ResizeMode)
        {
            case ResizeMode.OriginalSize:
                return;
            case ResizeMode.MaxWidth when settings.MaxWidth is > 0:
                image.Resize(new MagickGeometry((uint)settings.MaxWidth.Value, 0) { IgnoreAspectRatio = false });
                return;
            case ResizeMode.MaxHeight when settings.MaxHeight is > 0:
                image.Resize(new MagickGeometry(0, (uint)settings.MaxHeight.Value) { IgnoreAspectRatio = false });
                return;
            case ResizeMode.CustomWidthAndHeight when settings.CustomWidth is > 0 && settings.CustomHeight is > 0:
                image.Resize(new MagickGeometry((uint)settings.CustomWidth.Value, (uint)settings.CustomHeight.Value) { IgnoreAspectRatio = false });
                return;
        }
    }
}
