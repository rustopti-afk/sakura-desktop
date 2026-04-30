using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sakura.Core.Profile;

public static class ProfileSerializer
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented            = true,
        PropertyNamingPolicy     = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition   = JsonIgnoreCondition.WhenWritingNull,
        Converters               = { new JsonStringEnumConverter() }
    };

    public static string Serialize(RiceProfile profile)
        => JsonSerializer.Serialize(profile, _opts);

    public static RiceProfile Deserialize(string json)
        => JsonSerializer.Deserialize<RiceProfile>(json, _opts)
           ?? throw new InvalidDataException("Profile JSON is null or empty");

    public static void SaveToFile(RiceProfile profile, string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, Serialize(profile));
    }

    public static RiceProfile LoadFromFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Profile file not found", path);
        return Deserialize(File.ReadAllText(path));
    }

    public static string GetDefaultProfileDir()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sakura", "profiles");

    public static IReadOnlyList<RiceProfile> LoadAll(string? dir = null)
    {
        string profileDir = dir ?? GetDefaultProfileDir();
        if (!Directory.Exists(profileDir)) return [];

        return Directory.GetFiles(profileDir, "*.json")
            .Select(f => { try { return LoadFromFile(f); } catch { return null; } })
            .Where(p => p != null)
            .Cast<RiceProfile>()
            .ToList();
    }
}
