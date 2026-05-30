namespace AppleLegacyMediaConverter.Core.Models;

public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;

    public OutputFormat DefaultImageOutputFormat { get; set; } = OutputFormat.Jpg;

    public ConversionMode DefaultConversionMode { get; set; } = ConversionMode.Auto;

    public ConversionMode DefaultVideoConversionMode { get; set; } = ConversionMode.VideoToMp4;

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

    public int VideoParallelConversionLimit { get; set; } = 1;

    public int FFmpegThreadCount { get; set; } = CreateDefaultFFmpegThreadCount();

    public string VideoEncoderPreset { get; set; } = "veryfast";

    public int VideoConstantRateFactor { get; set; } = 23;

    public string? FFmpegPath { get; set; }

    public int FrameIntervalSeconds { get; set; } = 5;

    public LivePhotoAction LivePhotoAction { get; set; } = LivePhotoAction.RemoveMotionKeepStill;

    public bool ConfirmExtractAllFrames { get; set; }

    public static int CreateDefaultParallelism()
    {
        var processorCount = Environment.ProcessorCount;
        return Math.Clamp(processorCount <= 2 ? 1 : processorCount / 2, 1, 3);
    }

    public static int CreateDefaultFFmpegThreadCount()
    {
        var processorCount = Environment.ProcessorCount;
        return Math.Clamp(processorCount / 2, 1, 4);
    }

    public void Normalize()
    {
        JpegQuality = Math.Clamp(JpegQuality, 1, 100);
        PngCompressionLevel = Math.Clamp(PngCompressionLevel, 0, 9);
        ParallelConversionLimit = Math.Clamp(ParallelConversionLimit, 1, Math.Max(1, Environment.ProcessorCount));
        VideoParallelConversionLimit = Math.Clamp(VideoParallelConversionLimit, 1, 2);
        FFmpegThreadCount = Math.Clamp(FFmpegThreadCount, 1, Math.Max(1, Environment.ProcessorCount));
        VideoConstantRateFactor = Math.Clamp(VideoConstantRateFactor, 18, 30);
        VideoEncoderPreset = NormalizePreset(VideoEncoderPreset);
        DefaultVideoConversionMode = NormalizeVideoMode(DefaultVideoConversionMode);
        FrameIntervalSeconds = Math.Clamp(FrameIntervalSeconds, 1, 3600);
    }

    private static ConversionMode NormalizeVideoMode(ConversionMode mode)
    {
        return mode switch
        {
            ConversionMode.VideoToMp4 => ConversionMode.VideoToMp4,
            ConversionMode.ExtractFirstFrame => ConversionMode.ExtractFirstFrame,
            ConversionMode.ExtractFramesEveryNSeconds => ConversionMode.ExtractFramesEveryNSeconds,
            ConversionMode.ExtractAllFrames => ConversionMode.ExtractAllFrames,
            _ => ConversionMode.VideoToMp4
        };
    }

    private static string NormalizePreset(string? preset)
    {
        var value = string.IsNullOrWhiteSpace(preset) ? "veryfast" : preset.Trim().ToLowerInvariant();
        return value switch
        {
            "ultrafast" => "ultrafast",
            "superfast" => "superfast",
            "veryfast" => "veryfast",
            "faster" => "faster",
            "fast" => "fast",
            "medium" => "medium",
            _ => "veryfast"
        };
    }
}
