using Microsoft.Extensions.Logging.Abstractions;
using Sakura.Core.Profile;
using Sakura.Core.Theme;

namespace Sakura.Core.Tests.Profile;

public sealed class ProfileApplicatorTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public ProfileApplicatorTests() => Directory.CreateDirectory(_tempDir);

    private ProfileApplicator MakeApplicator() => new(
        NullLogger<ProfileApplicator>.Instance,
        new ThemeEngine(NullLogger<ThemeEngine>.Instance),
        backupRootOverride: _tempDir);

    private static RiceProfile MinimalProfile(string name = "Test") => new()
    {
        Profile    = new ProfileMeta { Name = name, MinOsBuild = 0 },
        Compositor = new CompositorSettings
        {
            DarkMode     = true,
            AccentColor  = "#FFE8A0BF",
            CaptionColor = "#FF0D1418",
            TextColor    = "#FFEDE6EE",
            BorderColor  = "#FFE8A0BF"
        }
    };

    [Fact]
    public async Task ApplyAsync_ReturnsFailure_WhenOsBuildTooLow()
    {
        var profile = new RiceProfile
        {
            Profile    = new ProfileMeta { Name = "High Build", MinOsBuild = 99999999 },
            Compositor = MinimalProfile().Compositor
        };

        var result = await MakeApplicator().ApplyAsync(profile);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("build");
    }

    [Fact]
    public async Task ApplyAsync_ReportsAtLeastOneProgressStep_BeforeFailure()
    {
        var reports  = new List<ApplyProgress>();
        var progress = new Progress<ApplyProgress>(p => reports.Add(p));

        await MakeApplicator().ApplyAsync(MinimalProfile(), progress);

        reports.Should().NotBeEmpty("at least step 1 must be reported before any failure");
        reports.Should().AllSatisfy(r =>
        {
            r.TotalSteps.Should().Be(13);
            r.Step.Should().BeInRange(1, 11);
        });
    }

    [Fact]
    public async Task ApplyAsync_AllFractions_AreNormalisedBetweenZeroAndOne()
    {
        var fractions = new List<double>();
        var progress  = new Progress<ApplyProgress>(p => fractions.Add(p.Fraction));

        await MakeApplicator().ApplyAsync(MinimalProfile(), progress);

        fractions.Should().NotBeEmpty();
        fractions.Should().OnlyContain(f => f >= 0.0 && f <= 1.0);
    }

    [Fact]
    public void ApplyProgress_Fraction_CalculatesCorrectly()
    {
        new ApplyProgress(1, 4, "s1").Fraction.Should().BeApproximately(0.25, 1e-9);
        new ApplyProgress(4, 4, "s4").Fraction.Should().BeApproximately(1.0,  1e-9);
        new ApplyProgress(0, 0, "s0").Fraction.Should().Be(0.0);
    }

    [Fact]
    public void ApplyResult_Ok_HasSuccessTrueAndBackupDir()
    {
        var r = ApplyResult.Ok(@"C:\backup\s1");
        r.Success.Should().BeTrue();
        r.BackupDir.Should().Be(@"C:\backup\s1");
        r.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ApplyResult_Fail_HasSuccessFalseAndMessage()
    {
        var r = ApplyResult.Fail("oops");
        r.Success.Should().BeFalse();
        r.BackupDir.Should().BeNull();
        r.ErrorMessage.Should().Be("oops");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
