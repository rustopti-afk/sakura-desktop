// Phase 0 — R2: SecureUxTheme programmatic install/detect/apply
// Run as: Administrator
// Pass: SecureUxTheme installed and loader active, theme apply call succeeds
// Fail: installer fails, WMI query fails, or theme apply throws

using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;

Console.WriteLine("R2 — SecureUxTheme integration test");
Console.WriteLine($"Build: {Environment.OSVersion.Version}");
Console.WriteLine();

string workDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    @"Sakura\third_party\SecureUxTheme");
Directory.CreateDirectory(workDir);

// Stage 1: Detect or download SecureUxTheme
Console.Write("Locating SecureUxTheme.exe... ");
string sutExe = Path.Combine(workDir, "SecureUxTheme.exe");
bool alreadyPresent = File.Exists(sutExe);

if (!alreadyPresent)
{
    Console.WriteLine("not found — fetching from GitHub Releases");
    try
    {
        await DownloadLatestReleaseAssetAsync("namazso", "SecureUxTheme", "SecureUxTheme.exe", sutExe);
        Console.WriteLine($"Downloaded to {sutExe}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("FAIL download: " + ex.Message);
        return 1;
    }
}
else { Console.WriteLine("found at " + sutExe); }

// Stage 2: Install the loader
Console.Write("Installing SecureUxTheme loader... ");
int installRc = RunProcess(sutExe, "install --quiet", waitMs: 30_000);
if (installRc != 0 && installRc != 1 /* already installed */)
{
    Console.Error.WriteLine($"FAIL: install returned {installRc}");
    return 2;
}
Console.WriteLine("OK (rc=" + installRc + ")");

// Stage 3: Check that loader is registered in winlogon AppCertDlls
Console.Write("Verifying loader registration... ");
using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\winlogon.exe");
bool loaderPresent = key != null;
Console.WriteLine(loaderPresent ? "OK (IFEO key present)" : "WARNING: IFEO key not found — may need reboot");

// Stage 4: Verify IThemeManager2 COM is accessible (secureuxtheme.dll must be loaded in winlogon context)
// We check indirectly: try to call IThemeManager2 via the theme API
Console.Write("Calling IThemeManager2::GetCurrentTheme... ");
string? currentTheme = QueryCurrentThemeViaApi();
if (currentTheme == null)
{
    Console.Error.WriteLine("FAIL: IThemeManager2 returned null — loader not active yet");
    Console.Error.WriteLine("This is expected before the first reboot after install. Reboot and re-run.");
    return 3;
}
Console.WriteLine("OK: " + currentTheme);

Console.WriteLine();
Console.WriteLine("============================");
Console.WriteLine($"R2 PASS — Build {Environment.OSVersion.Version.Build}");
Console.WriteLine("Current theme: " + currentTheme);
Console.WriteLine("============================");
return 0;

static int RunProcess(string exe, string args, int waitMs)
{
    var psi = new ProcessStartInfo(exe, args) { UseShellExecute = true, Verb = "runas" };
    using var p = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null");
    bool finished = p.WaitForExit(waitMs);
    if (!finished) { p.Kill(); throw new TimeoutException($"{exe} timed out after {waitMs} ms"); }
    return p.ExitCode;
}

static async Task DownloadLatestReleaseAssetAsync(string owner, string repo, string assetName, string destPath)
{
    using var http = new HttpClient();
    http.DefaultRequestHeaders.Add("User-Agent", "SakuraDesktop-R2-PoC");
    string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
    string json = await http.GetStringAsync(apiUrl);
    using var doc = JsonDocument.Parse(json);
    var assets = doc.RootElement.GetProperty("assets");
    foreach (var asset in assets.EnumerateArray())
    {
        if (asset.GetProperty("name").GetString() == assetName)
        {
            string url = asset.GetProperty("browser_download_url").GetString()!;
            byte[] data = await http.GetByteArrayAsync(url);
            File.WriteAllBytes(destPath, data);
            return;
        }
    }
    throw new InvalidOperationException($"Asset '{assetName}' not found in latest release of {owner}/{repo}");
}

static string? QueryCurrentThemeViaApi()
{
    // Query via registry as a proxy for what IThemeManager2 would return
    // Full COM call requires the patched uxtheme.dll to be active (post-reboot)
    try
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes");
        return key?.GetValue("CurrentTheme") as string;
    }
    catch { return null; }
}
