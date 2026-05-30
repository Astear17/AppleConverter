using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Services;

public sealed class LivePhotoDetector : ILivePhotoDetector
{
    public IReadOnlyList<LivePhotoGroup> Detect(IEnumerable<MediaFileItem> items)
    {
        var supported = items
            .Where(item => item.IsSupported)
            .GroupBy(item => CreateKey(item.SourcePath), StringComparer.OrdinalIgnoreCase);

        var groups = new List<LivePhotoGroup>();
        foreach (var group in supported)
        {
            var still = group.FirstOrDefault(item => item.MediaKind == MediaKind.Image && item.Extension is ".heic" or ".heif" or ".jpg" or ".jpeg" or ".png");
            var motion = group.FirstOrDefault(item => item.MediaKind == MediaKind.Video && item.Extension.Equals(".mov", StringComparison.OrdinalIgnoreCase));

            if (still is null || motion is null)
            {
                continue;
            }

            var groupId = $"{Path.GetDirectoryName(still.SourcePath)}|{Path.GetFileNameWithoutExtension(still.SourcePath)}";
            still.IsLivePhoto = true;
            motion.IsLivePhoto = true;
            still.LivePhotoGroupId = groupId;
            motion.LivePhotoGroupId = groupId;
            still.PairedLivePhotoPath = motion.SourcePath;
            motion.PairedLivePhotoPath = still.SourcePath;

            groups.Add(new LivePhotoGroup(
                groupId,
                Path.GetFileNameWithoutExtension(still.SourcePath),
                Path.GetDirectoryName(still.SourcePath) ?? string.Empty,
                still,
                motion));
        }

        return groups;
    }

    private static string CreateKey(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        return Path.Combine(directory, name);
    }
}
