using AppleLegacyMediaConverter.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace AppleLegacyMediaConverter.Views;

public sealed partial class BackendStatusPage : Page
{
    public BackendStatusPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<BackendStatusViewModel>();
        DataContext = ViewModel;
    }

    public BackendStatusViewModel ViewModel { get; }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
    }

    private void CopyDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText(ViewModel.DiagnosticText);
        Clipboard.SetContent(package);
    }
}
