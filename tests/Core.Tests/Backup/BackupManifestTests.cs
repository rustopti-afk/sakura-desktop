using FluentAssertions;
using Sakura.Core.Backup;
using System.Text.Json;

namespace Sakura.Core.Tests.Backup;

public sealed class BackupManifestTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public BackupManifestTests() => Directory.CreateDirectory(_tempDir);

    [Fact]
    public void Save_CreatesManifestJson_WithAllArtifacts()
    {
        var artifacts = new[]
        {
            new BackupArtifact(ArtifactKind.Registry, @"C:\backup\key.hiv", "abc123", DateTime.UtcNow),
            new BackupArtifact(ArtifactKind.File, @"C:\backup\file.dll", "def456", DateTime.UtcNow)
        };
        var manifest = new BackupManifest("test-id", DateTime.UtcNow, "test", 22621, artifacts);

        manifest.Save(_tempDir);

        string manifestPath = Path.Combine(_tempDir, "manifest.json");
        File.Exists(manifestPath).Should().BeTrue();
        string json = File.ReadAllText(manifestPath);
        json.Should().Contain("test-id");
        json.Should().Contain("abc123");
        json.Should().Contain("def456");
    }

    [Fact]
    public void Save_CreatesRestoreCmd_WithExpectedContent()
    {
        var artifacts = new[]
        {
            new BackupArtifact(ArtifactKind.Registry, "key.hiv", "abc", DateTime.UtcNow)
        };
        var manifest = new BackupManifest("id2", DateTime.UtcNow, "test", 22621, artifacts);
        manifest.Save(_tempDir);

        string restoreCmd = Path.Combine(_tempDir, "restore.cmd");
        File.Exists(restoreCmd).Should().BeTrue();
        string content = File.ReadAllText(restoreCmd);
        content.Should().Contain("@echo off");
        content.Should().Contain("reg restore");
    }

    [Fact]
    public void RoundTrip_SerializeDeserialize_PreservesAllFields()
    {
        var original = new BackupManifest(
            "roundtrip-id",
            new DateTime(2026, 4, 29, 12, 0, 0, DateTimeKind.Utc),
            "round trip test",
            22621,
            [new BackupArtifact(ArtifactKind.File, "f.dll", "sha", DateTime.UtcNow)]);

        original.Save(_tempDir);
        var loaded = BackupManifest.Load(_tempDir);

        loaded.Id.Should().Be(original.Id);
        loaded.Description.Should().Be(original.Description);
        loaded.OsBuild.Should().Be(22621u);
        loaded.Artifacts.Should().HaveCount(1);
        loaded.Artifacts[0].Sha256.Should().Be("sha");
    }

    [Fact]
    public void Load_MissingManifest_ThrowsFileNotFoundException()
    {
        string emptyDir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(emptyDir);
        var act = () => BackupManifest.Load(emptyDir);
        act.Should().Throw<FileNotFoundException>();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
