using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Sakura.Core.Privilege;

public sealed class TrustedInstallerSession : IDisposable
{
    // ── P/Invoke declarations ─────────────────────────────────────────────

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out SafeAccessTokenHandle tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LookupPrivilegeValueW(string? systemName, string name, out LUID luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(SafeAccessTokenHandle tokenHandle, bool disableAllPrivileges, ref TOKEN_PRIVILEGES newState, int bufferLength, IntPtr previousState, IntPtr returnLength);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool ImpersonateLoggedOnUser(SafeAccessTokenHandle token);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool RevertToSelf();

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DuplicateTokenEx(SafeAccessTokenHandle existingToken, uint desiredAccess, IntPtr tokenAttributes, int impersonationLevel, int tokenType, out SafeAccessTokenHandle newToken);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr OpenSCManagerW(string? machineName, string? databaseName, uint dwDesiredAccess);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr OpenServiceW(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool StartServiceW(IntPtr hService, int dwNumServiceArgs, IntPtr lpServiceArgVectors);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool QueryServiceStatusEx(IntPtr hService, int infoLevel, out SERVICE_STATUS_PROCESS lpBuffer, int cbBufSize, out int pcbBytesNeeded);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CloseServiceHandle(IntPtr hSCObject);

    // ── Structs ───────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID { public uint LowPart; public int HighPart; }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID_AND_ATTRIBUTES { public LUID Luid; public uint Attributes; }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public LUID_AND_ATTRIBUTES Privileges0; }

    [StructLayout(LayoutKind.Sequential)]
    private struct SERVICE_STATUS_PROCESS
    {
        public uint dwServiceType, dwCurrentState, dwControlsAccepted, dwWin32ExitCode;
        public uint dwServiceSpecificExitCode, dwCheckPoint, dwWaitHint, dwProcessId, dwServiceFlags;
    }

    // ── Constants ─────────────────────────────────────────────────────────

    private const uint TOKEN_ALL_ACCESS        = 0x000F01FF;
    private const uint MAXIMUM_ALLOWED         = 0x02000000;
    private const uint SC_MANAGER_ALL_ACCESS   = 0xF003F;
    private const uint SERVICE_ALL_ACCESS      = 0xF01FF;
    private const uint PROCESS_ALL_ACCESS      = 0x1FFFFF;
    private const uint SE_PRIVILEGE_ENABLED    = 2;
    private const int  SecurityImpersonation   = 2;
    private const int  TokenImpersonation      = 2;
    private const int  SERVICE_RUNNING         = 4;

    // ── State ─────────────────────────────────────────────────────────────

    private SafeAccessTokenHandle? _tiToken;
    private bool _impersonating;
    private bool _disposed;

    // ── Public API ────────────────────────────────────────────────────────

    public void Acquire()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnableSelfPrivilege("SeDebugPrivilege");
        EnableSelfPrivilege("SeImpersonatePrivilege");
        EnableSelfPrivilege("SeAssignPrimaryTokenPrivilege");

        uint tiPid = StartTrustedInstallerService();

        IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, tiPid);
        if (hProcess == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcess(TrustedInstaller)");

        try
        {
            if (!OpenProcessToken(hProcess, TOKEN_ALL_ACCESS, out SafeAccessTokenHandle primaryToken))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcessToken(TrustedInstaller)");

            using (primaryToken)
            {
                if (!DuplicateTokenEx(primaryToken, MAXIMUM_ALLOWED, IntPtr.Zero, SecurityImpersonation, TokenImpersonation, out SafeAccessTokenHandle impersonationToken))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "DuplicateTokenEx");

                _tiToken = impersonationToken;
            }
        }
        finally
        {
            CloseHandle(hProcess);
        }

        if (!ImpersonateLoggedOnUser(_tiToken))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "ImpersonateLoggedOnUser");

        _impersonating = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_impersonating)
        {
            RevertToSelf();
            _impersonating = false;
        }

        _tiToken?.Dispose();
        _tiToken = null;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static uint StartTrustedInstallerService()
    {
        IntPtr hScm = OpenSCManagerW(null, null, SC_MANAGER_ALL_ACCESS);
        if (hScm == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenSCManager");

        try
        {
            IntPtr hSvc = OpenServiceW(hScm, "TrustedInstaller", SERVICE_ALL_ACCESS);
            if (hSvc == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenService(TrustedInstaller)");

            try
            {
                StartServiceW(hSvc, 0, IntPtr.Zero); // ERROR_SERVICE_ALREADY_RUNNING(1056) is acceptable

                const int maxWaitMs  = 10_000;
                const int pollMs     = 100;
                int waited = 0;

                while (waited < maxWaitMs)
                {
                    if (!QueryServiceStatusEx(hSvc, 0, out SERVICE_STATUS_PROCESS status, Marshal.SizeOf<SERVICE_STATUS_PROCESS>(), out _))
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "QueryServiceStatusEx");

                    if (status.dwCurrentState == SERVICE_RUNNING)
                        return status.dwProcessId;

                    Thread.Sleep(pollMs);
                    waited += pollMs;
                }

                throw new TimeoutException("TrustedInstaller service did not reach RUNNING within 10 seconds");
            }
            finally { CloseServiceHandle(hSvc); }
        }
        finally { CloseServiceHandle(hScm); }
    }

    private static void EnableSelfPrivilege(string privilegeName)
    {
        if (!OpenProcessToken(System.Diagnostics.Process.GetCurrentProcess().Handle, TOKEN_ALL_ACCESS, out SafeAccessTokenHandle hToken))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcessToken(self)");

        using (hToken)
        {
            if (!LookupPrivilegeValueW(null, privilegeName, out LUID luid))
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"LookupPrivilegeValue({privilegeName})");

            var tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privileges0    = new LUID_AND_ATTRIBUTES { Luid = luid, Attributes = SE_PRIVILEGE_ENABLED }
            };

            if (!AdjustTokenPrivileges(hToken, false, ref tp, Marshal.SizeOf<TOKEN_PRIVILEGES>(), IntPtr.Zero, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"AdjustTokenPrivileges({privilegeName})");

            int lastErr = Marshal.GetLastWin32Error();
            if (lastErr == 1300) // ERROR_NOT_ALL_ASSIGNED
                throw new UnauthorizedAccessException($"Privilege {privilegeName} is not held — process must be elevated (run as Administrator).");
        }
    }
}
