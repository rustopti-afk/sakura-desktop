using Sakura.App.Localization;
using System.Globalization;

namespace Sakura.App.Tests;

public sealed class LocalizationServiceTests
{
    // Reset to English after each test so tests don't bleed into each other
    private static void ResetToEnglish() =>
        LocalizationService.Instance.SetLanguage("en");

    [Fact]
    public void Instance_IsSingleton()
    {
        LocalizationService.Instance.Should().BeSameAs(LocalizationService.Instance);
    }

    [Fact]
    public void SetLanguage_En_SetsInvariantCulture()
    {
        LocalizationService.Instance.SetLanguage("en");
        LocalizationService.Instance.CurrentLanguageCode.Should().Be("iv"); // InvariantCulture two-letter = "iv"
    }

    [Fact]
    public void SetLanguage_Uk_SetsCultureUk()
    {
        LocalizationService.Instance.SetLanguage("uk");
        LocalizationService.Instance.Culture.Name.Should().Be("uk");
        ResetToEnglish();
    }

    [Fact]
    public void SetLanguage_Ja_SetsCultureJa()
    {
        LocalizationService.Instance.SetLanguage("ja");
        LocalizationService.Instance.Culture.Name.Should().Be("ja");
        ResetToEnglish();
    }

    [Fact]
    public void Indexer_KnownKey_ReturnsNonEmptyString()
    {
        LocalizationService.Instance.SetLanguage("en");
        var value = LocalizationService.Instance["Btn_Save"];
        value.Should().NotBeNullOrEmpty();
        ResetToEnglish();
    }

    [Fact]
    public void Indexer_UnknownKey_ReturnsFallbackKey()
    {
        LocalizationService.Instance.SetLanguage("en");
        var key = "NonExistent_Key_XYZ";
        var value = LocalizationService.Instance[key];
        value.Should().Be(key, "unknown keys fall back to the key itself");
        ResetToEnglish();
    }

    [Fact]
    public void Indexer_UkrainianCulture_ReturnsDifferentValueThanEnglish()
    {
        LocalizationService.Instance.SetLanguage("en");
        var en = LocalizationService.Instance["Btn_Save"];

        LocalizationService.Instance.SetLanguage("uk");
        var uk = LocalizationService.Instance["Btn_Save"];

        en.Should().NotBe(uk, "Ukrainian translation should differ from English");
        ResetToEnglish();
    }

    [Fact]
    public void SetLanguage_SameCulture_DoesNotFirePropertyChanged()
    {
        LocalizationService.Instance.SetLanguage("en");
        int fired = 0;
        LocalizationService.Instance.PropertyChanged += (_, _) => fired++;

        LocalizationService.Instance.SetLanguage("en"); // same → no change

        fired.Should().Be(0);
        ResetToEnglish();
    }

    [Fact]
    public void AvailableLanguages_ContainsEnUkJa()
    {
        var codes = LocalizationService.AvailableLanguages.Select(l => l.Code);
        codes.Should().Contain("en").And.Contain("uk").And.Contain("ja");
    }

    [Fact]
    public void AvailableLanguages_AllHaveNonEmptyDisplayNames()
    {
        foreach (var lang in LocalizationService.AvailableLanguages)
            lang.DisplayName.Should().NotBeNullOrEmpty();
    }
}
