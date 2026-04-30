using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json;

namespace Sakura.Core.Integrations;

/// <summary>
/// Controls Lively Wallpaper via its command-line interface.
/// Lively CLI: lively.exe setwp --filepath path [--monitor index] [--layout mode]
/// </summary>
public static class LivelyManager
{
    private const string AppDataSubPath = @"Packages\12030rocksdanister.LivelyWallpaper_97hta09mmv6hy\LocalState";

    // ── Detection ─────────────────────────────────────────────────────────────

    public static string? FindExe(string? localAppDataOverride = null)
    {
        if (!OperatingSystem.IsWindows() && localAppDataOverride is null) return null;

        string localApp = localAppDataOverride
            ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Packaged (Store) installation
        string packaged = Path.Combine(localApp, AppDataSubPath, "app-0.0.0.0", "lively.exe");
        if (File.Exists(packaged)) return packaged;

        // Portable / manual installation — search common paths
        foreach (string candidate in new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),   "Lively Wallpaper", "lively.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Lively Wallpaper", "lively.exe"),
        })
        {
            if (File.Exists(candidate)) return candidate;
        }

        // Registry (Windows-only)
        if (!OperatingSystem.IsWindows()) return null;
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\lively");
        if (key?.GetValue("") is string reg)
        {
            string path = reg.Replace("\"", "").Split(' ')[0];
            if (File.Exists(path)) return path;
        }

        return null;
    }

    public static bool IsInstalled(string? localAppDataOverride = null)
        => FindExe(localAppDataOverride) is not null;

    // ── Wallpaper control ─────────────────────────────────────────────────────

    /// <summary>
    /// Sets a wallpaper file/URL on one or all monitors.
    /// wallpaperType: "video" | "gif" | "html" | "youtube" | "stream" | "picture"
    /// monitor: 0-based index, or null for all monitors.
    /// </summary>
    public static async Task SetWallpaperAsync(
        string wallpaperPath,
        string wallpaperType = "video",
        int? monitor = null,
        string layout = "per-display",
        CancellationToken ct = default,
        string? localAppDataOverride = null)
    {
        string exe = FindExe(localAppDataOverride)
                     ?? throw new InvalidOperationException("Lively Wallpaper is not installed.");

        var args = new List<string>
        {
            "setwp",
            "--filepath", $"\"{wallpaperPath}\"",
            "--type",     wallpaperType,
            "--layout",   layout
        };

        if (monitor.HasValue)
            args.AddRange(["--monitor", monitor.Value.ToString()]);

        await RunAsync(exe, string.Join(" ", args), ct).ConfigureAwait(false);
    }

    /// <summary>Closes all active Lively wallpapers (closes the window).</summary>
    public static async Task CloseAllAsync(CancellationToken ct = default, string? localAppDataOverride = null)
    {
        string exe = FindExe(localAppDataOverride)
                     ?? throw new InvalidOperationException("Lively Wallpaper is not installed.");
        await RunAsync(exe, "closewp --monitor -1", ct).ConfigureAwait(false);
    }

    /// <summary>Applies a full LivelySettings block from a profile.</summary>
    public static async Task ApplyProfileAsync(
        LivelySettings settings,
        CancellationToken ct = default,
        string? localAppDataOverride = null)
    {
        if (!settings.Apply) return;
        if (string.IsNullOrWhiteSpace(settings.WallpaperPath)) return;

        await SetWallpaperAsync(
            settings.WallpaperPath,
            settings.WallpaperType,
            settings.Monitor,
            settings.Layout,
            ct,
            localAppDataOverride).ConfigureAwait(false);
    }

    /// <summary>Reads active wallpapers from Lively's settings.json.</summary>
    public static IReadOnlyList<LivelyActiveWallpaper> GetActiveWallpapers(string? localAppDataOverride = null)
    {
        string localApp = localAppDataOverride
            ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string settingsPath = Path.Combine(localApp, AppDataSubPath, "Settings", "WallpaperLayout.json");
        if (!File.Exists(settingsPath)) return [];

        try
        {
            string json = File.ReadAllText(settingsPath);
            var doc     = JsonDocument.Parse(json);
            var result  = new List<LivelyActiveWallpaper>();

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in doc.RootElement.EnumerateArray())
                {
                    string path    = entry.TryGetProperty("LivelyInfoPath", out var p) ? p.GetString() ?? "" : "";
                    string display = entry.TryGetProperty("Display",        out var d) ? d.GetString() ?? "" : "";
                    result.Add(new LivelyActiveWallpaper(path, display));
                }
            }

            return result;
        }
        catch { return []; }
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private static async Task RunAsync(string exe, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true
        };

        using var proc = Process.Start(psi)
                         ?? throw new InvalidOperationException($"Failed to start Lively: {args}");

        await proc.WaitForExitAsync(ct).ConfigureAwait(false);

        if (proc.ExitCode != 0)
        {
            string err = await proc.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException($"Lively command failed (exit {proc.ExitCode}): {err}");
        }
    }
}

public sealed record LivelyActiveWallpaper(string InfoPath, string Display);
