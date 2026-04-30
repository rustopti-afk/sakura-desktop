using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sakura.App.Localization;
using Sakura.Core.Profile;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Sakura.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty] private string _backupRoot;
    [ObservableProperty] private string _profilesDir;
    [ObservableProperty] private string _appVersion;
    [ObservableProperty] private string _statusMessage = "";

    public ObservableCollection<LanguageOption> Languages { get; } =
        new(LocalizationService.AvailableLanguages);

    private LanguageOption _selectedLanguage;
    public LanguageOption SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value) && value is not null)
                LocalizationService.Instance.SetLanguage(value.Code);
        }
    }

    public SettingsViewModel(ILogger<SettingsViewModel> logger)
    {
        _logger      = logger;
        _backupRoot  = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Sakura", "backup");
        _profilesDir = ProfileSerializer.GetDefaultProfileDir();
        _appVersion  = Assembly.GetExecutingAssembly()
                           .GetName().Version?.ToString(3) ?? "0.0.0";

        var currentCode = LocalizationService.Instance.CurrentLanguageCode;
        _selectedLanguage = Languages.FirstOrDefault(l => l.Code == currentCode)
                            ?? Languages[0];
    }

    [RelayCommand]
    public void OpenBackupFolder()
    {
        try
        {
            Directory.CreateDirectory(BackupRoot);
            Process.Start("explorer.exe", BackupRoot);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Cannot open folder: {ex.Message}";
        }
    }

    [RelayCommand]
    public void OpenProfilesFolder()
    {
        try
        {
            Directory.CreateDirectory(ProfilesDir);
            Process.Start("explorer.exe", ProfilesDir);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Cannot open folder: {ex.Message}";
        }
    }

    [RelayCommand]
    public void OpenLogsFolder()
    {
        string logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Sakura", "logs");
        try
        {
            Directory.CreateDirectory(logsDir);
            Process.Start("explorer.exe", logsDir);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Cannot open folder: {ex.Message}";
        }
    }

    [RelayCommand]
    public void CopyVersionToClipboard()
    {
        try
        {
            System.Windows.Clipboard.SetText(AppVersion);
            StatusMessage = "Version copied to clipboard";
        }
        catch { }
    }
}
