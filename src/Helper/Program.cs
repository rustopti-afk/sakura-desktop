using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sakura.Core.Backup;
using Sakura.Core.Ipc;
using Sakura.Core.Native;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(o => o.ServiceName = "SakuraHelperAdmin");
builder.Services.AddHostedService<HelperService>();
await builder.Build().RunAsync();

internal sealed class HelperService : BackgroundService
{
    private readonly ILogger<HelperService> _logger;
    private readonly ILogger<PipeServer>    _pipeLogger;
    private PipeServer? _pipe;

    private static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public HelperService(ILogger<HelperService> logger, ILoggerFactory loggerFactory)
    {
        _logger     = logger;
        _pipeLogger = loggerFactory.CreateLogger<PipeServer>();
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("SakuraHelperAdmin started (PID {Pid})", Environment.ProcessId);

        _pipe = new PipeServer(PipeNames.Admin, DispatchAsync, _pipeLogger);
        await _pipe.RunAsync(ct).ConfigureAwait(false);
    }

    private async Task<ResponseEnvelope> DispatchAsync(RequestEnvelope req, CancellationToken ct)
    {
        _logger.LogInformation("Op={Op} Id={Id}", req.Op, req.Id);

        return req.Op switch
        {
            Ops.Ping => Ok(req.Id, new { version = "0.1.0", pid = Environment.ProcessId }),

            Ops.RegSave => await HandleRegSaveAsync(req, ct),

            Ops.SetRegistryValue => await HandleSetRegistryValueAsync(req, ct),

            Ops.CreateRestorePoint => await HandleCreateRestorePointAsync(req, ct),

            Ops.GetHelperVersion => Ok(req.Id, new { version = "0.1.0", kind = "admin" }),

            _ => Error(req.Id, 404, $"Unknown op: {req.Op}")
        };
    }

    private Task<ResponseEnvelope> HandleRegSaveAsync(RequestEnvelope req, CancellationToken ct)
    {
        try
        {
            string hive    = req.Payload.GetProperty("hive").GetString()!;
            string subKey  = req.Payload.GetProperty("subKey").GetString()!;
            string outDir  = req.Payload.GetProperty("outDir").GetString()!;
            var artifact   = RegistryBackup.Save(hive, subKey, outDir);
            return Task.FromResult(Ok(req.Id, artifact));
        }
        catch (Exception ex) { return Task.FromResult(Error(req.Id, 500, ex.Message)); }
    }

    private Task<ResponseEnvelope> HandleSetRegistryValueAsync(RequestEnvelope req, CancellationToken ct)
    {
        try
        {
            string hive   = req.Payload.GetProperty("hive").GetString()!;
            string subKey = req.Payload.GetProperty("subKey").GetString()!;
            string name   = req.Payload.GetProperty("name").GetString()!;
            string kind   = req.Payload.GetProperty("kind").GetString()!;

            if (kind == "DWORD")
            {
                uint val = req.Payload.GetProperty("value").GetUInt32();
                RegistryWriter.SetDword(ParseHive(hive), subKey, name, val);
            }
            else if (kind == "STRING")
            {
                string val = req.Payload.GetProperty("value").GetString()!;
                RegistryWriter.SetString(ParseHive(hive), subKey, name, val);
            }
            else return Task.FromResult(Error(req.Id, 400, $"Unknown registry kind: {kind}"));

            return Task.FromResult(Ok(req.Id, new { }));
        }
        catch (Exception ex) { return Task.FromResult(Error(req.Id, 500, ex.Message)); }
    }

    private Task<ResponseEnvelope> HandleCreateRestorePointAsync(RequestEnvelope req, CancellationToken ct)
    {
        try
        {
            string desc = req.Payload.GetProperty("description").GetString()!;
            int rc = SystemRestoreHelper.CreateRestorePoint(desc);
            return Task.FromResult(rc == 0
                ? Ok(req.Id, new { })
                : Error(req.Id, 500, $"SystemRestore returned {rc}"));
        }
        catch (Exception ex) { return Task.FromResult(Error(req.Id, 500, ex.Message)); }
    }

    private static Microsoft.Win32.RegistryHive ParseHive(string s) => s.ToUpperInvariant() switch
    {
        "HKLM" or "HKEY_LOCAL_MACHINE" => Microsoft.Win32.RegistryHive.LocalMachine,
        "HKCU" or "HKEY_CURRENT_USER"  => Microsoft.Win32.RegistryHive.CurrentUser,
        _ => throw new ArgumentException("Unknown hive: " + s)
    };

    private static ResponseEnvelope Ok(Guid id, object data)
        => new(id, 200, JsonSerializer.SerializeToElement(data, _json), 0, null);

    private static ResponseEnvelope Error(Guid id, int status, string msg)
        => new(id, status, null, status, msg);
}
