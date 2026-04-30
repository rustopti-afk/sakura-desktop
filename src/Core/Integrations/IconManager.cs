using Microsoft.Win32;
using Sakura.Core.Profile;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Sakura.Core.Integrations;

/// <summary>
/// Applies icon packs and cursor packs from profile settings.
///
/// Shell icon pack  — writes HKCU Shell Icons registry entries (user-level).
/// Cursor pack      — writes HKCU Control Panel\Cursors, calls SPI_SETCURSORS.
/// Resource patching — uses Win32 UpdateResource to embed .ico into exe/dll
///                    (works for user-owned targets; system files need elevation).
/// </summary>
public static class IconManager
{
    // ── Shell icon index → canonical file name mapping ────────────────────
    // Keys are shell32/imageres indices used by Windows 11.
    private static readonly Dictionary<int, string> ShellIconNames = new()
    {
        [3]  = "folder",
        [4]  = "folder_open",
        [15] = "drive",
        [16] = "computer",
        [17] = "network",
        [20] = "file",
        [22] = "program",
        [29] = "recycle_empty",
        [31] = "recycle_full",
        [36] = "control_panel",
        [43] = "network_drive",
        [44] = "network_drive_disconnected",
    };

    // ── Cursor name → registry value name mapping ─────────────────────────
    private static readonly Dictionary<string, string> CursorValueNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["arrow"]        = "Arrow",
        ["help"]         = "Help",
        ["appstarting"]  = "AppStarting",
        ["wait"]         = "Wait",
        ["crosshair"]    = "Crosshair",
        ["ibeam"]        = "IBeam",
        ["nwpen"]        = "NWPen",
        ["no"]           = "No",
        ["sizens"]       = "SizeNS",
        ["sizewe"]       = "SizeWE",
        ["sizenwse"]     = "SizeNWSE",
        ["sizenesw"]     = "SizeNESW",
        ["sizeall"]      = "SizeAll",
        ["uparrow"]      = "UpArrow",
        ["hand"]         = "Hand",
    };

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Applies icon pack and cursor pack from profile.
    /// On non-Windows this is a no-op.
    /// </summary>
    public static void ApplyProfile(IconSettings settings, string? iconsBaseDir = null)
    {
        if (!OperatingSystem.IsWindows()) return;
        if (settings.Pack is null && settings.CursorPack is null && settings.PatchTargets.Length == 0)
            return;

        string baseDir = iconsBaseDir
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "Sakura", "icons");

        if (settings.Pack is not null)
        {
            string packDir = Path.Combine(baseDir, settings.Pack);
            if (Directory.Exists(packDir))
                ApplyShellIconPack(packDir);
        }

        if (settings.CursorPack is not null)
        {
            string cursorDir = Path.Combine(baseDir, settings.CursorPack);
            if (Directory.Exists(cursorDir))
                ApplyCursorPack(cursorDir);
        }

        if (settings.PatchTargets.Length > 0 && settings.Pack is not null)
        {
            string packDir = Path.Combine(baseDir, settings.Pack);
            if (Directory.Exists(packDir))
                PatchResourceIcons(settings.PatchTargets, packDir);
        }
    }

    /// <summary>
    /// Writes shell icon overrides into HKCU Shell Icons registry key.
    /// packDir must contain .ico files named by the canonical names in ShellIconNames
    /// (e.g. "folder.ico", "recycle_empty.ico").
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static void ApplyShellIconPack(string packDir)
    {
        const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons";
        using var key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);

        foreach (var (index, name) in ShellIconNames)
        {
            string icoPath = Path.Combine(packDir, name + ".ico");
            if (File.Exists(icoPath))
                key.SetValue(index.ToString(), icoPath, RegistryValueKind.String);
        }

        // Notify Explorer to reload icons
        NativeMethods.SHChangeNotify(0x08000000 /* SHCNE_ASSOCCHANGED */, 0, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>
    /// Removes all shell icon overrides from HKCU Shell Icons, restoring system defaults.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static void RestoreDefaultShellIcons()
    {
        const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons";
        Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
        NativeMethods.SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>
    /// Applies cursor pack by writing paths to HKCU\Control Panel\Cursors
    /// and calling SystemParametersInfo(SPI_SETCURSORS) to activate them immediately.
    /// packDir must contain .cur/.ani files named by cursor type (arrow.cur, wait.ani…).
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static void ApplyCursorPack(string packDir)
    {
        const string keyPath = @"Control Panel\Cursors";
        using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(keyPath, writable: true);

        foreach (var (fileName, valueName) in CursorValueNames)
        {
            // Accept both .cur and .ani variants
            string? cursorPath = FindCursorFile(packDir, fileName);
            if (cursorPath is not null)
                key.SetValue(valueName, cursorPath, RegistryValueKind.ExpandString);
        }

        // Activate cursors immediately (SPIF_UPDATEINIFILE | SPIF_SENDCHANGE)
        NativeMethods.SystemParametersInfo(0x0057 /* SPI_SETCURSORS */, 0, IntPtr.Zero, 0x03);
    }

    /// <summary>
    /// Restores Windows default cursors by clearing all cursor registry values.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static void RestoreDefaultCursors()
    {
        const string keyPath = @"Control Panel\Cursors";
        using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
        if (key is null) return;

        foreach (var valueName in CursorValueNames.Values)
            key.SetValue(valueName, "", RegistryValueKind.ExpandString);

        NativeMethods.SystemParametersInfo(0x0057, 0, IntPtr.Zero, 0x03);
    }

    /// <summary>
    /// Patches the RT_ICON/RT_GROUP_ICON resources inside each target exe/dll
    /// using the matching .ico from packDir. Skips targets that don't exist
    /// or for which no matching icon is found.
    /// Note: system-owned targets (shell32.dll, imageres.dll) require elevation.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static void PatchResourceIcons(string[] targets, string packDir)
    {
        foreach (string target in targets)
        {
            if (!File.Exists(target)) continue;

            string baseName = Path.GetFileNameWithoutExtension(target).ToLowerInvariant();
            string icoPath  = Path.Combine(packDir, baseName + ".ico");
            if (!File.Exists(icoPath)) continue;

            byte[] icoData = File.ReadAllBytes(icoPath);
            TryPatchIconResource(target, icoData);
        }
    }

    // ── Internals ──────────────────────────────────────────────────────────

    [SupportedOSPlatform("windows")]
    private static void TryPatchIconResource(string target, byte[] icoData)
    {
        IntPtr handle = NativeMethods.BeginUpdateResource(target, false);
        if (handle == IntPtr.Zero) return;

        try
        {
            // RT_ICON = 3, index 1 (first icon in file), language neutral
            IntPtr dataPtr = Marshal.AllocHGlobal(icoData.Length);
            Marshal.Copy(icoData, 0, dataPtr, icoData.Length);
            NativeMethods.UpdateResource(handle, (IntPtr)3, (IntPtr)1, 0, dataPtr, (uint)icoData.Length);
            Marshal.FreeHGlobal(dataPtr);
            NativeMethods.EndUpdateResource(handle, false);
        }
        catch
        {
            NativeMethods.EndUpdateResource(handle, true); // discard changes on error
        }
    }

    private static string? FindCursorFile(string dir, string baseName)
    {
        foreach (string ext in new[] { ".ani", ".cur" })
        {
            string path = Path.Combine(dir, baseName + ext);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    // ── P/Invoke declarations ──────────────────────────────────────────────

    [SupportedOSPlatform("windows")]
    private static class NativeMethods
    {
        [DllImport("shell32.dll")]
        internal static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName,
            ushort wLanguage, IntPtr lpData, uint cbData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);
    }
}
