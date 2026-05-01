using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sakura.App.Services;
using Sakura.Core.Integrations;
using Sakura.Core.Profile;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace Sakura.App.ViewModels;

public sealed partial class IconsViewModel : ObservableObject
{
    private readonly ILogger<IconsViewModel> _logger;

    private readonly string _iconsBaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Sakura");

    [ObservableProperty] private string? _selectedIconPack   = null;
    [ObservableProperty] private string? _selectedCursorPack = null;
    [ObservableProperty] private bool    _isBusy             = false;
    [ObservableProperty] private string  _statusMessage      = "";
    [ObservableProperty] private bool    _statusSuccess      = false;

    public ObservableCollection<string> IconPacks   { get; } = [];
    public ObservableCollection<string> CursorPacks { get; } = [];

    public IconsViewModel(ILogger<IconsViewModel> logger)
    {
        _logger = logger;
        Refresh();
    }

    [RelayCommand]
    public void Refresh()
    {
        IconPacks.Clear();
        CursorPacks.Clear();

        string iconDir  = Path.Combine(_iconsBaseDir, "icons");
        string cursorDir = Path.Combine(_iconsBaseDir, "cursors");

        if (Directory.Exists(iconDir))
            foreach (string dir in Directory.GetDirectories(iconDir))
                IconPacks.Add(Path.GetFileName(dir));

        if (Directory.Exists(cursorDir))
            foreach (string dir in Directory.GetDirectories(cursorDir))
                CursorPacks.Add(Path.GetFileName(dir));

        StatusMessage = IconPacks.Count == 0 && CursorPacks.Count == 0
            ? $"No packs found in {_iconsBaseDir}"
            : $"{IconPacks.Count} icon pack(s), {CursorPacks.Count} cursor pack(s)";
    }

    [RelayCommand]
    public void ApplyIconPack()
    {
        if (string.IsNullOrEmpty(SelectedIconPack))
        {
            StatusMessage = "Select an icon pack first";
            return;
        }
        if (!OperatingSystem.IsWindows())
        {
            StatusMessage = "Windows only";
            return;
        }
        try
        {
            var settings = new IconSettings { Pack = SelectedIconPack };
            IconManager.ApplyProfile(settings, _iconsBaseDir);
            StatusSuccess = true;
            StatusMessage = $"Icon pack '{SelectedIconPack}' applied — restart Explorer to see changes";
            ToastService.Instance.Show($"✓ Icons: {SelectedIconPack}", ToastKind.Success);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Apply failed: {ex.Message}";
            _logger.LogError(ex, "Icon pack apply failed");
        }
    }

    [RelayCommand]
    public void ApplyCursorPack()
    {
        if (string.IsNullOrEmpty(SelectedCursorPack))
        {
            StatusMessage = "Select a cursor pack first";
            return;
        }
        if (!OperatingSystem.IsWindows())
        {
            StatusMessage = "Windows only";
            return;
        }
        try
        {
            var settings = new IconSettings { CursorPack = SelectedCursorPack };
            IconManager.ApplyProfile(settings, _iconsBaseDir);
            StatusSuccess = true;
            StatusMessage = $"Cursor pack '{SelectedCursorPack}' applied";
            ToastService.Instance.Show($"✓ Cursors: {SelectedCursorPack}", ToastKind.Success);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Apply failed: {ex.Message}";
            _logger.LogError(ex, "Cursor pack apply failed");
        }
    }

    [RelayCommand]
    public void RestoreDefaults()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            IconManager.RestoreDefaultShellIcons();
            IconManager.RestoreDefaultCursors();
            StatusSuccess = true;
            StatusMessage = "Shell icons and cursors restored to Windows defaults";
            ToastService.Instance.Show("✓ Icons restored", ToastKind.Info);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
            _logger.LogError(ex, "Restore defaults failed");
        }
    }

    [RelayCommand]
    public void OpenIconsFolder()
    {
        string iconsDir = Path.Combine(_iconsBaseDir, "icons");
        Directory.CreateDirectory(iconsDir);
        Process.Start("explorer.exe", iconsDir);
    }
}
