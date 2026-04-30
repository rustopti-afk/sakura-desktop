using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sakura.Core.Backup;

public enum ArtifactKind { Registry, File, VssShadow, RestorePoint }

public sealed record BackupArtifact(
    ArtifactKind Kind,
    string Path,
    string Sha256,
    DateTime CreatedUtc);

public sealed record BackupManifest(
    string Id,
    DateTime CreatedUtc,
    string Description,
    uint OsBuild,
    BackupArtifact[] Artifacts)
{
    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    public void Save(string dir)
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "manifest.json"), JsonSerializer.Serialize(this, _opts));
        EmitRestoreScript(dir);
    }

    public static BackupManifest Load(string dir)
    {
        string json = File.ReadAllText(Path.Combine(dir, "manifest.json"));
        return JsonSerializer.Deserialize<BackupManifest>(json, _opts)
               ?? throw new InvalidDataException("Invalid manifest at " + dir);
    }

    private void EmitRestoreScript(string dir)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine(":: Sakura Desktop emergency restore — run from WinPE or Safe Mode as Administrator");
        sb.AppendLine($":: Manifest: {Id}  Created: {CreatedUtc:u}");
        sb.AppendLine($":: Description: {Description}");
        sb.AppendLine("setlocal");
        sb.AppendLine($"set DIR=%~dp0");

        foreach (var a in Artifacts)
        {
            switch (a.Kind)
            {
                case ArtifactKind.Registry:
                    string hiveName = System.IO.Path.GetFileNameWithoutExtension(a.Path);
                    (string hiveRoot, string subKey) = ParseHiveName(hiveName);
                    sb.AppendLine($"reg restore \"{hiveRoot}\\{subKey}\" \"%DIR%{System.IO.Path.GetFileName(a.Path)}\"");
                    break;
                case ArtifactKind.File:
                    string destPath = DecompressFilePath(System.IO.Path.GetFileNameWithoutExtension(a.Path));
                    sb.AppendLine($"copy /Y \"%DIR%{System.IO.Path.GetFileName(a.Path)}\" \"{destPath}\"");
                    break;
            }
        }

        sb.AppendLine("echo Restore complete. Reboot now.");
        sb.AppendLine("pause");
        File.WriteAllText(Path.Combine(dir, "restore.cmd"), sb.ToString());
    }

    private static (string root, string sub) ParseHiveName(string name)
    {
        string s = name.Replace('_', '\\');
        int sep = s.IndexOf('\\');
        return sep < 0 ? (s, string.Empty) : (s[..sep], s[(sep + 1)..]);
    }

    private static string DecompressFilePath(string name)
        => name.Replace("__SYSTEM32__", @"C:\Windows\System32")
               .Replace("__WINDIR__", @"C:\Windows")
               .Replace('_', Path.DirectorySeparatorChar);
}
