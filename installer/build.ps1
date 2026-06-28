<#
.SYNOPSIS
  Builds the Sentinel CMS Windows installer (SentinelSetup-<version>.exe).

.DESCRIPTION
  1. Publishes DigitalSignage.CMS in Release mode (framework-dependent, win-x64).
  2. Copies the vendored FFmpeg binaries into the publish output so the
     installed app never needs internet access to fetch them at first run.
  3. Compiles installer\sentinel.iss with Inno Setup (ISCC.exe) into
     installer\output\SentinelSetup-<version>.exe.

.PARAMETER Version
  Version number to stamp on the installer and the app. Defaults to 1.0.0.
#>
param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$installerDir = Join-Path $root "installer"
$stagingDir = Join-Path $installerDir "staging"
$vendorFfmpeg = Join-Path $installerDir "vendor\ffmpeg-bin"
$cmsProject = Join-Path $root "DigitalSignage.CMS\DigitalSignage.CMS.csproj"

function Find-Iscc {
    $candidates = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )
    foreach ($path in $candidates) {
        if (Test-Path $path) { return $path }
    }
    throw "ISCC.exe (Inno Setup compiler) not found. Install Inno Setup 6 first: winget install JRSoftware.InnoSetup"
}

Write-Host "==> Publishing DigitalSignage.CMS (Release, win-x64)..." -ForegroundColor Cyan
if (Test-Path $stagingDir) {
    Remove-Item $stagingDir -Recurse -Force
}
dotnet publish $cmsProject -c Release -r win-x64 --self-contained false -o $stagingDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Write-Host "==> Staging FFmpeg binaries (so the installed app works without internet on first run)..." -ForegroundColor Cyan
if (-not (Test-Path $vendorFfmpeg)) {
    throw "FFmpeg binaries not found at $vendorFfmpeg - see installer/vendor/README.md to fetch them."
}
$destFfmpeg = Join-Path $stagingDir "ffmpeg-bin"
New-Item -ItemType Directory -Force -Path $destFfmpeg | Out-Null
Copy-Item "$vendorFfmpeg\*" $destFfmpeg -Force

Write-Host "==> Compiling installer with Inno Setup..." -ForegroundColor Cyan
$iscc = Find-Iscc
$outputDir = Join-Path $installerDir "output"
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

& $iscc "/DAppVersion=$Version" "/DStagingDir=$stagingDir" (Join-Path $installerDir "sentinel.iss")
if ($LASTEXITCODE -ne 0) { throw "ISCC.exe failed." }

Write-Host "==> Done: $outputDir\SentinelSetup-$Version.exe" -ForegroundColor Green
