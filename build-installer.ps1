#Requires -Version 7
<#
.SYNOPSIS
  Builds all Sakura Desktop projects and produces a WiX installer bundle.
.DESCRIPTION
  1. Restores NuGet packages
  2. Publishes App, Helper, Helper.TI, Watchdog (win-x64 self-contained)
  3. Builds WiX MSI (Product.wxs) -> publish\msi\Sakura.msi
  4. Builds WiX Bundle (Bundle.wxs) -> publish\SakuraDesktopSetup.exe
.EXAMPLE
  .\build-installer.ps1
  .\build-installer.ps1 -Configuration Release -Version 0.3.0
#>
param(
    [string] $Configuration = "Release",
    [string] $Version        = "0.2.0",
    [switch] $SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = $PSScriptRoot
$publish = Join-Path $root "publish"

function Step([string]$msg) {
    Write-Host "`n[$([datetime]::Now.ToString('HH:mm:ss'))] $msg" -ForegroundColor Cyan
}

function Exec([string]$cmd, [string[]]$args) {
    & $cmd @args
    if ($LASTEXITCODE -ne 0) { throw "Command failed ($LASTEXITCODE): $cmd $args" }
}

# ── 1. Restore ────────────────────────────────────────────────────────────────
Step "Restoring NuGet packages"
Exec dotnet @("restore", "$root\SakuraDesktop.sln")

# ── 2. Tests ──────────────────────────────────────────────────────────────────
if (-not $SkipTests) {
    Step "Running tests"
    Exec dotnet @("test", "$root\SakuraDesktop.sln", "-c", $Configuration,
                  "--logger", "console;verbosity=minimal",
                  "--no-restore")
}

# ── 3. Publish all executables ────────────────────────────────────────────────
$publishArgs = @(
    "-c", $Configuration,
    "-r", "win-x64",
    "--self-contained", "false",
    "--no-restore",
    "-p:Version=$Version"
)

$projects = @(
    @{ Csproj = "src\App\App.csproj";           Out = "app"       },
    @{ Csproj = "src\Helper\Helper.csproj";     Out = "helper"    },
    @{ Csproj = "src\Helper.TI\Helper.TI.csproj"; Out = "helper-ti" },
    @{ Csproj = "src\Watchdog\Watchdog.csproj"; Out = "watchdog"  }
)

foreach ($p in $projects) {
    Step "Publishing $($p.Out)"
    $outDir = Join-Path $publish $p.Out
    Exec dotnet @("publish", (Join-Path $root $p.Csproj), @publishArgs,
                  "-o", $outDir)
}

# ── 4. WiX toolcheck ─────────────────────────────────────────────────────────
Step "Checking WiX toolset"
$wixCmd = Get-Command wix -ErrorAction SilentlyContinue
if (-not $wixCmd) {
    Write-Host "  WiX not found — installing..." -ForegroundColor Yellow
    Exec dotnet @("tool", "install", "--global", "wix", "--version", "4.*")
}
Exec wix @("--version")

# WiX extensions needed
Exec wix @("extension", "add", "-g", "WixToolset.Util.wixext/4.*")
Exec wix @("extension", "add", "-g", "WixToolset.Bal.wixext/4.*")
Exec wix @("extension", "add", "-g", "WixToolset.Firewall.wixext/4.*")
Exec wix @("extension", "add", "-g", "WixToolset.UI.wixext/4.*")

# ── 5. Build MSI ─────────────────────────────────────────────────────────────
Step "Building MSI package"
$msiDir = Join-Path $publish "msi"
New-Item -ItemType Directory -Path $msiDir -Force | Out-Null

Exec wix @(
    "build",
    (Join-Path $root "setup\Product.wxs"),
    "-ext", "WixToolset.Util.wixext",
    "-ext", "WixToolset.UI.wixext",
    "-ext", "WixToolset.Firewall.wixext",
    "-d", "Version=$Version",
    "-o", (Join-Path $msiDir "Sakura.msi")
)

# ── 6. Build Bundle ───────────────────────────────────────────────────────────
Step "Building bootstrapper bundle"
Exec wix @(
    "build",
    (Join-Path $root "setup\Bundle.wxs"),
    "-ext", "WixToolset.Bal.wixext",
    "-ext", "WixToolset.Util.wixext",
    "-d", "Version=$Version",
    "-o", (Join-Path $publish "SakuraDesktopSetup.exe")
)

# ── Done ──────────────────────────────────────────────────────────────────────
Step "Build complete"
Write-Host ""
Write-Host "  MSI:    $msiDir\Sakura.msi" -ForegroundColor Green
Write-Host "  Bundle: $publish\SakuraDesktopSetup.exe" -ForegroundColor Green
Write-Host ""
