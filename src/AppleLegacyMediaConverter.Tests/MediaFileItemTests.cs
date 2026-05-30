using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Tests;

public sealed class MediaFileItemTests
{
    [Fact]
    public void StatusMethodsMoveQueueItemThroughExpectedStates()
    {
        var item = new MediaFileItem(@"C:\Photos\IMG_0001.HEIC", MediaKind.Image, "Apple HEIC/HEIF image", true);

        item.MarkScanning();
        Assert.Equal(ConversionStatus.Scanning, item.Status);

        item.MarkPending();
        Assert.Equal(ConversionStatus.Pending, item.Status);

        item.MarkConverting();
        item.MarkProgress(42);
        Assert.Equal(ConversionStatus.Converting, item.Status);
        Assert.Equal(42, item.Progress);

        item.MarkCompleted(@"C:\Out\IMG_0001.jpg");
        Assert.Equal(ConversionStatus.Completed, item.Status);
        Assert.Equal(100, item.Progress);
        Assert.Equal(@"C:\Out\IMG_0001.jpg", item.OutputPath);
    }

    [Fact]
    public void UnsupportedItemStartsSkippedWithReason()
    {
        var item = new MediaFileItem(@"C:\Photos\document.pages", MediaKind.Unknown, "Unsupported file", false, skipReason: "Not supported");

        Assert.Equal(ConversionStatus.Skipped, item.Status);
        Assert.Equal("Not supported", item.ErrorMessage);
    }
}
