using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace AppleLegacyMediaConverter.Helpers;

public static class WindowHelper
{
    public static nint GetWindowHandle(Window window)
    {
        return WindowNative.GetWindowHandle(window);
    }
}
