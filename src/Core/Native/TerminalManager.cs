using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sakura.Core.Native;

public static class TerminalManager
{
    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public static async Task ApplyColorSchemeAsync(
        string schemeName,
        string fontFace,
        int fontSize,
        int opacity,
        bool useAcrylic,
        CancellationToken ct = default,
        string? localAppDataOverride = null,
        string? appDataOverride = null)
    {
        string localApp = localAppDataOverride
            ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appData = appDataOverride
            ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        string? path = GetSettingsPath(localApp);
        if (path is null)
        {
            string staging = Path.Combine(appData, "Sakura", "pending", "wt_settings_overlay.json");
            Directory.CreateDirectory(Path.GetDirectoryName(staging)!);
            await File.WriteAllTextAsync(staging, BuildSchemeJson(schemeName), ct)
                .ConfigureAwait(false);
            return;
        }

        string json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        var root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip })
                   as JsonObject
                   ?? throw new InvalidDataException("Windows Terminal settings.json is not a JSON object");

        if (root["schemes"] is not JsonArray schemes)
        {
            schemes = new JsonArray();
            root["schemes"] = schemes;
        }

        for (int i = schemes.Count - 1; i >= 0; i--)
        {
            if (schemes[i]?["name"]?.GetValue<string>() == schemeName)
                schemes.RemoveAt(i);
        }

        schemes.Add(JsonNode.Parse(BuildSchemeJson(schemeName)));

        if (root["profiles"] is not JsonObject profiles)
        {
            profiles = new JsonObject();
            root["profiles"] = profiles;
        }
        if (profiles["defaults"] is not JsonObject defaults)
        {
            defaults = new JsonObject();
            profiles["defaults"] = defaults;
        }

        defaults["colorScheme"]      = schemeName;
        defaults["font"]             = JsonNode.Parse($"{{\"face\":\"{fontFace}\",\"size\":{fontSize}}}");
        defaults["opacity"]          = opacity;
        defaults["useAcrylic"]       = useAcrylic;
        defaults["padding"]          = "12, 12, 12, 12";
        defaults["antialiasingMode"] = "cleartype";

        await File.WriteAllTextAsync(path, root.ToJsonString(_opts), ct).ConfigureAwait(false);
    }

    private static string? GetSettingsPath(string localApp)
    {
        string packaged   = Path.Combine(localApp, "Packages", "Microsoft.WindowsTerminal_8wekyb3d8bbwe", "LocalState", "settings.json");
        string unpackaged = Path.Combine(localApp, "Microsoft", "Windows Terminal", "settings.json");
        if (File.Exists(packaged))   return packaged;
        if (File.Exists(unpackaged)) return unpackaged;
        return null;
    }

    private static string BuildSchemeJson(string name)
        => $$"""
            {
                "name": "{{name}}",
                "background": "#1B1B2F",
                "foreground": "#E8DFE8",
                "cursorColor": "#FFB7C5",
                "selectionBackground": "#FFB7C5",
                "black": "#1B1B2F",
                "red": "#E36F8E",
                "green": "#A1C181",
                "yellow": "#F4C28B",
                "blue": "#7AA2D9",
                "purple": "#C792E5",
                "cyan": "#89DDFF",
                "white": "#E8DFE8",
                "brightBlack": "#3E3E5E",
                "brightRed": "#FF8FAB",
                "brightGreen": "#B5D99C",
                "brightYellow": "#FFD9A0",
                "brightBlue": "#9DBEEC",
                "brightPurple": "#DDB6F2",
                "brightCyan": "#A4F1FF",
                "brightWhite": "#FFFFFF"
            }
            """;
}
