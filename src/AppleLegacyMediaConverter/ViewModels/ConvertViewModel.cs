using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;
using AppleLegacyMediaConverter.Helpers;
using AppleLegacyMediaConverter.Models;
using Microsoft.UI.Dispatching;

namespace AppleLegacyMediaConverter.ViewModels;

public sealed partial class ConvertViewModel : ObservableObject
{
    private readonly IConversionQueueService _queueService;
    private readonly ISettingsService _settingsService;
    private readonly ApplicationState _state;
    private readonly DispatcherQueue _dispatcherQueue;
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
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
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

        VideoModeOptions = new[]
        {
            new OptionItem<ConversionMode>("Video sang MP4 (nặng CPU)", ConversionMode.VideoToMp4),
            new OptionItem<ConversionMode>("Tách khung đầu tiên", ConversionMode.ExtractFirstFrame),
            new OptionItem<ConversionMode>("Tách khung mỗi N giây (nặng)", ConversionMode.ExtractFramesEveryNSeconds),
            new OptionItem<ConversionMode>("Tách toàn bộ khung (rất nặng)", ConversionMode.ExtractAllFrames)
        };

        OutputFolderModeOptions = new[]
        {
            new OptionItem<OutputFolderMode>("Thư mục xuất tùy chọn", OutputFolderMode.CustomFolder),
            new OptionItem<OutputFolderMode>("Cùng thư mục với tệp gốc", OutputFolderMode.SameFolderAsSource)
        };

        LivePhotoActionOptions = new[]
        {
            new OptionItem<LivePhotoAction>("Loại bỏ Live Photo, chỉ giữ ảnh tĩnh nhẹ nhất", LivePhotoAction.RemoveMotionKeepStill),
            new OptionItem<LivePhotoAction>("Chuyển cả ảnh và video", LivePhotoAction.ConvertBoth),
            new OptionItem<LivePhotoAction>("Chỉ ảnh tĩnh", LivePhotoAction.ConvertStillOnly),
            new OptionItem<LivePhotoAction>("Chỉ video chuyển động", LivePhotoAction.ConvertVideoOnly),
            new OptionItem<LivePhotoAction>("Tách ảnh xem trước từ MOV", LivePhotoAction.ExtractPreviewFrameFromVideo)
        };

        FilterOptions = new[]
        {
            new OptionItem<QueueFilter>("Tất cả", QueueFilter.All),
            new OptionItem<QueueFilter>("Đang chờ", QueueFilter.Pending),
            new OptionItem<QueueFilter>("Hoàn tất", QueueFilter.Completed),
            new OptionItem<QueueFilter>("Lỗi", QueueFilter.Failed),
            new OptionItem<QueueFilter>("Bỏ qua", QueueFilter.Skipped)
        };

        _suppressSettingsSave = true;
        SelectedImageFormat = ImageFormatOptions.First(option => option.Value == Settings.DefaultImageOutputFormat);
        SelectedVideoMode = VideoModeOptions.FirstOrDefault(option => option.Value == Settings.DefaultVideoConversionMode)
            ?? VideoModeOptions[0];
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

    public IReadOnlyList<OptionItem<ConversionMode>> VideoModeOptions { get; }

    public IReadOnlyList<OptionItem<OutputFolderMode>> OutputFolderModeOptions { get; }

    public IReadOnlyList<OptionItem<LivePhotoAction>> LivePhotoActionOptions { get; }

    public IReadOnlyList<OptionItem<QueueFilter>> FilterOptions { get; }

    [ObservableProperty]
    private OptionItem<OutputFormat> _selectedImageFormat = null!;

    [ObservableProperty]
    private OptionItem<ConversionMode> _selectedVideoMode = null!;

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
    private string _statusText = "Sẵn sàng";

    [ObservableProperty]
    private string _summaryText = "Chưa có tệp trong hàng đợi";

    [ObservableProperty]
    private string _estimatedTimeText = "Ước tính: chưa có dữ liệu";

    [ObservableProperty]
    private string _summaryDetailText = "Chưa có lần chuyển đổi nào.";

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

    partial void OnSelectedVideoModeChanged(OptionItem<ConversionMode> value)
    {
        Settings.DefaultVideoConversionMode = value.Value;
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
        StatusText = "Đang quét tệp";

        try
        {
            var added = await _queueService.AddInputsAsync(paths, Settings).ConfigureAwait(true);
            foreach (var item in added)
            {
                item.NotificationDispatcher = Dispatch;
                item.PropertyChanged += OnQueueItemPropertyChanged;
                Queue.Add(item);
            }

            RefreshFilter();
            UpdateSummary();
            StatusText = added.Count == 0 ? "Không tìm thấy tệp phù hợp" : $"Đã thêm {added.Count} tệp";
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

        if (SelectedVideoMode.Value == ConversionMode.ExtractAllFrames && !await confirmExtractAllFrames().ConfigureAwait(true))
        {
            StatusText = "Đã hủy tách toàn bộ khung hình";
            return;
        }

        _conversionCancellation?.Dispose();
        _conversionCancellation = new CancellationTokenSource();
        IsConverting = true;
        TotalProgress = 0;
        EstimatedTimeText = "Ước tính: đang tính...";
        SummaryDetailText = "Đang chuyển đổi, vui lòng giữ ứng dụng mở.";
        StatusText = "Đang chuyển đổi";
        var timer = Stopwatch.StartNew();

        var options = new ConversionBatchOptions
        {
            Settings = Settings,
            ConversionMode = ConversionMode.Auto,
            VideoConversionMode = SelectedVideoMode.Value,
            ImageOutputFormat = SelectedImageFormat.Value,
            OutputFolderMode = SelectedOutputFolderMode.Value,
            CustomOutputFolder = CustomOutputFolder,
            KeepFolderStructure = Settings.KeepFolderStructure,
            CollisionBehavior = Settings.CollisionBehavior,
            LivePhotoAction = Settings.LivePhotoAction,
            FrameIntervalSeconds = FrameIntervalSeconds,
            ConfirmExtractAllFrames = true
        };

        var progress = new Progress<ConversionProgressUpdate>(update =>
        {
            Dispatch(() =>
            {
                TotalProgress = update.TotalProgress;
                EstimatedTimeText = CreateEstimatedTimeText(update.TotalProgress, timer.Elapsed);
            });
        });

        try
        {
            var summary = await _queueService.ConvertAsync(Queue.ToArray(), options, progress, _conversionCancellation.Token)
                .ConfigureAwait(true);

            TotalProgress = 100;
            LastOutputFolder = options.OutputFolderMode == OutputFolderMode.CustomFolder
                ? options.CustomOutputFolder ?? AppPaths.DefaultOutputFolder
                : "Cùng thư mục với tệp gốc";
            SummaryText = $"{summary.Completed} hoàn tất, {summary.Failed} lỗi, {summary.Skipped} bỏ qua";
            SummaryDetailText = $"Đầu ra: {LastOutputFolder}";
            StatusText = summary.Failed > 0 ? "Hoàn tất nhưng có lỗi" : "Hoàn tất";
            EstimatedTimeText = $"Thời gian đã chạy: {FormatDuration(timer.Elapsed)}";
            await _settingsService.SaveAsync(Settings).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Đã hủy";
            EstimatedTimeText = $"Đã hủy sau {FormatDuration(timer.Elapsed)}";
        }
        finally
        {
            timer.Stop();
            IsConverting = false;
            UpdateSummary();
            RefreshFilter();
        }
    }

    [RelayCommand]
    private void CancelConversion()
    {
        _conversionCancellation?.Cancel();
        StatusText = "Đang hủy";
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
        EstimatedTimeText = "Ước tính: chưa có dữ liệu";
        SummaryText = "Chưa có tệp trong hàng đợi";
        SummaryDetailText = "Chưa có lần chuyển đổi nào.";
        StatusText = "Sẵn sàng";
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
            return "Không có tệp lỗi.";
        }

        return string.Join(
            Environment.NewLine + Environment.NewLine,
            failures.Select(item => string.Join(
                Environment.NewLine,
                item.FileName,
                item.SourcePath,
                item.ErrorMessage ?? "Lỗi không xác định",
                item.TechnicalDetails ?? "Không có chi tiết kỹ thuật.")));
    }

    private void RefreshFilter()
    {
        var filtered = Queue.Where(MatchesSelectedFilter).ToArray();

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
            SummaryText = "Chưa có tệp trong hàng đợi";
            return;
        }

        var completed = Queue.Count(static item => item.Status == ConversionStatus.Completed);
        var failed = Queue.Count(static item => item.Status == ConversionStatus.Failed);
        var skipped = Queue.Count(static item => item.Status == ConversionStatus.Skipped);
        var pending = Queue.Count(static item => item.Status == ConversionStatus.Pending);
        SummaryText = $"{pending} đang chờ, {completed} hoàn tất, {failed} lỗi, {skipped} bỏ qua";
    }

    private static string CreateEstimatedTimeText(double totalProgress, TimeSpan elapsed)
    {
        if (totalProgress < 2 || elapsed.TotalSeconds < 2)
        {
            return "Ước tính: đang tính...";
        }

        var remainingSeconds = elapsed.TotalSeconds * (100 - totalProgress) / totalProgress;
        if (remainingSeconds <= 1)
        {
            return "Ước tính: sắp xong";
        }

        return $"Ước tính còn lại: {FormatDuration(TimeSpan.FromSeconds(remainingSeconds))}";
    }

    private static string FormatDuration(TimeSpan value)
    {
        if (value.TotalHours >= 1)
        {
            return $"{(int)value.TotalHours} giờ {value.Minutes} phút";
        }

        if (value.TotalMinutes >= 1)
        {
            return $"{value.Minutes} phút {value.Seconds} giây";
        }

        return $"{Math.Max(1, value.Seconds)} giây";
    }

    private void NotifyQueueDependentProperties()
    {
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(HasCompleted));
        OnPropertyChanged(nameof(HasFailures));
    }

    private void OnQueueItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatch(() =>
        {
            if (sender is MediaFileItem item && e.PropertyName == nameof(MediaFileItem.Status))
            {
                UpdateFilteredMembership(item);
                UpdateSummary();
                NotifyQueueDependentProperties();
                return;
            }

            if (e.PropertyName is null or nameof(MediaFileItem.OutputPath) or nameof(MediaFileItem.ErrorMessage))
            {
                UpdateSummary();
                NotifyQueueDependentProperties();
            }
        });
    }

    private void UpdateFilteredMembership(MediaFileItem item)
    {
        var existingIndex = FilteredQueue.IndexOf(item);
        var shouldShow = MatchesSelectedFilter(item);

        if (!shouldShow)
        {
            if (existingIndex >= 0)
            {
                FilteredQueue.RemoveAt(existingIndex);
            }

            return;
        }

        if (existingIndex >= 0)
        {
            return;
        }

        var queueIndex = Queue.IndexOf(item);
        for (var index = 0; index < FilteredQueue.Count; index++)
        {
            if (Queue.IndexOf(FilteredQueue[index]) > queueIndex)
            {
                FilteredQueue.Insert(index, item);
                return;
            }
        }

        FilteredQueue.Add(item);
    }

    private bool MatchesSelectedFilter(MediaFileItem item)
    {
        var filter = SelectedFilter?.Value ?? QueueFilter.All;
        return filter switch
        {
            QueueFilter.Pending => item.Status == ConversionStatus.Pending,
            QueueFilter.Completed => item.Status == ConversionStatus.Completed,
            QueueFilter.Failed => item.Status == ConversionStatus.Failed,
            QueueFilter.Skipped => item.Status == ConversionStatus.Skipped,
            _ => true
        };
    }

    private void Dispatch(Action action)
    {
        if (_dispatcherQueue.HasThreadAccess)
        {
            action();
            return;
        }

        _dispatcherQueue.TryEnqueue(() => action());
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
