namespace AppleLegacyMediaConverter.Core.Models;

public sealed record LivePhotoGroup(
    string GroupId,
    string BaseName,
    string DirectoryPath,
    MediaFileItem StillImage,
    MediaFileItem MotionVideo);
