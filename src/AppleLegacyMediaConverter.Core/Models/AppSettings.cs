namespace AppleLegacyMediaConverter.Core.Models;

public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;

    public OutputFormat DefaultImageOutputFormat { get; set; } = OutputFormat.Jpg;

    public ConversionMode DefaultConversionMode { get; set; } = ConversionMode.Auto;

    public int JpegQuality { get; set; } = 90;

    public int PngCompressionLevel { get; set; } = 6;

    public ResizeMode ResizeMode { get; set; } = ResizeMode.OriginalSize;

    public int? MaxWidth { get; set; }

    public int? MaxHeight { get; set; }

    public int? CustomWidth { get; set; }

    public int? CustomHeight { get; set; }

    public MetadataBehavior MetadataBehavior { get; set; } = MetadataBehavior.PreserveWhenPossible;

    public bool PreserveTimestamps { get; set; } = true;

    public bool RecursiveFolderScanning { get; set; } = true;

    public bool KeepFolderStructure { get; set; } = true;

    public CollisionBehavior CollisionBehavior { get; set; } = CollisionBehavior.AutoRename;

    public OutputFolderMode OutputFolderMode { get; set; } = OutputFolderMode.CustomFolder;

    public string? CustomOutputFolder { get; set; }

    public int ParallelConversionLimit { get; set; } = CreateDefaultParallelism();

    public string? FFmpegPath { get; set; }

    public int FrameIntervalSeconds { get; set; } = 5;

    public LivePhotoAction LivePhotoAction { get; set; } = LivePhotoAction.ConvertBoth;

    public bool ConfirmExtractAllFrames { get; set; }

    public static int CreateDefaultParallelism()
    {
        var processorCount = Environment.ProcessorCount;
        return Math.Clamp(processorCount <= 2 ? 1 : processorCount - 1, 1, 4);
    }

    public void Normalize()
    {
        JpegQuality = Math.Clamp(JpegQuality, 1, 100);
        PngCompressionLevel = Math.Clamp(PngCompressionLevel, 0, 9);
        ParallelConversionLimit = Math.Clamp(ParallelConversionLimit, 1, Math.Max(1, Environment.ProcessorCount));
        FrameIntervalSeconds = Math.Clamp(FrameIntervalSeconds, 1, 3600);
    }
}
