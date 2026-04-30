using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Win32;

namespace Sakura.Core.Theme;

public enum SecureUxThemeStatus { NotInstalled, Installed, LoaderActive }

public static class SecureUxThemeHelper
{
    private const string InstallDir = @"C:\ProgramData\Sakura\third_party\SecureUxTheme";
    private const string ExeName    = "SecureUxTheme.exe";
    private const string GhOwner    = "namazso";
    private const string GhRepo     = "SecureUxTheme";

    public static string ExePath => Path.Combine(InstallDir, ExeName);

    public static SecureUxThemeStatus Detect()
    {
        if (!File.Exists(ExePath)) return SecureUxThemeStatus.NotInstalled;

        // Check if loader is registered in winlogon IFEO
        using var key = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\winlogon.exe");
        if (key is null) return SecureUxThemeStatus.Installed;

        // AppCertDlls under winlogon means loader is patched in
        using var certKey = key.OpenSubKey("AppCertDlls");
        return certKey != null ? SecureUxThemeStatus.LoaderActive : SecureUxThemeStatus.Installed;
    }

    public static async Task EnsureInstalledAsync(CancellationToken ct = default)
    {
        if (!File.Exists(ExePath))
            await DownloadAsync(ct).ConfigureAwait(false);

        SecureUxThemeStatus status = Detect();
        if (status == SecureUxThemeStatus.NotInstalled)
            throw new InvalidOperationException("SecureUxTheme download failed");

        if (status == SecureUxThemeStatus.Installed)
            Install();
    }

    public static void Install()
    {
        if (!File.Exists(ExePath))
            throw new FileNotFoundException("SecureUxTheme.exe not found — run EnsureInstalledAsync first", ExePath);

        int rc = RunElevated(ExePath, "install --quiet", waitMs: 30_000);
        if (rc != 0 && rc != 1) // 1 = already installed
            throw new InvalidOperationException($"SecureUxTheme install exited with code {rc}");
    }

    public static void Uninstall()
    {
        if (!File.Exists(ExePath)) return;
        RunElevated(ExePath, "remove --quiet", waitMs: 15_000);
    }

    /// <summary>
    /// Deploys a .msstyles file to the Windows Themes folder and patches it for unsigned loading.
    /// Returns the full path of the deployed .theme file.
    /// </summary>
    public static string DeployTheme(string msstylesSourcePath, string themeName)
    {
        if (!File.Exists(msstylesSourcePath))
            throw new FileNotFoundException("msstyles not found", msstylesSourcePath);

        string themeDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "Resources", "Themes", themeName);
        Directory.CreateDirectory(themeDir);

        string destMsstyles = Path.Combine(themeDir, themeName + ".msstyles");
        File.Copy(msstylesSourcePath, destMsstyles, overwrite: true);

        string themeFilePath = Path.Combine(themeDir, themeName + ".theme");
        WriteThemeFile(themeFilePath, themeName, destMsstyles);

        // Patch the theme file via SecureUxTheme so unsigned msstyles loads
        if (File.Exists(ExePath))
        {
            int rc = RunElevated(ExePath, $"patch \"{themeFilePath}\" --quiet", waitMs: 10_000);
            if (rc != 0)
                throw new InvalidOperationException($"SecureUxTheme patch exited with code {rc}");
        }

        return themeFilePath;
    }

    /// <summary>
    /// Applies a patched .theme file via IThemeManager2 COM (requires SecureUxTheme loader active).
    /// Falls back to Desktop.SwitchDesktopTheme if COM fails.
    /// </summary>
    public static void ApplyTheme(string themeFilePath)
    {
        if (!File.Exists(themeFilePath))
            throw new FileNotFoundException("Theme file not found", themeFilePath);

        // Try IThemeManager2 COM (available when SecureUxTheme loader is active in winlogon)
        try
        {
            ApplyViaThemeManagerCom(themeFilePath);
            return;
        }
        catch (COMException) { }
        catch (InvalidCastException) { }

        // Fallback: rundll32 the theme setting
        var psi = new ProcessStartInfo("rundll32.exe",
            $"shell32.dll,Control_RunDLL desk.cpl,,\"{themeFilePath}\"")
        { UseShellExecute = false };
        using var p = Process.Start(psi)!;
        p.WaitForExit(10_000);
    }

    private static void ApplyViaThemeManagerCom(string themeFile)
    {
        // IThemeManager2 CLSID/IID known from SecureUxTheme source
        var tmType = Type.GetTypeFromProgID("Theme.Manager2")
            ?? throw new COMException("Theme.Manager2 ProgID not found — SecureUxTheme loader not active");
        dynamic? tm = Activator.CreateInstance(tmType)
            ?? throw new InvalidCastException("Could not create IThemeManager2");
        tm.ApplyTheme(themeFile);
        Marshal.ReleaseComObject(tm);
    }

    private static void WriteThemeFile(string path, string name, string msstylesPath)
    {
        string content = $"""
[Theme]
DisplayName={name}

[VisualStyles]
Path={msstylesPath}
ColorStyle=NormalColor
Size=NormalSize
ColorizationColor=0X6B74B8FC
Transparency=1

[boot]
SCRNSAVE.EXE=

[MasterThemeSelector]
MTSM=RJSPBS
""";
        File.WriteAllText(path, content);
    }

    private static async Task DownloadAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(InstallDir);
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "SakuraDesktop/0.1");
        string apiUrl = $"https://api.github.com/repos/{GhOwner}/{GhRepo}/releases/latest";

        string json = await http.GetStringAsync(apiUrl, ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);

        foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
        {
            if (asset.GetProperty("name").GetString() == ExeName)
            {
                string url = asset.GetProperty("browser_download_url").GetString()!;
                byte[] data = await http.GetByteArrayAsync(url, ct).ConfigureAwait(false);
                File.WriteAllBytes(ExePath, data);
                return;
            }
        }
        throw new InvalidOperationException($"{ExeName} not found in latest {GhOwner}/{GhRepo} release");
    }

    private static int RunElevated(string exe, string args, int waitMs)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            UseShellExecute = true,
            Verb = "runas"
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null");
        bool ok = p.WaitForExit(waitMs);
        if (!ok) { p.Kill(); throw new TimeoutException($"{exe} timed out after {waitMs} ms"); }
        return p.ExitCode;
    }
}
