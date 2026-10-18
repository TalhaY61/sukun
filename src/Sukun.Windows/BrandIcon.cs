using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Sukun.Windows;

internal static class BrandIcon
{
    public static Icon Create()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        var lightTaskbar = key?.GetValue("SystemUsesLightTheme") is int value && value == 1;
        return Load(lightTaskbar ? "sukun-tray-light.ico" : "sukun-tray-dark.ico", System.Windows.Forms.SystemInformation.SmallIconSize);
    }
    private static Icon Load(string name, System.Drawing.Size size)
    {
        using var source = System.Windows.Application.GetResourceStream(new Uri("/Assets/Brand/" + name, UriKind.Relative))!.Stream;
        using var icon = new Icon(source, size);
        return (Icon)icon.Clone();
    }
    public static BitmapSource WindowImage()
    {
        // WPF derives both WM_SETICON sizes from this source. A 16 px source made
        // the Windows 11 taskbar render an undersized icon. Keep tray sizing separate.
        using var icon = Load("app.ico", new System.Drawing.Size(256, 256));
        var result = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        result.Freeze();
        return result;
    }
}
