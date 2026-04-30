using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Sakura.Core.Backup;

public static class RegistryBackup
{
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int RegOpenKeyExW(IntPtr hKey, string lpSubKey, uint ulOptions, uint samDesired, out IntPtr phkResult);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int RegSaveKeyExW(IntPtr hKey, string lpFile, IntPtr lpSecurityAttributes, uint flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegCloseKey(IntPtr hKey);

    private static readonly IntPtr HKLM = new(unchecked((int)0x80000002));
    private static readonly IntPtr HKCU = new(unchecked((int)0x80000001));
    private static readonly IntPtr HKCR = new(unchecked((int)0x80000000));
    private static readonly IntPtr HKU  = new(unchecked((int)0x80000003));

    private const uint KEY_READ          = 0x20019;
    private const uint KEY_QUERY_VALUE   = 0x0001;
    private const uint KEY_ENUMERATE_SUB_KEYS = 0x0008;
    private const uint KEY_READ_ALL      = KEY_READ | KEY_QUERY_VALUE | KEY_ENUMERATE_SUB_KEYS;
    private const uint REG_LATEST_FORMAT = 2;

    public static BackupArtifact Save(string hive, string subKey, string outDir)
    {
        Directory.CreateDirectory(outDir);

        string safeName = $"{hive}_{subKey.Replace('\\', '_').Replace('/', '_')}";
        string filePath = Path.Combine(outDir, safeName + ".hiv");
        if (File.Exists(filePath)) File.Delete(filePath);

        IntPtr hRoot = hive.ToUpperInvariant() switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE"   => HKLM,
            "HKCU" or "HKEY_CURRENT_USER"    => HKCU,
            "HKCR" or "HKEY_CLASSES_ROOT"    => HKCR,
            "HKU"  or "HKEY_USERS"           => HKU,
            _ => throw new ArgumentException($"Unknown hive: {hive}")
        };

        int rc = RegOpenKeyExW(hRoot, subKey, 0, KEY_READ_ALL, out IntPtr hKey);
        if (rc != 0) throw new Win32Exception(rc, $"RegOpenKeyEx {hive}\\{subKey} failed (rc={rc})");

        try
        {
            rc = RegSaveKeyExW(hKey, filePath, IntPtr.Zero, REG_LATEST_FORMAT);
            if (rc != 0) throw new Win32Exception(rc, $"RegSaveKeyEx → {filePath} failed (rc={rc})");
        }
        finally
        {
            RegCloseKey(hKey);
        }

        byte[] hash = SHA256.HashData(File.ReadAllBytes(filePath));
        string sha256 = Convert.ToHexString(hash).ToLowerInvariant();
        File.WriteAllText(filePath + ".sha256", sha256);

        return new BackupArtifact(ArtifactKind.Registry, filePath, sha256, DateTime.UtcNow);
    }

    public static void Restore(BackupArtifact artifact)
    {
        if (artifact.Kind != ArtifactKind.Registry)
            throw new InvalidOperationException("Artifact is not a registry backup");

        if (!File.Exists(artifact.Path))
            throw new FileNotFoundException("Registry backup file missing", artifact.Path);

        string actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(artifact.Path))).ToLowerInvariant();
        if (!string.Equals(actual, artifact.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"SHA256 mismatch on {artifact.Path}: expected {artifact.Sha256}, got {actual}");

        string name = Path.GetFileNameWithoutExtension(artifact.Path);
        int firstUnderscore = name.IndexOf('_');
        if (firstUnderscore < 0) throw new InvalidDataException("Cannot parse hive from artifact path " + artifact.Path);

        string hive = name[..firstUnderscore];
        string subKey = name[(firstUnderscore + 1)..].Replace('_', '\\');

        IntPtr hRoot = hive.ToUpperInvariant() switch
        {
            "HKLM" => HKLM,
            "HKCU" => HKCU,
            "HKCR" => HKCR,
            "HKU"  => HKU,
            _ => throw new InvalidDataException("Unknown hive in artifact name: " + hive)
        };

        int rc = RegOpenKeyExW(hRoot, subKey, 0, 0x00020000 /*KEY_WRITE*/ | KEY_READ_ALL, out IntPtr hKey);
        if (rc != 0) throw new Win32Exception(rc, $"RegOpenKeyEx for restore: {hive}\\{subKey}");

        try
        {
            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            static extern int RegRestoreKeyW(IntPtr hKey, string lpFile, uint dwFlags);

            rc = RegRestoreKeyW(hKey, artifact.Path, 0);
            if (rc != 0) throw new Win32Exception(rc, $"RegRestoreKey from {artifact.Path}");
        }
        finally
        {
            RegCloseKey(hKey);
        }
    }
}
