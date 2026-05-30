using AppleLegacyMediaConverter.Core.Models;
using AppleLegacyMediaConverter.Core.Services;

namespace AppleLegacyMediaConverter.Tests;

public sealed class FileDetectionServiceTests
{
    private readonly FileDetectionService _service = new();

    [Theory]
    [InlineData("IMG_1234.HEIC", MediaKind.Image)]
    [InlineData("IMG_1234.heif", MediaKind.Image)]
    [InlineData("IMG_1234.webp", MediaKind.Image)]
    [InlineData("IMG_1234.MOV", MediaKind.Video)]
    [InlineData("IMG_1234.m4v", MediaKind.Video)]
    public void DetectsSupportedFormats(string fileName, MediaKind expectedKind)
    {
        var result = _service.Detect(fileName);

        Assert.True(result.IsSupported);
        Assert.Equal(expectedKind, result.Kind);
    }

    [Fact]
    public void UnsupportedExtensionReturnsSkippedReason()
    {
        var result = _service.Detect("notes.pages");

        Assert.False(result.IsSupported);
        Assert.Equal(MediaKind.Unknown, result.Kind);
        Assert.Contains(".pages", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
