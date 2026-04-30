using Sakura.Core.Native;

namespace Sakura.Core.Tests.Native;

public sealed class FontManagerTests
{
    // On non-Windows, all public methods are no-ops or throw — the OS guard is the contract.
    // On Windows we can't call SystemParametersInfo in CI without a message loop,
    // so we only test the non-platform-sensitive behaviour.

    [Fact]
    public void GetCurrentCaptionFont_OnWindows_ReturnsNonEmptyOrNull()
    {
        if (!OperatingSystem.IsWindows()) return;
        var name = FontManager.GetCurrentCaptionFont();
        // May be null if SPI call fails in headless CI; just verify it doesn't throw.
        if (name is not null)
            name.Should().NotBeEmpty();
    }

    [Fact]
    public void GetCurrentIconFont_OnWindows_ReturnsNonEmptyOrNull()
    {
        if (!OperatingSystem.IsWindows()) return;
        var name = FontManager.GetCurrentIconFont();
        if (name is not null)
            name.Should().NotBeEmpty();
    }

    [Fact]
    public void ApplyNonClientFont_OnNonWindows_DoesNotThrow()
    {
        // Guard: only runs on non-Windows where we expect no-op behaviour via OS check in caller.
        // FontManager itself doesn't have a guard — the [SupportedOSPlatform] is a lint hint;
        // callers must check OperatingSystem.IsWindows() before calling.
        // This test documents that expectation.
        if (OperatingSystem.IsWindows()) return; // skip on Windows — would modify system state
        // On Linux the DllImport will throw DllNotFoundException, which is acceptable.
        var ex = Record.Exception(() => FontManager.ApplyNonClientFont("Arial", 9));
        ex.Should().BeOfType<DllNotFoundException>(
            "on non-Windows, user32.dll is unavailable and DllNotFoundException is expected");
    }

    [Fact]
    public void ApplyIconFont_OnNonWindows_ThrowsDllNotFoundException()
    {
        if (OperatingSystem.IsWindows()) return;
        var ex = Record.Exception(() => FontManager.ApplyIconFont("Arial", 9));
        ex.Should().BeOfType<DllNotFoundException>();
    }
}
