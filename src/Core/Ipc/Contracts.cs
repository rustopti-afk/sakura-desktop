using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sakura.Core.Ipc;

public sealed record RequestEnvelope(
    [property: JsonPropertyName("id")]      Guid        Id,
    [property: JsonPropertyName("op")]      string      Op,
    [property: JsonPropertyName("payload")] JsonElement Payload,
    [property: JsonPropertyName("sha256")]  string      Sha256);

public sealed record ResponseEnvelope(
    [property: JsonPropertyName("id")]           Guid         Id,
    [property: JsonPropertyName("status")]       int          Status,
    [property: JsonPropertyName("result")]       JsonElement? Result,
    [property: JsonPropertyName("errorCode")]    int          ErrorCode,
    [property: JsonPropertyName("errorMessage")] string?      ErrorMessage);

public static class Ops
{
    public const string Ping            = "ping";
    public const string RegSave         = "reg.save";
    public const string RegRestore      = "reg.restore";
    public const string FileBackup      = "file.backup";
    public const string FileRestore     = "file.restore";
    public const string CreateRestorePoint = "system.createRestorePoint";
    public const string SetRegistryValue = "reg.setValue";
    public const string DeleteRegistryValue = "reg.deleteValue";
    public const string ApplyTheme      = "theme.apply";
    public const string SnapshotRegistry = "reg.snapshot";
    public const string GetHelperVersion = "helper.version";
}

public static class PipeNames
{
    public const string Admin = "sakura.helper.admin";
    public const string TI    = "sakura.helper.ti";
}
