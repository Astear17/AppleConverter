using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;
using AppleLegacyMediaConverter.Helpers;
using AppleLegacyMediaConverter.Models;

namespace AppleLegacyMediaConverter.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ApplicationState _state;
    private bool _suppressSave;

    public SettingsViewModel(ISettingsService settingsService, ILoggerService logger, ApplicationState state)
    {
        _settingsService = settingsService;
        _state = state;
        Settings = _state.Settings;
        LogsLocation = logger.LogDirectory;

        ThemeOptions = new[]
        {
            new OptionItem<AppTheme>("Theo hệ thống", AppTheme.System),
            new OptionItem<AppTheme>("Sáng", AppTheme.Light),
            new OptionItem<AppTheme>("Tối", AppTheme.Dark)
        };

        ImageFormatOptions = new[]
        {
            new OptionItem<OutputFormat>("JPG", OutputFormat.Jpg),
            new OptionItem<OutputFormat>("JPEG", OutputFormat.Jpeg),
            new OptionItem<OutputFormat>("PNG", OutputFormat.Png)
        };

        MetadataOptions = new[]
        {
            new OptionItem<MetadataBehavior>("Giữ metadata khi có thể", MetadataBehavior.PreserveWhenPossible),
            new OptionItem<MetadataBehavior>("Xóa metadata để riêng tư", MetadataBehavior.StripForPrivacy)
        };

        CollisionOptions = new[]
        {
            new OptionItem<CollisionBehavior>("Tự đổi tên", CollisionBehavior.AutoRename),
            new OptionItem<CollisionBehavior>("Ghi đè", CollisionBehavior.Overwrite),
            new OptionItem<CollisionBehavior>("Bỏ qua", CollisionBehavior.Skip)
        };

        ResizeOptions = new[]
        {
            new OptionItem<ResizeMode>("Giữ kích thước gốc", ResizeMode.OriginalSize),
            new OptionItem<ResizeMode>("Giới hạn chiều rộng", ResizeMode.MaxWidth),
            new OptionItem<ResizeMode>("Giới hạn chiều cao", ResizeMode.MaxHeight),
            new OptionItem<ResizeMode>("Chiều rộng và chiều cao tùy chỉnh", ResizeMode.CustomWidthAndHeight)
        };

        LivePhotoActionOptions = new[]
        {
            new OptionItem<LivePhotoAction>("Loại bỏ Live Photo, chỉ giữ ảnh tĩnh nhẹ nhất", LivePhotoAction.RemoveMotionKeepStill),
            new OptionItem<LivePhotoAction>("Chuyển cả ảnh và video", LivePhotoAction.ConvertBoth),
            new OptionItem<LivePhotoAction>("Chỉ ảnh tĩnh", LivePhotoAction.ConvertStillOnly),
            new OptionItem<LivePhotoAction>("Chỉ video chuyển động", LivePhotoAction.ConvertVideoOnly),
            new OptionItem<LivePhotoAction>("Tách ảnh xem trước từ MOV", LivePhotoAction.ExtractPreviewFrameFromVideo)
        };

        VideoPresetOptions = new[]
        {
            new OptionItem<string>("Very fast (khuyên dùng)", "veryfast"),
            new OptionItem<string>("Super fast (nhanh hơn, file lớn hơn)", "superfast"),
            new OptionItem<string>("Ultra fast (nhanh nhất, file lớn nhất)", "ultrafast"),
            new OptionItem<string>("Faster", "faster"),
            new OptionItem<string>("Fast", "fast"),
            new OptionItem<string>("Medium (nhỏ hơn, chậm hơn)", "medium")
        };

        _suppressSave = true;
        SelectedTheme = ThemeOptions.First(option => option.Value == Settings.Theme);
        SelectedImageFormat = ImageFormatOptions.First(option => option.Value == Settings.DefaultImageOutputFormat);
        SelectedMetadataBehavior = MetadataOptions.First(option => option.Value == Settings.MetadataBehavior);
        SelectedCollisionBehavior = CollisionOptions.First(option => option.Value == Settings.CollisionBehavior);
        SelectedResizeMode = ResizeOptions.First(option => option.Value == Settings.ResizeMode);
        SelectedLivePhotoAction = LivePhotoActionOptions.First(option => option.Value == Settings.LivePhotoAction);
        SelectedVideoPreset = VideoPresetOptions.FirstOrDefault(option => option.Value == Settings.VideoEncoderPreset)
            ?? VideoPresetOptions[0];
        JpegQuality = Settings.JpegQuality;
        PngCompressionLevel = Settings.PngCompressionLevel;
        PreserveTimestamps = Settings.PreserveTimestamps;
        RecursiveFolderScanning = Settings.RecursiveFolderScanning;
        KeepFolderStructure = Settings.KeepFolderStructure;
        ParallelConversionLimit = Settings.ParallelConversionLimit;
        VideoParallelConversionLimit = Settings.VideoParallelConversionLimit;
        FFmpegThreadCount = Settings.FFmpegThreadCount;
        VideoConstantRateFactor = Settings.VideoConstantRateFactor;
        FFmpegPath = Settings.FFmpegPath;
        MaxWidth = Settings.MaxWidth ?? 1920;
        MaxHeight = Settings.MaxHeight ?? 1080;
        CustomWidth = Settings.CustomWidth ?? 1920;
        CustomHeight = Settings.CustomHeight ?? 1080;
        _suppressSave = false;
    }

    public AppSettings Settings { get; }

    public string LogsLocation { get; }

    public IReadOnlyList<OptionItem<AppTheme>> ThemeOptions { get; }

    public IReadOnlyList<OptionItem<OutputFormat>> ImageFormatOptions { get; }

    public IReadOnlyList<OptionItem<MetadataBehavior>> MetadataOptions { get; }

    public IReadOnlyList<OptionItem<CollisionBehavior>> CollisionOptions { get; }

    public IReadOnlyList<OptionItem<ResizeMode>> ResizeOptions { get; }

    public IReadOnlyList<OptionItem<LivePhotoAction>> LivePhotoActionOptions { get; }

    public IReadOnlyList<OptionItem<string>> VideoPresetOptions { get; }

    [ObservableProperty]
    private OptionItem<AppTheme> _selectedTheme = null!;

    [ObservableProperty]
    private OptionItem<OutputFormat> _selectedImageFormat = null!;

    [ObservableProperty]
    private OptionItem<MetadataBehavior> _selectedMetadataBehavior = null!;

    [ObservableProperty]
    private OptionItem<CollisionBehavior> _selectedCollisionBehavior = null!;

    [ObservableProperty]
    private OptionItem<ResizeMode> _selectedResizeMode = null!;

    [ObservableProperty]
    private OptionItem<LivePhotoAction> _selectedLivePhotoAction = null!;

    [ObservableProperty]
    private OptionItem<string> _selectedVideoPreset = null!;

    [ObservableProperty]
    private int _jpegQuality;

    [ObservableProperty]
    private int _pngCompressionLevel;

    [ObservableProperty]
    private bool _preserveTimestamps;

    [ObservableProperty]
    private bool _recursiveFolderScanning;

    [ObservableProperty]
    private bool _keepFolderStructure;

    [ObservableProperty]
    private int _parallelConversionLimit;

    [ObservableProperty]
    private int _videoParallelConversionLimit;

    [ObservableProperty]
    private int _fFmpegThreadCount;

    [ObservableProperty]
    private int _videoConstantRateFactor;

    [ObservableProperty]
    private string? _fFmpegPath;

    [ObservableProperty]
    private int _maxWidth;

    [ObservableProperty]
    private int _maxHeight;

    [ObservableProperty]
    private int _customWidth;

    [ObservableProperty]
    private int _customHeight;

    partial void OnSelectedThemeChanged(OptionItem<AppTheme> value)
    {
        Settings.Theme = value.Value;
        SaveFireAndForget();
    }

    partial void OnSelectedImageFormatChanged(OptionItem<OutputFormat> value)
    {
        Settings.DefaultImageOutputFormat = value.Value;
        SaveFireAndForget();
    }

    partial void OnSelectedMetadataBehaviorChanged(OptionItem<MetadataBehavior> value)
    {
        Settings.MetadataBehavior = value.Value;
        SaveFireAndForget();
    }

    partial void OnSelectedCollisionBehaviorChanged(OptionItem<CollisionBehavior> value)
    {
        Settings.CollisionBehavior = value.Value;
        SaveFireAndForget();
    }

    partial void OnSelectedResizeModeChanged(OptionItem<ResizeMode> value)
    {
        Settings.ResizeMode = value.Value;
        SaveFireAndForget();
    }

    partial void OnSelectedLivePhotoActionChanged(OptionItem<LivePhotoAction> value)
    {
        Settings.LivePhotoAction = value.Value;
        SaveFireAndForget();
    }

    partial void OnSelectedVideoPresetChanged(OptionItem<string> value)
    {
        Settings.VideoEncoderPreset = value.Value;
        SaveFireAndForget();
    }

    partial void OnJpegQualityChanged(int value)
    {
        Settings.JpegQuality = value;
        SaveFireAndForget();
    }

    partial void OnPngCompressionLevelChanged(int value)
    {
        Settings.PngCompressionLevel = value;
        SaveFireAndForget();
    }

    partial void OnPreserveTimestampsChanged(bool value)
    {
        Settings.PreserveTimestamps = value;
        SaveFireAndForget();
    }

    partial void OnRecursiveFolderScanningChanged(bool value)
    {
        Settings.RecursiveFolderScanning = value;
        SaveFireAndForget();
    }

    partial void OnKeepFolderStructureChanged(bool value)
    {
        Settings.KeepFolderStructure = value;
        SaveFireAndForget();
    }

    partial void OnParallelConversionLimitChanged(int value)
    {
        Settings.ParallelConversionLimit = value;
        SaveFireAndForget();
    }

    partial void OnVideoParallelConversionLimitChanged(int value)
    {
        Settings.VideoParallelConversionLimit = value;
        SaveFireAndForget();
    }

    partial void OnFFmpegThreadCountChanged(int value)
    {
        Settings.FFmpegThreadCount = value;
        SaveFireAndForget();
    }

    partial void OnVideoConstantRateFactorChanged(int value)
    {
        Settings.VideoConstantRateFactor = value;
        SaveFireAndForget();
    }

    partial void OnFFmpegPathChanged(string? value)
    {
        Settings.FFmpegPath = value;
        SaveFireAndForget();
    }

    partial void OnMaxWidthChanged(int value)
    {
        Settings.MaxWidth = value;
        SaveFireAndForget();
    }

    partial void OnMaxHeightChanged(int value)
    {
        Settings.MaxHeight = value;
        SaveFireAndForget();
    }

    partial void OnCustomWidthChanged(int value)
    {
        Settings.CustomWidth = value;
        SaveFireAndForget();
    }

    partial void OnCustomHeightChanged(int value)
    {
        Settings.CustomHeight = value;
        SaveFireAndForget();
    }

    [RelayCommand]
    private async Task ResetSettingsAsync()
    {
        var defaults = new AppSettings
        {
            CustomOutputFolder = AppPaths.DefaultOutputFolder
        };

        _suppressSave = true;
        Settings.Theme = defaults.Theme;
        Settings.DefaultImageOutputFormat = defaults.DefaultImageOutputFormat;
        Settings.DefaultVideoConversionMode = defaults.DefaultVideoConversionMode;
        Settings.JpegQuality = defaults.JpegQuality;
        Settings.PngCompressionLevel = defaults.PngCompressionLevel;
        Settings.MetadataBehavior = defaults.MetadataBehavior;
        Settings.PreserveTimestamps = defaults.PreserveTimestamps;
        Settings.RecursiveFolderScanning = defaults.RecursiveFolderScanning;
        Settings.KeepFolderStructure = defaults.KeepFolderStructure;
        Settings.CollisionBehavior = defaults.CollisionBehavior;
        Settings.ParallelConversionLimit = defaults.ParallelConversionLimit;
        Settings.VideoParallelConversionLimit = defaults.VideoParallelConversionLimit;
        Settings.FFmpegThreadCount = defaults.FFmpegThreadCount;
        Settings.VideoEncoderPreset = defaults.VideoEncoderPreset;
        Settings.VideoConstantRateFactor = defaults.VideoConstantRateFactor;
        Settings.LivePhotoAction = defaults.LivePhotoAction;
        Settings.FFmpegPath = defaults.FFmpegPath;
        Settings.ResizeMode = defaults.ResizeMode;
        Settings.MaxWidth = defaults.MaxWidth;
        Settings.MaxHeight = defaults.MaxHeight;
        Settings.CustomWidth = defaults.CustomWidth;
        Settings.CustomHeight = defaults.CustomHeight;

        SelectedTheme = ThemeOptions.First(option => option.Value == Settings.Theme);
        SelectedImageFormat = ImageFormatOptions.First(option => option.Value == Settings.DefaultImageOutputFormat);
        SelectedMetadataBehavior = MetadataOptions.First(option => option.Value == Settings.MetadataBehavior);
        SelectedCollisionBehavior = CollisionOptions.First(option => option.Value == Settings.CollisionBehavior);
        SelectedResizeMode = ResizeOptions.First(option => option.Value == Settings.ResizeMode);
        SelectedLivePhotoAction = LivePhotoActionOptions.First(option => option.Value == Settings.LivePhotoAction);
        SelectedVideoPreset = VideoPresetOptions.First(option => option.Value == Settings.VideoEncoderPreset);
        JpegQuality = Settings.JpegQuality;
        PngCompressionLevel = Settings.PngCompressionLevel;
        PreserveTimestamps = Settings.PreserveTimestamps;
        RecursiveFolderScanning = Settings.RecursiveFolderScanning;
        KeepFolderStructure = Settings.KeepFolderStructure;
        ParallelConversionLimit = Settings.ParallelConversionLimit;
        VideoParallelConversionLimit = Settings.VideoParallelConversionLimit;
        FFmpegThreadCount = Settings.FFmpegThreadCount;
        VideoConstantRateFactor = Settings.VideoConstantRateFactor;
        FFmpegPath = Settings.FFmpegPath;
        MaxWidth = Settings.MaxWidth ?? 1920;
        MaxHeight = Settings.MaxHeight ?? 1080;
        CustomWidth = Settings.CustomWidth ?? 1920;
        CustomHeight = Settings.CustomHeight ?? 1080;
        _suppressSave = false;

        await _settingsService.SaveAsync(Settings).ConfigureAwait(true);
    }

    private void SaveFireAndForget()
    {
        if (_suppressSave)
        {
            return;
        }

        _ = _settingsService.SaveAsync(Settings);
    }
}
