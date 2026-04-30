using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Sakura.Core.Native;

public static class RegistryWriter
{
    public static void SetDword(RegistryHive hive, string subKey, string valueName, uint data)
    {
        using var key = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64).CreateSubKey(subKey, writable: true)
            ?? throw new InvalidOperationException($"Cannot open/create {hive}\\{subKey}");
        key.SetValue(valueName, (int)data, RegistryValueKind.DWord);
    }

    public static uint? GetDword(RegistryHive hive, string subKey, string valueName)
    {
        using var key = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64).OpenSubKey(subKey);
        if (key?.GetValue(valueName) is int v) return (uint)v;
        return null;
    }

    public static void SetString(RegistryHive hive, string subKey, string valueName, string data)
    {
        using var key = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64).CreateSubKey(subKey, writable: true)
            ?? throw new InvalidOperationException($"Cannot open/create {hive}\\{subKey}");
        key.SetValue(valueName, data, RegistryValueKind.String);
    }

    public static string? GetString(RegistryHive hive, string subKey, string valueName)
    {
        using var key = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64).OpenSubKey(subKey);
        return key?.GetValue(valueName) as string;
    }

    public static void DeleteValue(RegistryHive hive, string subKey, string valueName)
    {
        using var key = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64).OpenSubKey(subKey, writable: true);
        key?.DeleteValue(valueName, throwOnMissingValue: false);
    }

    // Applies all persona DWM tweaks: dark mode, transparency, accent, colorization.
    public static void ApplyDwmSettings(bool darkMode, bool transparency, uint colorizationArgb, bool colorPrevalence)
    {
        const string personalize = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        const string dwm         = @"SOFTWARE\Microsoft\Windows\DWM";

        SetDword(RegistryHive.CurrentUser, personalize, "AppsUseLightTheme",   darkMode ? 0u : 1u);
        SetDword(RegistryHive.CurrentUser, personalize, "SystemUsesLightTheme", darkMode ? 0u : 1u);
        SetDword(RegistryHive.CurrentUser, personalize, "EnableTransparency",  transparency ? 1u : 0u);
        SetDword(RegistryHive.CurrentUser, personalize, "ColorPrevalence",     colorPrevalence ? 1u : 0u);

        // COLORREF in DWM is AARRGGBB format (not ToBgr)
        SetDword(RegistryHive.CurrentUser, dwm, "ColorizationColor", colorizationArgb);
        SetDword(RegistryHive.CurrentUser, dwm, "AccentColor",       colorizationArgb);
    }

    // Taskbar alignment: 0 = left, 1 = center
    public static void SetTaskbarAlignment(int alignment)
    {
        const string advanced = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        SetDword(RegistryHive.CurrentUser, advanced, "TaskbarAl", (uint)alignment);
    }

    // Animation speed: "0" = no animations, "1" = enable
    public static void SetAnimations(bool enable)
    {
        const string desktop = @"Control Panel\Desktop";
        SetString(RegistryHive.CurrentUser, desktop, "MinAnimate", enable ? "1" : "0");
        SetDword(RegistryHive.CurrentUser,  desktop, "DragFullWindows", enable ? 1u : 0u);

        const string windowMetrics = @"Control Panel\Desktop\WindowMetrics";
        SetString(RegistryHive.CurrentUser, windowMetrics, "MinAnimate", enable ? "1" : "0");
    }

    // Menu show delay in ms
    public static void SetMenuShowDelay(int ms)
    {
        const string desktop = @"Control Panel\Desktop";
        SetString(RegistryHive.CurrentUser, desktop, "MenuShowDelay", ms.ToString());
    }

    // System font substitution
    public static void SetFontSubstitute(string requestedFace, string actualFace)
    {
        const string fontSub = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontSubstitutes";
        SetString(RegistryHive.LocalMachine, fontSub, requestedFace, actualFace);
    }

    public static void RemoveFontSubstitute(string requestedFace)
    {
        const string fontSub = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontSubstitutes";
        DeleteValue(RegistryHive.LocalMachine, fontSub, requestedFace);
    }
}
