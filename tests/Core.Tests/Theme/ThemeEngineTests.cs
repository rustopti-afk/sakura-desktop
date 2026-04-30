using Microsoft.Extensions.Logging.Abstractions;
using Sakura.Core.Backup;
using Sakura.Core.Theme;

namespace Sakura.Core.Tests.Theme;

public sealed class ThemeEngineTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly ThemeEngine _engine;

    public ThemeEngineTests()
    {
        Directory.CreateDirectory(_tempDir);
        _engine = new ThemeEngine(NullLogger<ThemeEngine>.Instance);
    }

    private ApplySession MakeSession() =>
        new(_tempDir, "test session", NullLogger<ApplySession>.Instance);

    [Fact]
    public void ThemeEngine_Constructs_WithoutThrowing()
    {
        var act = () => new ThemeEngine(NullLogger<ThemeEngine>.Instance);
        act.Should().NotThrow();
    }

    [Fact]
    public void ThemeSettings_DarkMode_DefaultsToTrue()
    {
        var s = new ThemeSettings();
        s.DarkMode.Should().BeTrue();
    }

    [Fact]
    public void ThemeSettings_Transparency_DefaultsToTrue()
    {
        var s = new ThemeSettings();
        s.Transparency.Should().BeTrue();
    }

    [Fact]
    public void ThemeSettings_MsstylesPath_DefaultsToNull()
    {
        var s = new ThemeSettings();
        s.MsstylesPath.Should().BeNull();
    }

    [Fact]
    public void ThemeSettings_AllColorFields_CanBeSet()
    {
        var s = new ThemeSettings
        {
            DarkMode         = false,
            Transparency     = true,
            ColorizationArgb = 0xFFE8A0BF,
            ColorPrevalence  = true,
            BackdropType     = 3,
            CornerPref       = 1,
            CaptionColorBgr  = 0x00BFA0E8,
            TextColorBgr     = 0x00EEE6ED,
            BorderColorBgr   = 0x00BFA0E8
        };

        s.DarkMode.Should().BeFalse();
        s.ColorizationArgb.Should().Be(0xFFE8A0BF);
        s.BackdropType.Should().Be(3);
    }

    [Fact]
    [Trait("Category", "Windows")]
    public void Apply_WithValidSettings_ThrowsDllNotFound_OnLinux()
    {
        if (OperatingSystem.IsWindows()) return; // skip on Windows — real apply runs there

        using var session = MakeSession();
        var settings = new ThemeSettings
        {
            DarkMode         = true,
            ColorizationArgb = 0xFFE8A0BF,
            BackdropType     = 3,
            CornerPref       = 1,
            CaptionColorBgr  = 0x00BFA0E8,
            TextColorBgr     = 0x00EEE6ED,
            BorderColorBgr   = 0x00BFA0E8
        };

        var act = () => _engine.Apply(settings, session);
        act.Should().Throw<Exception>("P/Invoke to user32/dwmapi will fail on Linux");
    }

    [Fact]
    public void Revert_WithEmptyArtifacts_ThrowsDllNotFound_OnLinux()
    {
        if (OperatingSystem.IsWindows()) return; // passes on Windows

        // Revert always calls BroadcastSettingChange (user32) even with no artifacts
        var manifest = new BackupManifest("empty-id", DateTime.UtcNow, "empty", 22621, []);
        var act = () => _engine.Revert(manifest);
        act.Should().Throw<Exception>("BroadcastSettingChange calls user32 which is unavailable on Linux");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
