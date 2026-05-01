using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Sakura.App.Services;
using Sakura.Core.Theme;
using System.IO;

namespace Sakura.App.ViewModels;

public sealed partial class ThemeViewModel : ObservableObject
{
    private readonly ILogger<ThemeViewModel> _logger;

    [ObservableProperty] private string _msstylesPath     = "";
    [ObservableProperty] private string _themeName        = "Sakura";
    [ObservableProperty] private bool   _secureUxInstalled = false;
    [ObservableProperty] private string _secureUxStatus   = "Checking...";
    [ObservableProperty] private bool   _isBusy           = false;
    [ObservableProperty] private string _statusMessage    = "";
    [ObservableProperty] private bool   _statusSuccess    = false;

    public ThemeViewModel(ILogger<ThemeViewModel> logger)
    {
        _logger = logger;
        RefreshStatus();
    }

    [RelayCommand]
    public void RefreshStatus()
    {
        if (!OperatingSystem.IsWindows())
        {
            SecureUxStatus    = "Windows only";
            SecureUxInstalled = false;
            return;
        }
        var status = SecureUxThemeHelper.Detect();
        SecureUxInstalled = status != SecureUxThemeStatus.NotInstalled;
        SecureUxStatus = status switch
        {
            SecureUxThemeStatus.NotInstalled  => "Not installed",
            SecureUxThemeStatus.Installed     => "Installed — loader not active",
            SecureUxThemeStatus.LoaderActive  => "Active ✓",
            _ => "Unknown"
        };
    }

    [RelayCommand]
    public void BrowseMsstyles()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select .msstyles file",
            Filter = "Theme files (*.msstyles)|*.msstyles|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
            MsstylesPath = dlg.FileName;
    }

    [RelayCommand]
    public async Task ApplyThemeAsync()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(MsstylesPath))
        {
            StatusMessage = "Please select an .msstyles file first";
            StatusSuccess = false;
            return;
        }
        if (!File.Exists(MsstylesPath))
        {
            StatusMessage = "Selected file does not exist";
            StatusSuccess = false;
            return;
        }

        IsBusy        = true;
        StatusSuccess = false;
        StatusMessage = "Applying theme...";

        try
        {
            await Task.Run(() =>
            {
                string themeFile = SecureUxThemeHelper.DeployTheme(MsstylesPath, ThemeName);
                SecureUxThemeHelper.ApplyTheme(themeFile);
            }).ConfigureAwait(false);

            StatusSuccess = true;
            StatusMessage = $"Theme '{ThemeName}' applied";
            ToastService.Instance.Show($"✓ Theme applied: {ThemeName}", ToastKind.Success);
            _logger.LogInformation("Theme applied: {Theme} from {Path}", ThemeName, MsstylesPath);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Apply failed: {ex.Message}";
            ToastService.Instance.Show($"Theme failed: {ex.Message}", ToastKind.Error);
            _logger.LogError(ex, "Theme apply failed");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task InstallSecureUxThemeAsync()
    {
        if (IsBusy) return;
        IsBusy        = true;
        StatusMessage = "Installing SecureUxTheme...";
        StatusSuccess = false;

        try
        {
            await SecureUxThemeHelper.EnsureInstalledAsync().ConfigureAwait(false);
            RefreshStatus();
            StatusSuccess = true;
            StatusMessage = "SecureUxTheme installed and activated";
            ToastService.Instance.Show("✓ SecureUxTheme ready", ToastKind.Success);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Install failed: {ex.Message}";
            _logger.LogError(ex, "SecureUxTheme install failed");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task UninstallSecureUxThemeAsync()
    {
        if (IsBusy) return;
        IsBusy        = true;
        StatusMessage = "Removing SecureUxTheme...";
        StatusSuccess = false;

        try
        {
            await Task.Run(SecureUxThemeHelper.Uninstall).ConfigureAwait(false);
            RefreshStatus();
            StatusSuccess = true;
            StatusMessage = "SecureUxTheme removed — default themes restored";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Uninstall failed: {ex.Message}";
            _logger.LogError(ex, "SecureUxTheme uninstall failed");
        }
        finally { IsBusy = false; }
    }
}
