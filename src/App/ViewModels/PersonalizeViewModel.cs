using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sakura.App.Services;
using Sakura.Core.Backup;
using Sakura.Core.Native;
using Sakura.Core.Profile;
using System.Windows.Media;
using WpfFonts = System.Windows.Media.Fonts;

namespace Sakura.App.ViewModels;

public sealed partial class PersonalizeViewModel : ObservableObject
{
    private readonly ProfileApplicator _applicator;
    private readonly ILogger<PersonalizeViewModel> _logger;

    // ── DWM / compositor ───────────────────────────────────────────────────

    [ObservableProperty] private bool   _darkMode        = true;
    [ObservableProperty] private bool   _transparency    = true;
    [ObservableProperty] private bool   _colorPrevalence = true;
    [ObservableProperty] private int    _backdropType    = DwmTitlebar.DWMSBT_MICA;
    [ObservableProperty] private int    _cornerPref      = DwmTitlebar.DWMWCP_ROUND;
    [ObservableProperty] private string _accentHex       = "#E8A0BF";
    [ObservableProperty] private string _captionHex      = "#0D1418";
    [ObservableProperty] private string _textHex         = "#EDE6EE";
    [ObservableProperty] private string _borderHex       = "#E8A0BF";

    // ── Shell ──────────────────────────────────────────────────────────────

    [ObservableProperty] private int  _taskbarAlignment = 1;
    [ObservableProperty] private bool _showSearch       = false;
    [ObservableProperty] private bool _showTaskView     = false;
    [ObservableProperty] private bool _showWidgets      = false;
    [ObservableProperty] private bool _showChat         = false;

    // ── Fonts ──────────────────────────────────────────────────────────────

    public IReadOnlyList<string> SystemFonts { get; } =
        WpfFonts.SystemFontFamilies
            .Select(f => f.Source)
            .Order()
            .ToList();

    [ObservableProperty] private string _selectedUiFont   = "Segoe UI Variable";
    [ObservableProperty] private string _selectedIconFont = "Segoe UI Variable";
    [ObservableProperty] private int    _uiFontSize       = 9;
    [ObservableProperty] private int    _iconFontSize     = 9;

    // ── Animations ─────────────────────────────────────────────────────────

    [ObservableProperty] private bool _animationsEnabled = true;
    [ObservableProperty] private int  _menuDelay         = 0;

    // ── Status ─────────────────────────────────────────────────────────────

    [ObservableProperty] private bool   _isBusy        = false;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private double _applyProgress = 0;
    [ObservableProperty] private bool   _applySuccess  = false;
    [ObservableProperty] private string _lastBackupDir = "";

    // ── Backdrop options ───────────────────────────────────────────────────

    public IReadOnlyList<BackdropOption> BackdropOptions { get; } =
    [
        new(DwmTitlebar.DWMSBT_NONE,    "None"),
        new(DwmTitlebar.DWMSBT_MICA,    "Mica"),
        new(DwmTitlebar.DWMSBT_ACRYLIC, "Acrylic"),
        new(DwmTitlebar.DWMSBT_TABBED,  "Mica Alt"),
        new(DwmTitlebar.DWMSBT_AUTO,    "Auto"),
    ];

    public IReadOnlyList<CornerOption> CornerOptions { get; } =
    [
        new(DwmTitlebar.DWMWCP_DEFAULT,    "Default"),
        new(DwmTitlebar.DWMWCP_ROUND,      "Rounded"),
        new(DwmTitlebar.DWMWCP_ROUNDSMALL, "Rounded Small"),
        new(DwmTitlebar.DWMWCP_DONOTROUND, "Square"),
    ];

    public PersonalizeViewModel(ProfileApplicator applicator, ILogger<PersonalizeViewModel> logger)
    {
        _applicator = applicator;
        _logger     = logger;

        // Pre-select current system fonts if running on Windows
        if (OperatingSystem.IsWindows())
        {
            try
            {
                _selectedUiFont   = FontManager.GetCurrentCaptionFont() ?? _selectedUiFont;
                _selectedIconFont = FontManager.GetCurrentIconFont()    ?? _selectedIconFont;
            }
            catch { /* non-critical — defaults remain */ }
        }
    }

    [RelayCommand]
    public async Task PreviewDwmAsync()
    {
        if (IsBusy) return;
        try
        {
            DwmTitlebar.ApplyToProcess("notepad",
                DwmTitlebar.ToBgr(ParseHex(CaptionHex)),
                DwmTitlebar.ToBgr(ParseHex(TextHex)),
                DwmTitlebar.ToBgr(ParseHex(BorderHex)),
                DarkMode, CornerPref, BackdropType);
            StatusMessage = "Preview applied to Notepad";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Preview failed: {ex.Message}";
            _logger.LogWarning(ex, "DWM preview failed");
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task ApplyAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ApplyProgress = 0;
        ApplySuccess  = false;
        StatusMessage = "Applying...";

        try
        {
            var profile = BuildProfile();
            var progress = new Progress<ApplyProgress>(p =>
            {
                ApplyProgress  = p.Fraction * 100;
                StatusMessage  = p.Message;
            });

            var result = await _applicator.ApplyAsync(profile, progress);

            if (result.Success)
            {
                ApplySuccess  = true;
                LastBackupDir = result.BackupDir ?? "";
                StatusMessage = "Applied successfully";
                ToastService.Instance.Show("✓ Settings applied", ToastKind.Success);
            }
            else
            {
                StatusMessage = $"Failed: {result.ErrorMessage}";
                ToastService.Instance.Show($"Apply failed: {result.ErrorMessage}", ToastKind.Error);
                _logger.LogError("Apply failed: {Msg}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "Unexpected error during apply");
        }
        finally
        {
            IsBusy = false;
            ApplyProgress = 0;
        }
    }

    [RelayCommand]
    public async Task QuickApplyDwmAsync()
    {
        if (IsBusy) return;
        StatusMessage = "Applying DWM settings...";
        try
        {
            RegistryWriter.ApplyDwmSettings(DarkMode, Transparency, ParseHex(AccentHex), ColorPrevalence);
            DwmTitlebar.ApplyToProcess("explorer",
                DwmTitlebar.ToBgr(ParseHex(CaptionHex)),
                DwmTitlebar.ToBgr(ParseHex(TextHex)),
                DwmTitlebar.ToBgr(ParseHex(BorderHex)),
                DarkMode, CornerPref, BackdropType);
            StatusMessage = "DWM settings applied";
            ToastService.Instance.Show("✓ DWM settings applied", ToastKind.Success);
        }
        catch (Exception ex)
        {
            StatusMessage = $"DWM apply failed: {ex.Message}";
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task QuickApplyTaskbarAsync()
    {
        if (IsBusy) return;
        try
        {
            RegistryWriter.SetTaskbarAlignment(TaskbarAlignment);
            RegistryWriter.SetDword(Microsoft.Win32.RegistryHive.CurrentUser,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "TaskbarDa", ShowWidgets ? 1u : 0u);
            RegistryWriter.SetDword(Microsoft.Win32.RegistryHive.CurrentUser,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "ShowTaskViewButton", ShowTaskView ? 1u : 0u);
            RegistryWriter.SetDword(Microsoft.Win32.RegistryHive.CurrentUser,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "TaskbarMn", ShowChat ? 1u : 0u);
            StatusMessage = "Taskbar settings applied — restart explorer to see changes";
            ToastService.Instance.Show("✓ Taskbar applied — restart Explorer", ToastKind.Info);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Taskbar apply failed: {ex.Message}";
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task QuickApplyAnimationsAsync()
    {
        RegistryWriter.SetAnimations(AnimationsEnabled);
        RegistryWriter.SetMenuShowDelay(MenuDelay);
        StatusMessage = AnimationsEnabled ? "Animations enabled" : "Animations disabled";
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task QuickApplyFontsAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            StatusMessage = "Font apply is only supported on Windows";
            return;
        }
        try
        {
            FontManager.ApplyNonClientFont(SelectedUiFont, UiFontSize);
            FontManager.ApplyIconFont(SelectedIconFont, IconFontSize);
            StatusMessage = $"Fonts applied: UI=\"{SelectedUiFont} {UiFontSize}pt\", Icon=\"{SelectedIconFont} {IconFontSize}pt\"";
            _logger.LogInformation("Fonts applied — UI: {UiFont} {UiSize}pt, Icon: {IconFont} {IconSize}pt",
                SelectedUiFont, UiFontSize, SelectedIconFont, IconFontSize);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Font apply failed: {ex.Message}";
            _logger.LogWarning(ex, "Font apply failed");
        }
        await Task.CompletedTask;
    }

    private RiceProfile BuildProfile() => new()
    {
        Profile = new() { Name = "Quick Apply", Description = "Applied from Personalize panel" },
        Compositor = new()
        {
            DarkMode         = DarkMode,
            Transparency     = Transparency,
            ColorPrevalence  = ColorPrevalence,
            BackdropType     = BackdropType,
            CornerPref       = CornerPref,
            AccentColor      = AccentHex,
            CaptionColor     = CaptionHex,
            TextColor        = TextHex,
            BorderColor      = BorderHex,
            AnimationsEnabled = AnimationsEnabled,
            MenuDelay        = MenuDelay
        },
        Shell = new()
        {
            TaskbarAlignment = TaskbarAlignment,
            ShowSearch       = ShowSearch,
            ShowTaskView     = ShowTaskView,
            ShowWidgets      = ShowWidgets,
            ShowChat         = ShowChat
        }
    };

    private static uint ParseHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return 0xFF000000;
        hex = hex.TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        try { return Convert.ToUInt32(hex, 16); }
        catch { return 0xFF000000; }
    }
}

public sealed record BackdropOption(int Value, string Label);
public sealed record CornerOption(int Value, string Label);
