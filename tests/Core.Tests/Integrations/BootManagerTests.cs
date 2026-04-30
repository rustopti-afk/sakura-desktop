using Sakura.Core.Integrations;
using Sakura.Core.Profile;

namespace Sakura.Core.Tests.Integrations;

public sealed class BootManagerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public BootManagerTests() => Directory.CreateDirectory(_tempDir);

    // ── Detection ──────────────────────────────────────────────────────────

    [Fact]
    public void FindHackBgrtDir_OnNonWindows_ReturnsNull()
    {
        if (OperatingSystem.IsWindows()) return;
        BootManager.FindHackBgrtDir().Should().BeNull();
    }

    [Fact]
    public void IsHackBgrtInstalled_OnNonWindows_ReturnsFalse()
    {
        if (OperatingSystem.IsWindows()) return;
        BootManager.IsHackBgrtInstalled().Should().BeFalse();
    }

    // ── DeploySplash ───────────────────────────────────────────────────────

    [Fact]
    public void DeploySplash_WhenHackBgrtDirOverrideNull_ReturnsNull()
    {
        if (!OperatingSystem.IsWindows())
        {
            // On Linux FindHackBgrtDir returns null → DeploySplash returns null
            string dummy = Path.Combine(_tempDir, "splash.bmp");
            File.WriteAllBytes(dummy, [0x42, 0x4D]); // BM header stub
            BootManager.DeploySplash(dummy, null).Should().BeNull();
        }
    }

    [Fact]
    public void DeploySplash_BmpFile_CopiesAndWritesConfig()
    {
        // Create fake HackBGRT dir with setup.exe stub
        string hackDir = Path.Combine(_tempDir, "hackbgrt");
        Directory.CreateDirectory(hackDir);
        File.WriteAllBytes(Path.Combine(hackDir, "setup.exe"), [0x4D, 0x5A]);

        // Create a BMP source
        string splash = Path.Combine(_tempDir, "mysplash.bmp");
        File.WriteAllBytes(splash, CreateMinimalBmpBytes());

        string? dest = BootManager.DeploySplash(splash, hackDir);

        dest.Should().NotBeNull();
        dest.Should().EndWith("splash.bmp");
        File.Exists(dest!).Should().BeTrue();

        string config = Path.Combine(hackDir, "config.txt");
        File.Exists(config).Should().BeTrue();
        File.ReadAllText(config).Should().Contain("image=splash.bmp");
    }

    [Fact]
    public void DeploySplash_NonBmpFile_CopiesWithOriginalNameAndWritesConfig()
    {
        string hackDir = Path.Combine(_tempDir, "hackbgrt-png");
        Directory.CreateDirectory(hackDir);
        File.WriteAllBytes(Path.Combine(hackDir, "setup.exe"), [0x4D, 0x5A]);

        string splash = Path.Combine(_tempDir, "sakura.png");
        File.WriteAllBytes(splash, [0x89, 0x50, 0x4E, 0x47]); // PNG magic

        string? dest = BootManager.DeploySplash(splash, hackDir);

        dest.Should().NotBeNull();
        dest.Should().EndWith("sakura.png");
        File.Exists(Path.Combine(hackDir, "sakura.png")).Should().BeTrue();
        File.ReadAllText(Path.Combine(hackDir, "config.txt")).Should().Contain("image=sakura.png");
    }

    [Fact]
    public void DeploySplash_ConfigContainsExpectedKeys()
    {
        string hackDir = Path.Combine(_tempDir, "hackbgrt-cfg");
        Directory.CreateDirectory(hackDir);
        File.WriteAllBytes(Path.Combine(hackDir, "setup.exe"), [0x4D, 0x5A]);

        string splash = Path.Combine(_tempDir, "s.bmp");
        File.WriteAllBytes(splash, CreateMinimalBmpBytes());

        BootManager.DeploySplash(splash, hackDir);

        string config = File.ReadAllText(Path.Combine(hackDir, "config.txt"));
        config.Should().Contain("quality=");
        config.Should().Contain("x=");
        config.Should().Contain("y=");
    }

    // ── ApplyProfileAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ApplyProfileAsync_WhenDisabled_DoesNothing()
    {
        var settings = new BootSettings { HackBgrtEnabled = false, SplashPath = null };
        // Should return immediately without touching filesystem
        await BootManager.ApplyProfileAsync(settings, runInstall: false, hackBgrtDirOverride: _tempDir);
    }

    [Fact]
    public async Task ApplyProfileAsync_WhenEnabledButNoSplashPath_DoesNothing()
    {
        var settings = new BootSettings { HackBgrtEnabled = true, SplashPath = null };
        await BootManager.ApplyProfileAsync(settings, runInstall: false, hackBgrtDirOverride: _tempDir);
    }

    [Fact]
    public async Task ApplyProfileAsync_WhenSplashPathMissing_DoesNothing()
    {
        var settings = new BootSettings
        {
            HackBgrtEnabled = true,
            SplashPath = Path.Combine(_tempDir, "nonexistent.bmp")
        };
        await BootManager.ApplyProfileAsync(settings, runInstall: false, hackBgrtDirOverride: _tempDir);
    }

    [Fact]
    public async Task ApplyProfileAsync_WithValidSplash_DeploysConfig()
    {
        string hackDir = Path.Combine(_tempDir, "hackbgrt-apply");
        Directory.CreateDirectory(hackDir);
        File.WriteAllBytes(Path.Combine(hackDir, "setup.exe"), [0x4D, 0x5A]);

        string splash = Path.Combine(_tempDir, "boot.bmp");
        File.WriteAllBytes(splash, CreateMinimalBmpBytes());

        var settings = new BootSettings { HackBgrtEnabled = true, SplashPath = splash };

        await BootManager.ApplyProfileAsync(settings, runInstall: false, hackBgrtDirOverride: hackDir);

        File.Exists(Path.Combine(hackDir, "splash.bmp")).Should().BeTrue();
        File.Exists(Path.Combine(hackDir, "config.txt")).Should().BeTrue();
    }

    // ── BootSettings defaults ──────────────────────────────────────────────

    [Fact]
    public void BootSettings_Defaults_AreConservative()
    {
        var s = new BootSettings();
        s.HackBgrtEnabled.Should().BeFalse();
        s.SplashPath.Should().BeNull();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    // Minimal 1×1 BMP (54-byte header + 4 bytes pixel data)
    private static byte[] CreateMinimalBmpBytes() =>
    [
        0x42, 0x4D,             // BM
        0x3A, 0x00, 0x00, 0x00, // file size: 58
        0x00, 0x00, 0x00, 0x00, // reserved
        0x36, 0x00, 0x00, 0x00, // pixel data offset: 54
        0x28, 0x00, 0x00, 0x00, // BITMAPINFOHEADER size: 40
        0x01, 0x00, 0x00, 0x00, // width: 1
        0x01, 0x00, 0x00, 0x00, // height: 1
        0x01, 0x00,             // planes: 1
        0x18, 0x00,             // bits per pixel: 24
        0x00, 0x00, 0x00, 0x00, // compression: none
        0x04, 0x00, 0x00, 0x00, // image size: 4
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0xFF, 0x00, 0x80, 0x00, // pixel: BGR + padding
    ];

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
