namespace AppleLegacyMediaConverter.Core.Models;

public enum AppTheme
{
    System,
    Light,
    Dark
}

public enum CollisionBehavior
{
    AutoRename,
    Overwrite,
    Skip
}

public enum ConversionMode
{
    Auto,
    ImageConversion,
    VideoToMp4,
    ExtractFirstFrame,
    ExtractFramesEveryNSeconds,
    ExtractAllFrames
}

public enum ConversionStatus
{
    Pending,
    Scanning,
    Converting,
    Completed,
    Failed,
    Skipped,
    Cancelled
}

public enum LivePhotoAction
{
    RemoveMotionKeepStill,
    ConvertBoth,
    ConvertStillOnly,
    ConvertVideoOnly,
    ExtractPreviewFrameFromVideo
}

public enum MediaKind
{
    Unknown,
    Image,
    Video
}

public enum MetadataBehavior
{
    PreserveWhenPossible,
    StripForPrivacy
}

public enum OutputFolderMode
{
    SameFolderAsSource,
    CustomFolder
}

public enum OutputFormat
{
    Jpg,
    Jpeg,
    Png,
    Mp4
}

public enum QueueFilter
{
    All,
    Pending,
    Completed,
    Failed,
    Skipped
}

public enum ResizeMode
{
    OriginalSize,
    MaxWidth,
    MaxHeight,
    CustomWidthAndHeight
}
