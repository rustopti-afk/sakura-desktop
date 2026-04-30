using System.Management;

namespace Sakura.Core.Backup;

public static class SystemRestoreHelper
{
    public static int CreateRestorePoint(string description)
    {
        using var cls = new ManagementClass(@"\\.\root\default", "SystemRestore", null);
        using ManagementBaseObject inParams = cls.GetMethodParameters("CreateRestorePoint");
        inParams["Description"]     = description;
        inParams["RestorePointType"] = 0;   // APPLICATION_INSTALL
        inParams["EventType"]        = 100; // BEGIN_SYSTEM_CHANGE
        using ManagementBaseObject result = cls.InvokeMethod("CreateRestorePoint", inParams, null);
        return Convert.ToInt32(result["ReturnValue"]);
    }

    public static string CreateVssSnapshot(string volume)
    {
        using var cls = new ManagementClass(@"\\.\root\cimv2", "Win32_ShadowCopy", null);
        using ManagementBaseObject inParams = cls.GetMethodParameters("Create");
        inParams["Volume"]  = volume;
        inParams["Context"] = "ClientAccessible";
        using ManagementBaseObject result = cls.InvokeMethod("Create", inParams, null);
        int rc = Convert.ToInt32(result["ReturnValue"]);
        if (rc != 0) throw new InvalidOperationException($"VSS Create returned {rc}");
        return result["ShadowID"]?.ToString()
               ?? throw new InvalidOperationException("VSS CreateSnapshot returned null ShadowID");
    }

    public static string? GetVssMountPath(string shadowId)
    {
        using var searcher = new ManagementObjectSearcher(
            @"\\.\root\cimv2",
            $"SELECT DeviceObject FROM Win32_ShadowCopy WHERE ID='{shadowId}'");
        foreach (ManagementObject obj in searcher.Get())
        {
            string? device = obj["DeviceObject"]?.ToString();
            if (device != null) return device + @"\";
        }
        return null;
    }

    public static void DeleteVssSnapshot(string shadowId)
    {
        using var searcher = new ManagementObjectSearcher(
            @"\\.\root\cimv2",
            $"SELECT * FROM Win32_ShadowCopy WHERE ID='{shadowId}'");
        foreach (ManagementObject obj in searcher.Get())
            obj.Delete();
    }
}
