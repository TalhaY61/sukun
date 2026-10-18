using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using SystemColors = System.Windows.SystemColors;

namespace Sukun.Windows;

public static class ThemeManager
{
    public static bool IsDark { get; private set; }
    public static void Apply(string preference, Window window)
    {
        var dark = preference == "Dark" || preference == "System" && !SystemUsesLightTheme();
        IsDark = dark;
        var colors = dark
            ? new[] { "#161B19", "#1D2421", "#222A26", "#313A35", "#F2F4F0", "#C5CDC7", "#98A39C", "#7FB09B", "#6A9A86", "#22342D", "#456356", "#222A26", "#E7C58B" }
            : new[] { "#F6F5F1", "#FFFFFF", "#F1F3EF", "#D9DED6", "#1E2421", "#5E6962", "#7F8A83", "#5E8B7E", "#4F786C", "#E6F0EB", "#A8C3B8", "#F1F3EF", "#865D12" };
        var keys = new[] { "Canvas", "Surface", "Input", "Stroke", "Text", "Secondary", "Muted", "Accent", "AccentHover", "AccentSoft", "AccentStroke", "Hover", "Warning" };
        for (var i = 0; i < keys.Length; i++) System.Windows.Application.Current.Resources[keys[i]] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i]));
        if (SystemParameters.HighContrast)
        {
            foreach (var key in new[] { "Canvas", "Surface", "Input", "Hover", "AccentSoft" }) System.Windows.Application.Current.Resources[key] = SystemColors.WindowBrush;
            foreach (var key in new[] { "Text", "Secondary", "Muted", "Stroke", "Warning", "AccentStroke" }) System.Windows.Application.Current.Resources[key] = SystemColors.WindowTextBrush;
            System.Windows.Application.Current.Resources["Accent"] = SystemColors.HighlightBrush;
        }
        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero) { var enabled = dark ? 1 : 0; _ = DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int)); }
    }
    private static bool SystemUsesLightTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is not int value || value != 0;
    }
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
