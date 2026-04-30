using Microsoft.Win32;
using System.Diagnostics;

namespace Sakura.Core.Integrations;

/// <summary>Detects Rainmeter and drives it via its IPC command-line interface.</summary>
public static class RainmeterManager
{
    // ── Detection ─────────────────────────────────────────────────────────────

    public static string? FindExe()
    {
        if (!OperatingSystem.IsWindows()) return null;

        // 1. Registry uninstall key (most reliable)
        foreach (string hive in new[] { @"SOFTWARE\Rainmeter", @"SOFTWARE\WOW6432Node\Rainmeter" })
        {
            using var key = Registry.LocalMachine.OpenSubKey(hive);
            if (key?.GetValue("") is string dir)
            {
                string path = Path.Combine(dir, "Rainmeter.exe");
                if (File.Exists(path)) return path;
            }
        }

        // 2. Common install paths
        foreach (string candidate in new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),   "Rainmeter", "Rainmeter.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Rainmeter", "Rainmeter.exe"),
        })
        {
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    public static bool IsInstalled() => FindExe() is not null;

    // ── Skin / layout discovery ────────────────────────────────────────────────

    public static string GetSkinsFolder()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Rainmeter", "Skins");

    public static string GetLayoutsFolder()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Rainmeter", "Layouts");

    /// <summary>Lists all skin config folders (immediate subdirs of each skin root).</summary>
    public static IReadOnlyList<string> GetInstalledSkins(string? skinsFolder = null)
    {
        string root = skinsFolder ?? GetSkinsFolder();
        if (!Directory.Exists(root)) return [];

        return Directory.GetDirectories(root)
            .Select(d => Path.GetFileName(d)!)
            .OrderBy(n => n)
            .ToList();
    }

    /// <summary>Lists saved Rainmeter layouts.</summary>
    public static IReadOnlyList<string> GetLayouts(string? layoutsFolder = null)
    {
        string root = layoutsFolder ?? GetLayoutsFolder();
        if (!Directory.Exists(root)) return [];

        return Directory.GetDirectories(root)
            .Select(d => Path.GetFileName(d)!)
            .OrderBy(n => n)
            .ToList();
    }

    // ── IPC commands ──────────────────────────────────────────────────────────

    /// <summary>Loads a named Rainmeter layout (!LoadLayout).</summary>
    public static async Task LoadLayoutAsync(string layoutName, CancellationToken ct = default)
    {
        string exe = FindExe() ?? throw new InvalidOperationException("Rainmeter is not installed.");
        await RunCommandAsync(exe, $"!LoadLayout \"{layoutName}\"", ct).ConfigureAwait(false);
    }

    /// <summary>Activates a specific skin config + variant (!ActivateConfig).</summary>
    public static async Task ActivateSkinAsync(string config, string variant, CancellationToken ct = default)
    {
        string exe = FindExe() ?? throw new InvalidOperationException("Rainmeter is not installed.");
        await RunCommandAsync(exe, $"!ActivateConfig \"{config}\" \"{variant}\"", ct).ConfigureAwait(false);
    }

    /// <summary>Deactivates all variants of a skin config (!DeactivateConfig).</summary>
    public static async Task DeactivateSkinAsync(string config, CancellationToken ct = default)
    {
        string exe = FindExe() ?? throw new InvalidOperationException("Rainmeter is not installed.");
        await RunCommandAsync(exe, $"!DeactivateConfig \"{config}\"", ct).ConfigureAwait(false);
    }

    /// <summary>Refreshes all active skins (!RefreshApp).</summary>
    public static async Task RefreshAsync(CancellationToken ct = default)
    {
        string exe = FindExe() ?? throw new InvalidOperationException("Rainmeter is not installed.");
        await RunCommandAsync(exe, "!RefreshApp", ct).ConfigureAwait(false);
    }

    /// <summary>Applies a full RainmeterSettings block from a profile.</summary>
    public static async Task ApplyProfileAsync(
        RainmeterSettings settings,
        CancellationToken ct = default)
    {
        if (!settings.Apply) return;
        string exe = FindExe() ?? throw new InvalidOperationException("Rainmeter is not installed.");

        // Load named layout if specified (overrides individual skin settings)
        if (!string.IsNullOrWhiteSpace(settings.Layout))
        {
            await RunCommandAsync(exe, $"!LoadLayout \"{settings.Layout}\"", ct).ConfigureAwait(false);
            return;
        }

        // Activate individual skins
        foreach (var skin in settings.Skins)
        {
            ct.ThrowIfCancellationRequested();
            if (skin.Enabled)
                await RunCommandAsync(exe, $"!ActivateConfig \"{skin.Config}\" \"{skin.Variant}\"", ct).ConfigureAwait(false);
            else
                await RunCommandAsync(exe, $"!DeactivateConfig \"{skin.Config}\"", ct).ConfigureAwait(false);
        }
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private static async Task RunCommandAsync(string exe, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = false,
            RedirectStandardError  = false
        };

        using var proc = Process.Start(psi)
                         ?? throw new InvalidOperationException($"Failed to start Rainmeter: {args}");

        await proc.WaitForExitAsync(ct).ConfigureAwait(false);

        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"Rainmeter command failed (exit {proc.ExitCode}): {args}");
    }
}
