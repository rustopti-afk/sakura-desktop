using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sakura.Core.Ipc;
using System.ServiceProcess;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(o => o.ServiceName = "SakuraWatchdog");
builder.Services.AddHostedService<WatchdogService>();
await builder.Build().RunAsync();

internal sealed class WatchdogService : BackgroundService
{
    private readonly ILogger<WatchdogService> _logger;
    private const string AdminSvc = "SakuraHelperAdmin";
    private const string TiSvc    = "SakuraHelperTI";
    private const int ProbeIntervalMs    = 5_000;
    private const int RestartCooldownMs  = 3_000;
    private const int MaxConsecutiveFails = 3;

    private sealed class ServiceState { public int Fails; }
    private readonly ServiceState _adminState = new();
    private readonly ServiceState _tiState    = new();

    public WatchdogService(ILogger<WatchdogService> logger) => _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Watchdog started");

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(ProbeIntervalMs, ct).ConfigureAwait(false);
            await ProbeAndRestartAsync(AdminSvc, PipeNames.Admin, _adminState, ct).ConfigureAwait(false);
            await ProbeAndRestartAsync(TiSvc,    PipeNames.TI,    _tiState,    ct).ConfigureAwait(false);
        }
    }

    private async Task ProbeAndRestartAsync(string serviceName, string pipeName, ServiceState state, CancellationToken ct)
    {
        bool alive = await PingPipeAsync(pipeName, ct).ConfigureAwait(false);
        if (alive)
        {
            state.Fails = 0;
            return;
        }

        state.Fails++;
        _logger.LogWarning("Service {Name} probe failed ({Count}/{Max})", serviceName, state.Fails, MaxConsecutiveFails);

        if (state.Fails < MaxConsecutiveFails) return;
        state.Fails = 0;

        try
        {
            using var sc = new ServiceController(serviceName);
            if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.StopPending)
            {
                await Task.Delay(RestartCooldownMs, ct).ConfigureAwait(false);
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                _logger.LogInformation("Restarted {Name}", serviceName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart {Name}", serviceName);
        }
    }

    private static async Task<bool> PingPipeAsync(string pipeName, CancellationToken ct)
    {
        await using var client = new PipeClient(pipeName);
        try
        {
            await client.ConnectAsync(timeoutMs: 2000, ct).ConfigureAwait(false);
            var resp = await client.PingAsync(ct).ConfigureAwait(false);
            return resp.Status == 200;
        }
        catch { return false; }
    }
}
