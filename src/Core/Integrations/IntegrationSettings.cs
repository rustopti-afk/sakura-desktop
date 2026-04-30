using System.Text.Json.Serialization;

namespace Sakura.Core.Integrations;

public sealed class RainmeterSettings
{
    [JsonPropertyName("apply")]    public bool              Apply    { get; init; } = false;
    [JsonPropertyName("layout")]   public string?           Layout   { get; init; }
    [JsonPropertyName("skins")]    public RainmeterSkin[]   Skins    { get; init; } = [];
}

public sealed class RainmeterSkin
{
    [JsonPropertyName("config")]   public string Config   { get; init; } = "";
    [JsonPropertyName("variant")]  public string Variant  { get; init; } = "";
    [JsonPropertyName("enabled")]  public bool   Enabled  { get; init; } = true;
}

public sealed class WindhawkSettings
{
    [JsonPropertyName("apply")]  public bool           Apply { get; init; } = false;
    [JsonPropertyName("mods")]   public WindhawkMod[]  Mods  { get; init; } = [];
}

public sealed class WindhawkMod
{
    [JsonPropertyName("id")]      public string Id      { get; init; } = "";
    [JsonPropertyName("enabled")] public bool   Enabled { get; init; } = true;
}

public sealed class WmSettings
{
    [JsonPropertyName("engine")]        public string  Engine        { get; init; } = "none"; // none|komorebi|glazewm
    [JsonPropertyName("layout")]        public string  Layout        { get; init; } = "bsp";
    [JsonPropertyName("outerGap")]      public int     OuterGap      { get; init; } = 12;
    [JsonPropertyName("innerGap")]      public int     InnerGap      { get; init; } = 8;
    [JsonPropertyName("borderEnabled")] public bool    BorderEnabled { get; init; } = true;
    [JsonPropertyName("borderWidth")]   public int     BorderWidth   { get; init; } = 2;
    [JsonPropertyName("borderActive")]  public string  BorderActive  { get; init; } = "#E8A0BF";
    [JsonPropertyName("borderInactive")] public string BorderInactive { get; init; } = "#3E3E5E";
}

public sealed class LivelySettings
{
    [JsonPropertyName("apply")]         public bool    Apply         { get; init; } = false;
    [JsonPropertyName("wallpaperPath")] public string? WallpaperPath { get; init; }
    [JsonPropertyName("wallpaperType")] public string  WallpaperType { get; init; } = "video";
    [JsonPropertyName("monitor")]       public int?    Monitor       { get; init; }
    [JsonPropertyName("layout")]        public string  Layout        { get; init; } = "per-display";
}
