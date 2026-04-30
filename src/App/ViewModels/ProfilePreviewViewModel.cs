using CommunityToolkit.Mvvm.ComponentModel;
using Sakura.Core.Profile;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Sakura.App.ViewModels;

public sealed record ColorSwatch(string Label, string Hex, SolidColorBrush Brush);

public sealed partial class ProfilePreviewViewModel : ObservableObject
{
    [ObservableProperty] private string _profileName   = "No profile";
    [ObservableProperty] private string _profileAuthor = "";
    [ObservableProperty] private bool   _darkMode      = true;

    public ObservableCollection<ColorSwatch> Swatches { get; } = [];

    public void LoadProfile(RiceProfile profile)
    {
        ProfileName   = profile.Profile.Name;
        ProfileAuthor = profile.Profile.Author;
        DarkMode      = profile.Compositor.DarkMode;

        Swatches.Clear();
        AddSwatch("Accent",   profile.Compositor.AccentColor);
        AddSwatch("Caption",  profile.Compositor.CaptionColor);
        AddSwatch("Border",   profile.Compositor.BorderColor);
        AddSwatch("Text",     profile.Compositor.TextColor);
    }

    private void AddSwatch(string label, string hex)
    {
        var brush = ParseHex(hex);
        Swatches.Add(new ColorSwatch(label, hex.ToUpperInvariant(), brush));
    }

    private static SolidColorBrush ParseHex(string hex)
    {
        try   { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch { return Brushes.Transparent; }
    }
}
