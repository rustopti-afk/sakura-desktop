using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sakura.App.Services;
using Sakura.Core.Profile;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace Sakura.App.ViewModels;

public sealed partial class ProfilesViewModel : ObservableObject
{
    private readonly ProfileApplicator              _applicator;
    private readonly ILogger<ProfilesViewModel>     _logger;
    private MainViewModel?                          _mainVm;

    public ObservableCollection<ProfileEntry> Profiles { get; } = [];

    // Filtered view — bind ListBox to this instead of Profiles directly
    public ICollectionView ProfilesView { get; }

    [ObservableProperty] private ProfileEntry? _selectedProfile;
    [ObservableProperty] private bool          _isBusy        = false;
    [ObservableProperty] private string        _statusMessage = "";
    [ObservableProperty] private double        _applyProgress = 0;
    [ObservableProperty] private bool          _applySuccess  = false;
    [ObservableProperty] private string        _searchText    = "";

    partial void OnSearchTextChanged(string value) => ProfilesView.Refresh();

    public ProfilesViewModel(ProfileApplicator applicator, ILogger<ProfilesViewModel> logger)
    {
        _applicator = applicator;
        _logger     = logger;

        ProfilesView = CollectionViewSource.GetDefaultView(Profiles);
        ProfilesView.Filter = obj =>
        {
            if (string.IsNullOrWhiteSpace(_searchText)) return true;
            if (obj is not ProfileEntry e) return false;
            var q = _searchText.Trim();
            return e.Name.Contains(q,   StringComparison.OrdinalIgnoreCase)
                || e.Author.Contains(q, StringComparison.OrdinalIgnoreCase);
        };

        LoadBuiltInProfiles();
    }

    public void SetMainViewModel(MainViewModel mainVm) => _mainVm = mainVm;

    [RelayCommand]
    public void ClearSearch() => SearchText = "";

    partial void OnSelectedProfileChanged(ProfileEntry? value)
    {
        if (value is not null)
            _mainVm?.ShowProfilePreview(value.Profile);
    }

    // ── Commands ───────────────────────────────────────────────────────────

    [RelayCommand]
    public void LoadFromFile()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title      = "Open Rice Profile",
            Filter     = "Rice Profile (*.json)|*.json|All files (*.*)|*.*",
            Multiselect = true
        };

        if (dlg.ShowDialog() != true) return;

        int loaded = 0;
        foreach (string path in dlg.FileNames)
        {
            try
            {
                var profile = ProfileSerializer.LoadFromFile(path);
                var existing = Profiles.FirstOrDefault(p => p.Id == profile.Profile.Id);
                if (existing is not null)
                    Profiles.Remove(existing);

                Profiles.Add(new ProfileEntry(profile, path));
                loaded++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load profile from {Path}", path);
            }
        }

        StatusMessage = loaded == 0 ? "No profiles loaded" : $"Loaded {loaded} profile(s)";
        ApplySuccess  = false;

        if (loaded > 0 && SelectedProfile is null)
            SelectedProfile = Profiles.Last();
    }

    /// <summary>Called from code-behind when .json files are dropped onto the page.</summary>
    public void LoadFilesFromPaths(IEnumerable<string> paths)
    {
        int loaded = 0;
        foreach (string path in paths.Where(p =>
            p.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var profile  = ProfileSerializer.LoadFromFile(path);
                var existing = Profiles.FirstOrDefault(p => p.Id == profile.Profile.Id);
                if (existing is not null) Profiles.Remove(existing);
                Profiles.Add(new ProfileEntry(profile, path));
                loaded++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Drag-drop: failed to load {Path}", path);
            }
        }

        if (loaded > 0)
        {
            StatusMessage = $"Dropped {loaded} profile(s)";
            SelectedProfile = Profiles.Last();
        }
        else
        {
            StatusMessage = "No valid .json profiles in dropped files";
        }
        ApplySuccess = false;
    }

    [RelayCommand]
    public void ScanFolder()
    {
        // OpenFolderDialog requires .NET 8 / WPF — available without WinForms
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title      = "Select folder with .json profiles",
            Multiselect = false
        };

        if (dlg.ShowDialog() != true) return;

        int loaded = 0;
        foreach (string path in Directory.GetFiles(dlg.FolderName, "*.json"))
        {
            try
            {
                var profile = ProfileSerializer.LoadFromFile(path);
                var existing = Profiles.FirstOrDefault(p => p.Id == profile.Profile.Id);
                if (existing is not null)
                    Profiles.Remove(existing);

                Profiles.Add(new ProfileEntry(profile, path));
                loaded++;
            }
            catch { }
        }

        StatusMessage = $"Found {loaded} profile(s) in folder";
        ApplySuccess  = false;
    }

    [RelayCommand]
    public async Task ApplySelectedAsync()
    {
        if (IsBusy || SelectedProfile is null) return;

        IsBusy = true;
        ApplyProgress = 0;
        ApplySuccess  = false;
        StatusMessage = $"Applying '{SelectedProfile.Name}'...";

        try
        {
            var progress = new Progress<ApplyProgress>(p =>
            {
                ApplyProgress = p.Fraction * 100;
                StatusMessage = p.Message;
            });

            var result = await _applicator.ApplyAsync(SelectedProfile.Profile, progress);

            if (result.Success)
            {
                ApplySuccess  = true;
                StatusMessage = $"'{SelectedProfile.Name}' applied successfully";
                ToastService.Instance.Show($"✓ '{SelectedProfile.Name}' applied", ToastKind.Success);
                _logger.LogInformation("Profile '{Name}' applied", SelectedProfile.Name);
            }
            else
            {
                StatusMessage = $"Failed: {result.ErrorMessage}";
                ToastService.Instance.Show($"Apply failed: {result.ErrorMessage}", ToastKind.Error);
                _logger.LogError("Profile apply failed: {Msg}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            ToastService.Instance.Show($"Error: {ex.Message}", ToastKind.Error);
            _logger.LogError(ex, "Unexpected error during profile apply");
        }
        finally
        {
            IsBusy = false;
            ApplyProgress = 0;
        }
    }

    [RelayCommand]
    public void ExportSelected()
    {
        if (SelectedProfile is null) return;

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title      = "Export Rice Profile",
            Filter     = "Rice Profile (*.json)|*.json",
            FileName   = SanitizeFileName(SelectedProfile.Name) + ".json"
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            ProfileSerializer.SaveToFile(SelectedProfile.Profile, dlg.FileName);
            StatusMessage = $"Exported to {dlg.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    public void RemoveSelected()
    {
        if (SelectedProfile is null) return;
        string name = SelectedProfile.Name;
        Profiles.Remove(SelectedProfile);
        SelectedProfile = Profiles.Count > 0 ? Profiles[0] : null;
        StatusMessage   = $"Removed '{name}' from list";
        ApplySuccess    = false;
    }

    // ── Internals ──────────────────────────────────────────────────────────

    private void LoadBuiltInProfiles()
    {
        // Load bundled sakura-yoru.json from app directory
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "profiles", "sakura-yoru.json"),
            Path.Combine(AppContext.BaseDirectory, "sakura-yoru.json"),
        ];

        foreach (string path in candidates)
        {
            if (!File.Exists(path)) continue;
            try
            {
                var profile = ProfileSerializer.LoadFromFile(path);
                Profiles.Add(new ProfileEntry(profile, path));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load built-in profile from {Path}", path);
            }
            break;
        }

        // Also scan %AppData%\Sakura\profiles
        string userProfileDir = ProfileSerializer.GetDefaultProfileDir();
        if (Directory.Exists(userProfileDir))
        {
            foreach (string path in Directory.GetFiles(userProfileDir, "*.json"))
            {
                try
                {
                    var profile = ProfileSerializer.LoadFromFile(path);
                    if (Profiles.All(p => p.Id != profile.Profile.Id))
                        Profiles.Add(new ProfileEntry(profile, path));
                }
                catch { }
            }
        }

        if (Profiles.Count > 0)
            SelectedProfile = Profiles[0];
    }

    private static string SanitizeFileName(string name)
        => string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}

public sealed class ProfileEntry
{
    public RiceProfile Profile  { get; }
    public string      FilePath { get; }

    public string   Id          => Profile.Profile.Id;
    public string   Name        => Profile.Profile.Name;
    public string   Author      => Profile.Profile.Author;
    public string   Description => Profile.Profile.Description;
    public string   Tags        => string.Join(", ", Profile.Profile.Tags);
    public uint     MinOsBuild  => Profile.Profile.MinOsBuild;
    public DateTime CreatedUtc  => Profile.Profile.CreatedUtc;

    public bool HasRainmeter  => Profile.Rainmeter.Apply;
    public bool HasWindhawk   => Profile.Windhawk.Apply;
    public bool HasLively     => Profile.Lively.Apply;
    public bool HasTheme      => Profile.Theme.MsstylesPath is not null;
    public bool HasWallpaper  => Profile.Wallpaper.Path is not null || Profile.Wallpaper.PerMonitor.Length > 0;

    public string FeatureSummary
    {
        get
        {
            var parts = new List<string>();
            parts.Add("Shell");
            parts.Add("DWM");
            if (HasTheme)     parts.Add("Theme");
            if (HasWallpaper) parts.Add("Wallpaper");
            if (HasRainmeter) parts.Add("Rainmeter");
            if (HasWindhawk)  parts.Add("Windhawk");
            if (HasLively)    parts.Add("Lively");
            return string.Join(" · ", parts);
        }
    }

    public ProfileEntry(RiceProfile profile, string filePath)
    {
        Profile  = profile;
        FilePath = filePath;
    }
}
