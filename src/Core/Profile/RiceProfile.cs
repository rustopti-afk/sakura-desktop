using Sakura.Core.Integrations;
using System.Text.Json.Serialization;

namespace Sakura.Core.Profile;

public sealed class RiceProfile
{
    [JsonPropertyName("$schema")]
    public string Schema { get; init; } = "https://sakuradesktop.dev/schemas/profile-1.json";

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("profile")]
    public ProfileMeta Profile { get; init; } = new();

    [JsonPropertyName("shell")]
    public ShellSettings Shell { get; init; } = new();

    [JsonPropertyName("wm")]
    public WmSettings Wm { get; init; } = new();

    [JsonPropertyName("compositor")]
    public CompositorSettings Compositor { get; init; } = new();

    [JsonPropertyName("theme")]
    public ThemeSettings Theme { get; init; } = new();

    [JsonPropertyName("fonts")]
    public FontSettings Fonts { get; init; } = new();

    [JsonPropertyName("icons")]
    public IconSettings Icons { get; init; } = new();

    [JsonPropertyName("wallpaper")]
    public WallpaperSettings Wallpaper { get; init; } = new();

    [JsonPropertyName("terminal")]
    public TerminalSettings Terminal { get; init; } = new();

    [JsonPropertyName("boot")]
    public BootSettings Boot { get; init; } = new();

    [JsonPropertyName("rainmeter")]
    public RainmeterSettings Rainmeter { get; init; } = new();

    [JsonPropertyName("windhawk")]
    public WindhawkSettings Windhawk { get; init; } = new();

    [JsonPropertyName("lively")]
    public LivelySettings Lively { get; init; } = new();

    [JsonPropertyName("dependencies")]
    public DependencyList Dependencies { get; init; } = new();
}

public sealed class ProfileMeta
{
    [JsonPropertyName("id")]          public string Id          { get; init; } = Guid.NewGuid().ToString("N");
    [JsonPropertyName("name")]        public string Name        { get; init; } = "Default";
    [JsonPropertyName("author")]      public string Author      { get; init; } = "";
    [JsonPropertyName("description")] public string Description { get; init; } = "";
    [JsonPropertyName("createdUtc")]  public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
    [JsonPropertyName("minOsBuild")]  public uint MinOsBuild    { get; init; } = 22621;
    [JsonPropertyName("tags")]        public string[] Tags      { get; init; } = [];
}

public sealed class ShellSettings
{
    [JsonPropertyName("taskbarAlignment")]    public int     TaskbarAlignment   { get; init; } = 1; // 1=center
    [JsonPropertyName("showSearch")]          public bool    ShowSearch          { get; init; } = false;
    [JsonPropertyName("showTaskView")]        public bool    ShowTaskView        { get; init; } = false;
    [JsonPropertyName("showWidgets")]         public bool    ShowWidgets         { get; init; } = false;
    [JsonPropertyName("showChat")]            public bool    ShowChat            { get; init; } = false;
    [JsonPropertyName("clockFormat")]         public string  ClockFormat         { get; init; } = "yyyy/MM/dd  HH:mm";
    [JsonPropertyName("explorerPatcherJson")] public string? ExplorerPatcherJson { get; init; }
}

public sealed class CompositorSettings
{
    [JsonPropertyName("backdrop")]         public int    BackdropType    { get; init; } = 3; // Mica
    [JsonPropertyName("cornerPref")]       public int    CornerPref      { get; init; } = 2; // Round
    [JsonPropertyName("captionColor")]     public string CaptionColor    { get; init; } = "#0D1418";
    [JsonPropertyName("textColor")]        public string TextColor        { get; init; } = "#EDE6EE";
    [JsonPropertyName("borderColor")]      public string BorderColor      { get; init; } = "#E8A0BF";
    [JsonPropertyName("darkMode")]         public bool   DarkMode         { get; init; } = true;
    [JsonPropertyName("transparency")]     public bool   Transparency     { get; init; } = true;
    [JsonPropertyName("colorPrevalence")]  public bool   ColorPrevalence  { get; init; } = true;
    [JsonPropertyName("accentColor")]      public string AccentColor      { get; init; } = "#C4E8A0BF";
    [JsonPropertyName("animationsEnabled")] public bool  AnimationsEnabled { get; init; } = true;
    [JsonPropertyName("menuDelay")]        public int    MenuDelay        { get; init; } = 0;
}

public sealed class ThemeSettings
{
    [JsonPropertyName("msstylesPath")]        public string? MsstylesPath     { get; init; }
    [JsonPropertyName("themeName")]           public string? ThemeName        { get; init; }
    [JsonPropertyName("secureUxRequired")]    public bool    SecureUxRequired { get; init; } = true;
}

public sealed class FontSettings
{
    [JsonPropertyName("systemUi")]     public string  SystemUi     { get; init; } = "Noto Sans JP";
    [JsonPropertyName("mono")]         public string  Mono         { get; init; } = "JetBrainsMono Nerd Font";
    [JsonPropertyName("substitutes")]  public Dictionary<string, string> Substitutes { get; init; } = new()
    {
        ["Segoe UI"]       = "Noto Sans JP",
        ["Segoe UI Bold"]  = "Noto Sans JP Bold",
        ["Segoe UI Light"] = "Noto Sans JP Light"
    };
    [JsonPropertyName("macTypeProfile")] public string? MacTypeProfile { get; init; }
}

public sealed class IconSettings
{
    [JsonPropertyName("pack")]           public string?   Pack          { get; init; }
    [JsonPropertyName("patchTargets")]   public string[]  PatchTargets  { get; init; } = [];
    [JsonPropertyName("cursorPack")]     public string?   CursorPack    { get; init; }
}

public sealed class WallpaperSettings
{
    [JsonPropertyName("engine")]      public string Engine { get; init; } = "windows"; // windows|lively
    [JsonPropertyName("path")]        public string? Path  { get; init; }
    [JsonPropertyName("fit")]         public int     Fit   { get; init; } = 4;          // Fill
    [JsonPropertyName("perMonitor")]  public PerMonitorWallpaper[] PerMonitor { get; init; } = [];
}

public sealed class PerMonitorWallpaper
{
    [JsonPropertyName("monitorPath")] public string MonitorPath { get; init; } = "";
    [JsonPropertyName("wallpaper")]   public string Wallpaper   { get; init; } = "";
    [JsonPropertyName("fit")]         public int    Fit         { get; init; } = 4;
}

public sealed class TerminalSettings
{
    [JsonPropertyName("applyColorScheme")] public bool   ApplyColorScheme { get; init; } = true;
    [JsonPropertyName("schemeName")]       public string SchemeName       { get; init; } = "Sakura Yoru";
    [JsonPropertyName("applyOhMyPosh")]    public bool   ApplyOhMyPosh    { get; init; } = true;
    [JsonPropertyName("fontFace")]         public string FontFace         { get; init; } = "JetBrainsMono Nerd Font";
    [JsonPropertyName("fontSize")]         public int    FontSize         { get; init; } = 11;
    [JsonPropertyName("opacity")]          public int    Opacity          { get; init; } = 88;
    [JsonPropertyName("useAcrylic")]       public bool   UseAcrylic       { get; init; } = true;
}

public sealed class BootSettings
{
    [JsonPropertyName("hackBgrtEnabled")] public bool    HackBgrtEnabled { get; init; } = false;
    [JsonPropertyName("splashPath")]      public string? SplashPath      { get; init; }
}

public sealed class DependencyList
{
    [JsonPropertyName("required")] public string[] Required { get; init; } = [];
    [JsonPropertyName("optional")] public string[] Optional { get; init; } =
    [
        "Rainmeter.Rainmeter",
        "LGUG2Z.komorebi",
        "rocksdanister.LivelyWallpaper",
        "ModernFlyouts.ModernFlyouts",
        "Ramensoftware.Windhawk"
    ];
}

