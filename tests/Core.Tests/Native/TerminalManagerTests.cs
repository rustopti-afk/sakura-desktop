using Sakura.Core.Native;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sakura.Core.Tests.Native;

public sealed class TerminalManagerTests : IDisposable
{
    private readonly string _tempDir    = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly string _localApp;
    private readonly string _appData;

    public TerminalManagerTests()
    {
        _localApp = Path.Combine(_tempDir, "LocalAppData");
        _appData  = Path.Combine(_tempDir, "AppData");
        Directory.CreateDirectory(_localApp);
        Directory.CreateDirectory(_appData);
    }

    [Fact]
    public async Task ApplyColorSchemeAsync_WritesStagingFile_WhenWtNotInstalled()
    {
        await TerminalManager.ApplyColorSchemeAsync(
            "Sakura Yoru", "Consolas", 12, 90, true,
            localAppDataOverride: _localApp, appDataOverride: _appData);

        string staging = Path.Combine(_appData, "Sakura", "pending", "wt_settings_overlay.json");
        File.Exists(staging).Should().BeTrue();
    }

    [Fact]
    public async Task ApplyColorSchemeAsync_StagingJson_ContainsSchemeName()
    {
        await TerminalManager.ApplyColorSchemeAsync(
            "Sakura Yoru", "Consolas", 12, 90, true,
            localAppDataOverride: _localApp, appDataOverride: _appData);

        string staging = Path.Combine(_appData, "Sakura", "pending", "wt_settings_overlay.json");
        string json    = await File.ReadAllTextAsync(staging);

        json.Should().Contain("Sakura Yoru");
        json.Should().Contain("#1B1B2F");
        json.Should().Contain("#E8DFE8");
    }

    [Fact]
    public async Task ApplyColorSchemeAsync_StagingJson_IsValidJson()
    {
        await TerminalManager.ApplyColorSchemeAsync(
            "Sakura Yoru", "Consolas", 12, 90, false,
            localAppDataOverride: _localApp, appDataOverride: _appData);

        string staging = Path.Combine(_appData, "Sakura", "pending", "wt_settings_overlay.json");
        string json    = await File.ReadAllTextAsync(staging);

        var act = () => JsonDocument.Parse(json);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task ApplyColorSchemeAsync_ExistingWtSettings_InjectsSchemeAndDefaults()
    {
        string wtDir  = Path.Combine(_localApp, "Microsoft", "Windows Terminal");
        string wtPath = Path.Combine(wtDir, "settings.json");
        Directory.CreateDirectory(wtDir);
        await File.WriteAllTextAsync(wtPath, """{"defaultProfile":"{}","profiles":{},"schemes":[]}""");

        await TerminalManager.ApplyColorSchemeAsync(
            "Sakura Yoru", "JetBrainsMono Nerd Font", 12, 85, true,
            localAppDataOverride: _localApp, appDataOverride: _appData);

        string result  = await File.ReadAllTextAsync(wtPath);
        var root       = JsonNode.Parse(result) as JsonObject;
        var schemes    = root!["schemes"] as JsonArray;
        var defaults   = root["profiles"]!["defaults"] as JsonObject;

        schemes.Should().NotBeNull();
        schemes!.Count.Should().Be(1);
        schemes[0]!["name"]!.GetValue<string>().Should().Be("Sakura Yoru");
        defaults.Should().NotBeNull();
        defaults!["colorScheme"]!.GetValue<string>().Should().Be("Sakura Yoru");
        defaults["opacity"]!.GetValue<int>().Should().Be(85);
        defaults["useAcrylic"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task ApplyColorSchemeAsync_ReplacesExistingScheme_WithSameName()
    {
        string wtDir  = Path.Combine(_localApp, "Microsoft", "Windows Terminal");
        string wtPath = Path.Combine(wtDir, "settings.json");
        Directory.CreateDirectory(wtDir);
        await File.WriteAllTextAsync(wtPath,
            """{"schemes":[{"name":"Sakura Yoru","background":"#000000"}],"profiles":{}}""");

        await TerminalManager.ApplyColorSchemeAsync(
            "Sakura Yoru", "Consolas", 12, 90, false,
            localAppDataOverride: _localApp, appDataOverride: _appData);

        string result  = await File.ReadAllTextAsync(wtPath);
        var root       = JsonNode.Parse(result) as JsonObject;
        var schemes    = root!["schemes"] as JsonArray;

        schemes!.Count.Should().Be(1, "duplicate scheme should be replaced, not appended");
        schemes[0]!["background"]!.GetValue<string>().Should().Be("#1B1B2F");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
