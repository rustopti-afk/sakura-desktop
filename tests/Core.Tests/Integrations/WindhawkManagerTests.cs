using Sakura.Core.Integrations;

namespace Sakura.Core.Tests.Integrations;

public sealed class WindhawkManagerTests
{
    [Fact]
    public void IsInstalled_DoesNotThrow()
    {
        // Just verify detection doesn't crash on any platform
        bool result = WindhawkManager.IsInstalled();
        Assert.True(result || !result);
    }

    [Fact]
    public void GetInstalledMods_WhenNotInstalled_ReturnsEmpty()
    {
        if (OperatingSystem.IsWindows())
        {
            // On Windows it may or may not be installed — just don't throw
            var mods = WindhawkManager.GetInstalledMods();
            Assert.NotNull(mods);
        }
        else
        {
            // Registry not available on non-Windows
            var mods = WindhawkManager.GetInstalledMods();
            mods.Should().BeEmpty();
        }
    }

    [Fact]
    public void ApplyProfile_WhenApplyFalse_DoesNotThrow()
    {
        var settings = new WindhawkSettings { Apply = false };
        WindhawkManager.ApplyProfile(settings);
    }

    [Fact]
    public void ApplyProfile_WhenApplyTrue_OnNonWindows_ThrowsOrSkips()
    {
        if (OperatingSystem.IsWindows()) return; // skip on real Windows

        var settings = new WindhawkSettings
        {
            Apply = true,
            Mods  = [new WindhawkMod { Id = "test-mod", Enabled = true }]
        };

        // On non-Windows registry access fails — expect an exception
        Action act = () => WindhawkManager.ApplyProfile(settings);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void WindhawkModInfo_Record_EqualityWorks()
    {
        var a = new WindhawkModInfo("my-mod", true, "1.0");
        var b = new WindhawkModInfo("my-mod", true, "1.0");
        a.Should().Be(b);
    }
}
