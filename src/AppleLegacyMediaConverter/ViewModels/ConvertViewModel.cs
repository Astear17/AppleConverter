using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;
using AppleLegacyMediaConverter.Helpers;
using AppleLegacyMediaConverter.Models;

namespace AppleLegacyMediaConverter.ViewModels;

public sealed partial class ConvertViewModel : ObservableObject
{
    private readonly IConversionQueueService _queueService;
    private readonly ISettingsService _settingsService;
    private readonly ApplicationState _state;
    private CancellationTokenSource? _conversionCancellation;
    private bool _suppressSettingsSave;

    public ConvertViewModel(
        IConversionQueueService queueService,
        ISettingsService settingsService,
        ApplicationState state)
    {
        _queueService = queueService;
        _settingsService = settingsService;
        _state = state;
        Settings = _state.Settings;

        if (string.IsNullOrWhiteSpace(Settings.CustomOutputFolder))
        {
            Settings.CustomOutputFolder = AppPaths.DefaultOutputFolder;
        }

        ImageFormatOptions = new[]
        {
            new OptionItem<OutputFormat>("JPG", OutputFormat.Jpg),
            new OptionItem<OutputFormat>("JPEG", OutputFormat.Jpeg),
            new OptionItem<OutputFormat>("PNG", OutputFormat.Png)
        };

        ConversionModeOptions = new[]
        {
            new OptionItem<ConversionMode>("Auto mode", ConversionMode.Auto),
            new OptionItem<ConversionMode>("Image conversion", ConversionMode.ImageConversion),
            new OptionItem<ConversionMode>("Video to MP4", ConversionMode.VideoToMp4),
            new OptionItem<ConversionMode>("Extract first frame", ConversionMode.ExtractFirstFrame),
            new OptionItem<ConversionMode>("Extract frames every N seconds", ConversionMode.ExtractFramesEveryNSeconds),
            new OptionItem<ConversionMode>("Extract all frames", ConversionMode.ExtractAllFrames)
        };

        OutputFolderModeOptions = new[]
        {
            new OptionItem<OutputFolderMode>("Custom output folder", OutputFolderMode.CustomFolder),
            new OptionItem<OutputFolderMode>("Same folder as source", OutputFolderMode.SameFolderAsSource)
        };

        LivePhotoActionOptions = new[]
        {
            new OptionItem<LivePhotoAction>("Convert both", LivePhotoAction.ConvertBoth),
            new OptionItem<LivePhotoAction>("Still image only", LivePhotoAction.ConvertStillOnly),
            new OptionItem<LivePhotoAction>("Motion video only", LivePhotoAction.ConvertVideoOnly),
            new OptionItem<LivePhotoAction>("Preview frame from MOV", LivePhotoAction.ExtractPreviewFrameFromVideo)
        };

        FilterOptions = new[]
        {
            new OptionItem<QueueFilter>("All", QueueFilter.All),
            new OptionItem<QueueFilter>("Pending", QueueFilter.Pending),
            new OptionItem<QueueFilter>("Completed", QueueFilter.Completed),
            new OptionItem<QueueFilter>("Failed", QueueFilter.Failed),
            new OptionItem<QueueFilter>("Skipped", QueueFilter.Skipped)
        };

        _suppressSettingsSave = true;
        SelectedImageFormat = ImageFormatOptions.First(option => option.Value == Settings.DefaultImageOutputFormat);
        SelectedConversionMode = ConversionModeOptions.First(option => option.Value == Settings.DefaultConversionMode);
        SelectedOutputFolderMode = OutputFolderModeOptions.First(option => option.Value == Settings.OutputFolderMode);
        SelectedLivePhotoAction = LivePhotoActionOptions.First(option => option.Value == Settings.LivePhotoAction);
        SelectedFilter = FilterOptions[0];
        CustomOutputFolder = Settings.CustomOutputFolder;
        FrameIntervalSeconds = Settings.FrameIntervalSeconds;
        _suppressSettingsSave = false;
    }

    public AppSettings Settings { get; }

    public ObservableCollection<MediaFileItem> Queue { get; } = new();

    public ObservableCollection<MediaFileItem> FilteredQueue { get; } = new();

    public IReadOnlyList<OptionItem<OutputFormat>> ImageFormatOptions { get; }

    public IReadOnlyList<OptionItem<ConversionMode>> ConversionModeOptions { get; }

    public IReadOnlyList<OptionItem<OutputFolderMode>> OutputFolderModeOptions { get; }

    public IReadOnlyList<OptionItem<LivePhotoAction>> LivePhotoActionOptions { get; }

    public IReadOnlyList<OptionItem<QueueFilter>> FilterOptions { get; }

    [ObservableProperty]
    private OptionItem<OutputFormat> _selectedImageFormat = null!;

    [ObservableProperty]
    private OptionItem<ConversionMode> _selectedConversionMode = null!;

    [ObservableProperty]
    private OptionItem<OutputFolderMode> _selectedOutputFolderMode = null!;

    [ObservableProperty]
    private OptionItem<LivePhotoAction> _selectedLivePhotoAction = null!;

    [ObservableProperty]
    private OptionItem<QueueFilter> _selectedFilter = null!;

    [ObservableProperty]
    private string? _customOutputFolder;

    [ObservableProperty]
    private int _frameIntervalSeconds = 5;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isConverting;

    [ObservableProperty]
    private double _totalProgress;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _summaryText = "No files queued";

    [ObservableProperty]
    private string _lastOutputFolder = AppPaths.DefaultOutputFolder;

    public bool HasFiles => Queue.Count > 0;

    public bool HasCompleted => Queue.Any(static item => item.Status == ConversionStatus.Completed);

    public bool HasFailures => Queue.Any(static item => item.Status == ConversionStatus.Failed);

    partial void OnSelectedImageFormatChanged(OptionItem<OutputFormat> value)
    {
        Settings.DefaultImageOutputFormat = value.Value;
        SaveSettingsFireAndForget();
    }

    partial void OnSelectedConversionModeChanged(OptionItem<ConversionMode> value)
    {
        Settings.DefaultConversionMode = value.Value;
        SaveSettingsFireAndForget();
    }

    partial void OnSelectedOutputFolderModeChanged(OptionItem<OutputFolderMode> value)
    {
        Settings.OutputFolderMode = value.Value;
        SaveSettingsFireAndForget();
    }

    partial void OnSelectedLivePhotoActionChanged(OptionItem<LivePhotoAction> value)
    {
        Settings.LivePhotoAction = value.Value;
        SaveSettingsFireAndForget();
    }

    partial void OnSelectedFilterChanged(OptionItem<QueueFilter> value)
    {
        RefreshFilter();
    }

    partial void OnCustomOutputFolderChanged(string? value)
    {
        Settings.CustomOutputFolder = value;
        LastOutputFolder = value ?? AppPaths.DefaultOutputFolder;
        SaveSettingsFireAndForget();
    }

    partial void OnFrameIntervalSecondsChanged(int value)
    {
        Settings.FrameIntervalSeconds = Math.Clamp(value, 1, 3600);
        SaveSettingsFireAndForget();
    }

    public async Task AddPathsAsync(IEnumerable<string> paths)
    {
        IsScanning = true;
        StatusText = "Scanning files";

        try
        {
            var added = await _queueService.AddInputsAsync(paths, Settings).ConfigureAwait(true);
            foreach (var item in added)
            {
                item.PropertyChanged += OnQueueItemPropertyChanged;
                Queue.Add(item);
            }

            RefreshFilter();
            UpdateSummary();
            StatusText = added.Count == 0 ? "No files found" : $"Added {added.Count} file(s)";
        }
        finally
        {
            IsScanning = false;
        }
    }

    public async Task StartConversionAsync(Func<Task<bool>> confirmExtractAllFrames)
    {
        if (Queue.Count == 0 || IsConverting)
        {
            return;
        }

        if (SelectedConversionMode.Value == ConversionMode.ExtractAllFrames && !await confirmExtractAllFrames().ConfigureAwait(true))
        {
            StatusText = "Extract all frames was cancelled";
            return;
        }

        _conversionCancellation?.Dispose();
        _conversionCancellation = new CancellationTokenSource();
        IsConverting = true;
        TotalProgress = 0;
        StatusText = "Converting";

        var options = new ConversionBatchOptions
        {
            Settings = Settings,
            ConversionMode = SelectedConversionMode.Value,
            ImageOutputFormat = SelectedImageFormat.Value,
            OutputFolderMode = SelectedOutputFolderMode.Value,
            CustomOutputFolder = CustomOutputFolder,
            KeepFolderStructure = Settings.KeepFolderStructure,
            CollisionBehavior = Settings.CollisionBehavior,
            LivePhotoAction = SelectedLivePhotoAction.Value,
            FrameIntervalSeconds = FrameIntervalSeconds,
            ConfirmExtractAllFrames = true
        };

        var progress = new Progress<ConversionProgressUpdate>(update =>
        {
            TotalProgress = update.TotalProgress;
            RefreshFilter();
        });

        try
        {
            var summary = await _queueService.ConvertAsync(Queue.ToArray(), options, progress, _conversionCancellation.Token)
                .ConfigureAwait(true);

            TotalProgress = 100;
            LastOutputFolder = options.OutputFolderMode == OutputFolderMode.CustomFolder
                ? options.CustomOutputFolder ?? AppPaths.DefaultOutputFolder
                : "Same folder as source";
            SummaryText = $"{summary.Completed} completed, {summary.Failed} failed, {summary.Skipped} skipped";
            StatusText = summary.Failed > 0 ? "Finished with issues" : "Finished";
            await _settingsService.SaveAsync(Settings).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cancelled";
        }
        finally
        {
            IsConverting = false;
            UpdateSummary();
            RefreshFilter();
        }
    }

    [RelayCommand]
    private void CancelConversion()
    {
        _conversionCancellation?.Cancel();
        StatusText = "Cancelling";
    }

    [RelayCommand]
    private void ClearQueue()
    {
        foreach (var item in Queue)
        {
            item.PropertyChanged -= OnQueueItemPropertyChanged;
        }

        Queue.Clear();
        FilteredQueue.Clear();
        TotalProgress = 0;
        SummaryText = "No files queued";
        StatusText = "Ready";
        NotifyQueueDependentProperties();
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        foreach (var item in Queue.Where(static item => item.Status == ConversionStatus.Completed).ToArray())
        {
            item.PropertyChanged -= OnQueueItemPropertyChanged;
            Queue.Remove(item);
        }

        RefreshFilter();
        UpdateSummary();
    }

    [RelayCommand]
    private void RetryFailed()
    {
        foreach (var item in Queue.Where(static item => item.Status is ConversionStatus.Failed or ConversionStatus.Cancelled))
        {
            item.MarkPending();
        }

        RefreshFilter();
        UpdateSummary();
    }

    public string BuildFailedReport()
    {
        var failures = Queue.Where(static item => item.Status == ConversionStatus.Failed).ToArray();
        if (failures.Length == 0)
        {
            return "No failed files.";
        }

        return string.Join(
            Environment.NewLine + Environment.NewLine,
            failures.Select(item => string.Join(
                Environment.NewLine,
                item.FileName,
                item.SourcePath,
                item.ErrorMessage ?? "Unknown error",
                item.TechnicalDetails ?? "No technical details.")));
    }

    private void RefreshFilter()
    {
        var filter = SelectedFilter?.Value ?? QueueFilter.All;
        var filtered = Queue.Where(item => filter switch
        {
            QueueFilter.Pending => item.Status == ConversionStatus.Pending,
            QueueFilter.Completed => item.Status == ConversionStatus.Completed,
            QueueFilter.Failed => item.Status == ConversionStatus.Failed,
            QueueFilter.Skipped => item.Status == ConversionStatus.Skipped,
            _ => true
        }).ToArray();

        FilteredQueue.Clear();
        foreach (var item in filtered)
        {
            FilteredQueue.Add(item);
        }

        NotifyQueueDependentProperties();
    }

    private void UpdateSummary()
    {
        if (Queue.Count == 0)
        {
            SummaryText = "No files queued";
            return;
        }

        var completed = Queue.Count(static item => item.Status == ConversionStatus.Completed);
        var failed = Queue.Count(static item => item.Status == ConversionStatus.Failed);
        var skipped = Queue.Count(static item => item.Status == ConversionStatus.Skipped);
        var pending = Queue.Count(static item => item.Status == ConversionStatus.Pending);
        SummaryText = $"{pending} pending, {completed} completed, {failed} failed, {skipped} skipped";
    }

    private void NotifyQueueDependentProperties()
    {
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(HasCompleted));
        OnPropertyChanged(nameof(HasFailures));
    }

    private void OnQueueItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateSummary();
        NotifyQueueDependentProperties();
    }

    private void SaveSettingsFireAndForget()
    {
        if (_suppressSettingsSave)
        {
            return;
        }

        _ = _settingsService.SaveAsync(Settings);
    }
}
