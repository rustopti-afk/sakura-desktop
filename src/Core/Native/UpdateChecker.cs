using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Sakura.Core.Native;

public sealed record UpdateInfo(
    string CurrentVersion,
    string LatestVersion,
    bool   IsUpdateAvailable,
    string ReleasePageUrl,
    string ReleaseNotes);

public static class UpdateChecker
{
    private const string ApiUrl    = "https://api.github.com/repos/brobots-school-ua/georgiys-project/releases/latest";
    private const string UserAgent = "SakuraDesktop/1.0 (update-check)";

    // ── Public API ─────────────────────────────────────────────────────────

    public static Version GetCurrentVersion()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return asm.GetName().Version ?? new Version(0, 0, 0);
    }

    /// <summary>
    /// Queries GitHub Releases for the latest tag and compares with the
    /// running assembly version. Returns null if the check fails (network
    /// error, rate limit, offline) — caller should treat null as "unknown".
    /// </summary>
    public static async Task<UpdateInfo?> CheckAsync(
        HttpClient? httpClient = null,
        CancellationToken ct   = default)
    {
        bool ownsClient = httpClient is null;
        httpClient ??= new HttpClient();

        try
        {
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);

            var release = await httpClient
                .GetFromJsonAsync<GitHubRelease>(ApiUrl, ct)
                .ConfigureAwait(false);

            if (release is null) return null;

            string tag = release.TagName.TrimStart('v', 'V');
            if (!Version.TryParse(tag, out var latest)) return null;

            var current = GetCurrentVersion();
            return new UpdateInfo(
                CurrentVersion:    current.ToString(3),
                LatestVersion:     latest.ToString(3),
                IsUpdateAvailable: latest > current,
                ReleasePageUrl:    release.HtmlUrl,
                ReleaseNotes:      release.Body ?? "");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                     or OperationCanceledException or InvalidOperationException
                                     or System.Text.Json.JsonException)
        {
            return null;
        }
        finally
        {
            if (ownsClient) httpClient.Dispose();
        }
    }

    // ── GitHub API response model ──────────────────────────────────────────

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]   public string TagName { get; init; } = "";
        [JsonPropertyName("html_url")]   public string HtmlUrl { get; init; } = "";
        [JsonPropertyName("body")]       public string? Body   { get; init; }
        [JsonPropertyName("prerelease")] public bool Prerelease { get; init; }
        [JsonPropertyName("draft")]      public bool Draft      { get; init; }
    }
}
