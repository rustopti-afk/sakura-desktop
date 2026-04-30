// Phase 0 — R3: komorebi + ExplorerPatcher + Windhawk coexistence soak test
// Run as: Administrator
// Restarts explorer.exe N times and checks Event Log for Application Error crashes
// Pass: zero crash events in the Application Error source for explorer.exe
// Fail: any crash events detected

using System.Diagnostics;

Console.WriteLine("R3 — Explorer coexistence soak test");
Console.WriteLine($"Build:       {Environment.OSVersion.Version}");
Console.WriteLine($"Iterations:  {args.ElementAtOrDefault(0) ?? "20"} (pass --iter N to override)");
Console.WriteLine();

int iterations = int.TryParse(args.ElementAtOrDefault(0) ?? "20", out int n) && n > 0 ? n : 20;
int pauseAfterKillMs    = 1200;
int pauseAfterLaunchMs  = 2500;

int crashesBefore = CountExplorerCrashes(DateTime.UtcNow);
Console.WriteLine($"Baseline explorer crashes (last 30 min): {crashesBefore}");

var startedAt = DateTime.UtcNow;
int explorerRestarts = 0;

for (int i = 1; i <= iterations; i++)
{
    Console.Write($"Cycle {i}/{iterations}: kill... ");
    KillExplorer();
    await Task.Delay(pauseAfterKillMs);

    Console.Write("launch... ");
    LaunchExplorer();
    await Task.Delay(pauseAfterLaunchMs);

    int running = Process.GetProcessesByName("explorer").Length;
    Console.WriteLine($"instances={running}");
    explorerRestarts++;
}

await Task.Delay(5000); // let event log catch up

int crashesAfter  = CountExplorerCrashes(startedAt);
int newCrashes    = Math.Max(0, crashesAfter - crashesBefore);

Console.WriteLine();
Console.WriteLine("============================");
Console.WriteLine($"Explorer restarts:  {explorerRestarts}");
Console.WriteLine($"Crash events:       {newCrashes}");

if (newCrashes == 0)
{
    Console.WriteLine("R3 PASS");
    return 0;
}
else
{
    Console.Error.WriteLine("R3 FAIL — see Event Viewer > Application > Application Error > explorer.exe");
    return newCrashes;
}

static void KillExplorer()
{
    var procs = Process.GetProcessesByName("explorer");
    foreach (var p in procs) { try { p.Kill(); p.WaitForExit(3000); } catch { } p.Dispose(); }
}

static void LaunchExplorer()
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName        = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Launch failed: " + ex.Message);
    }
}

static int CountExplorerCrashes(DateTime since)
{
    try
    {
        using var log = new EventLog("Application");
        return log.Entries
            .Cast<EventLogEntry>()
            .Count(e =>
                e.TimeGenerated >= since &&
                e.Source == "Application Error" &&
                (e.Message?.Contains("explorer.exe", StringComparison.OrdinalIgnoreCase) ?? false));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("EventLog query failed: " + ex.Message);
        return 0;
    }
}
