using Microsoft.Extensions.Logging;
using Sakura.Core.Backup;
using Sakura.Core.Integrations;
using Sakura.Core.Native;
using Sakura.Core.Theme;

namespace Sakura.Core.Profile;

public sealed class ProfileApplicator
{
    private readonly ILogger<ProfileApplicator> _logger;
    private readonly ThemeEngine _themeEngine;
    private readonly string _backupRoot;

    public ProfileApplicator(ILogger<ProfileApplicator> logger, ThemeEngine themeEngine, string? backupRootOverride = null)
    {
        _logger      = logger;
        _themeEngine = themeEngine;
        _backupRoot  = backupRootOverride
                       ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                       "Sakura", "backup");
    }

    public async Task<ApplyResult> ApplyAsync(RiceProfile profile, IProgress<ApplyProgress>? progress = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Applying profile '{Name}' (id={Id})", profile.Profile.Name, profile.Profile.Id);

        uint currentBuild = (uint)Environment.OSVersion.Version.Build;
        if (currentBuild < profile.Profile.MinOsBuild)
            return ApplyResult.Fail($"Profile requires build {profile.Profile.MinOsBuild}, current build is {currentBuild}");

        var missingDeps = DependencyDetector.CheckProfile(profile.Dependencies.Required);
        if (missingDeps.Count > 0)
        {
            string list = string.Join(", ", missingDeps);
            _logger.LogWarning("Profile '{Name}' requires missing dependencies: {Deps}", profile.Profile.Name, list);
            return ApplyResult.Fail($"Missing required dependencies: {list}");
        }

        var session = new ApplySession(_backupRoot, $"Apply '{profile.Profile.Name}'",
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ApplySession>.Instance);

        try
        {
            // ── 1. Shell (taskbar, clock, search icons) ─────────────────────
            progress?.Report(new(1, 13,"Applying shell settings"));
            ct.ThrowIfCancellationRequested();
            ApplyShell(profile.Shell, session);

            // ── 2. Compositor + DWM ──────────────────────────────────────────
            progress?.Report(new(2, 13,"Applying compositor settings"));
            ct.ThrowIfCancellationRequested();
            ApplyCompositor(profile.Compositor, session);

            // ── 3. Theme (msstyles) ──────────────────────────────────────────
            if (profile.Theme.MsstylesPath != null)
            {
                progress?.Report(new(3, 13,"Applying msstyles theme"));
                ct.ThrowIfCancellationRequested();
                ApplyTheme(profile.Theme, profile.Compositor, session);
            }

            // ── 4. Fonts ─────────────────────────────────────────────────────
            progress?.Report(new(4, 13,"Applying font substitutes"));
            ct.ThrowIfCancellationRequested();
            ApplyFonts(profile.Fonts, session);

            // ── 5. Icons + cursors ───────────────────────────────────────────
            if (profile.Icons.Pack != null || profile.Icons.CursorPack != null || profile.Icons.PatchTargets.Length > 0)
            {
                progress?.Report(new(5, 13, "Applying icon pack"));
                ct.ThrowIfCancellationRequested();
                IconManager.ApplyProfile(profile.Icons);
            }

            // ── 6. Boot splash (HackBGRT) ─────────────────────────────────────
            if (profile.Boot.HackBgrtEnabled && profile.Boot.SplashPath != null)
            {
                progress?.Report(new(6, 13, "Deploying boot splash"));
                ct.ThrowIfCancellationRequested();
                await BootManager.ApplyProfileAsync(profile.Boot, runInstall: false, ct: ct).ConfigureAwait(false);
            }

            // ── 7. Wallpaper ─────────────────────────────────────────────────
            if (profile.Wallpaper.Path != null || profile.Wallpaper.PerMonitor.Length > 0)
            {
                progress?.Report(new(7, 13, "Setting wallpaper"));
                ct.ThrowIfCancellationRequested();
                ApplyWallpaper(profile.Wallpaper);
            }

            // ── 8. Terminal ──────────────────────────────────────────────────
            progress?.Report(new(8, 13, "Configuring terminal"));
            ct.ThrowIfCancellationRequested();
            await ApplyTerminalAsync(profile.Terminal, ct).ConfigureAwait(false);

            // ── 9. Window manager ────────────────────────────────────────────
            if (profile.Wm.Engine != "none")
            {
                progress?.Report(new(9, 13, "Deploying WM config"));
                ct.ThrowIfCancellationRequested();
                await WmManager.ApplyProfileAsync(profile.Wm, startWm: false, ct).ConfigureAwait(false);
            }

            // ── 10. Rainmeter ─────────────────────────────────────────────────
            if (profile.Rainmeter.Apply)
            {
                progress?.Report(new(10, 13, "Applying Rainmeter layout"));
                ct.ThrowIfCancellationRequested();
                await RainmeterManager.ApplyProfileAsync(profile.Rainmeter, ct).ConfigureAwait(false);
            }

            // ── 11. Windhawk ─────────────────────────────────────────────────
            if (profile.Windhawk.Apply)
            {
                progress?.Report(new(11, 13, "Applying Windhawk mods"));
                ct.ThrowIfCancellationRequested();
                WindhawkManager.ApplyProfile(profile.Windhawk);
            }

            // ── 12. Lively Wallpaper ─────────────────────────────────────────
            if (profile.Lively.Apply)
            {
                progress?.Report(new(12, 13, "Applying Lively wallpaper"));
                ct.ThrowIfCancellationRequested();
                await LivelyManager.ApplyProfileAsync(profile.Lively, ct).ConfigureAwait(false);
            }

            // ── 13. Commit ───────────────────────────────────────────────────
            progress?.Report(new(13, 13, "Committing backup manifest"));
            session.Commit();

            _logger.LogInformation("Profile '{Name}' applied successfully", profile.Profile.Name);
            return ApplyResult.Ok(session.BackupDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Profile apply failed — rolling back");
            session.RollbackAll();
            return ApplyResult.Fail(ex.Message);
        }
    }

    private void ApplyShell(ShellSettings s, ApplySession session)
    {
        session.SnapshotRegistry("HKCU", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
        RegistryWriter.SetTaskbarAlignment(s.TaskbarAlignment);
        RegistryWriter.SetDword(Microsoft.Win32.RegistryHive.CurrentUser,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", s.ShowTaskView ? 1u : 0u);
        RegistryWriter.SetDword(Microsoft.Win32.RegistryHive.CurrentUser,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", s.ShowWidgets ? 1u : 0u);
        RegistryWriter.SetDword(Microsoft.Win32.RegistryHive.CurrentUser,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarMn", s.ShowChat ? 1u : 0u);
        _logger.LogInformation("Shell settings applied");
    }

    private void ApplyCompositor(CompositorSettings s, ApplySession session)
    {
        uint argb = ParseArgb(s.AccentColor);
        _themeEngine.Apply(new Core.Theme.ThemeSettings
        {
            DarkMode         = s.DarkMode,
            Transparency     = s.Transparency,
            ColorizationArgb = argb,
            ColorPrevalence  = s.ColorPrevalence,
            BackdropType     = s.BackdropType,
            CornerPref       = s.CornerPref,
            CaptionColorBgr  = DwmTitlebar.ToBgr(ParseArgb(s.CaptionColor)),
            TextColorBgr     = DwmTitlebar.ToBgr(ParseArgb(s.TextColor)),
            BorderColorBgr   = DwmTitlebar.ToBgr(ParseArgb(s.BorderColor))
        }, session);

        RegistryWriter.SetAnimations(s.AnimationsEnabled);
        RegistryWriter.SetMenuShowDelay(s.MenuDelay);
    }

    private void ApplyTheme(ThemeSettings t, CompositorSettings c, ApplySession session)
    {
        _themeEngine.Apply(new Core.Theme.ThemeSettings
        {
            MsstylesPath = t.MsstylesPath,
            ThemeName    = t.ThemeName,
            DarkMode     = c.DarkMode
        }, session);
    }

    private void ApplyFonts(FontSettings f, ApplySession session)
    {
        session.SnapshotRegistry("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontSubstitutes");
        foreach (var (requested, actual) in f.Substitutes)
            RegistryWriter.SetFontSubstitute(requested, actual);
    }

    private static void ApplyWallpaper(WallpaperSettings w)
    {
        if (w.PerMonitor.Length > 0)
        {
            var map = w.PerMonitor.ToDictionary(
                m => m.MonitorPath,
                m => (m.Wallpaper, (WallpaperFit)m.Fit));
            WallpaperManager.SetWallpaperPerMonitor(map);
        }
        else if (w.Path != null)
        {
            WallpaperManager.SetWallpaperAllMonitors(w.Path, (WallpaperFit)w.Fit);
        }
    }

    private static async Task ApplyTerminalAsync(TerminalSettings t, CancellationToken ct)
    {
        if (t.ApplyColorScheme)
            await TerminalManager.ApplyColorSchemeAsync(t.SchemeName, t.FontFace, t.FontSize, t.Opacity, t.UseAcrylic, ct)
                .ConfigureAwait(false);
        if (t.ApplyOhMyPosh)
            await OhMyPoshManager.DeploySakuraThemeAsync(ct).ConfigureAwait(false);
    }

    private static uint ParseArgb(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        return Convert.ToUInt32(hex, 16);
    }
}

public sealed record ApplyResult(bool Success, string? BackupDir, string? ErrorMessage)
{
    public static ApplyResult Ok(string backupDir) => new(true, backupDir, null);
    public static ApplyResult Fail(string msg)     => new(false, null, msg);
}

public sealed record ApplyProgress(int Step, int TotalSteps, string Message)
{
    public double Fraction => TotalSteps > 0 ? (double)Step / TotalSteps : 0;
}
