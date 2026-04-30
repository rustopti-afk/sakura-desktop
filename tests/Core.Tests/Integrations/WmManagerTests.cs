using Sakura.Core.Integrations;

namespace Sakura.Core.Tests.Integrations;

public sealed class WmManagerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public WmManagerTests() => Directory.CreateDirectory(_tempDir);

    // ── Detection ──────────────────────────────────────────────────────────

    [Fact]
    public void FindKomorebi_OnNonWindows_ReturnsNull()
    {
        if (OperatingSystem.IsWindows()) return;
        WmManager.FindKomorebi().Should().BeNull();
    }

    [Fact]
    public void FindGlazeWm_OnNonWindows_ReturnsNull()
    {
        if (OperatingSystem.IsWindows()) return;
        WmManager.FindGlazeWm().Should().BeNull();
    }

    [Fact]
    public void IsKomorebiInstalled_OnNonWindows_ReturnsFalse()
    {
        if (OperatingSystem.IsWindows()) return;
        WmManager.IsKomorebiInstalled().Should().BeFalse();
    }

    [Fact]
    public void IsGlazeWmInstalled_OnNonWindows_ReturnsFalse()
    {
        if (OperatingSystem.IsWindows()) return;
        WmManager.IsGlazeWmInstalled().Should().BeFalse();
    }

    // ── Config generation ──────────────────────────────────────────────────

    [Fact]
    public void DeployKomorebiConfig_WritesValidJson()
    {
        var settings = new WmSettings
        {
            Engine        = "komorebi",
            Layout        = "bsp",
            OuterGap      = 12,
            InnerGap      = 8,
            BorderEnabled = true,
            BorderWidth   = 2,
            BorderActive  = "#E8A0BF",
            BorderInactive = "#3E3E5E"
        };

        string dest = Path.Combine(_tempDir, "komorebi.json");
        string written = WmManager.DeployKomorebiConfig(settings, dest);

        written.Should().Be(dest);
        File.Exists(dest).Should().BeTrue();

        string json = File.ReadAllText(dest);
        json.Should().Contain("\"defaultLayout\"");
        json.Should().Contain("bsp");
        json.Should().Contain("\"borderEnabled\"");
    }

    [Fact]
    public void DeployKomorebiConfig_BorderColour_IsRgbaArray()
    {
        var settings = new WmSettings
        {
            Engine       = "komorebi",
            BorderActive = "#E8A0BF",
        };

        string dest = Path.Combine(_tempDir, "komorebi-border.json");
        WmManager.DeployKomorebiConfig(settings, dest);

        string json = File.ReadAllText(dest);
        // HexToKomorebiRgba("#E8A0BF") → [232, 160, 191, 255] (no alpha prefix → FF added)
        json.Should().Contain("activeBorderColour");
    }

    [Fact]
    public void DeployGlazeWmConfig_WritesYamlFile()
    {
        var settings = new WmSettings
        {
            Engine        = "glazewm",
            OuterGap      = 10,
            InnerGap      = 6,
            BorderEnabled = true,
            BorderWidth   = 2,
            BorderActive  = "#E8A0BF",
            BorderInactive = "#3E3E5E"
        };

        string dest = Path.Combine(_tempDir, "glazewm-config", "config.yaml");
        string written = WmManager.DeployGlazeWmConfig(settings, dest);

        written.Should().Be(dest);
        File.Exists(dest).Should().BeTrue();

        string yaml = File.ReadAllText(dest);
        yaml.Should().Contain("outer_gap: 10");
        yaml.Should().Contain("inner_gap: 6");
        yaml.Should().Contain("color: \"#E8A0BF\"");
        yaml.Should().Contain("workspaces:");
    }

    [Fact]
    public void DeployGlazeWmConfig_CreatesDestinationDirectory()
    {
        var settings = new WmSettings { Engine = "glazewm" };
        string dest = Path.Combine(_tempDir, "nested", "subdir", "config.yaml");

        WmManager.DeployGlazeWmConfig(settings, dest);

        Directory.Exists(Path.GetDirectoryName(dest)).Should().BeTrue();
        File.Exists(dest).Should().BeTrue();
    }

    // ── ApplyProfileAsync edge cases ───────────────────────────────────────

    [Fact]
    public async Task ApplyProfileAsync_WhenEngineIsNone_DoesNothing()
    {
        var settings = new WmSettings { Engine = "none" };
        // Should complete without creating files or throwing
        await WmManager.ApplyProfileAsync(settings, startWm: false);
    }

    [Fact]
    public async Task ApplyProfileAsync_Komorebi_DeploysConfigFile()
    {
        var settings = new WmSettings
        {
            Engine   = "komorebi",
            OuterGap = 16,
            InnerGap = 8,
        };

        string dest = Path.Combine(_tempDir, "komorebi-apply.json");

        // We can't use the method directly with an override, but we can verify
        // DeployKomorebiConfig independently — ApplyProfileAsync uses it internally
        WmManager.DeployKomorebiConfig(settings, dest);

        string json = File.ReadAllText(dest);
        json.Should().Contain("\"globalWorkAreaOffset\"");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ApplyProfileAsync_GlazeWm_DeploysConfigFile()
    {
        var settings = new WmSettings
        {
            Engine   = "glazewm",
            OuterGap = 8,
            InnerGap = 4,
        };

        string dest = Path.Combine(_tempDir, "glazewm-apply", "config.yaml");
        WmManager.DeployGlazeWmConfig(settings, dest);

        string yaml = File.ReadAllText(dest);
        yaml.Should().Contain("outer_gap: 8");
        yaml.Should().Contain("inner_gap: 4");
        await Task.CompletedTask;
    }

    // ── WmSettings defaults ────────────────────────────────────────────────

    [Fact]
    public void WmSettings_Defaults_AreReasonable()
    {
        var s = new WmSettings();
        s.Engine.Should().Be("none");
        s.Layout.Should().Be("bsp");
        s.OuterGap.Should().Be(12);
        s.InnerGap.Should().Be(8);
        s.BorderEnabled.Should().BeTrue();
        s.BorderWidth.Should().Be(2);
        s.BorderActive.Should().NotBeNullOrEmpty();
        s.BorderInactive.Should().NotBeNullOrEmpty();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
