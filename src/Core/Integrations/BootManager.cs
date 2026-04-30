using Sakura.Core.Profile;
using System.Diagnostics;
using System.Text;

namespace Sakura.Core.Integrations;

/// <summary>
/// Manages boot splash customization via HackBGRT.
///
/// HackBGRT replaces the ACPI BGRT (Boot Graphics Resource Table) logo shown
/// during Windows boot. It lives on the EFI partition and reads a config.txt
/// that points to a .bmp splash image.
///
/// Detection order: EFI partition mount (B:\EFI\HackBGRT), ProgramFiles, Scoop.
/// Applying: copies splash image → writes config.txt → runs setup.exe install.
/// Requires elevation (EFI partition writes need admin).
/// </summary>
public static class BootManager
{
    private const string HackBgrtDir      = @"EFI\HackBGRT";
    private const string HackBgrtExe      = "setup.exe";
    private const string HackBgrtConfig   = "config.txt";
    private const string SplashFileName   = "splash.bmp";

    // ── Detection ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the HackBGRT installation directory, or null if not found.
    /// Searches EFI partition (B:\ or mounted via mountvol), then ProgramFiles, Scoop.
    /// </summary>
    public static string? FindHackBgrtDir()
    {
        if (!OperatingSystem.IsWindows()) return null;

        // EFI partition is typically mounted at B:\ or can be found via bcdedit
        foreach (string drive in CandidateEfiDrives())
        {
            string dir = Path.Combine(drive, HackBgrtDir);
            if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, HackBgrtExe)))
                return dir;
        }

        // ProgramFiles installation
        string pf = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "HackBGRT");
        if (Directory.Exists(pf) && File.Exists(Path.Combine(pf, HackBgrtExe)))
            return pf;

        // Scoop
        string scoop = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "scoop", "apps", "hackbgrt", "current");
        if (Directory.Exists(scoop) && File.Exists(Path.Combine(scoop, HackBgrtExe)))
            return scoop;

        return null;
    }

    public static bool IsHackBgrtInstalled() => FindHackBgrtDir() is not null;

    // ── Config + image deployment ─────────────────────────────────────────

    /// <summary>
    /// Copies the splash image to the HackBGRT directory and writes config.txt.
    /// Returns the path of the deployed image, or null if hackBgrtDir is null.
    /// </summary>
    public static string? DeploySplash(string splashSourcePath, string? hackBgrtDirOverride = null)
    {
        string? dir = hackBgrtDirOverride ?? FindHackBgrtDir();
        if (dir is null) return null;

        string destImage = Path.Combine(dir, SplashFileName);

        // Convert PNG/JPG → BMP if needed (copy as-is for .bmp, otherwise flag)
        if (string.Equals(Path.GetExtension(splashSourcePath), ".bmp", StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(splashSourcePath, destImage, overwrite: true);
        }
        else
        {
            // Non-BMP: copy anyway — HackBGRT 0.5+ accepts PNG as well
            File.Copy(splashSourcePath, Path.Combine(dir, Path.GetFileName(splashSourcePath)), overwrite: true);
            // Point config at the copied file name
            WriteConfig(dir, Path.GetFileName(splashSourcePath));
            return Path.Combine(dir, Path.GetFileName(splashSourcePath));
        }

        WriteConfig(dir, SplashFileName);
        return destImage;
    }

    /// <summary>
    /// Installs HackBGRT by running setup.exe install from the HackBGRT directory.
    /// Requires elevation — the caller is responsible for UAC/TI promotion.
    /// </summary>
    public static async Task InstallAsync(string? hackBgrtDirOverride = null, CancellationToken ct = default)
    {
        string? dir = hackBgrtDirOverride ?? FindHackBgrtDir();
        if (dir is null)
            throw new InvalidOperationException("HackBGRT is not installed.");

        string exe = Path.Combine(dir, HackBgrtExe);
        await RunAsync(exe, "install", dir, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Uninstalls (restores original BGRT) by running setup.exe uninstall.
    /// </summary>
    public static async Task UninstallAsync(string? hackBgrtDirOverride = null, CancellationToken ct = default)
    {
        string? dir = hackBgrtDirOverride ?? FindHackBgrtDir();
        if (dir is null)
            throw new InvalidOperationException("HackBGRT is not installed.");

        string exe = Path.Combine(dir, HackBgrtExe);
        await RunAsync(exe, "uninstall", dir, ct).ConfigureAwait(false);
    }

    // ── Profile wiring ─────────────────────────────────────────────────────

    /// <summary>
    /// Applies BootSettings from a profile.
    /// If HackBgrtEnabled and SplashPath are set, deploys the splash and installs.
    /// If HackBgrtEnabled is false, this is a no-op (does not uninstall existing config).
    /// </summary>
    public static async Task ApplyProfileAsync(
        BootSettings settings,
        bool runInstall = false,
        string? hackBgrtDirOverride = null,
        CancellationToken ct = default)
    {
        if (!settings.HackBgrtEnabled) return;
        if (settings.SplashPath is null) return;
        if (!File.Exists(settings.SplashPath)) return;

        DeploySplash(settings.SplashPath, hackBgrtDirOverride);

        if (runInstall)
            await InstallAsync(hackBgrtDirOverride, ct).ConfigureAwait(false);
    }

    // ── Internals ──────────────────────────────────────────────────────────

    private static void WriteConfig(string dir, string imageFileName)
    {
        // HackBGRT config.txt format: key=value lines
        var sb = new StringBuilder();
        sb.AppendLine("# Generated by Sakura Desktop");
        sb.AppendLine($"image={imageFileName}");
        sb.AppendLine("quality=0");   // 0 = best quality
        sb.AppendLine("x=0");         // centre — HackBGRT centres when x/y=0
        sb.AppendLine("y=0");
        File.WriteAllText(Path.Combine(dir, HackBgrtConfig), sb.ToString(), Encoding.UTF8);
    }

    private static IEnumerable<string> CandidateEfiDrives()
    {
        // Common EFI partition mount letters used by HackBGRT installer
        yield return @"B:\";
        yield return @"S:\";
        // Any other drive that has an EFI folder
        foreach (var di in DriveInfo.GetDrives())
        {
            if (di.DriveType == DriveType.Fixed && di.IsReady)
                yield return di.RootDirectory.FullName;
        }
    }

    private static async Task RunAsync(string exe, string args, string workingDir, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            WorkingDirectory       = workingDir,
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start: {exe} {args}");

        await proc.WaitForExitAsync(ct).ConfigureAwait(false);

        if (proc.ExitCode != 0)
        {
            string err = await proc.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException($"HackBGRT setup failed (exit {proc.ExitCode}): {err}");
        }
    }
}
