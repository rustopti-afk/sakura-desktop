namespace Sakura.Core.Native;

public static class OhMyPoshManager
{
    public static async Task DeploySakuraThemeAsync(
        CancellationToken ct = default,
        string? appDataOverride = null,
        string? documentsOverride = null)
    {
        string appData  = appDataOverride  ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string docs     = documentsOverride ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string configDir  = Path.Combine(appData, "Sakura", "omp");
        string configPath = Path.Combine(configDir, "sakura.omp.yaml");

        Directory.CreateDirectory(configDir);
        await File.WriteAllTextAsync(configPath, SakuraOmpYaml, ct).ConfigureAwait(false);
        await RegisterInPowerShellProfilesAsync(configPath, docs, ct).ConfigureAwait(false);
    }

    private static async Task RegisterInPowerShellProfilesAsync(string configPath, string docs, CancellationToken ct)
    {
        string initLine = $@"oh-my-posh init pwsh --config ""{configPath.Replace(@"\", @"\\")}"" | Invoke-Expression";

        foreach (string profilePath in GetPowerShellProfiles(docs))
        {
            string? dir = Path.GetDirectoryName(profilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string existing = File.Exists(profilePath)
                ? await File.ReadAllTextAsync(profilePath, ct).ConfigureAwait(false)
                : string.Empty;

            if (existing.Contains("oh-my-posh init pwsh"))
            {
                var lines = existing.Split('\n').ToList();
                int idx = lines.FindIndex(l => l.TrimStart().StartsWith("oh-my-posh init pwsh", StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) lines[idx] = initLine;
                await File.WriteAllTextAsync(profilePath, string.Join('\n', lines), ct).ConfigureAwait(false);
            }
            else
            {
                string appended = existing.TrimEnd() + Environment.NewLine + initLine + Environment.NewLine;
                await File.WriteAllTextAsync(profilePath, appended, ct).ConfigureAwait(false);
            }
        }
    }

    private static IEnumerable<string> GetPowerShellProfiles(string docs)
    {
        yield return Path.Combine(docs, "PowerShell", "Microsoft.PowerShell_profile.ps1");
        yield return Path.Combine(docs, "PowerShell", "Profile.ps1");
        yield return Path.Combine(docs, "WindowsPowerShell", "Microsoft.PowerShell_profile.ps1");
    }

    private const string SakuraOmpYaml = """
version: 2
final_space: true
console_title_template: '{{ .Folder }}'
blocks:
  - type: prompt
    alignment: left
    segments:
      - type: text
        style: plain
        foreground: '#E8A0BF'
        template: ' '
      - type: path
        style: powerline
        powerline_symbol: ''
        foreground: '#0D0F14'
        background: '#E8A0BF'
        properties:
          style: agnoster_short
          max_depth: 3
          home_icon: '~'
        template: ' {{ .Path }} '
      - type: git
        style: powerline
        powerline_symbol: ''
        foreground: '#0D0F14'
        background: '#A1C181'
        background_templates:
          - '{{ if or (.Working.Changed) (.Staging.Changed) }}#F4C28B{{ end }}'
          - '{{ if and (gt .Ahead 0) (gt .Behind 0) }}#E36F8E{{ end }}'
          - '{{ if gt .Ahead 0 }}#7AA2D9{{ end }}'
          - '{{ if gt .Behind 0 }}#C792E5{{ end }}'
        properties:
          fetch_status: true
          fetch_upstream_icon: true
        template: '  {{ .HEAD }}{{ if .Working.Changed }} *{{ end }}{{ if gt .Ahead 0 }} ↑{{ .Ahead }}{{ end }}{{ if gt .Behind 0 }} ↓{{ .Behind }}{{ end }} '
  - type: prompt
    alignment: right
    segments:
      - type: executiontime
        style: plain
        foreground: '#3E3E5E'
        properties:
          threshold: 500
          style: austin
        template: ' {{ .FormattedMs }} '
      - type: time
        style: plain
        foreground: '#7AA2D9'
        properties:
          time_format: '15:04'
        template: ' {{ .CurrentDate | date .Format }}'
  - type: prompt
    alignment: left
    newline: true
    segments:
      - type: text
        style: plain
        foreground: '#E8A0BF'
        template: '❯ '
""";
}
