using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AppleLegacyMediaConverter.Core.Models;

public sealed class MediaFileItem : INotifyPropertyChanged
{
    private ConversionStatus _status = ConversionStatus.Pending;
    private double _progress;
    private string? _outputPath;
    private string? _errorMessage;
    private string? _technicalDetails;
    private bool _isLivePhoto;
    private string? _livePhotoGroupId;

    public MediaFileItem(string sourcePath, MediaKind mediaKind, string displayType, bool isSupported, string? relativePath = null, string? skipReason = null)
    {
        Id = Guid.NewGuid();
        SourcePath = sourcePath;
        FileName = Path.GetFileName(sourcePath);
        Extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        MediaKind = mediaKind;
        DisplayType = displayType;
        IsSupported = isSupported;
        RelativePath = relativePath;

        if (!isSupported)
        {
            _status = ConversionStatus.Skipped;
            _errorMessage = skipReason ?? "This file type is not supported.";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; }

    public string SourcePath { get; }

    public string FileName { get; }

    public string Extension { get; }

    public MediaKind MediaKind { get; }

    public string DisplayType { get; }

    public bool IsSupported { get; }

    public string? RelativePath { get; }

    public string? PairedLivePhotoPath { get; set; }

    public string OutputTarget => OutputPath is null ? "Not converted yet" : Path.GetFileName(OutputPath);

    public ConversionStatus Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public double Progress
    {
        get => _progress;
        private set => SetField(ref _progress, Math.Clamp(value, 0, 100));
    }

    public string? OutputPath
    {
        get => _outputPath;
        private set
        {
            if (SetField(ref _outputPath, value))
            {
                OnPropertyChanged(nameof(OutputTarget));
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }

    public string? TechnicalDetails
    {
        get => _technicalDetails;
        private set => SetField(ref _technicalDetails, value);
    }

    public bool IsLivePhoto
    {
        get => _isLivePhoto;
        set
        {
            if (SetField(ref _isLivePhoto, value))
            {
                OnPropertyChanged(nameof(LivePhotoBadgeText));
                OnPropertyChanged(nameof(LivePhotoBadgeWidth));
            }
        }
    }

    public string LivePhotoBadgeText => IsLivePhoto ? "Live Photo" : string.Empty;

    public double LivePhotoBadgeWidth => IsLivePhoto ? 72 : 0;

    public string? LivePhotoGroupId
    {
        get => _livePhotoGroupId;
        set => SetField(ref _livePhotoGroupId, value);
    }

    public bool CanRetry => Status is ConversionStatus.Failed or ConversionStatus.Cancelled;

    public void MarkScanning()
    {
        Status = ConversionStatus.Scanning;
        Progress = 0;
        ErrorMessage = null;
        TechnicalDetails = null;
    }

    public void MarkPending()
    {
        Status = ConversionStatus.Pending;
        Progress = 0;
        ErrorMessage = null;
        TechnicalDetails = null;
    }

    public void MarkConverting()
    {
        Status = ConversionStatus.Converting;
        Progress = Math.Max(Progress, 1);
        ErrorMessage = null;
        TechnicalDetails = null;
    }

    public void MarkProgress(double progress)
    {
        Progress = progress;
    }

    public void MarkCompleted(string outputPath)
    {
        OutputPath = outputPath;
        Progress = 100;
        Status = ConversionStatus.Completed;
        ErrorMessage = null;
        TechnicalDetails = null;
    }

    public void MarkFailed(string userMessage, string? technicalDetails = null)
    {
        ErrorMessage = userMessage;
        TechnicalDetails = technicalDetails;
        Progress = 0;
        Status = ConversionStatus.Failed;
    }

    public void MarkSkipped(string reason)
    {
        ErrorMessage = reason;
        Progress = 0;
        Status = ConversionStatus.Skipped;
    }

    public void MarkCancelled()
    {
        Progress = 0;
        Status = ConversionStatus.Cancelled;
        ErrorMessage = "Conversion was cancelled.";
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
