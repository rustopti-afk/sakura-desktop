using FluentAssertions;
using Sakura.Core.Ipc;
using System.Text.Json;

namespace Sakura.Core.Tests.Ipc;

public sealed class PipeContractsTests
{
    [Fact]
    public void RequestEnvelope_Roundtrips_ViaJson()
    {
        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var id = Guid.NewGuid();
        var payload = JsonSerializer.SerializeToElement(new { key = "value", num = 42 }, opts);
        var req = new RequestEnvelope(id, Ops.Ping, payload, "sha256abc");

        string json = JsonSerializer.Serialize(req, opts);
        var deserialized = JsonSerializer.Deserialize<RequestEnvelope>(json, opts);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(id);
        deserialized.Op.Should().Be(Ops.Ping);
        deserialized.Sha256.Should().Be("sha256abc");
    }

    [Fact]
    public void ResponseEnvelope_OkStatus_HasStatusCode200()
    {
        var resp = new ResponseEnvelope(Guid.NewGuid(), 200, null, 0, null);
        resp.Status.Should().Be(200);
        resp.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ResponseEnvelope_ErrorStatus_HasNonZeroErrorCode()
    {
        var resp = new ResponseEnvelope(Guid.NewGuid(), 500, null, -1, "something broke");
        resp.Status.Should().Be(500);
        resp.ErrorCode.Should().Be(-1);
        resp.ErrorMessage.Should().Be("something broke");
    }

    [Fact]
    public void PipeNames_AreNonEmpty()
    {
        PipeNames.Admin.Should().NotBeNullOrWhiteSpace();
        PipeNames.TI.Should().NotBeNullOrWhiteSpace();
        PipeNames.Admin.Should().NotBe(PipeNames.TI);
    }

    [Fact]
    public void Ops_Constants_AreUnique()
    {
        var allOps = new[]
        {
            Ops.Ping, Ops.RegSave, Ops.RegRestore, Ops.FileBackup, Ops.FileRestore,
            Ops.CreateRestorePoint, Ops.SetRegistryValue, Ops.DeleteRegistryValue,
            Ops.ApplyTheme, Ops.SnapshotRegistry, Ops.GetHelperVersion
        };
        allOps.Distinct().Should().HaveCount(allOps.Length);
    }
}
