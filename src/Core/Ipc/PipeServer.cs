using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Sakura.Core.Ipc;

public sealed class PipeServer
{
    private readonly string _pipeName;
    private readonly Func<RequestEnvelope, CancellationToken, Task<ResponseEnvelope>> _dispatch;
    private readonly ILogger<PipeServer> _logger;
    private readonly CancellationTokenSource _cts = new();

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PipeServer(
        string pipeName,
        Func<RequestEnvelope, CancellationToken, Task<ResponseEnvelope>> dispatch,
        ILogger<PipeServer> logger)
    {
        _pipeName = pipeName;
        _dispatch = dispatch;
        _logger   = logger;
    }

    public async Task RunAsync(CancellationToken externalCt = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, externalCt);
        CancellationToken ct = linked.Token;

        _logger.LogInformation("Pipe server starting on \\\\.\\pipe\\{Name}", _pipeName);

        while (!ct.IsCancellationRequested)
        {
            var pipe = CreateSecurePipe();
            try
            {
                await pipe.WaitForConnectionAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { pipe.Dispose(); break; }

            _ = Task.Run(() => HandleClientAsync(pipe, ct), ct);
        }

        _logger.LogInformation("Pipe server stopped");
    }

    public void Stop() => _cts.Cancel();

    private NamedPipeServerStream CreateSecurePipe()
    {
        var ownerSid = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("Cannot determine current user SID");

        var ps = new PipeSecurity();
        ps.AddAccessRule(new PipeAccessRule(ownerSid, PipeAccessRights.ReadWrite, AccessControlType.Allow));
        ps.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            _pipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Message,
            PipeOptions.Asynchronous,
            inBufferSize:  64 * 1024,
            outBufferSize: 64 * 1024,
            ps);
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken ct)
    {
        using (pipe)
        {
            try
            {
                while (!ct.IsCancellationRequested && pipe.IsConnected)
                {
                    RequestEnvelope? req = await ReadFrameAsync<RequestEnvelope>(pipe, ct).ConfigureAwait(false);
                    if (req is null) return;

                    _logger.LogDebug("Received op={Op} id={Id}", req.Op, req.Id);

                    ResponseEnvelope resp;
                    try
                    {
                        resp = await _dispatch(req, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Dispatch failed for op={Op}", req.Op);
                        resp = new ResponseEnvelope(req.Id, 500, null, ex.HResult, ex.Message);
                    }

                    await WriteFrameAsync(pipe, resp, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (IOException ex) { _logger.LogDebug("Client disconnected: {Msg}", ex.Message); }
            catch (Exception ex)   { _logger.LogError(ex, "Unhandled error in pipe client handler"); }
        }
    }

    private static async Task<T?> ReadFrameAsync<T>(PipeStream pipe, CancellationToken ct)
    {
        byte[] lenBuf = new byte[4];
        int read = 0;
        while (read < 4)
        {
            int r = await pipe.ReadAsync(lenBuf.AsMemory(read, 4 - read), ct).ConfigureAwait(false);
            if (r == 0) return default;
            read += r;
        }
        int len = BitConverter.ToInt32(lenBuf, 0);
        if (len <= 0 || len > 16 * 1024 * 1024)
            throw new InvalidDataException($"Frame length out of range: {len}");

        byte[] body = new byte[len];
        int got = 0;
        while (got < len)
            got += await pipe.ReadAsync(body.AsMemory(got, len - got), ct).ConfigureAwait(false);

        return JsonSerializer.Deserialize<T>(body, _jsonOpts);
    }

    private static async Task WriteFrameAsync<T>(PipeStream pipe, T value, CancellationToken ct)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOpts);
        await pipe.WriteAsync(BitConverter.GetBytes(body.Length), ct).ConfigureAwait(false);
        await pipe.WriteAsync(body, ct).ConfigureAwait(false);
        await pipe.FlushAsync(ct).ConfigureAwait(false);
    }
}
