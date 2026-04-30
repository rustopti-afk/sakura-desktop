using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sakura.Core.Native;

namespace Sakura.App.ViewModels;

public sealed partial class TerminalViewModel : ObservableObject
{
    private readonly ILogger<TerminalViewModel> _logger;

    // ── Color scheme ───────────────────────────────────────────────────────
    [ObservableProperty] private bool   _applyColorScheme = true;
    [ObservableProperty] private string _schemeName       = "Sakura Yoru";
    [ObservableProperty] private string _fontFace         = "JetBrainsMono Nerd Font";
    [ObservableProperty] private int    _fontSize         = 11;
    [ObservableProperty] private int    _opacity          = 88;
    [ObservableProperty] private bool   _useAcrylic       = true;

    // ── Oh My Posh ─────────────────────────────────────────────────────────
    [ObservableProperty] private bool _applyOhMyPosh = true;

    // ── Status ─────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isBusy        = false;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool   _statusSuccess = false;

    public IReadOnlyList<int> FontSizeOptions { get; } = [9, 10, 11, 12, 13, 14, 16, 18];
    public IReadOnlyList<int> OpacityOptions  { get; } = [70, 75, 80, 85, 88, 90, 95, 100];

    public TerminalViewModel(ILogger<TerminalViewModel> logger) => _logger = logger;

    [RelayCommand]
    public async Task ApplyAsync()
    {
        if (IsBusy) return;
        IsBusy        = true;
        StatusSuccess = false;
        StatusMessage = "Applying terminal settings...";

        try
        {
            if (ApplyColorScheme)
            {
                await TerminalManager.ApplyColorSchemeAsync(
                    SchemeName, FontFace, FontSize, Opacity, UseAcrylic)
                    .ConfigureAwait(false);
                StatusMessage = "Color scheme applied";
            }

            if (ApplyOhMyPosh)
            {
                StatusMessage = "Deploying Oh My Posh theme...";
                await OhMyPoshManager.DeploySakuraThemeAsync().ConfigureAwait(false);
                StatusMessage = "Oh My Posh theme deployed";
            }

            StatusSuccess = true;
            StatusMessage = "Terminal settings applied — restart terminal to see changes";
            _logger.LogInformation("Terminal settings applied");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed: {ex.Message}";
            _logger.LogError(ex, "Terminal apply failed");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task ApplySchemeOnlyAsync()
    {
        if (IsBusy) return;
        IsBusy        = true;
        StatusSuccess = false;
        StatusMessage = "Applying color scheme...";

        try
        {
            await TerminalManager.ApplyColorSchemeAsync(
                SchemeName, FontFace, FontSize, Opacity, UseAcrylic)
                .ConfigureAwait(false);

            StatusSuccess = true;
            StatusMessage = "Color scheme applied — restart terminal to see changes";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed: {ex.Message}";
            _logger.LogError(ex, "Color scheme apply failed");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task DeployOhMyPoshAsync()
    {
        if (IsBusy) return;
        IsBusy        = true;
        StatusSuccess = false;
        StatusMessage = "Deploying Oh My Posh theme...";

        try
        {
            await OhMyPoshManager.DeploySakuraThemeAsync().ConfigureAwait(false);
            StatusSuccess = true;
            StatusMessage = "Sakura theme deployed — restart terminal to see changes";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed: {ex.Message}";
            _logger.LogError(ex, "Oh My Posh deploy failed");
        }
        finally { IsBusy = false; }
    }
}
