// Phase 0 — R1: TrustedInstaller token acquisition
// Run as: Administrator (elevated cmd)
// Pass: writes + deletes canary file in System32 drivers\etc
// Fail: any Win32Exception = TI token or privilege chain broken on this build

using Sakura.Core.Privilege;
using System.Diagnostics;
using System.Runtime.InteropServices;

Console.WriteLine("R1 — TrustedInstaller token test");
Console.WriteLine($"Build:   {Environment.OSVersion.Version}");
Console.WriteLine($"Process: {Environment.ProcessId}");
Console.WriteLine();

bool isAdmin = false;
try
{
    using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
    var principal = new System.Security.Principal.WindowsPrincipal(id);
    isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
}
catch { }

if (!isAdmin)
{
    Console.Error.WriteLine("FAIL: not running as Administrator. Elevate first.");
    return 1;
}

string canary = @"C:\Windows\System32\drivers\etc\sakura_r1_canary.tmp";

// Stage 1: Acquire TI session
Console.Write("Acquiring TrustedInstaller session... ");
var sw = Stopwatch.StartNew();
using var ti = new TrustedInstallerSession();
try
{
    ti.Acquire();
}
catch (Exception ex)
{
    Console.Error.WriteLine("\nFAIL: " + ex.Message);
    Console.Error.WriteLine("HResult: 0x" + ex.HResult.ToString("X8"));
    return 2;
}
sw.Stop();
Console.WriteLine($"OK ({sw.ElapsedMilliseconds} ms)");

// Stage 2: Write canary file (requires TI-level filesystem access)
Console.Write("Writing canary file... ");
try
{
    File.WriteAllText(canary, $"R1 canary — {DateTime.UtcNow:o} — build {Environment.OSVersion.Version.Build}");
    Console.WriteLine("OK");
}
catch (Exception ex)
{
    Console.Error.WriteLine("\nFAIL write: " + ex.Message);
    return 3;
}

// Stage 3: Delete canary (verify we can modify the file)
Console.Write("Deleting canary file... ");
try
{
    File.Delete(canary);
    Console.WriteLine("OK");
}
catch (Exception ex)
{
    Console.Error.WriteLine("\nFAIL delete: " + ex.Message);
    return 4;
}

// Stage 4: Verify session disposes cleanly (RevertToSelf, handle released)
ti.Dispose();
Console.WriteLine("TI session disposed cleanly.");

Console.WriteLine();
Console.WriteLine("============================");
Console.WriteLine($"R1 PASS — Build {Environment.OSVersion.Version.Build}");
Console.WriteLine("============================");
return 0;
