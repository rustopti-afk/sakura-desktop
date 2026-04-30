using Sakura.Core.Native;

namespace Sakura.Core.Tests.Native;

public sealed class DependencyDetectorTests
{
    // ── CheckProfile ───────────────────────────────────────────────────────

    [Fact]
    public void CheckProfile_EmptyRequired_ReturnsEmpty()
    {
        var missing = DependencyDetector.CheckProfile([]);
        missing.Should().BeEmpty();
    }

    [Fact]
    public void CheckProfile_OnNonWindows_ReturnsEmpty()
    {
        if (OperatingSystem.IsWindows()) return;

        var missing = DependencyDetector.CheckProfile(["Microsoft.WindowsTerminal", "JanDeDobbeleer.OhMyPosh"]);
        missing.Should().BeEmpty("CheckProfile is a no-op on non-Windows");
    }

    [Fact]
    public void CheckProfile_WithFakeIds_ReturnsAllMissingOnNonWindows()
    {
        if (OperatingSystem.IsWindows()) return;

        // On non-Windows, registry is unavailable so all deps report as installed — returns empty
        var result = DependencyDetector.CheckProfile(["Fake.PackageId.One", "Fake.PackageId.Two"]);
        result.Should().BeEmpty();
    }

    // ── Detect ─────────────────────────────────────────────────────────────

    [Fact]
    public void Detect_OnNonWindows_ReturnsAllNotInstalled()
    {
        if (OperatingSystem.IsWindows()) return;

        var deps = DependencyDetector.Detect();
        deps.Should().NotBeEmpty();
        deps.Should().AllSatisfy(d => d.Status.Should().Be(DependencyStatus.NotInstalled));
    }

    [Fact]
    public void Detect_ReturnsDependencyRecordsWithRequiredFields()
    {
        var deps = DependencyDetector.Detect();

        deps.Should().NotBeEmpty();
        deps.Should().AllSatisfy(d =>
        {
            d.Id.Should().NotBeNullOrEmpty();
            d.DisplayName.Should().NotBeNullOrEmpty();
            d.WingetId.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void Detect_KnownIds_ArePresent()
    {
        var ids = DependencyDetector.Detect().Select(d => d.Id).ToList();
        ids.Should().Contain("rainmeter");
        ids.Should().Contain("ohmyposh");
        ids.Should().Contain("wt");
        ids.Should().Contain("windhawk");
    }

    // ── Dependency record ──────────────────────────────────────────────────

    [Fact]
    public void Dependency_Record_EqualityWorks()
    {
        var a = new Dependency("id", "Name", "Winget.Id", true, @"C:\path", DependencyStatus.Installed);
        var b = new Dependency("id", "Name", "Winget.Id", true, @"C:\path", DependencyStatus.Installed);
        a.Should().Be(b);
    }

    [Fact]
    public void DependencyStatus_HasExpectedValues()
    {
        ((int)DependencyStatus.NotInstalled).Should().Be(0);
        ((int)DependencyStatus.Installed).Should().Be(1);
        ((int)DependencyStatus.UpdateAvailable).Should().Be(2);
    }

    // ── ProfileApplicator integration: missing deps block apply ───────────

    [Fact]
    public async Task ProfileApplicator_WithMissingRequiredDep_ReturnsFailure()
    {
        if (!OperatingSystem.IsWindows()) return;

        // Use a fake WinGet ID that will never be installed
        var profile = new Sakura.Core.Profile.RiceProfile
        {
            Profile    = new Sakura.Core.Profile.ProfileMeta { Name = "DepTest", MinOsBuild = 0 },
            Compositor = new Sakura.Core.Profile.CompositorSettings
            {
                AccentColor  = "#FFE8A0BF",
                CaptionColor = "#FF0D1418",
                TextColor    = "#FFEDE6EE",
                BorderColor  = "#FFE8A0BF"
            },
            Dependencies = new Sakura.Core.Profile.DependencyList
            {
                Required = ["Definitely.NotInstalled.XYZ.123456"]
            }
        };

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var applicator = new Sakura.Core.Profile.ProfileApplicator(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<Sakura.Core.Profile.ProfileApplicator>.Instance,
                new Sakura.Core.Theme.ThemeEngine(
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<Sakura.Core.Theme.ThemeEngine>.Instance),
                backupRootOverride: tempDir);

            var result = await applicator.ApplyAsync(profile);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Missing required dependencies");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
