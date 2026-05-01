using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sakura.App.Services;
using Sakura.Core.Integrations;

namespace Sakura.App.ViewModels;

public sealed partial class WmViewModel : ObservableObject
{
    private readonly ILogger<WmViewModel> _logger;

    [ObservableProperty] private string _engine        = "none"; // none|komorebi|glazewm
    [ObservableProperty] private string _layout        = "bsp";
    [ObservableProperty] private int    _outerGap      = 12;
    [ObservableProperty] private int    _innerGap      = 8;
    [ObservableProperty] private bool   _borderEnabled = true;
    [ObservableProperty] private int    _borderWidth   = 2;
    [ObservableProperty] private string _borderActive  = "#E8A0BF";
    [ObservableProperty] private string _borderInactive = "#3E3E5E";
    [ObservableProperty] private bool   _startOnApply  = true;

    [ObservableProperty] private bool   _komorebiInstalled = false;
    [ObservableProperty] private bool   _glazeWmInstalled  = false;
    [ObservableProperty] private bool   _isBusy            = false;
    [ObservableProperty] private string _statusMessage     = "";
    [ObservableProperty] private bool   _statusSuccess     = false;

    public IReadOnlyList<WmEngineOption> EngineOptions { get; } =
    [
        new("none",      "None — disable"),
        new("komorebi",  "Komorebi"),
        new("glazewm",   "GlazeWM"),
    ];

    public IReadOnlyList<string> LayoutOptions { get; } = ["bsp", "columns", "rows", "monocle"];

    public WmViewModel(ILogger<WmViewModel> logger)
    {
        _logger = logger;
        RefreshInstallState();
    }

    [RelayCommand]
    public void RefreshInstallState()
    {
        if (!OperatingSystem.IsWindows()) return;
        KomorebiInstalled = WmManager.IsKomorebiInstalled();
        GlazeWmInstalled  = WmManager.IsGlazeWmInstalled();
    }

    [RelayCommand]
    public async Task DeployConfigAsync()
    {
        if (IsBusy || Engine == "none") return;
        IsBusy        = true;
        StatusSuccess = false;
        StatusMessage = "Writing config...";

        try
        {
            var settings = BuildSettings();
            string path = await Task.Run(() =>
                Engine == "komorebi"
                    ? WmManager.DeployKomorebiConfig(settings)
                    : WmManager.DeployGlazeWmConfig(settings))
                .ConfigureAwait(false);

            StatusSuccess = true;
            StatusMessage = $"Config written to {path}";
            ToastService.Instance.Show($"✓ {Engine} config deployed", ToastKind.Success);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed: {ex.Message}";
            _logger.LogError(ex, "WM config deploy failed");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task ApplyAndStartAsync()
    {
        if (IsBusy) return;
        if (Engine == "none")
        {
            StatusMessage = "Select an engine first";
            return;
        }
        IsBusy        = true;
        StatusSuccess = false;
        StatusMessage = $"Deploying config and starting {Engine}...";

        try
        {
            await WmManager.ApplyProfileAsync(BuildSettings(), startWm: true).ConfigureAwait(false);
            StatusSuccess = true;
            StatusMessage = $"{Engine} started";
            ToastService.Instance.Show($"✓ {Engine} running", ToastKind.Success);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed: {ex.Message}";
            ToastService.Instance.Show($"WM failed: {ex.Message}", ToastKind.Error);
            _logger.LogError(ex, "WM start failed");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task StopAsync()
    {
        if (IsBusy) return;
        IsBusy        = true;
        StatusMessage = "Stopping window manager...";
        try
        {
            if (Engine == "komorebi")
                await WmManager.StopKomorebiAsync().ConfigureAwait(false);
            else if (Engine == "glazewm")
                await WmManager.StopGlazeWmAsync().ConfigureAwait(false);
            else
            {
                StatusMessage = "No window manager is running";
                return;
            }
            StatusSuccess = true;
            StatusMessage = "Window manager stopped";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Stop failed: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void InstallKomorebi()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
            "winget", "install LGUG2Z.komorebi") { UseShellExecute = true });
    }

    [RelayCommand]
    public void InstallGlazeWm()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
            "winget", "install lars-berger.GlazeWM") { UseShellExecute = true });
    }

    private WmSettings BuildSettings() => new()
    {
        Engine        = Engine,
        Layout        = Layout,
        OuterGap      = OuterGap,
        InnerGap      = InnerGap,
        BorderEnabled = BorderEnabled,
        BorderWidth   = BorderWidth,
        BorderActive  = BorderActive,
        BorderInactive = BorderInactive,
    };
}

public sealed record WmEngineOption(string Key, string Label);
