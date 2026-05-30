using AppleLegacyMediaConverter.Core.Models;
using AppleLegacyMediaConverter.Core.Services;

namespace AppleLegacyMediaConverter.Tests;

public sealed class LivePhotoDetectorTests
{
    [Fact]
    public void DetectsStillAndMovWithSameBaseNameAsLivePhoto()
    {
        var still = new MediaFileItem(@"C:\Photos\IMG_1234.HEIC", MediaKind.Image, "Apple HEIC/HEIF image", true);
        var motion = new MediaFileItem(@"C:\Photos\IMG_1234.MOV", MediaKind.Video, "Apple QuickTime MOV video", true);
        var other = new MediaFileItem(@"C:\Photos\IMG_9999.MOV", MediaKind.Video, "Apple QuickTime MOV video", true);

        var groups = new LivePhotoDetector().Detect(new[] { still, motion, other });

        var group = Assert.Single(groups);
        Assert.Equal(still, group.StillImage);
        Assert.Equal(motion, group.MotionVideo);
        Assert.True(still.IsLivePhoto);
        Assert.True(motion.IsLivePhoto);
        Assert.Equal(motion.SourcePath, still.PairedLivePhotoPath);
    }
}
