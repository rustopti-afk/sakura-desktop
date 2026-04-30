using Sakura.App.ViewModels;
using Sakura.Core.Profile;

namespace Sakura.App.Tests;

public sealed class ProfilePreviewViewModelTests
{
    private static RiceProfile MakeProfile(
        string name   = "Test",
        string author = "Author",
        bool dark     = true,
        string accent = "#E8A0BF",
        string caption = "#0D1418",
        string border  = "#E8A0BF",
        string text    = "#EDE6EE") => new()
    {
        Profile    = new() { Name = name, Author = author },
        Compositor = new()
        {
            DarkMode     = dark,
            AccentColor  = accent,
            CaptionColor = caption,
            BorderColor  = border,
            TextColor    = text,
        }
    };

    [Fact]
    public void LoadProfile_SetsNameAndAuthor()
    {
        var vm = new ProfilePreviewViewModel();
        vm.LoadProfile(MakeProfile(name: "Sakura Yoru", author: "Alice"));
        vm.ProfileName.Should().Be("Sakura Yoru");
        vm.ProfileAuthor.Should().Be("Alice");
    }

    [Fact]
    public void LoadProfile_SetsDarkMode()
    {
        var vm = new ProfilePreviewViewModel();
        vm.LoadProfile(MakeProfile(dark: false));
        vm.DarkMode.Should().BeFalse();
    }

    [Fact]
    public void LoadProfile_PopulatesFourSwatches()
    {
        var vm = new ProfilePreviewViewModel();
        vm.LoadProfile(MakeProfile());
        vm.Swatches.Should().HaveCount(4);
    }

    [Fact]
    public void LoadProfile_SwatchLabelsAreCorrect()
    {
        var vm = new ProfilePreviewViewModel();
        vm.LoadProfile(MakeProfile());
        vm.Swatches.Select(s => s.Label)
          .Should().ContainInOrder("Accent", "Caption", "Border", "Text");
    }

    [Fact]
    public void LoadProfile_SwatchHexIsUppercase()
    {
        var vm = new ProfilePreviewViewModel();
        vm.LoadProfile(MakeProfile(accent: "#e8a0bf"));
        vm.Swatches[0].Hex.Should().Be("#E8A0BF");
    }

    [Fact]
    public void LoadProfile_InvalidHex_DoesNotThrow()
    {
        var vm = new ProfilePreviewViewModel();
        var ex = Record.Exception(() =>
            vm.LoadProfile(MakeProfile(accent: "not-a-color")));
        ex.Should().BeNull();
    }

    [Fact]
    public void LoadProfile_CalledTwice_ReplacesPreviousSwatches()
    {
        var vm = new ProfilePreviewViewModel();
        vm.LoadProfile(MakeProfile(name: "First"));
        vm.LoadProfile(MakeProfile(name: "Second"));
        vm.ProfileName.Should().Be("Second");
        vm.Swatches.Should().HaveCount(4);
    }

    [Fact]
    public void DefaultState_ProfileNameIsPlaceholder()
    {
        var vm = new ProfilePreviewViewModel();
        vm.ProfileName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void DefaultState_SwatchesIsEmpty()
    {
        var vm = new ProfilePreviewViewModel();
        vm.Swatches.Should().BeEmpty();
    }
}
