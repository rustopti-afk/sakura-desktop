using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json;

namespace Sakura.Core.Integrations;

/// <summary>
/// Manages komorebi and GlazeWM tiling window managers.
/// Generates config from profile and starts/stops the WM process.
/// </summary>
public static class WmManager
{
    private const string KomorebiExe  = "komorebic.exe";
    private const string GlazeWmExe   = "glazewm.exe";

    // ── Detection ─────────────────────────────────────────────────────────

    public static string? FindKomorebi()
    {
        if (!OperatingSystem.IsWindows()) return null;

        // SCOOP install (most common for komorebi)
        string scoop = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "scoop", "shims", KomorebiExe);
        if (File.Exists(scoop)) return scoop;

        // winget / manual install
        foreach (string candidate in new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "komorebi", KomorebiExe),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "komorebi", KomorebiExe),
        })
        {
            if (File.Exists(candidate)) return candidate;
        }

        if (!OperatingSystem.IsWindows()) return null;
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\komorebi");
        if (key?.GetValue("InstallPath") is string dir)
        {
            string path = Path.Combine(dir, KomorebiExe);
            if (File.Exists(path)) return path;
        }

        return null;
    }

    public static string? FindGlazeWm()
    {
        if (!OperatingSystem.IsWindows()) return null;

        string scoop = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "scoop", "shims", GlazeWmExe);
        if (File.Exists(scoop)) return scoop;

        foreach (string candidate in new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "GlazeWM", GlazeWmExe),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "GlazeWM", GlazeWmExe),
        })
        {
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    public static bool IsKomorebiInstalled() => FindKomorebi() is not null;
    public static bool IsGlazeWmInstalled()  => FindGlazeWm()  is not null;

    // ── Process control ───────────────────────────────────────────────────

    /// <summary>Starts komorebi (komorebic start --whkd).</summary>
    public static async Task StartKomorebiAsync(bool withWhkd = true, CancellationToken ct = default)
    {
        string exe = FindKomorebi()
            ?? throw new InvalidOperationException("komorebi is not installed.");

        string args = withWhkd ? "start --whkd" : "start";
        await RunAsync(exe, args, ct).ConfigureAwait(false);
    }

    /// <summary>Stops komorebi (komorebic stop).</summary>
    public static async Task StopKomorebiAsync(CancellationToken ct = default)
    {
        string exe = FindKomorebi()
            ?? throw new InvalidOperationException("komorebi is not installed.");
        await RunAsync(exe, "stop", ct).ConfigureAwait(false);
    }

    /// <summary>Reloads komorebi config (komorebic reload-configuration).</summary>
    public static async Task ReloadKomorebiAsync(CancellationToken ct = default)
    {
        string exe = FindKomorebi()
            ?? throw new InvalidOperationException("komorebi is not installed.");
        await RunAsync(exe, "reload-configuration", ct).ConfigureAwait(false);
    }

    /// <summary>Starts GlazeWM with --config pointing to the deployed config.</summary>
    public static async Task StartGlazeWmAsync(string? configPath = null, CancellationToken ct = default)
    {
        string exe = FindGlazeWm()
            ?? throw new InvalidOperationException("GlazeWM is not installed.");

        string args = configPath is not null
            ? $"--config \"{configPath}\""
            : string.Empty;

        await RunAsync(exe, args, ct).ConfigureAwait(false);
    }

    // ── Config generation ─────────────────────────────────────────────────

    /// <summary>
    /// Generates a komorebi.json config file from WmSettings and writes it
    /// to %USERPROFILE%\komorebi.json (the default path komorebi looks for).
    /// Returns the written path.
    /// </summary>
    public static string DeployKomorebiConfig(WmSettings settings, string? destPathOverride = null)
    {
        string dest = destPathOverride
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "komorebi.json");

        var config = BuildKomorebiConfig(settings);
        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented     = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        File.WriteAllText(dest, json);
        return dest;
    }

    /// <summary>
    /// Generates a GlazeWM config.yaml from WmSettings and writes it to
    /// %USERPROFILE%\.glaze-wm\config.yaml (the default path GlazeWM looks for).
    /// Returns the written path.
    /// </summary>
    public static string DeployGlazeWmConfig(WmSettings settings, string? destPathOverride = null)
    {
        string dir  = destPathOverride is not null
            ? Path.GetDirectoryName(destPathOverride)!
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                           ".glaze-wm");
        string dest = destPathOverride
            ?? Path.Combine(dir, "config.yaml");

        Directory.CreateDirectory(dir);
        File.WriteAllText(dest, BuildGlazeWmYaml(settings));
        return dest;
    }

    /// <summary>Applies WmSettings from a profile — deploys config and optionally starts the WM.</summary>
    public static async Task ApplyProfileAsync(
        WmSettings settings,
        bool startWm = false,
        CancellationToken ct = default)
    {
        if (settings.Engine == "none") return;

        if (settings.Engine == "komorebi")
        {
            DeployKomorebiConfig(settings);
            if (startWm)
            {
                try { await StopKomorebiAsync(ct).ConfigureAwait(false); } catch { }
                await Task.Delay(500, ct).ConfigureAwait(false);
                await StartKomorebiAsync(ct: ct).ConfigureAwait(false);
            }
        }
        else if (settings.Engine == "glazewm")
        {
            DeployGlazeWmConfig(settings);
            if (startWm)
                await StartGlazeWmAsync(ct: ct).ConfigureAwait(false);
        }
    }

    // ── Config builders ────────────────────────────────────────────────────

    private static object BuildKomorebiConfig(WmSettings s) => new
    {
        defaultLayout                = s.Layout,
        globalWorkAreaOffset         = new { left = s.OuterGap, top = s.OuterGap,
                                             right = s.OuterGap, bottom = s.OuterGap },
        defaultContainerPadding      = s.InnerGap / 2,
        defaultWorkspacePadding      = s.InnerGap / 2,
        borderEnabled                = s.BorderEnabled,
        borderWidth                  = s.BorderWidth,
        activeBorderColour           = HexToKomorebiRgba(s.BorderActive),
        inactiveBorderColour         = HexToKomorebiRgba(s.BorderInactive),
        focusFollowsMouse            = "NoFocusChange",
        mouseFollowsFocus            = false,
        windowHidingBehaviour        = "Hide",
        crossMonitorMoveBehaviour    = "Unmanaged",
        workspaceRules               = Array.Empty<object>(),
        managedWorkspaces            = new object[]
        {
            new { name = "1", monitor = 0, initialWorkspaceIndex = 0 },
            new { name = "2", monitor = 0, initialWorkspaceIndex = 1 },
            new { name = "3", monitor = 0, initialWorkspaceIndex = 2 },
        }
    };

    private static string BuildGlazeWmYaml(WmSettings s) =>
$$"""
# Generated by Sakura Desktop
general:
  show_floating_on_top: false
  cursor_follows_focus: false
  focus_follows_cursor: false

gaps:
  outer_gap: {{s.OuterGap}}
  inner_gap: {{s.InnerGap}}

focus_borders:
  active:
    enabled: {{s.BorderEnabled.ToString().ToLower()}}
    color: "{{s.BorderActive}}"
    width: {{s.BorderWidth}}
  inactive:
    enabled: false
    color: "{{s.BorderInactive}}"
    width: {{s.BorderWidth}}

bar:
  enabled: false

workspaces:
  - name: "1"
  - name: "2"
  - name: "3"
  - name: "4"
  - name: "5"

window_rules:
  - commands: ["ignore"]
    match:
      - window_class: { regex: "^Shell_TrayWnd$" }
      - window_class: { regex: "^Progman$" }
""";

    private static int[] HexToKomorebiRgba(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        byte a = Convert.ToByte(hex[0..2], 16);
        byte r = Convert.ToByte(hex[2..4], 16);
        byte g = Convert.ToByte(hex[4..6], 16);
        byte b = Convert.ToByte(hex[6..8], 16);
        return [r, g, b, a];
    }

    // ── Internals ──────────────────────────────────────────────────────────

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
            ?? throw new InvalidOperationException($"Failed to start: {exe} {args}");

        await proc.WaitForExitAsync(ct).ConfigureAwait(false);

        if (proc.ExitCode != 0)
        {
            string err = await proc.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException($"WM command failed (exit {proc.ExitCode}): {err}");
        }
    }
}
