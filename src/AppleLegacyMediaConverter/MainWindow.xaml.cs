using AppleLegacyMediaConverter.Views;
using AppleLegacyMediaConverter.Core.Models;
using AppleLegacyMediaConverter.Models;
using Microsoft.UI;
using Microsoft.UI.Windowing;
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
        ConfigureWindowChrome();

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

    private void ConfigureWindowChrome()
    {
        Title = "Apple Converter";
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
        }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = ColorHelper.FromArgb(28, 128, 128, 128);
            titleBar.ButtonPressedBackgroundColor = ColorHelper.FromArgb(42, 128, 128, 128);
        }
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
