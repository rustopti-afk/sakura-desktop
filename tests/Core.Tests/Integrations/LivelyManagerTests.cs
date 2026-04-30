using Sakura.Core.Integrations;

namespace Sakura.Core.Tests.Integrations;

public sealed class LivelyManagerTests
{
    [Fact]
    public void IsInstalled_WhenLocalAppDataEmpty_ReturnsFalse()
    {
        string emptyRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(emptyRoot);

        try
        {
            bool result = LivelyManager.IsInstalled(emptyRoot);
            result.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(emptyRoot, recursive: true);
        }
    }

    [Fact]
    public void FindExe_WhenPackagedExeExists_ReturnsPath()
    {
        if (!OperatingSystem.IsWindows()) return; // path separators in AppDataSubPath are Windows-only

        string root    = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string exeDir  = Path.Combine(root, "Packages", "12030rocksdanister.LivelyWallpaper_97hta09mmv6hy", "LocalState", "app-0.0.0.0");
        Directory.CreateDirectory(exeDir);
        string exePath = Path.Combine(exeDir, "lively.exe");
        File.WriteAllText(exePath, "fake");

        try
        {
            string? found = LivelyManager.FindExe(root);
            found.Should().Be(exePath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyProfileAsync_WhenApplyFalse_DoesNotThrow()
    {
        var settings = new LivelySettings { Apply = false };
        await LivelyManager.ApplyProfileAsync(settings);
    }

    [Fact]
    public async Task ApplyProfileAsync_WhenWallpaperPathNull_DoesNotThrow()
    {
        var settings = new LivelySettings { Apply = true, WallpaperPath = null };
        await LivelyManager.ApplyProfileAsync(settings);
    }

    [Fact]
    public async Task ApplyProfileAsync_WhenNotInstalled_ThrowsInvalidOperation()
    {
        string emptyRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(emptyRoot);

        try
        {
            var settings = new LivelySettings { Apply = true, WallpaperPath = "test.mp4" };
            Func<Task> act = () => LivelyManager.ApplyProfileAsync(settings, default, emptyRoot);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*not installed*");
        }
        finally
        {
            Directory.Delete(emptyRoot, recursive: true);
        }
    }

    [Fact]
    public void GetActiveWallpapers_WhenLayoutFileAbsent_ReturnsEmpty()
    {
        string emptyRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(emptyRoot);

        try
        {
            var wallpapers = LivelyManager.GetActiveWallpapers(emptyRoot);
            wallpapers.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(emptyRoot, recursive: true);
        }
    }

    [Fact]
    public void LivelyActiveWallpaper_Record_EqualityWorks()
    {
        var a = new LivelyActiveWallpaper("path/to/info.json", "Display1");
        var b = new LivelyActiveWallpaper("path/to/info.json", "Display1");
        a.Should().Be(b);
    }
}
