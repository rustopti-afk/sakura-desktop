using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sakura.App.Views.Controls;
using Sakura.App.Views.Pages;
using Sakura.Core.Native;
using Sakura.Core.Profile;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Sakura.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly ProfilePreviewViewModel _previewVm = new();

    [ObservableProperty] private NavItem? _selectedNav;
    [ObservableProperty] private object?  _currentPage;
    [ObservableProperty] private object?  _previewView;
    [ObservableProperty] private string   _statusText        = "Ready";
    [ObservableProperty] private string   _lastApplyHash     = "—";
    [ObservableProperty] private string   _activeProfileName = "Default";
    [ObservableProperty] private bool     _updateAvailable   = false;
    [ObservableProperty] private string   _updateVersion     = "";
    [ObservableProperty] private string   _updateUrl         = "";

    public ObservableCollection<NavItem> NavItems { get; } =
    [
        new() { Key = "personalize",  Label = "Personalize",    IconGlyph = "" },
        new() { Key = "profiles",     Label = "Profiles",       IconGlyph = "" },
        new() { Key = "editor",       Label = "New Profile",    IconGlyph = "✏" },
        new() { Key = "terminal",     Label = "Terminal",       IconGlyph = "" },
        new() { Key = "integrations", Label = "Integrations",   IconGlyph = "" },
        new() { Key = "backup",       Label = "Backups",        IconGlyph = "" },
        new() { Key = "settings",     Label = "Settings",       IconGlyph = "" },
    ];

    public MainViewModel(IServiceProvider services)
    {
        _services = services;

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedNav))
                NavigateTo(SelectedNav?.Key);
        };

        SelectedNav = NavItems[0];

        InitPreviewPanel();

        // Fire-and-forget update check on startup; failures are silently ignored
        _ = CheckForUpdateAsync();
    }

    private void InitPreviewPanel()
    {
        var ctrl = new ProfilePreviewControl { DataContext = _previewVm };
        PreviewView = ctrl;

        // Load default Sakura Yoru profile for the initial preview
        var builtIn = System.IO.Path.Combine(
            AppContext.BaseDirectory, "profiles", "sakura-yoru.json");
        if (System.IO.File.Exists(builtIn))
        {
            try
            {
                var profile = ProfileSerializer.Deserialize(
                    System.IO.File.ReadAllText(builtIn));
                if (profile is not null)
                    _previewVm.LoadProfile(profile);
            }
            catch { /* preview is non-critical */ }
        }
    }

    /// <summary>Called by ProfilesViewModel when the user selects a profile.</summary>
    public void ShowProfilePreview(RiceProfile profile)
    {
        _previewVm.LoadProfile(profile);
    }

    private async Task CheckForUpdateAsync()
    {
        var info = await UpdateChecker.CheckAsync().ConfigureAwait(false);
        if (info is null || !info.IsUpdateAvailable) return;

        UpdateVersion   = info.LatestVersion;
        UpdateUrl       = info.ReleasePageUrl;
        UpdateAvailable = true;
    }

    private void NavigateTo(string? key)
    {
        CurrentPage = key switch
        {
            "personalize" => _services.GetRequiredService<PersonalizePage>(),
            "profiles"    => _services.GetRequiredService<ProfilesPage>(),
            "backup"       => _services.GetRequiredService<BackupPage>(),
            "integrations" => _services.GetRequiredService<IntegrationsPage>(),
            "editor"       => _services.GetRequiredService<ProfileEditorPage>(),
            "terminal"     => _services.GetRequiredService<TerminalPage>(),
            "settings"     => _services.GetRequiredService<SettingsPage>(),
            _              => null
        };

        StatusText = SelectedNav is not null ? $"Section: {SelectedNav.Label}" : "Ready";
    }

    [RelayCommand]
    public void SetStatus(string text) => StatusText = text;

    [RelayCommand]
    public void OpenReleasePage()
    {
        if (!string.IsNullOrEmpty(UpdateUrl))
            Process.Start(new ProcessStartInfo(UpdateUrl) { UseShellExecute = true });
    }

    [RelayCommand]
    public void DismissUpdate() => UpdateAvailable = false;
}
