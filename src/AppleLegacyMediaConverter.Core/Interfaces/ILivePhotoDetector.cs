using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Interfaces;

public interface ILivePhotoDetector
{
    IReadOnlyList<LivePhotoGroup> Detect(IEnumerable<MediaFileItem> items);
}
