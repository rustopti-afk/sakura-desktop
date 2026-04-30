using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Sakura.Core.Native;

public static class FontManager
{
    // ── P/Invoke ───────────────────────────────────────────────────────────

    private const uint SPI_GETNONCLIENTMETRICS = 0x0029;
    private const uint SPI_SETNONCLIENTMETRICS = 0x002A;
    private const uint SPI_GETICONMETRICS      = 0x002D;
    private const uint SPI_SETICONMETRICS      = 0x002E;
    private const uint SPIF_UPDATEINIFILE      = 0x01;
    private const uint SPIF_SENDCHANGE         = 0x02;
    private const uint FLAGS = SPIF_UPDATEINIFILE | SPIF_SENDCHANGE;

    private const int LF_FACESIZE = 32;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct LOGFONT
    {
        public int  lfHeight;
        public int  lfWidth;
        public int  lfEscapement;
        public int  lfOrientation;
        public int  lfWeight;
        public byte lfItalic;
        public byte lfUnderline;
        public byte lfStrikeOut;
        public byte lfCharSet;
        public byte lfOutPrecision;
        public byte lfClipPrecision;
        public byte lfQuality;
        public byte lfPitchAndFamily;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE)]
        public string lfFaceName;
    }

    // Sizes are in twips (logical units). cbSize must be set by caller.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NONCLIENTMETRICS
    {
        public uint    cbSize;
        public int     iBorderWidth;
        public int     iScrollWidth;
        public int     iScrollHeight;
        public int     iCaptionWidth;
        public int     iCaptionHeight;
        public LOGFONT lfCaptionFont;
        public int     iSmCaptionWidth;
        public int     iSmCaptionHeight;
        public LOGFONT lfSmCaptionFont;
        public int     iMenuWidth;
        public int     iMenuHeight;
        public LOGFONT lfMenuFont;
        public LOGFONT lfStatusFont;
        public LOGFONT lfMessageFont;
        public int     iPaddedBorderWidth; // Windows Vista+
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ICONMETRICS
    {
        public uint    cbSize;
        public int     iHorzSpacing;
        public int     iVertSpacing;
        public int     iTitleWrap;
        public LOGFONT lfFont;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(
        uint uiAction, uint uiParam, ref NONCLIENTMETRICS pvParam, uint fWinIni);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(
        uint uiAction, uint uiParam, ref ICONMETRICS pvParam, uint fWinIni);

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Applies fontName to all non-client UI elements (caption, menu, status, message).
    /// size is in points; pass 0 to keep current height.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static void ApplyNonClientFont(string fontName, int sizePoints = 0)
    {
        var ncm = new NONCLIENTMETRICS { cbSize = (uint)Marshal.SizeOf<NONCLIENTMETRICS>() };
        if (!SystemParametersInfo(SPI_GETNONCLIENTMETRICS, ncm.cbSize, ref ncm, 0))
            throw new InvalidOperationException($"SPI_GETNONCLIENTMETRICS failed: {Marshal.GetLastWin32Error()}");

        int lfHeight = sizePoints > 0 ? PointsToLogical(sizePoints) : ncm.lfCaptionFont.lfHeight;

        ApplyFontToLogFont(ref ncm.lfCaptionFont,   fontName, lfHeight);
        ApplyFontToLogFont(ref ncm.lfSmCaptionFont, fontName, lfHeight);
        ApplyFontToLogFont(ref ncm.lfMenuFont,      fontName, lfHeight);
        ApplyFontToLogFont(ref ncm.lfStatusFont,    fontName, lfHeight);
        ApplyFontToLogFont(ref ncm.lfMessageFont,   fontName, lfHeight);

        if (!SystemParametersInfo(SPI_SETNONCLIENTMETRICS, ncm.cbSize, ref ncm, FLAGS))
            throw new InvalidOperationException($"SPI_SETNONCLIENTMETRICS failed: {Marshal.GetLastWin32Error()}");
    }

    /// <summary>Applies fontName to desktop icon labels.</summary>
    [SupportedOSPlatform("windows")]
    public static void ApplyIconFont(string fontName, int sizePoints = 0)
    {
        var im = new ICONMETRICS { cbSize = (uint)Marshal.SizeOf<ICONMETRICS>() };
        if (!SystemParametersInfo(SPI_GETICONMETRICS, im.cbSize, ref im, 0))
            throw new InvalidOperationException($"SPI_GETICONMETRICS failed: {Marshal.GetLastWin32Error()}");

        int lfHeight = sizePoints > 0 ? PointsToLogical(sizePoints) : im.lfFont.lfHeight;
        ApplyFontToLogFont(ref im.lfFont, fontName, lfHeight);

        if (!SystemParametersInfo(SPI_SETICONMETRICS, im.cbSize, ref im, FLAGS))
            throw new InvalidOperationException($"SPI_SETICONMETRICS failed: {Marshal.GetLastWin32Error()}");
    }

    /// <summary>Returns the current caption (title bar) font face name.</summary>
    [SupportedOSPlatform("windows")]
    public static string? GetCurrentCaptionFont()
    {
        var ncm = new NONCLIENTMETRICS { cbSize = (uint)Marshal.SizeOf<NONCLIENTMETRICS>() };
        return SystemParametersInfo(SPI_GETNONCLIENTMETRICS, ncm.cbSize, ref ncm, 0)
            ? ncm.lfCaptionFont.lfFaceName
            : null;
    }

    /// <summary>Returns the current icon label font face name.</summary>
    [SupportedOSPlatform("windows")]
    public static string? GetCurrentIconFont()
    {
        var im = new ICONMETRICS { cbSize = (uint)Marshal.SizeOf<ICONMETRICS>() };
        return SystemParametersInfo(SPI_GETICONMETRICS, im.cbSize, ref im, 0)
            ? im.lfFont.lfFaceName
            : null;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static void ApplyFontToLogFont(ref LOGFONT lf, string fontName, int lfHeight)
    {
        lf.lfFaceName = fontName;
        if (lfHeight != 0) lf.lfHeight = lfHeight;
    }

    // Converts point size to LOGFONT lfHeight (negative = character height, screen DPI assumed 96).
    private static int PointsToLogical(int pts) => -(int)Math.Round(pts * 96.0 / 72.0);
}
