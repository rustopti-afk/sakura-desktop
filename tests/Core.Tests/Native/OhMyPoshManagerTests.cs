using Sakura.Core.Native;

namespace Sakura.Core.Tests.Native;

public sealed class OhMyPoshManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _appData;
    private readonly string _docs;

    public OhMyPoshManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _appData = Path.Combine(_tempDir, "AppData");
        _docs    = Path.Combine(_tempDir, "Documents");
        Directory.CreateDirectory(_appData);
        Directory.CreateDirectory(_docs);
    }

    [Fact]
    public async Task DeploySakuraThemeAsync_CreatesOmpYamlFile()
    {
        await OhMyPoshManager.DeploySakuraThemeAsync(
            appDataOverride: _appData, documentsOverride: _docs);

        string configPath = Path.Combine(_appData, "Sakura", "omp", "sakura.omp.yaml");
        File.Exists(configPath).Should().BeTrue();
    }

    [Fact]
    public async Task DeploySakuraThemeAsync_YamlContainsSakuraColors()
    {
        await OhMyPoshManager.DeploySakuraThemeAsync(
            appDataOverride: _appData, documentsOverride: _docs);

        string yaml = await File.ReadAllTextAsync(
            Path.Combine(_appData, "Sakura", "omp", "sakura.omp.yaml"));

        yaml.Should().Contain("#E8A0BF");
        yaml.Should().Contain("#A1C181");
        yaml.Should().Contain("version: 2");
    }

    [Fact]
    public async Task DeploySakuraThemeAsync_YamlContainsGitSegment()
    {
        await OhMyPoshManager.DeploySakuraThemeAsync(
            appDataOverride: _appData, documentsOverride: _docs);

        string yaml = await File.ReadAllTextAsync(
            Path.Combine(_appData, "Sakura", "omp", "sakura.omp.yaml"));

        yaml.Should().Contain("type: git");
        yaml.Should().Contain("fetch_status: true");
    }

    [Fact]
    public async Task DeploySakuraThemeAsync_AppendsInitLine_ToPsProfile()
    {
        string psDir     = Path.Combine(_docs, "PowerShell");
        string psProfile = Path.Combine(psDir, "Microsoft.PowerShell_profile.ps1");
        Directory.CreateDirectory(psDir);
        await File.WriteAllTextAsync(psProfile, "# existing content\nImport-Module posh-git\n");

        await OhMyPoshManager.DeploySakuraThemeAsync(
            appDataOverride: _appData, documentsOverride: _docs);

        string content = await File.ReadAllTextAsync(psProfile);
        content.Should().Contain("oh-my-posh init pwsh");
        content.Should().Contain("# existing content");
    }

    [Fact]
    public async Task DeploySakuraThemeAsync_ReplacesExistingInitLine_NotDuplicate()
    {
        string psDir     = Path.Combine(_docs, "PowerShell");
        string psProfile = Path.Combine(psDir, "Microsoft.PowerShell_profile.ps1");
        Directory.CreateDirectory(psDir);
        await File.WriteAllTextAsync(psProfile,
            "oh-my-posh init pwsh --config \"old-config.yaml\" | Invoke-Expression\n");

        await OhMyPoshManager.DeploySakuraThemeAsync(
            appDataOverride: _appData, documentsOverride: _docs);

        string content = await File.ReadAllTextAsync(psProfile);
        int count = content.Split("oh-my-posh init pwsh", StringSplitOptions.None).Length - 1;
        count.Should().Be(1, "init line must be replaced, not appended a second time");
        content.Should().NotContain("old-config.yaml");
    }

    [Fact]
    public async Task DeploySakuraThemeAsync_CreatesProfileFile_WhenMissing()
    {
        string psProfile = Path.Combine(_docs, "PowerShell", "Microsoft.PowerShell_profile.ps1");

        await OhMyPoshManager.DeploySakuraThemeAsync(
            appDataOverride: _appData, documentsOverride: _docs);

        File.Exists(psProfile).Should().BeTrue();
        string content = await File.ReadAllTextAsync(psProfile);
        content.Should().Contain("oh-my-posh init pwsh");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
