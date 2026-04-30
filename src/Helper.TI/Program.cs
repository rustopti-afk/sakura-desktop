using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sakura.Core.Backup;
using Sakura.Core.Ipc;
using Sakura.Core.Privilege;
using System.Security.Cryptography;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(o => o.ServiceName = "SakuraHelperTI");
builder.Services.AddHostedService<HelperTiService>();
await builder.Build().RunAsync();

internal sealed class HelperTiService : BackgroundService
{
    private readonly ILogger<HelperTiService> _logger;
    private readonly ILogger<PipeServer>      _pipeLogger;
    private static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public HelperTiService(ILogger<HelperTiService> logger, ILoggerFactory loggerFactory)
    {
        _logger     = logger;
        _pipeLogger = loggerFactory.CreateLogger<PipeServer>();
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("SakuraHelperTI started (PID {Pid})", Environment.ProcessId);
        var pipe = new PipeServer(PipeNames.TI, DispatchAsync, _pipeLogger);
        await pipe.RunAsync(ct).ConfigureAwait(false);
    }

    private async Task<ResponseEnvelope> DispatchAsync(RequestEnvelope req, CancellationToken ct)
    {
        _logger.LogInformation("TI op={Op} id={Id}", req.Op, req.Id);

        return req.Op switch
        {
            Ops.Ping         => Ok(req.Id, new { version = "0.1.0", kind = "ti" }),
            Ops.FileRestore  => await HandleFileRestoreAsync(req, ct),
            Ops.FileBackup   => await HandleFileBackupAsync(req, ct),
            _                => Error(req.Id, 404, "Unknown TI op: " + req.Op)
        };
    }

    private Task<ResponseEnvelope> HandleFileBackupAsync(RequestEnvelope req, CancellationToken ct)
    {
        try
        {
            string src    = req.Payload.GetProperty("sourcePath").GetString()!;
            string outDir = req.Payload.GetProperty("outDir").GetString()!;

            using var ti = new TrustedInstallerSession();
            ti.Acquire();

            if (!File.Exists(src)) return Task.FromResult(Error(req.Id, 404, $"Source not found: {src}"));

            Directory.CreateDirectory(outDir);
            string dst = Path.Combine(outDir, Path.GetFileName(src));
            File.Copy(src, dst, overwrite: true);

            string sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dst))).ToLowerInvariant();
            return Task.FromResult(Ok(req.Id, new { destPath = dst, sha256 }));
        }
        catch (Exception ex) { return Task.FromResult(Error(req.Id, 500, ex.Message)); }
    }

    private Task<ResponseEnvelope> HandleFileRestoreAsync(RequestEnvelope req, CancellationToken ct)
    {
        try
        {
            string src     = req.Payload.GetProperty("sourcePath").GetString()!;
            string dest    = req.Payload.GetProperty("destPath").GetString()!;
            string sha256  = req.Payload.GetProperty("sha256").GetString()!;

            if (!File.Exists(src)) return Task.FromResult(Error(req.Id, 404, $"Backup not found: {src}"));

            string actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(src))).ToLowerInvariant();
            if (!string.Equals(actual, sha256, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(Error(req.Id, 400, $"SHA256 mismatch. Expected {sha256}, got {actual}"));

            using var ti = new TrustedInstallerSession();
            ti.Acquire();

            string? dir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.Copy(src, dest, overwrite: true);

            return Task.FromResult(Ok(req.Id, new { }));
        }
        catch (Exception ex) { return Task.FromResult(Error(req.Id, 500, ex.Message)); }
    }

    private static ResponseEnvelope Ok(Guid id, object data)
        => new(id, 200, JsonSerializer.SerializeToElement(data, _json), 0, null);

    private static ResponseEnvelope Error(Guid id, int status, string msg)
        => new(id, status, null, status, msg);
}
