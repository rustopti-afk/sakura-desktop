using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sakura.Core.Profile;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace Sakura.App.ViewModels;

public sealed partial class ProfileEditorViewModel : ObservableObject
{
    private readonly ILogger<ProfileEditorViewModel> _logger;
    private readonly string _profilesDir;

    // ── Profile meta ──────────────────────────────────────────────────────
    [ObservableProperty] private string _name        = "My Profile";
    [ObservableProperty] private string _author      = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _tagsRaw     = "";

    // ── Compositor ────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _darkMode         = true;
    [ObservableProperty] private bool   _transparency     = true;
    [ObservableProperty] private bool   _colorPrevalence  = true;
    [ObservableProperty] private string _accentColor      = "#E8A0BF";
    [ObservableProperty] private string _captionColor     = "#0D1418";
    [ObservableProperty] private string _textColor        = "#EDE6EE";
    [ObservableProperty] private string _borderColor      = "#E8A0BF";
    [ObservableProperty] private int    _backdropType     = 3;
    [ObservableProperty] private int    _cornerPref       = 2;
    [ObservableProperty] private bool   _animationsEnabled = true;
    [ObservableProperty] private int    _menuDelay        = 0;

    // ── Shell ─────────────────────────────────────────────────────────────
    [ObservableProperty] private int  _taskbarAlignment = 1;
    [ObservableProperty] private bool _showSearch       = false;
    [ObservableProperty] private bool _showTaskView     = false;
    [ObservableProperty] private bool _showWidgets      = false;
    [ObservableProperty] private bool _showChat         = false;

    // ── Terminal ──────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _applyColorScheme = true;
    [ObservableProperty] private string _schemeName       = "";
    [ObservableProperty] private bool   _applyOhMyPosh    = true;
    [ObservableProperty] private string _fontFace         = "JetBrainsMono Nerd Font";
    [ObservableProperty] private int    _fontSize         = 12;
    [ObservableProperty] private int    _opacity          = 88;
    [ObservableProperty] private bool   _useAcrylic       = true;

    // ── WM ────────────────────────────────────────────────────────────────
    [ObservableProperty] private string _wmEngine        = "none";
    [ObservableProperty] private string _wmLayout        = "bsp";
    [ObservableProperty] private int    _wmOuterGap      = 12;
    [ObservableProperty] private int    _wmInnerGap      = 8;
    [ObservableProperty] private bool   _wmBorderEnabled = true;
    [ObservableProperty] private string _wmBorderActive  = "#E8A0BF";
    [ObservableProperty] private string _wmBorderInactive = "#3E3E5E";

    // ── Integrations flags ────────────────────────────────────────────────
    [ObservableProperty] private bool _applyRainmeter = false;
    [ObservableProperty] private bool _applyWindhawk  = false;
    [ObservableProperty] private bool _applyLively    = false;

    // ── State ─────────────────────────────────────────────────────────────
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool   _statusSuccess = false;
    [ObservableProperty] private bool   _isDirty       = false;

    public ObservableCollection<string> BackdropTypes { get; } =
        ["None", "DWM", "Acrylic", "Mica", "Mica Alt"];

    public ObservableCollection<string> CornerPrefs { get; } =
        ["Default", "No rounding", "Rounded", "Rounded small"];

    public ObservableCollection<string> TaskbarAlignments { get; } =
        ["Left", "Center"];

    public ObservableCollection<string> WmEngines { get; } =
        ["none", "komorebi", "glazewm"];

    public ProfileEditorViewModel(ILogger<ProfileEditorViewModel> logger, string? profilesDirOverride = null)
    {
        _logger      = logger;
        _profilesDir = profilesDirOverride
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "Sakura", "profiles");

        // Default scheme name matches profile name
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Name) && SchemeNameMatchesName())
                SchemeName = Name;
            IsDirty = true;
        };
    }

    // ── Commands ───────────────────────────────────────────────────────────

    [RelayCommand]
    public void LoadDefaults()
    {
        Name             = "My Profile";
        Author           = "";
        Description      = "";
        TagsRaw          = "";
        DarkMode         = true;
        Transparency     = true;
        ColorPrevalence  = true;
        AccentColor      = "#E8A0BF";
        CaptionColor     = "#0D1418";
        TextColor        = "#EDE6EE";
        BorderColor      = "#E8A0BF";
        BackdropType     = 3;
        CornerPref       = 2;
        AnimationsEnabled = true;
        MenuDelay        = 0;
        TaskbarAlignment = 1;
        ShowSearch       = false;
        ShowTaskView     = false;
        ShowWidgets      = false;
        ShowChat         = false;
        ApplyColorScheme = true;
        SchemeName       = "My Profile";
        ApplyOhMyPosh    = true;
        FontFace         = "JetBrainsMono Nerd Font";
        FontSize         = 12;
        Opacity          = 88;
        UseAcrylic       = true;
        WmEngine         = "none";
        WmLayout         = "bsp";
        WmOuterGap       = 12;
        WmInnerGap       = 8;
        WmBorderEnabled  = true;
        WmBorderActive   = "#E8A0BF";
        WmBorderInactive = "#3E3E5E";
        ApplyRainmeter   = false;
        ApplyWindhawk    = false;
        ApplyLively      = false;
        StatusMessage    = "Defaults loaded";
        StatusSuccess    = false;
        IsDirty          = false;
    }

    [RelayCommand]
    public void LoadFromSakuraYoru()
    {
        string builtIn = Path.Combine(AppContext.BaseDirectory, "profiles", "sakura-yoru.json");
        if (!File.Exists(builtIn)) { StatusMessage = "Built-in profile not found"; return; }
        try { LoadFromFile(builtIn); }
        catch (Exception ex) { StatusMessage = $"Load failed: {ex.Message}"; }
    }

    [RelayCommand]
    public void SaveProfile()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Profile name is required";
            StatusSuccess = false;
            return;
        }

        try
        {
            Directory.CreateDirectory(_profilesDir);
            string safeName = string.Concat(Name.Split(Path.GetInvalidFileNameChars()));
            string path     = Path.Combine(_profilesDir, safeName + ".json");
            string json     = JsonSerializer.Serialize(BuildProfile(),
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);

            StatusMessage = $"Saved to {path}";
            StatusSuccess = true;
            IsDirty       = false;
            _logger.LogInformation("Profile saved to {Path}", path);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
            StatusSuccess = false;
            _logger.LogError(ex, "Profile save failed");
        }
    }

    [RelayCommand]
    public void ExportProfile()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title      = "Export Profile",
            Filter     = "JSON profile (*.json)|*.json",
            FileName   = string.Concat(Name.Split(Path.GetInvalidFileNameChars())) + ".json",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            string json = JsonSerializer.Serialize(BuildProfile(),
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dialog.FileName, json);
            StatusMessage = $"Exported to {dialog.FileName}";
            StatusSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    // ── Internals ──────────────────────────────────────────────────────────

    private void LoadFromFile(string path)
    {
        string json    = File.ReadAllText(path);
        var profile    = JsonSerializer.Deserialize<RiceProfile>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Invalid profile JSON");

        Name             = profile.Profile.Name;
        Author           = profile.Profile.Author;
        Description      = profile.Profile.Description;
        TagsRaw          = string.Join(", ", profile.Profile.Tags);
        DarkMode         = profile.Compositor.DarkMode;
        Transparency     = profile.Compositor.Transparency;
        ColorPrevalence  = profile.Compositor.ColorPrevalence;
        AccentColor      = profile.Compositor.AccentColor;
        CaptionColor     = profile.Compositor.CaptionColor;
        TextColor        = profile.Compositor.TextColor;
        BorderColor      = profile.Compositor.BorderColor;
        BackdropType     = profile.Compositor.BackdropType;
        CornerPref       = profile.Compositor.CornerPref;
        AnimationsEnabled = profile.Compositor.AnimationsEnabled;
        MenuDelay        = profile.Compositor.MenuDelay;
        TaskbarAlignment = profile.Shell.TaskbarAlignment;
        ShowSearch       = profile.Shell.ShowSearch;
        ShowTaskView     = profile.Shell.ShowTaskView;
        ShowWidgets      = profile.Shell.ShowWidgets;
        ShowChat         = profile.Shell.ShowChat;
        ApplyColorScheme = profile.Terminal.ApplyColorScheme;
        SchemeName       = profile.Terminal.SchemeName;
        ApplyOhMyPosh    = profile.Terminal.ApplyOhMyPosh;
        FontFace         = profile.Terminal.FontFace;
        FontSize         = profile.Terminal.FontSize;
        Opacity          = profile.Terminal.Opacity;
        UseAcrylic       = profile.Terminal.UseAcrylic;
        WmEngine         = profile.Wm.Engine;
        WmLayout         = profile.Wm.Layout;
        WmOuterGap       = profile.Wm.OuterGap;
        WmInnerGap       = profile.Wm.InnerGap;
        WmBorderEnabled  = profile.Wm.BorderEnabled;
        WmBorderActive   = profile.Wm.BorderActive;
        WmBorderInactive = profile.Wm.BorderInactive;
        ApplyRainmeter   = profile.Rainmeter.Apply;
        ApplyWindhawk    = profile.Windhawk.Apply;
        ApplyLively      = profile.Lively.Apply;
        StatusMessage    = $"Loaded '{profile.Profile.Name}'";
        StatusSuccess    = true;
        IsDirty          = false;
    }

    private RiceProfile BuildProfile() => new()
    {
        Profile = new ProfileMeta
        {
            Name        = Name,
            Author      = Author,
            Description = Description,
            Tags        = TagsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        },
        Compositor = new CompositorSettings
        {
            DarkMode          = DarkMode,
            Transparency      = Transparency,
            ColorPrevalence   = ColorPrevalence,
            AccentColor       = AccentColor,
            CaptionColor      = CaptionColor,
            TextColor         = TextColor,
            BorderColor       = BorderColor,
            BackdropType      = BackdropType,
            CornerPref        = CornerPref,
            AnimationsEnabled = AnimationsEnabled,
            MenuDelay         = MenuDelay
        },
        Shell = new ShellSettings
        {
            TaskbarAlignment = TaskbarAlignment,
            ShowSearch       = ShowSearch,
            ShowTaskView     = ShowTaskView,
            ShowWidgets      = ShowWidgets,
            ShowChat         = ShowChat
        },
        Terminal = new TerminalSettings
        {
            ApplyColorScheme = ApplyColorScheme,
            SchemeName       = SchemeName,
            ApplyOhMyPosh    = ApplyOhMyPosh,
            FontFace         = FontFace,
            FontSize         = FontSize,
            Opacity          = Opacity,
            UseAcrylic       = UseAcrylic
        },
        Wm = new Sakura.Core.Integrations.WmSettings
        {
            Engine        = WmEngine,
            Layout        = WmLayout,
            OuterGap      = WmOuterGap,
            InnerGap      = WmInnerGap,
            BorderEnabled = WmBorderEnabled,
            BorderActive  = WmBorderActive,
            BorderInactive = WmBorderInactive
        },
        Rainmeter = new Sakura.Core.Integrations.RainmeterSettings { Apply = ApplyRainmeter },
        Windhawk  = new Sakura.Core.Integrations.WindhawkSettings  { Apply = ApplyWindhawk },
        Lively    = new Sakura.Core.Integrations.LivelySettings    { Apply = ApplyLively }
    };

    private bool SchemeNameMatchesName()
        => string.IsNullOrEmpty(SchemeName) || SchemeName == Name;
}
