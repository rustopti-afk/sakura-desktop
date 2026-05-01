using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Sakura.App.Services;
using Sakura.Core.Integrations;
using System.IO;

namespace Sakura.App.ViewModels;

public sealed partial class BootViewModel : ObservableObject
{
    private readonly ILogger<BootViewModel> _logger;

    [ObservableProperty] private string _splashPath       = "";
    [ObservableProperty] private bool   _hackBgrtInstalled = false;
    [ObservableProperty] private string _hackBgrtDir      = "";
    [ObservableProperty] private bool   _isBusy           = false;
    [ObservableProperty] private string _statusMessage    = "";
    [ObservableProperty] private bool   _statusSuccess    = false;

    public BootViewModel(ILogger<BootViewModel> logger)
    {
        _logger = logger;
        RefreshStatus();
    }

    [RelayCommand]
    public void RefreshStatus()
    {
        if (!OperatingSystem.IsWindows())
        {
            StatusMessage     = "Windows only";
            HackBgrtInstalled = false;
            return;
        }
        string? dir = BootManager.FindHackBgrtDir();
        HackBgrtInstalled = dir is not null;
        HackBgrtDir       = dir ?? "";
        StatusMessage     = HackBgrtInstalled
            ? $"HackBGRT found at: {dir}"
            : "HackBGRT not found — install it manually to use boot splash";
    }

    [RelayCommand]
    public void BrowseSplash()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select splash image (BMP or PNG)",
            Filter = "Images (*.bmp;*.png;*.jpg)|*.bmp;*.png;*.jpg|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
            SplashPath = dlg.FileName;
    }

    [RelayCommand]
    public async Task DeployAndInstallAsync()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(SplashPath) || !File.Exists(SplashPath))
        {
            StatusMessage = "Please select a valid splash image";
            return;
        }
        if (!HackBgrtInstalled)
        {
            StatusMessage = "HackBGRT is not installed — cannot deploy";
            return;
        }

        IsBusy        = true;
        StatusSuccess = false;
        StatusMessage = "Deploying splash image...";

        try
        {
            await Task.Run(() => BootManager.DeploySplash(SplashPath)).ConfigureAwait(false);
            StatusMessage = "Running HackBGRT setup...";
            await BootManager.InstallAsync().ConfigureAwait(false);

            StatusSuccess = true;
            StatusMessage = "Boot splash installed — takes effect on next reboot";
            ToastService.Instance.Show("✓ Boot splash ready", ToastKind.Success);
            _logger.LogInformation("HackBGRT splash deployed from {Path}", SplashPath);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed: {ex.Message}";
            ToastService.Instance.Show($"Boot splash failed: {ex.Message}", ToastKind.Error);
            _logger.LogError(ex, "HackBGRT deploy failed");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task UninstallAsync()
    {
        if (IsBusy || !HackBgrtInstalled) return;
        IsBusy        = true;
        StatusMessage = "Removing HackBGRT boot splash...";
        StatusSuccess = false;

        try
        {
            await BootManager.UninstallAsync().ConfigureAwait(false);
            StatusSuccess = true;
            StatusMessage = "HackBGRT uninstalled — original boot logo restored on next reboot";
            RefreshStatus();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Uninstall failed: {ex.Message}";
            _logger.LogError(ex, "HackBGRT uninstall failed");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void OpenHackBgrtFolder()
    {
        if (!string.IsNullOrEmpty(HackBgrtDir) && Directory.Exists(HackBgrtDir))
            System.Diagnostics.Process.Start("explorer.exe", HackBgrtDir);
    }
}
