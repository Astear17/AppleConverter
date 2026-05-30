using System.Diagnostics;
using AppleLegacyMediaConverter.Helpers;
using AppleLegacyMediaConverter.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AppleLegacyMediaConverter.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<SettingsViewModel>();
        DataContext = ViewModel;
    }

    public SettingsViewModel ViewModel { get; }

    private async void BrowseFFmpeg_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowHelper.GetWindowHandle(App.MainWindowInstance!));
        picker.FileTypeFilter.Add(".exe");
        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            ViewModel.FFmpegPath = file.Path;
        }
    }

    private void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(ViewModel.LogsLocation))
        {
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(ViewModel.LogsLocation);
        Process.Start(startInfo);
    }
}
