using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Sakura.App.Localization;

/// <summary>
/// Singleton service that serves localized strings at runtime.
/// Supports switching language without app restart via INotifyPropertyChanged indexer.
/// Usage in XAML: Text="{Binding [Key_Name], Source={x:Static loc:LocalizationService.Instance}}"
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    private static readonly ResourceManager _rm =
        new("Sakura.App.Localization.Strings", typeof(LocalizationService).Assembly);

    private CultureInfo _culture = CultureInfo.CurrentUICulture;

    private LocalizationService() { }

    public CultureInfo Culture
    {
        get => _culture;
        set
        {
            if (Equals(_culture, value)) return;
            _culture = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Culture)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguageCode)));
        }
    }

    /// <summary>Short tag: "en", "uk", "ja".</summary>
    public string CurrentLanguageCode => _culture.TwoLetterISOLanguageName;

    /// <summary>Indexer for XAML binding: {Binding [Key_Name], Source={x:Static ...}}</summary>
    public string this[string key]
    {
        get
        {
            try   { return _rm.GetString(key, _culture) ?? key; }
            catch { return key; }
        }
    }

    public void SetLanguage(string twoLetterCode)
    {
        Culture = twoLetterCode switch
        {
            "uk" => new CultureInfo("uk"),
            "ja" => new CultureInfo("ja"),
            _    => CultureInfo.InvariantCulture, // "en" → invariant = base .resx
        };
    }

    public static readonly IReadOnlyList<LanguageOption> AvailableLanguages =
    [
        new("en", "English"),
        new("uk", "Українська"),
        new("ja", "日本語"),
    ];

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed record LanguageOption(string Code, string DisplayName);
