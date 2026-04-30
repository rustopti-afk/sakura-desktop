using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Sakura.Core.Native;

public static class DwmTitlebar
{
    // DWMWINDOWATTRIBUTE values (some are undocumented)
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE    = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE   = 33;
    private const int DWMWA_BORDER_COLOR               = 34;
    private const int DWMWA_CAPTION_COLOR              = 35;
    private const int DWMWA_TEXT_COLOR                 = 36;
    private const int DWMWA_SYSTEMBACKDROP_TYPE        = 38;

    // DWM_WINDOW_CORNER_PREFERENCE enum
    public const int DWMWCP_DEFAULT     = 0;
    public const int DWMWCP_DONOTROUND  = 1;
    public const int DWMWCP_ROUND       = 2;
    public const int DWMWCP_ROUNDSMALL  = 3;

    // DWM_SYSTEMBACKDROP_TYPE enum
    public const int DWMSBT_AUTO    = 1;
    public const int DWMSBT_NONE    = 2;
    public const int DWMSBT_MICA    = 3;
    public const int DWMSBT_ACRYLIC = 4;
    public const int DWMSBT_TABBED  = 5; // MicaAlt

    // COLORREF: 0x00BBGGRR (NOT ARGB). Use 0xFFFFFFFE to remove color.
    public const uint DWMWA_COLOR_NONE    = 0xFFFFFFFE;
    public const uint DWMWA_COLOR_DEFAULT = 0xFFFFFFFF;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    public static void SetTitleBarColor(IntPtr hwnd, uint captionColorRef, uint textColorRef, uint borderColorRef, bool darkMode)
    {
        int dark = darkMode ? 1 : 0;
        ThrowOnError(DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int)),
                     nameof(DWMWA_USE_IMMERSIVE_DARK_MODE));

        int caption = unchecked((int)captionColorRef);
        ThrowOnError(DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref caption, sizeof(int)),
                     nameof(DWMWA_CAPTION_COLOR));

        int text = unchecked((int)textColorRef);
        ThrowOnError(DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref text, sizeof(int)),
                     nameof(DWMWA_TEXT_COLOR));

        int border = unchecked((int)borderColorRef);
        ThrowOnError(DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref border, sizeof(int)),
                     nameof(DWMWA_BORDER_COLOR));
    }

    public static void SetCornerPreference(IntPtr hwnd, int preference)
    {
        ThrowOnError(DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int)),
                     nameof(DWMWA_WINDOW_CORNER_PREFERENCE));
    }

    public static void SetSystemBackdrop(IntPtr hwnd, int backdropType)
    {
        ThrowOnError(DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int)),
                     nameof(DWMWA_SYSTEMBACKDROP_TYPE));
    }

    public static int ApplyToProcess(
        string processName,
        uint captionColorRef,
        uint textColorRef,
        uint borderColorRef,
        bool darkMode,
        int cornerPref,
        int backdropType)
    {
        int touched = 0;
        var targets = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));

        foreach (var proc in targets)
        {
            EnumWindows((hwnd, _) =>
            {
                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid != (uint)proc.Id || !IsWindowVisible(hwnd)) return true;

                try
                {
                    SetTitleBarColor(hwnd, captionColorRef, textColorRef, borderColorRef, darkMode);
                    SetCornerPreference(hwnd, cornerPref);
                    SetSystemBackdrop(hwnd, backdropType);
                    Interlocked.Increment(ref touched);
                }
                catch { /* window may have closed between enum and call */ }

                return true;
            }, IntPtr.Zero);

            proc.Dispose();
        }

        return touched;
    }

    // Converts 0xAARRGGBB (WPF/HTML color) to 0x00BBGGRR (COLORREF)
    public static uint ToBgr(uint argb)
    {
        uint r = (argb >> 16) & 0xFF;
        uint g = (argb >>  8) & 0xFF;
        uint b =  argb        & 0xFF;
        return (b << 16) | (g << 8) | r;
    }

    private static void ThrowOnError(int hr, string attrName)
    {
        if (hr != 0) throw new Win32Exception(hr, $"DwmSetWindowAttribute({attrName}) failed with HRESULT 0x{hr:X8}");
    }
}
