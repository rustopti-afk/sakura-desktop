using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sakura.Core.Integrations;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Sakura.App.ViewModels;

public sealed partial class IntegrationsViewModel : ObservableObject
{
    private readonly ILogger<IntegrationsViewModel> _logger;

    // ── Rainmeter ──────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _rainmeterInstalled;
    [ObservableProperty] private string _rainmeterStatus = "";
    public ObservableCollection<string> InstalledSkins   { get; } = [];
    public ObservableCollection<string> InstalledLayouts { get; } = [];

    // ── Windhawk ───────────────────────────────────────────────────────────
    [ObservableProperty] private bool _windhawkInstalled;
    [ObservableProperty] private string _windhawkStatus = "";
    public ObservableCollection<WindhawkModInfo> InstalledMods { get; } = [];

    // ── Lively ─────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _livelyInstalled;
    [ObservableProperty] private string _livelyStatus = "";
    public ObservableCollection<LivelyActiveWallpaper> ActiveWallpapers { get; } = [];

    // ── Icons ──────────────────────────────────────────────────────────────
    [ObservableProperty] private string _iconsBaseDir  = "";
    [ObservableProperty] private int    _iconPackCount = 0;
    public ObservableCollection<string> IconPacks   { get; } = [];
    public ObservableCollection<string> CursorPacks { get; } = [];

    // ── Boot (HackBGRT) ────────────────────────────────────────────────────
    [ObservableProperty] private bool   _hackBgrtInstalled;
    [ObservableProperty] private string _hackBgrtStatus = "";
    [ObservableProperty] private string _hackBgrtDir    = "";

    // ── General ────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isBusy        = false;
    [ObservableProperty] private string _statusMessage = "";

    public IntegrationsViewModel(ILogger<IntegrationsViewModel> logger)
    {
        _logger = logger;
        Refresh();
    }

    [RelayCommand]
    public void Refresh()
    {
        RefreshRainmeter();
        RefreshWindhawk();
        RefreshLively();
        RefreshIcons();
        RefreshBoot();
        StatusMessage = "Integration status refreshed";
    }

    // ── Rainmeter commands ─────────────────────────────────────────────────

    [RelayCommand]
    public async Task RainmeterLoadLayoutAsync(string layout)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(layout)) return;
        IsBusy = true;
        try
        {
            await RainmeterManager.LoadLayoutAsync(layout);
            StatusMessage = $"Loaded layout '{layout}'";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load layout failed: {ex.Message}";
            _logger.LogError(ex, "Rainmeter load layout failed");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task RainmeterRefreshAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await RainmeterManager.RefreshAsync();
            StatusMessage = "Rainmeter refreshed";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Rainmeter refresh failed: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // ── Windhawk commands ──────────────────────────────────────────────────

    [RelayCommand]
    public void ToggleMod(WindhawkModInfo mod)
    {
        if (mod is null || !WindhawkManager.IsInstalled()) return;
        try
        {
            WindhawkManager.SetModEnabled(mod.Id, !mod.Enabled);
            RefreshWindhawk();
            StatusMessage = $"Mod '{mod.Id}' {(!mod.Enabled ? "enabled" : "disabled")}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Toggle failed: {ex.Message}";
            _logger.LogError(ex, "Windhawk toggle mod failed");
        }
    }

    // ── Lively commands ────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LivelyCloseAllAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await LivelyManager.CloseAllAsync();
            RefreshLively();
            StatusMessage = "All Lively wallpapers closed";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Close all failed: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // ── Icons commands ─────────────────────────────────────────────────────

    [RelayCommand]
    public void RestoreDefaultIcons()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            IconManager.RestoreDefaultShellIcons();
            IconManager.RestoreDefaultCursors();
            StatusMessage = "Shell icons and cursors restored to defaults";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
            _logger.LogError(ex, "Restore default icons failed");
        }
    }

    [RelayCommand]
    public void OpenIconsFolder()
    {
        if (!string.IsNullOrEmpty(IconsBaseDir) && System.IO.Directory.Exists(IconsBaseDir))
            Process.Start("explorer.exe", IconsBaseDir);
    }

    // ── Boot commands ──────────────────────────────────────────────────────

    [RelayCommand]
    public void OpenHackBgrtFolder()
    {
        if (!string.IsNullOrEmpty(HackBgrtDir) && System.IO.Directory.Exists(HackBgrtDir))
            Process.Start("explorer.exe", HackBgrtDir);
    }

    // ── Install shortcuts ──────────────────────────────────────────────────

    [RelayCommand]
    public void InstallRainmeter()
        => OpenWinget("Rainmeter.Rainmeter");

    [RelayCommand]
    public void InstallWindhawk()
        => OpenWinget("RamSoftware.Windhawk");

    [RelayCommand]
    public void InstallLively()
        => OpenWinget("rocksdanister.LivelyWallpaper");

    // ── Internals ──────────────────────────────────────────────────────────

    private void RefreshRainmeter()
    {
        RainmeterInstalled = RainmeterManager.IsInstalled();
        RainmeterStatus    = RainmeterInstalled ? "Installed" : "Not installed";

        InstalledSkins.Clear();
        InstalledLayouts.Clear();

        if (!RainmeterInstalled) return;

        foreach (string s in RainmeterManager.GetInstalledSkins())
            InstalledSkins.Add(s);
        foreach (string l in RainmeterManager.GetLayouts())
            InstalledLayouts.Add(l);
    }

    private void RefreshWindhawk()
    {
        WindhawkInstalled = WindhawkManager.IsInstalled();
        WindhawkStatus    = WindhawkInstalled ? "Installed" : "Not installed";

        InstalledMods.Clear();
        if (!WindhawkInstalled) return;

        foreach (var mod in WindhawkManager.GetInstalledMods())
            InstalledMods.Add(mod);
    }

    private void RefreshLively()
    {
        LivelyInstalled = LivelyManager.IsInstalled();
        LivelyStatus    = LivelyInstalled ? "Installed" : "Not installed";

        ActiveWallpapers.Clear();
        if (!LivelyInstalled) return;

        foreach (var wp in LivelyManager.GetActiveWallpapers())
            ActiveWallpapers.Add(wp);
    }

    private void RefreshIcons()
    {
        string baseDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Sakura", "icons");
        IconsBaseDir = baseDir;

        IconPacks.Clear();
        CursorPacks.Clear();

        if (!System.IO.Directory.Exists(baseDir)) return;

        foreach (string dir in System.IO.Directory.GetDirectories(baseDir))
        {
            string name = System.IO.Path.GetFileName(dir);
            bool hasCursors = System.IO.Directory.GetFiles(dir, "*.cur").Length > 0
                           || System.IO.Directory.GetFiles(dir, "*.ani").Length > 0;
            bool hasIcons   = System.IO.Directory.GetFiles(dir, "*.ico").Length > 0;

            if (hasCursors) CursorPacks.Add(name);
            if (hasIcons)   IconPacks.Add(name);
        }

        IconPackCount = IconPacks.Count + CursorPacks.Count;
    }

    private void RefreshBoot()
    {
        string? dir = BootManager.FindHackBgrtDir();
        HackBgrtInstalled = dir is not null;
        HackBgrtDir       = dir ?? "";
        HackBgrtStatus    = HackBgrtInstalled
            ? $"Installed at {HackBgrtDir}"
            : "Not installed";
    }

    private static void OpenWinget(string packageId)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName        = "cmd.exe",
                Arguments       = $"/c start winget install {packageId}",
                UseShellExecute = true
            });
        }
        catch { }
    }
}
