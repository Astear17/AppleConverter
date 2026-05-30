using AppleLegacyMediaConverter.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AppleLegacyMediaConverter.Views;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        DataContext = App.GetService<AboutViewModel>();
    }
}
