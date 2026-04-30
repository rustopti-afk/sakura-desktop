using Sakura.Core.Integrations;
using Sakura.Core.Profile;

namespace Sakura.Core.Tests.Integrations;

public sealed class IconManagerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public IconManagerTests() => Directory.CreateDirectory(_tempDir);

    // ── ApplyProfile — non-Windows guard ──────────────────────────────────

    [Fact]
    public void ApplyProfile_OnNonWindows_DoesNotThrow()
    {
        if (OperatingSystem.IsWindows()) return;

        var settings = new IconSettings { Pack = "SakuraIcons", CursorPack = "SakuraCursors" };
        var act = () => IconManager.ApplyProfile(settings, _tempDir);
        act.Should().NotThrow();
    }

    [Fact]
    public void ApplyProfile_WhenAllFieldsNull_DoesNotThrow()
    {
        var settings = new IconSettings();
        var act = () => IconManager.ApplyProfile(settings, _tempDir);
        act.Should().NotThrow();
    }

    // ── ApplyShellIconPack ─────────────────────────────────────────────────

    [Fact]
    public void ApplyShellIconPack_OnNonWindows_IsNoOp()
    {
        if (OperatingSystem.IsWindows()) return;
        // Method is [SupportedOSPlatform("windows")]; calling it on Linux is guarded by ApplyProfile.
        // Verify that ApplyProfile with a Pack set doesn't throw even if dir doesn't exist.
        var settings = new IconSettings { Pack = "nonexistent" };
        var act = () => IconManager.ApplyProfile(settings, _tempDir);
        act.Should().NotThrow();
    }

    [Fact]
    public void ApplyShellIconPack_WithIcoFiles_WritesRegistryEntries()
    {
        if (!OperatingSystem.IsWindows()) return;

        // Create a minimal pack dir with a folder.ico stub
        string packDir = Path.Combine(_tempDir, "icons");
        Directory.CreateDirectory(packDir);
        File.WriteAllBytes(Path.Combine(packDir, "folder.ico"), CreateMinimalIcoBytes());
        File.WriteAllBytes(Path.Combine(packDir, "recycle_empty.ico"), CreateMinimalIcoBytes());

        IconManager.ApplyShellIconPack(packDir);

        using var key = Microsoft.Win32.Registry.CurrentUser
            .OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons");

        key.Should().NotBeNull();
        key!.GetValue("3")?.ToString().Should().EndWith("folder.ico");
        key!.GetValue("29")?.ToString().Should().EndWith("recycle_empty.ico");

        // Cleanup
        IconManager.RestoreDefaultShellIcons();
    }

    // ── ApplyCursorPack ────────────────────────────────────────────────────

    [Fact]
    public void ApplyCursorPack_PackDirDoesNotExist_IsNoOp()
    {
        var settings = new IconSettings { CursorPack = "NonExistentCursors" };
        var act = () => IconManager.ApplyProfile(settings, _tempDir);
        act.Should().NotThrow();
    }

    [Fact]
    public void ApplyCursorPack_WithCursorFiles_WritesRegistryEntries()
    {
        if (!OperatingSystem.IsWindows()) return;

        string cursorDir = Path.Combine(_tempDir, "cursors");
        Directory.CreateDirectory(cursorDir);
        File.WriteAllBytes(Path.Combine(cursorDir, "arrow.cur"), CreateMinimalCurBytes());
        File.WriteAllBytes(Path.Combine(cursorDir, "wait.ani"), CreateMinimalCurBytes());

        IconManager.ApplyCursorPack(cursorDir);

        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors");
        key.Should().NotBeNull();
        key!.GetValue("Arrow")?.ToString().Should().EndWith("arrow.cur");

        // Cleanup
        IconManager.RestoreDefaultCursors();
    }

    // ── PatchResourceIcons — non-Windows / missing files ──────────────────

    [Fact]
    public void PatchResourceIcons_MissingTarget_SkipsGracefully()
    {
        if (!OperatingSystem.IsWindows()) return;

        string packDir = Path.Combine(_tempDir, "pack");
        Directory.CreateDirectory(packDir);
        File.WriteAllBytes(Path.Combine(packDir, "nonexistent.ico"), CreateMinimalIcoBytes());

        // Should not throw even though the target file doesn't exist
        var act = () => IconManager.PatchResourceIcons(
            [@"C:\nonexistent\program.exe"],
            packDir);

        act.Should().NotThrow();
    }

    [Fact]
    public void PatchResourceIcons_NoMatchingIco_SkipsGracefully()
    {
        if (!OperatingSystem.IsWindows()) return;

        // target exists but there's no matching .ico in pack dir
        string packDir = Path.Combine(_tempDir, "emptypack");
        Directory.CreateDirectory(packDir);

        string target = Path.Combine(_tempDir, "dummy.exe");
        File.WriteAllBytes(target, [0x4D, 0x5A]); // MZ header stub

        var act = () => IconManager.PatchResourceIcons([target], packDir);
        act.Should().NotThrow();
    }

    // ── IconSettings defaults ──────────────────────────────────────────────

    [Fact]
    public void IconSettings_Defaults_AreNullOrEmpty()
    {
        var s = new IconSettings();
        s.Pack.Should().BeNull();
        s.CursorPack.Should().BeNull();
        s.PatchTargets.Should().BeEmpty();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    // Minimal valid .ico file: 1×1 pixel, 32-bit color
    private static byte[] CreateMinimalIcoBytes() =>
    [
        0x00, 0x00,             // reserved
        0x01, 0x00,             // type: icon
        0x01, 0x00,             // image count: 1
        0x01,                   // width: 1
        0x01,                   // height: 1
        0x00,                   // color count: 0 (32-bit)
        0x00,                   // reserved
        0x01, 0x00,             // planes
        0x20, 0x00,             // bit count: 32
        0x28, 0x00, 0x00, 0x00, // image data size
        0x16, 0x00, 0x00, 0x00, // image data offset (22 bytes)
        // BITMAPINFOHEADER (40 bytes)
        0x28, 0x00, 0x00, 0x00, // header size
        0x01, 0x00, 0x00, 0x00, // width: 1
        0x02, 0x00, 0x00, 0x00, // height: 2 (×2 for XOR+AND masks)
        0x01, 0x00,             // planes
        0x20, 0x00,             // bits per pixel: 32
        0x00, 0x00, 0x00, 0x00, // compression: none
        0x00, 0x00, 0x00, 0x00, // image size: 0
        0x00, 0x00, 0x00, 0x00, // x pixels per meter
        0x00, 0x00, 0x00, 0x00, // y pixels per meter
        0x00, 0x00, 0x00, 0x00, // colors used
        0x00, 0x00, 0x00, 0x00, // colors important
        // pixel data (4 bytes BGRA) + AND mask (4 bytes aligned)
        0xFF, 0x00, 0x80, 0xFF, // pixel: pink
        0x00, 0x00, 0x00, 0x00, // AND mask row
    ];

    private static byte[] CreateMinimalCurBytes() => CreateMinimalIcoBytes();

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
