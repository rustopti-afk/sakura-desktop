using Microsoft.Extensions.Logging;
using Sakura.Core.Backup;
using Sakura.Core.Native;
using Microsoft.Win32;

namespace Sakura.Core.Theme;

public sealed class ThemeSettings
{
    public string? MsstylesPath   { get; init; }
    public string? ThemeName      { get; init; }
    public bool    DarkMode        { get; init; } = true;
    public bool    Transparency    { get; init; } = true;
    public uint    ColorizationArgb { get; init; } = 0xC4E8A0BF;
    public bool    ColorPrevalence { get; init; } = true;
    public int     BackdropType    { get; init; } = DwmTitlebar.DWMSBT_MICA;
    public int     CornerPref      { get; init; } = DwmTitlebar.DWMWCP_ROUND;
    public uint    CaptionColorBgr { get; init; } = 0x14180D;  // #0D1814 → BGR
    public uint    TextColorBgr    { get; init; } = 0xEEE6ED;  // #EDE6EE → BGR
    public uint    BorderColorBgr  { get; init; } = 0xBFA0E8;  // #E8A0BF → BGR
}

public sealed class ThemeEngine
{
    private readonly ILogger<ThemeEngine> _logger;

    public ThemeEngine(ILogger<ThemeEngine> logger) => _logger = logger;

    public void Apply(ThemeSettings settings, ApplySession session)
    {
        _logger.LogInformation("Applying theme settings (dark={Dark}, backdrop={Backdrop})",
            settings.DarkMode, settings.BackdropType);

        // 1. Snapshot registry before changes
        session.SnapshotRegistry("HKCU", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        session.SnapshotRegistry("HKCU", @"SOFTWARE\Microsoft\Windows\DWM");

        // 2. Apply DWM registry settings
        RegistryWriter.ApplyDwmSettings(
            settings.DarkMode,
            settings.Transparency,
            settings.ColorizationArgb,
            settings.ColorPrevalence);

        _logger.LogInformation("DWM registry settings applied");

        // 3. Apply msstyles if specified
        if (!string.IsNullOrEmpty(settings.MsstylesPath) && !string.IsNullOrEmpty(settings.ThemeName))
        {
            ApplyMsstyles(settings.MsstylesPath, settings.ThemeName, session);
        }

        // 4. Apply DWM window attributes to all currently open windows
        int patched = ApplyDwmToRunningApps(settings);
        _logger.LogInformation("Patched DWM attributes on {Count} windows", patched);

        // 5. Broadcast WM_SETTINGCHANGE so apps pick up theme change
        BroadcastSettingChange();

        session.Commit();
        _logger.LogInformation("Theme apply complete");
    }

    public void Revert(BackupManifest manifest)
    {
        _logger.LogInformation("Reverting theme from manifest {Id}", manifest.Id);
        foreach (var artifact in manifest.Artifacts.Reverse())
        {
            try
            {
                switch (artifact.Kind)
                {
                    case ArtifactKind.Registry:
                        RegistryBackup.Restore(artifact);
                        break;
                    case ArtifactKind.File:
                        string dest = ResolveFileDest(artifact.Path);
                        File.Copy(artifact.Path, dest, overwrite: true);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Revert failed for artifact {Path}", artifact.Path);
                throw;
            }
        }
        BroadcastSettingChange();
    }

    private void ApplyMsstyles(string msstylesPath, string themeName, ApplySession session)
    {
        _logger.LogInformation("Applying msstyles: {Path}", msstylesPath);

        SecureUxThemeStatus status = SecureUxThemeHelper.Detect();
        if (status == SecureUxThemeStatus.NotInstalled)
            throw new InvalidOperationException(
                "SecureUxTheme is not installed. Install it via Settings → Dependencies first.");

        // Snapshot current theme before replacing
        string? currentTheme = Registry.CurrentUser
            .OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes")
            ?.GetValue("CurrentTheme") as string;
        if (currentTheme != null && File.Exists(currentTheme))
            session.SnapshotFile(currentTheme);

        string themeFile = SecureUxThemeHelper.DeployTheme(msstylesPath, themeName);
        SecureUxThemeHelper.ApplyTheme(themeFile);
        _logger.LogInformation("msstyles applied via SecureUxTheme: {ThemeFile}", themeFile);
    }

    private static int ApplyDwmToRunningApps(ThemeSettings s)
    {
        // Apply to common productivity apps — safe windows that DWM attributes work on
        string[] targets = ["explorer", "notepad", "code", "firefox", "chrome", "msedge",
                             "WindowsTerminal", "powershell", "pwsh", "cmd"];
        int total = 0;
        foreach (string t in targets)
        {
            try
            {
                total += DwmTitlebar.ApplyToProcess(t, s.CaptionColorBgr, s.TextColorBgr,
                    s.BorderColorBgr, s.DarkMode, s.CornerPref, s.BackdropType);
            }
            catch { /* process may not be running */ }
        }
        return total;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern IntPtr SendMessageTimeoutW(IntPtr hwnd, uint msg, UIntPtr wParam,
        string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

    private static readonly IntPtr HWND_BROADCAST = new(-1);
    private const uint WM_SETTINGCHANGE = 0x001A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    private static void BroadcastSettingChange()
    {
        SendMessageTimeoutW(HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero,
            "ImmersiveColorSet", SMTO_ABORTIFHUNG, 1000, out _);
    }

    private static string ResolveFileDest(string backupPath)
    {
        string name = Path.GetFileNameWithoutExtension(backupPath);
        return name.Replace("__SYSTEM32__", @"C:\Windows\System32")
                   .Replace("__WINDIR__", @"C:\Windows")
                   .Replace('_', Path.DirectorySeparatorChar);
    }
}
