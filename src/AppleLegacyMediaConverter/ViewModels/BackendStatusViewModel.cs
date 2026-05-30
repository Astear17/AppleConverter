using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;
using AppleLegacyMediaConverter.Models;

namespace AppleLegacyMediaConverter.ViewModels;

public sealed partial class BackendStatusViewModel : ObservableObject
{
    private readonly IBackendStatusService _backendStatusService;
    private readonly ApplicationState _state;

    public BackendStatusViewModel(IBackendStatusService backendStatusService, ApplicationState state)
    {
        _backendStatusService = backendStatusService;
        _state = state;
    }

    [ObservableProperty]
    private BackendStatus? _status;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _statusHeadline = "Đang kiểm tra backend";

    public string DiagnosticText => Status?.ToDiagnosticText() ?? "Chưa tải trạng thái backend.";

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            Status = await _backendStatusService.GetStatusAsync(_state.Settings).ConfigureAwait(true);
            _state.BackendStatus = Status;
            StatusHeadline = Status.FFmpegFound ? "Backend video đã sẵn sàng" : "Thiếu backend video";
            OnPropertyChanged(nameof(DiagnosticText));
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}
