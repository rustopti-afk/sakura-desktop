using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Sakura.Core.Backup;

public sealed class ApplySession : IDisposable
{
    private readonly string _backupDir;
    private readonly string _description;
    private readonly ILogger<ApplySession> _logger;
    private readonly List<BackupArtifact> _artifacts = new();
    private bool _committed;
    private bool _disposed;

    public string BackupDir => _backupDir;

    public ApplySession(string backupRoot, string description, ILogger<ApplySession> logger)
    {
        _description = description;
        _logger = logger;
        string id = Guid.NewGuid().ToString("N");
        _backupDir = Path.Combine(backupRoot, $"{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ss}_{id[..8]}");
        Directory.CreateDirectory(_backupDir);
    }

    public BackupArtifact SnapshotRegistry(string hive, string subKey)
    {
        _logger.LogInformation("Snapshot registry {Hive}\\{SubKey}", hive, subKey);
        var artifact = RegistryBackup.Save(hive, subKey, _backupDir);
        _artifacts.Add(artifact);
        return artifact;
    }

    public BackupArtifact SnapshotFile(string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Cannot snapshot missing file", sourcePath);

        string safeName = sourcePath
            .Replace(@"C:\Windows\System32", "__SYSTEM32__")
            .Replace(@"C:\Windows", "__WINDIR__")
            .Replace(Path.DirectorySeparatorChar, '_')
            .Replace(':', '_')
            .TrimStart('_');
        string destPath = Path.Combine(_backupDir, safeName);
        File.Copy(sourcePath, destPath, overwrite: true);

        byte[] hash = SHA256.HashData(File.ReadAllBytes(destPath));
        string sha256 = Convert.ToHexString(hash).ToLowerInvariant();

        var artifact = new BackupArtifact(ArtifactKind.File, destPath, sha256, DateTime.UtcNow);
        _artifacts.Add(artifact);
        _logger.LogInformation("Snapshot file {Path} sha256={Hash}", sourcePath, sha256);
        return artifact;
    }

    public void Commit()
    {
        var manifest = new BackupManifest(
            Guid.NewGuid().ToString("N"),
            DateTime.UtcNow,
            _description,
            (uint)Environment.OSVersion.Version.Build,
            _artifacts.ToArray());
        manifest.Save(_backupDir);
        _committed = true;
        _logger.LogInformation("Apply session committed to {Dir}", _backupDir);
    }

    public void RollbackAll()
    {
        _logger.LogWarning("Rolling back apply session {Dir}", _backupDir);
        foreach (var artifact in Enumerable.Reverse(_artifacts))
        {
            try
            {
                switch (artifact.Kind)
                {
                    case ArtifactKind.Registry:
                        RegistryBackup.Restore(artifact);
                        _logger.LogInformation("Restored registry {Path}", artifact.Path);
                        break;
                    case ArtifactKind.File:
                        string dest = DecompressFilePath(Path.GetFileNameWithoutExtension(artifact.Path));
                        File.Copy(artifact.Path, dest, overwrite: true);
                        _logger.LogInformation("Restored file {Path}", dest);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rollback failed for artifact {Path}", artifact.Path);
            }
        }
    }

    private static string DecompressFilePath(string name)
        => name.Replace("__SYSTEM32__", @"C:\Windows\System32")
               .Replace("__WINDIR__", @"C:\Windows")
               .Replace('_', Path.DirectorySeparatorChar);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (!_committed)
        {
            _logger.LogWarning("ApplySession disposed without commit — rolling back");
            RollbackAll();
        }
    }
}
