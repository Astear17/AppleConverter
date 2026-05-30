using AppleLegacyMediaConverter.Views;
using AppleLegacyMediaConverter.Core.Models;
using AppleLegacyMediaConverter.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AppleLegacyMediaConverter;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 780));

        try
        {
            SystemBackdrop = new MicaBackdrop();
        }
        catch
        {
            // Mica is best-effort. The UI still works on systems that do not support it.
        }

        var theme = App.GetService<ApplicationState>().Settings.Theme;
        RootNavigation.RequestedTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        RootNavigation.SelectedItem = ConvertNavItem;
        ContentFrame.Navigate(typeof(ConvertPage));
    }

    private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        var pageType = tag switch
        {
            "convert" => typeof(ConvertPage),
            "backend" => typeof(BackendStatusPage),
            "settings" => typeof(SettingsPage),
            "about" => typeof(AboutPage),
            _ => typeof(ConvertPage)
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
