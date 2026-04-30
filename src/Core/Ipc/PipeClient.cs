using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text.Json;

namespace Sakura.Core.Ipc;

public sealed class PipeClient : IAsyncDisposable
{
    private NamedPipeClientStream? _pipe;
    private readonly string _pipeName;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PipeClient(string pipeName) => _pipeName = pipeName;

    public async Task ConnectAsync(int timeoutMs = 5000, CancellationToken ct = default)
    {
        _pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await _pipe.ConnectAsync(timeoutMs, ct).ConfigureAwait(false);
        _pipe.ReadMode = PipeTransmissionMode.Message;
    }

    public async Task<ResponseEnvelope> CallAsync<TPayload>(string op, TPayload payload, CancellationToken ct = default)
    {
        if (_pipe is null || !_pipe.IsConnected)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, _jsonOpts);
        string sha256 = Convert.ToHexString(SHA256.HashData(payloadBytes)).ToLowerInvariant();

        var req = new RequestEnvelope(
            Guid.NewGuid(),
            op,
            JsonSerializer.Deserialize<JsonElement>(payloadBytes, _jsonOpts),
            sha256);

        byte[] reqBytes = JsonSerializer.SerializeToUtf8Bytes(req, _jsonOpts);
        await _pipe.WriteAsync(BitConverter.GetBytes(reqBytes.Length), ct).ConfigureAwait(false);
        await _pipe.WriteAsync(reqBytes, ct).ConfigureAwait(false);
        await _pipe.FlushAsync(ct).ConfigureAwait(false);

        byte[] lenBuf = new byte[4];
        int read = 0;
        while (read < 4)
            read += await _pipe.ReadAsync(lenBuf.AsMemory(read, 4 - read), ct).ConfigureAwait(false);

        int len = BitConverter.ToInt32(lenBuf, 0);
        if (len <= 0 || len > 16 * 1024 * 1024)
            throw new InvalidDataException($"Response frame length out of range: {len}");

        byte[] body = new byte[len];
        int got = 0;
        while (got < len)
            got += await _pipe.ReadAsync(body.AsMemory(got, len - got), ct).ConfigureAwait(false);

        return JsonSerializer.Deserialize<ResponseEnvelope>(body, _jsonOpts)
               ?? throw new InvalidDataException("Null response envelope");
    }

    public async Task<ResponseEnvelope> PingAsync(CancellationToken ct = default)
        => await CallAsync(Ops.Ping, new { }, ct).ConfigureAwait(false);

    public async ValueTask DisposeAsync()
    {
        if (_pipe is not null)
        {
            await _pipe.DisposeAsync().ConfigureAwait(false);
            _pipe = null;
        }
    }
}
