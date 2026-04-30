using FluentAssertions;
using Sakura.Core.Profile;

namespace Sakura.Core.Tests.Profile;

public sealed class ProfileSerializerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public ProfileSerializerTests() => Directory.CreateDirectory(_tempDir);

    private static RiceProfile MakeProfile(string name = "Test Profile") => new()
    {
        Profile = new ProfileMeta { Name = name, Author = "tester" },
        Compositor = new CompositorSettings
        {
            DarkMode        = true,
            AccentColor     = "#E8A0BF",
            CaptionColor    = "#0D1418",
            TextColor       = "#EDE6EE",
            BorderColor     = "#E8A0BF",
            BackdropType    = 3,
            CornerPref      = 1,
            AnimationsEnabled = true,
            MenuDelay       = 0
        },
        Shell = new ShellSettings { TaskbarAlignment = 1 },
        Terminal = new TerminalSettings
        {
            ApplyColorScheme = true,
            SchemeName       = "Sakura Yoru",
            FontFace         = "JetBrainsMono Nerd Font",
            FontSize         = 12,
            Opacity          = 90,
            UseAcrylic       = true
        }
    };

    [Fact]
    public void Serialize_ProducesValidJson_WithCamelCase()
    {
        var profile = MakeProfile();
        string json = ProfileSerializer.Serialize(profile);

        json.Should().Contain("\"name\"");
        json.Should().Contain("Test Profile");
        json.Should().Contain("\"darkMode\"");
        json.Should().NotContain("\"DarkMode\"");
    }

    [Fact]
    public void Deserialize_RoundTrip_PreservesAllFields()
    {
        var original = MakeProfile("Sakura Yoru");
        string json = ProfileSerializer.Serialize(original);
        var loaded = ProfileSerializer.Deserialize(json);

        loaded.Profile.Name.Should().Be("Sakura Yoru");
        loaded.Profile.Author.Should().Be("tester");
        loaded.Compositor.DarkMode.Should().BeTrue();
        loaded.Compositor.AccentColor.Should().Be("#E8A0BF");
        loaded.Compositor.BackdropType.Should().Be(3);
        loaded.Shell.TaskbarAlignment.Should().Be(1);
        loaded.Terminal.SchemeName.Should().Be("Sakura Yoru");
        loaded.Terminal.FontSize.Should().Be(12);
    }

    [Fact]
    public void SaveToFile_ThenLoadFromFile_ProducesEqualProfile()
    {
        var profile = MakeProfile("File Round Trip");
        string path = Path.Combine(_tempDir, "test.json");

        ProfileSerializer.SaveToFile(profile, path);
        var loaded = ProfileSerializer.LoadFromFile(path);

        loaded.Profile.Name.Should().Be("File Round Trip");
        loaded.Compositor.AccentColor.Should().Be(profile.Compositor.AccentColor);
        loaded.Terminal.Opacity.Should().Be(90);
    }

    [Fact]
    public void SaveToFile_CreatesParentDirectory_WhenMissing()
    {
        string nestedPath = Path.Combine(_tempDir, "sub", "deep", "profile.json");
        var profile = MakeProfile();

        ProfileSerializer.SaveToFile(profile, nestedPath);

        File.Exists(nestedPath).Should().BeTrue();
    }

    [Fact]
    public void LoadFromFile_ThrowsFileNotFound_WhenFileMissing()
    {
        var act = () => ProfileSerializer.LoadFromFile(Path.Combine(_tempDir, "nonexistent.json"));
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Deserialize_ThrowsInvalidData_WhenJsonIsNull()
    {
        var act = () => ProfileSerializer.Deserialize("null");
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void LoadAll_ReturnsEmpty_WhenDirectoryMissing()
    {
        var profiles = ProfileSerializer.LoadAll(Path.Combine(_tempDir, "no_such_dir"));
        profiles.Should().BeEmpty();
    }

    [Fact]
    public void LoadAll_SkipsCorruptFiles_ReturnsValidOnes()
    {
        ProfileSerializer.SaveToFile(MakeProfile("Valid"), Path.Combine(_tempDir, "valid.json"));
        File.WriteAllText(Path.Combine(_tempDir, "corrupt.json"), "{ not valid json {{{{");

        var profiles = ProfileSerializer.LoadAll(_tempDir);

        profiles.Should().HaveCount(1);
        profiles[0].Profile.Name.Should().Be("Valid");
    }

    [Fact]
    public void ProfileMeta_HasUniqueIds_WhenCreatedSeparately()
    {
        var a = new ProfileMeta();
        var b = new ProfileMeta();
        a.Id.Should().NotBe(b.Id);
    }

    [Fact]
    public void Serialize_NullOptionalFields_OmittedFromJson()
    {
        var profile = MakeProfile();
        string json = ProfileSerializer.Serialize(profile);

        // WhenWritingNull means null strings/objects should not appear
        json.Should().NotContain(": null");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
