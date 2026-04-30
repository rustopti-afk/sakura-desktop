using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Sakura.Core.Profile;
using Sakura.Core.Theme;

namespace Sakura.Core.Tests.Profile;

/// <summary>
/// E2E tests that load the canonical sakura-yoru.json profile from disk,
/// deserialise it, and run it through ProfileApplicator.
/// Verifies that the profile file stays valid and the applicator handles it
/// without exceptions (apply will produce warnings for missing Windows deps,
/// but must not throw or corrupt anything).
/// </summary>
public sealed class SakuraYoruProfileTests : IDisposable
{
    private static readonly string ProfilePath = FindProfilePath();
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public SakuraYoruProfileTests() => Directory.CreateDirectory(_tempDir);

    // ── JSON deserialisation ───────────────────────────────────────────────

    [Fact]
    public void SakuraYoru_Json_CanBeDeserialised()
    {
        RiceProfile profile = LoadProfile();

        profile.Should().NotBeNull();
        profile.Profile.Name.Should().Be("Sakura Yoru");
    }

    [Fact]
    public void SakuraYoru_Profile_HasExpectedMetadata()
    {
        var p = LoadProfile().Profile;

        p.Name.Should().Be("Sakura Yoru");
        p.MinOsBuild.Should().BeGreaterThan(0u);
        p.Tags.Should().Contain("dark");
        p.Tags.Should().Contain("sakura");
    }

    [Fact]
    public void SakuraYoru_Compositor_HasValidColours()
    {
        var c = LoadProfile().Compositor;

        c.AccentColor.Should().StartWith("#");
        c.CaptionColor.Should().StartWith("#");
        c.TextColor.Should().StartWith("#");
        c.BorderColor.Should().StartWith("#");
        c.DarkMode.Should().BeTrue();
    }

    [Fact]
    public void SakuraYoru_Shell_HasCentredTaskbar()
    {
        var s = LoadProfile().Shell;
        s.TaskbarAlignment.Should().Be(1, "Sakura Yoru uses centred taskbar");
    }

    [Fact]
    public void SakuraYoru_Terminal_HasExpectedScheme()
    {
        var t = LoadProfile().Terminal;
        t.SchemeName.Should().Be("Sakura Yoru");
        t.FontFace.Should().Contain("JetBrains");
    }

    [Fact]
    public void SakuraYoru_Fonts_HasNotoSansJpSubstitutes()
    {
        var f = LoadProfile().Fonts;
        f.Substitutes.Should().ContainKey("Segoe UI");
        f.Substitutes["Segoe UI"].Should().Contain("Noto Sans");
    }

    [Fact]
    public void SakuraYoru_Wm_DefaultsToNoneEngine()
    {
        var wm = LoadProfile().Wm;
        wm.Engine.Should().Be("none", "default profile ships with WM disabled");
    }

    [Fact]
    public void SakuraYoru_Integrations_AreOptedOut()
    {
        var profile = LoadProfile();
        profile.Rainmeter.Apply.Should().BeFalse("default profile ships with Rainmeter disabled");
        profile.Windhawk.Apply.Should().BeFalse("default profile ships with Windhawk disabled");
        profile.Lively.Apply.Should().BeFalse("default profile ships with Lively disabled");
    }

    [Fact]
    public void SakuraYoru_Boot_IsDisabled()
    {
        var profile = LoadProfile();
        profile.Boot.HackBgrtEnabled.Should().BeFalse("default profile does not touch boot splash");
    }

    // ── ProfileApplicator E2E ──────────────────────────────────────────────

    [Fact]
    public async Task SakuraYoru_ApplyAsync_CompletesWithoutThrowing()
    {
        var profile = ProfileWithNoRequiredDeps();
        var reports  = new List<ApplyProgress>();
        var progress = new Progress<ApplyProgress>(p => reports.Add(p));

        // This will fail on individual steps (no Windows APIs on Linux / no admin)
        // but must not throw an unhandled exception — it should return ApplyResult.
        var result = await MakeApplicator().ApplyAsync(profile, progress);

        result.Should().NotBeNull();
        if (!result.Success)
            result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SakuraYoru_ApplyAsync_ReportsProgress()
    {
        var profile = ProfileWithNoRequiredDeps();

        var reports  = new List<ApplyProgress>();
        var progress = new Progress<ApplyProgress>(p => reports.Add(p));

        await MakeApplicator().ApplyAsync(profile, progress);

        reports.Should().NotBeEmpty("at least one progress step must be reported");
        reports.Should().AllSatisfy(r =>
        {
            r.TotalSteps.Should().Be(13);
            r.Fraction.Should().BeInRange(0.0, 1.0);
        });
    }

    [Fact]
    public async Task SakuraYoru_ApplyAsync_WithMissingRequiredDep_FailsBeforeAnyStep()
    {
        if (!OperatingSystem.IsWindows()) return;

        var profile = LoadProfile();
        // Override dependencies with a fake required ID
        var fakeReq = new DependencyList { Required = ["Definitely.NotInstalled.XYZ.SakuraTest"] };
        var profileWithFakeDeps = new RiceProfile
        {
            Profile      = profile.Profile,
            Shell        = profile.Shell,
            Compositor   = profile.Compositor,
            Theme        = profile.Theme,
            Fonts        = profile.Fonts,
            Icons        = profile.Icons,
            Wallpaper    = profile.Wallpaper,
            Terminal     = profile.Terminal,
            Boot         = profile.Boot,
            Wm           = profile.Wm,
            Rainmeter    = profile.Rainmeter,
            Windhawk     = profile.Windhawk,
            Lively       = profile.Lively,
            Dependencies = fakeReq
        };

        var reports  = new List<ApplyProgress>();
        var progress = new Progress<ApplyProgress>(p => reports.Add(p));

        var result = await MakeApplicator().ApplyAsync(profileWithFakeDeps, progress);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Missing required dependencies");
        reports.Should().BeEmpty("no steps should run when dependencies are missing");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    // Creates a new profile from JSON but with Required deps cleared so
    // the applicator doesn't fail on missing WinGet packages in CI.
    private static RiceProfile ProfileWithNoRequiredDeps()
    {
        string json = File.ReadAllText(ProfilePath);
        // Patch JSON to remove required deps before deserialising
        using var doc  = JsonDocument.Parse(json);
        using var ms   = new MemoryStream();
        using var writer = new System.Text.Json.Utf8JsonWriter(ms);

        writer.WriteStartObject();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name == "dependencies")
            {
                writer.WritePropertyName("dependencies");
                writer.WriteStartObject();
                writer.WritePropertyName("required");
                writer.WriteStartArray();
                writer.WriteEndArray();
                if (prop.Value.TryGetProperty("optional", out var opt))
                {
                    writer.WritePropertyName("optional");
                    opt.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
            else if (prop.Name == "profile")
            {
                // Zero out minOsBuild so the test runs on any OS/build
                writer.WritePropertyName("profile");
                writer.WriteStartObject();
                foreach (var pp in prop.Value.EnumerateObject())
                {
                    if (pp.Name == "minOsBuild")
                        writer.WriteNumber("minOsBuild", 0);
                    else
                        pp.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
            else
            {
                prop.WriteTo(writer);
            }
        }
        writer.WriteEndObject();
        writer.Flush();

        string patched = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        return JsonSerializer.Deserialize<RiceProfile>(patched,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private static RiceProfile LoadProfile()
    {
        string json = File.ReadAllText(ProfilePath);
        return JsonSerializer.Deserialize<RiceProfile>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Deserialisation returned null");
    }

    private ProfileApplicator MakeApplicator() => new(
        NullLogger<ProfileApplicator>.Instance,
        new ThemeEngine(NullLogger<ThemeEngine>.Instance),
        backupRootOverride: _tempDir);

    private static string FindProfilePath()
    {
        // Walk up from test binary to repo root, then into profiles/
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(dir, "profiles", "sakura-yoru.json");
            if (File.Exists(candidate)) return candidate;
            string parent = Path.GetDirectoryName(dir)!;
            if (parent == dir) break;
            dir = parent;
        }
        throw new FileNotFoundException("sakura-yoru.json not found relative to test binary");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
