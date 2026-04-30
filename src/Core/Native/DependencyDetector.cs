using System.Diagnostics;
using Microsoft.Win32;

namespace Sakura.Core.Native;

public enum DependencyStatus { NotInstalled, Installed, UpdateAvailable }

public sealed record Dependency(
    string Id,
    string DisplayName,
    string WingetId,
    bool IsRequired,
    string? InstallPath,
    DependencyStatus Status);

public static class DependencyDetector
{
    private static readonly IReadOnlyList<(string id, string name, string winget, bool req, string[] paths)> KnownDeps =
    [
        ("rainmeter",       "Rainmeter",          "Rainmeter.Rainmeter",              false, [@"C:\Program Files\Rainmeter\Rainmeter.exe"]),
        ("powertoys",       "PowerToys",          "Microsoft.PowerToys",              false, [@"C:\Program Files\PowerToys\PowerToys.exe"]),
        ("lively",          "Lively Wallpaper",   "rocksdanister.LivelyWallpaper",    false, [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Lively Wallpaper\Lively.exe")]),
        ("komorebi",        "komorebi",           "LGUG2Z.komorebi",                  false, [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WinGet\Packages\LGUG2Z.komorebi_Microsoft.Winget.Source_8wekyb3d8bbwe\komorebic.exe")]),
        ("glazewm",         "GlazeWM",            "glzr-io.glazewm",                  false, [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WinGet\Packages\glzr-io.glazewm_Microsoft.Winget.Source_8wekyb3d8bbwe\glazewm.exe")]),
        ("modernflyouts",   "ModernFlyouts",      "ModernFlyouts.ModernFlyouts",      false, [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\ModernFlyouts\ModernFlyouts.exe")]),
        ("windhawk",        "Windhawk",           "Ramensoftware.Windhawk",           false, [@"C:\Program Files\Windhawk\windhawk.exe"]),
        ("explorerpatcher", "ExplorerPatcher",    "valinet.ExplorerPatcher",          false, [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"ExplorerPatcher\ep_gui.exe")]),
        ("ohmyposh",        "Oh My Posh",         "JanDeDobbeleer.OhMyPosh",         false, [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\oh-my-posh\bin\oh-my-posh.exe")]),
        ("wt",              "Windows Terminal",   "Microsoft.WindowsTerminal",        false, [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WindowsApps\wt.exe")]),
        ("mactype",         "MacType",            "snowie2000.MacType",               false, [@"C:\Program Files\MacType\MacType.exe"]),
        ("elevenclock",     "ElevenClock",        "martinet63.ElevenClock",           false, [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\ElevenClock\ElevenClock.exe")]),
    ];

    public static IReadOnlyList<Dependency> Detect()
        => KnownDeps.Select(d =>
        {
            string? found = d.paths.FirstOrDefault(File.Exists)
                ?? FindInUninstallRegistry(d.id);
            var status = found != null ? DependencyStatus.Installed : DependencyStatus.NotInstalled;
            return new Dependency(d.id, d.name, d.winget, d.req, found, status);
        }).ToList();

    public static async Task InstallAsync(Dependency dep, CancellationToken ct = default)
    {
        if (dep.Status == DependencyStatus.Installed) return;

        var psi = new ProcessStartInfo("winget",
            $"install --id {dep.WingetId} --exact --silent --accept-package-agreements --accept-source-agreements")
        {
            UseShellExecute    = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow    = true
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start winget");

        await proc.WaitForExitAsync(ct).ConfigureAwait(false);
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"winget install {dep.WingetId} exited with code {proc.ExitCode}");
    }

    /// <summary>
    /// Checks which required dependencies listed in profile.Dependencies.Required
    /// are missing on the current machine. Returns display names of missing deps.
    /// Returns an empty list on non-Windows (no registry available).
    /// </summary>
    public static IReadOnlyList<string> CheckProfile(IReadOnlyList<string> requiredWingetIds)
    {
        if (!OperatingSystem.IsWindows()) return [];
        if (requiredWingetIds.Count == 0) return [];

        var installed = Detect()
            .Where(d => d.Status == DependencyStatus.Installed)
            .Select(d => d.WingetId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return requiredWingetIds
            .Where(id => !installed.Contains(id))
            .ToList();
    }

    private static string? FindInUninstallRegistry(string partialId)
    {
        if (!OperatingSystem.IsWindows()) return null;
        const string uninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            using var key = hive.OpenSubKey(uninstallPath);
            if (key is null) continue;
            foreach (var subName in key.GetSubKeyNames())
            {
                using var sub = key.OpenSubKey(subName);
                string? displayName = sub?.GetValue("DisplayName") as string;
                string? installLoc  = sub?.GetValue("InstallLocation") as string;
                if (displayName != null && displayName.Contains(partialId, StringComparison.OrdinalIgnoreCase)
                    && installLoc != null)
                    return installLoc;
            }
        }
        return null;
    }
}
