using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Sakura.Core.Native;

public enum WallpaperFit { Fill = 4, Fit = 3, Stretch = 2, Tile = 1, Center = 0, Span = 5 }

public static class WallpaperManager
{
    // IDesktopWallpaper CLSID and IID
    private static readonly Guid CLSID_DesktopWallpaper = new("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD");
    private static readonly Guid IID_DesktopWallpaper   = new("B92B56A9-8B55-4E14-9A89-0199BBB6F93B");

    [ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        [return: MarshalAs(UnmanagedType.LPWStr)] string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorID);
        [return: MarshalAs(UnmanagedType.LPWStr)] string GetMonitorDevicePathAt(uint monitorIndex);
        [return: MarshalAs(UnmanagedType.U4)] uint GetMonitorDevicePathCount();
        void GetMonitorRECT(string monitorID, out RECT displayRect);
        void SetBackgroundColor(uint color);
        [return: MarshalAs(UnmanagedType.U4)] uint GetBackgroundColor();
        void SetPosition([MarshalAs(UnmanagedType.I4)] int position);
        [return: MarshalAs(UnmanagedType.I4)] int GetPosition();
        void SetSlideshow(IShellItemArray items);
        void GetSlideshow(out IShellItemArray items);
        void SetSlideshowOptions(uint options, uint slideshowTick);
        void GetSlideshowOptions(out uint options, out uint slideshowTick);
        void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string? monitorID, [MarshalAs(UnmanagedType.I4)] int direction);
        [return: MarshalAs(UnmanagedType.I4)] int GetStatus();
        [return: MarshalAs(UnmanagedType.Bool)] bool Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
    }

    [ComImport, Guid("B63EA76D-1F85-456F-A19C-48159EFA858B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemArray { }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    public static void SetWallpaperAllMonitors(string imagePath, WallpaperFit fit = WallpaperFit.Fill)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Wallpaper file not found", imagePath);

        var wallpaper = (IDesktopWallpaper)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_DesktopWallpaper)!)!;
        try
        {
            wallpaper.SetPosition((int)fit);
            wallpaper.SetWallpaper(null, imagePath);
        }
        finally { Marshal.ReleaseComObject(wallpaper); }
    }

    public static void SetWallpaperPerMonitor(IReadOnlyDictionary<string, (string Path, WallpaperFit Fit)> perMonitor)
    {
        var wallpaper = (IDesktopWallpaper)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_DesktopWallpaper)!)!;
        try
        {
            foreach (var (monitorId, (path, fit)) in perMonitor)
            {
                if (!File.Exists(path)) throw new FileNotFoundException("Wallpaper file not found", path);
                wallpaper.SetPosition((int)fit);
                wallpaper.SetWallpaper(monitorId, path);
            }
        }
        finally { Marshal.ReleaseComObject(wallpaper); }
    }

    public static string[] GetMonitorDevicePaths()
    {
        var wallpaper = (IDesktopWallpaper)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_DesktopWallpaper)!)!;
        try
        {
            uint count = wallpaper.GetMonitorDevicePathCount();
            string[] paths = new string[count];
            for (uint i = 0; i < count; i++)
                paths[i] = wallpaper.GetMonitorDevicePathAt(i);
            return paths;
        }
        finally { Marshal.ReleaseComObject(wallpaper); }
    }
}
