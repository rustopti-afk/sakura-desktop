using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Sakura.App.Services;
using Sakura.Core.Integrations;
using Sakura.Core.Native;
using System.IO;

namespace Sakura.App.ViewModels;

public sealed partial class WallpaperViewModel : ObservableObject
{
    private readonly ILogger<WallpaperViewModel> _logger;

    [ObservableProperty] private string _imagePath        = "";
    [ObservableProperty] private int    _fit              = 4; // Fill
    [ObservableProperty] private bool   _useLively        = false;
    [ObservableProperty] private string _livelyVideoPath  = "";
    [ObservableProperty] private bool   _livelyInstalled  = false;
    [ObservableProperty] private bool   _isBusy           = false;
    [ObservableProperty] private string _statusMessage    = "";
    [ObservableProperty] private bool   _statusSuccess    = false;

    public IReadOnlyList<FitOption> FitOptions { get; } =
    [
        new(4, "Fill"),
        new(3, "Fit"),
        new(2, "Stretch"),
        new(1, "Tile"),
        new(0, "Center"),
        new(5, "Span"),
    ];

    public WallpaperViewModel(ILogger<WallpaperViewModel> logger)
    {
        _logger = logger;
        if (OperatingSystem.IsWindows())
            LivelyInstalled = LivelyManager.IsInstalled();
    }

    [RelayCommand]
    public void BrowseImage()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select wallpaper image",
            Filter = "Images (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
            ImagePath = dlg.FileName;
    }

    [RelayCommand]
    public void BrowseLivelyVideo()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select Lively wallpaper (video/HTML/GIF)",
            Filter = "Lively wallpapers (*.mp4;*.webm;*.html;*.gif)|*.mp4;*.webm;*.html;*.gif|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
            LivelyVideoPath = dlg.FileName;
    }

    [RelayCommand]
    public async Task ApplyAsync()
    {
        if (IsBusy) return;
        IsBusy        = true;
        StatusSuccess = false;
        StatusMessage = "Applying wallpaper...";

        try
        {
            if (UseLively)
            {
                if (string.IsNullOrWhiteSpace(LivelyVideoPath) || !File.Exists(LivelyVideoPath))
                    throw new InvalidOperationException("Please select a valid Lively wallpaper file.");

                await LivelyManager.SetWallpaperAsync(LivelyVideoPath).ConfigureAwait(false);
                StatusMessage = "Lively wallpaper set";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(ImagePath) || !File.Exists(ImagePath))
                    throw new InvalidOperationException("Please select a valid image file.");

                await Task.Run(() =>
                    WallpaperManager.SetWallpaperAllMonitors(ImagePath, (WallpaperFit)Fit))
                    .ConfigureAwait(false);
                StatusMessage = "Wallpaper applied to all monitors";
            }

            StatusSuccess = true;
            ToastService.Instance.Show("✓ Wallpaper applied", ToastKind.Success);
            _logger.LogInformation("Wallpaper applied — lively={Lively}", UseLively);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed: {ex.Message}";
            ToastService.Instance.Show($"Wallpaper failed: {ex.Message}", ToastKind.Error);
            _logger.LogError(ex, "Wallpaper apply failed");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task StopLivelyAsync()
    {
        if (IsBusy) return;
        IsBusy        = true;
        StatusMessage = "Stopping Lively...";
        try
        {
            await LivelyManager.CloseAllAsync().ConfigureAwait(false);
            StatusSuccess = true;
            StatusMessage = "Lively wallpapers stopped";
        }
        catch (Exception ex)
        {
            StatusSuccess = false;
            StatusMessage = $"Failed: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void InstallLively()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
            "winget", "install rocksdanister.LivelyWallpaper") { UseShellExecute = true });
    }
}

public sealed record FitOption(int Value, string Label);
