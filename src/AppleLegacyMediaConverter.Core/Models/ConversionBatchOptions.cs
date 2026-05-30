namespace AppleLegacyMediaConverter.Core.Models;

public sealed class ConversionBatchOptions
{
    public AppSettings Settings { get; init; } = new();

    public ConversionMode ConversionMode { get; init; } = ConversionMode.Auto;

    public ConversionMode VideoConversionMode { get; init; } = ConversionMode.VideoToMp4;

    public OutputFormat ImageOutputFormat { get; init; } = OutputFormat.Jpg;

    public string? CustomOutputFolder { get; init; }

    public OutputFolderMode OutputFolderMode { get; init; } = OutputFolderMode.CustomFolder;

    public bool KeepFolderStructure { get; init; } = true;

    public CollisionBehavior CollisionBehavior { get; init; } = CollisionBehavior.AutoRename;

    public LivePhotoAction LivePhotoAction { get; init; } = LivePhotoAction.ConvertBoth;

    public int FrameIntervalSeconds { get; init; } = 5;

    public bool ConfirmExtractAllFrames { get; init; }
}
