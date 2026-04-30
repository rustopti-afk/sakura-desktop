using Microsoft.Win32;

namespace Sakura.Core.Integrations;

/// <summary>
/// Manages Windhawk mods by reading and writing the Windhawk configuration registry hive.
/// Windhawk stores mod state under HKLM\SOFTWARE\Windhawk\Mods\{modId} → Disabled (DWORD).
/// </summary>
public static class WindhawkManager
{
    private const string RegBase    = @"SOFTWARE\Windhawk";
    private const string RegMods    = @"SOFTWARE\Windhawk\Mods";
    private const string ExeName    = "windhawk.exe";

    // ── Detection ─────────────────────────────────────────────────────────────

    public static string? FindExe()
    {
        if (!OperatingSystem.IsWindows()) return null;

        // Check registry for install path
        using var key = Registry.LocalMachine.OpenSubKey(RegBase);
        if (key?.GetValue("InstallPath") is string dir)
        {
            string path = Path.Combine(dir, ExeName);
            if (File.Exists(path)) return path;
        }

        // Common paths
        foreach (string candidate in new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),   "Windhawk", ExeName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Windhawk", ExeName),
        })
        {
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    public static bool IsInstalled() => FindExe() is not null;

    // ── Mod discovery ─────────────────────────────────────────────────────────

    /// <summary>Returns all installed mod IDs and their enabled state.</summary>
    public static IReadOnlyList<WindhawkModInfo> GetInstalledMods()
    {
        if (!OperatingSystem.IsWindows()) return [];

        using var modsKey = Registry.LocalMachine.OpenSubKey(RegMods);
        if (modsKey is null) return [];

        return modsKey.GetSubKeyNames()
            .Select(id =>
            {
                using var modKey = modsKey.OpenSubKey(id);
                bool disabled = modKey?.GetValue("Disabled") is int d && d != 0;
                string version = modKey?.GetValue("Version") as string ?? "";
                return new WindhawkModInfo(id, !disabled, version);
            })
            .ToList();
    }

    // ── Enable / disable ──────────────────────────────────────────────────────

    /// <summary>Enables or disables a single mod by writing to registry.</summary>
    public static void SetModEnabled(string modId, bool enabled)
    {
        using var modsKey = Registry.LocalMachine.OpenSubKey(RegMods, writable: true);
        if (modsKey is null)
            throw new InvalidOperationException("Windhawk registry key not found. Is Windhawk installed?");

        using var modKey = modsKey.OpenSubKey(modId, writable: true)
                           ?? throw new KeyNotFoundException($"Windhawk mod '{modId}' is not installed.");

        // Disabled=0 means enabled; Disabled=1 means disabled
        modKey.SetValue("Disabled", enabled ? 0 : 1, RegistryValueKind.DWord);
    }

    /// <summary>Applies a full WindhawkSettings block from a profile.</summary>
    public static void ApplyProfile(WindhawkSettings settings)
    {
        if (!settings.Apply) return;

        using var modsKey = Registry.LocalMachine.OpenSubKey(RegMods, writable: true);
        if (modsKey is null)
            throw new InvalidOperationException("Windhawk registry key not found. Is Windhawk installed?");

        foreach (var mod in settings.Mods)
        {
            using var modKey = modsKey.OpenSubKey(mod.Id, writable: true);
            if (modKey is null) continue; // mod not installed — skip silently

            modKey.SetValue("Disabled", mod.Enabled ? 0 : 1, RegistryValueKind.DWord);
        }
    }

    /// <summary>Reads the current state of a single mod.</summary>
    public static bool IsModEnabled(string modId)
    {
        using var modKey = Registry.LocalMachine.OpenSubKey(Path.Combine(RegMods, modId));
        if (modKey is null) return false;
        return modKey.GetValue("Disabled") is not int d || d == 0;
    }
}

public sealed record WindhawkModInfo(string Id, bool Enabled, string Version);
