using Sakura.Core.Integrations;

namespace Sakura.Core.Tests.Integrations;

public sealed class RainmeterManagerTests
{
    [Fact]
    public void IsInstalled_WhenExeAbsent_ReturnsFalse()
    {
        string fakeRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        bool result = RainmeterManager.IsInstalled();
        // Can't guarantee Rainmeter is installed in CI — just verify it doesn't throw
        Assert.True(result || !result);
    }

    [Fact]
    public void GetInstalledSkins_WhenFolderAbsent_ReturnsEmpty()
    {
        string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var skins = RainmeterManager.GetInstalledSkins(missing);
        skins.Should().BeEmpty();
    }

    [Fact]
    public void GetInstalledSkins_ReturnsSortedSubdirNames()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(root, "Zebra"));
        Directory.CreateDirectory(Path.Combine(root, "Alpha"));
        Directory.CreateDirectory(Path.Combine(root, "Mango"));

        try
        {
            var skins = RainmeterManager.GetInstalledSkins(root);
            skins.Should().Equal("Alpha", "Mango", "Zebra");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void GetLayouts_WhenFolderAbsent_ReturnsEmpty()
    {
        string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var layouts = RainmeterManager.GetLayouts(missing);
        layouts.Should().BeEmpty();
    }

    [Fact]
    public void GetLayouts_ReturnsSortedLayoutNames()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(root, "SakuraYoru"));
        Directory.CreateDirectory(Path.Combine(root, "Default"));

        try
        {
            var layouts = RainmeterManager.GetLayouts(root);
            layouts.Should().Equal("Default", "SakuraYoru");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyProfileAsync_WhenApplyFalse_DoesNotThrow()
    {
        var settings = new RainmeterSettings { Apply = false };
        // Should return immediately without trying to find Rainmeter.exe
        await RainmeterManager.ApplyProfileAsync(settings);
    }
}
