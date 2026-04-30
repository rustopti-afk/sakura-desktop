using FluentAssertions;
using Sakura.Core.Privilege;
using System.Security.Principal;

namespace Sakura.Core.Tests.Privilege;

public sealed class TrustedInstallerSessionTests
{
    [Fact]
    public void Constructor_DoesNotThrow()
    {
        var act = () => { using var s = new TrustedInstallerSession(); };
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var session = new TrustedInstallerSession();
        var act = () => { session.Dispose(); session.Dispose(); };
        act.Should().NotThrow();
    }

    [Fact]
    public void Acquire_OnNonWindows_ThrowsException()
    {
        if (OperatingSystem.IsWindows()) return; // real test only on Windows

        using var session = new TrustedInstallerSession();
        var act = () => session.Acquire();
        act.Should().Throw<Exception>("Win32 P/Invoke is not available on Linux");
    }
}
