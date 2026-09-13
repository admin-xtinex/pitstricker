#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Prepares this Windows Azure VM as the PitStriker GitHub Actions self-hosted builder.

.DESCRIPTION
  Installs Git, Git LFS, 7-Zip, Unity Hub, the GitHub Actions runner, and
  (optionally) Unity 6000.6.0f1 + Android Build Support.

  The workflow in .github/workflows/build-android-apk.yml expects:
    runs-on: [self-hosted, windows]
    Unity.exe at C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe

.PARAMETER GitHubToken
  A repo registration token from:
  https://github.com/admin-xtinex/pitstricker/settings/actions/runners/new
  Token expires in about 1 hour.

.PARAMETER SkipUnityEditor
  Install runner + tools only. Use this if you will install Unity Hub modules by hand.

.EXAMPLE
  Set-ExecutionPolicy Bypass -Scope Process -Force
  .\Setup-PitStrikerBuildRunner.ps1 -GitHubToken "AAAA..."
#>
param(
    [Parameter(Mandatory = $false)]
    [string]$GitHubToken,

    [Parameter(Mandatory = $false)]
    [string]$RepoUrl = "https://github.com/admin-xtinex/pitstricker",

    [Parameter(Mandatory = $false)]
    [string]$RunnerName = "azure-pitstriker-win",

    [Parameter(Mandatory = $false)]
    [switch]$SkipUnityEditor
)

$ErrorActionPreference = "Stop"
$UnityVersion = "6000.6.0f1"
$UnityExe = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
$HubExe = "C:\Program Files\Unity Hub\Unity Hub.exe"
$RunnerRoot = "C:\actions-runner"
$WorkRoot = "C:\pitstriker-work"

function Write-Step($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Write-Ok($msg) { Write-Host "  OK  $msg" -ForegroundColor Green }
function Write-Warn2($msg) { Write-Host "  WARN $msg" -ForegroundColor Yellow }

Write-Step "System checks"
$os = Get-CimInstance Win32_OperatingSystem
$ramGb = [math]::Round($os.TotalVisibleMemorySize / 1MB, 1)
$disk = Get-PSDrive C
$freeGb = [math]::Round($disk.Free / 1GB, 1)
Write-Host "  OS     : $($os.Caption) $($os.OSArchitecture)"
Write-Host "  RAM    : $ramGb GB"
Write-Host "  C: free: $freeGb GB"
if ($ramGb -lt 8) { Write-Warn2 "8 GB RAM is tight for Unity 6 Android. 16 GB+ recommended." }
if ($freeGb -lt 50) { Write-Warn2 "Unity 6 + Android modules need ~50-80 GB. Expand the OS disk if this is low." }

Write-Step "Enable long paths"
New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force | Out-Null
git config --system core.longpaths true 2>$null
Write-Ok "Long path support enabled"

Write-Step "Install Chocolatey if missing"
if (-not (Get-Command choco -ErrorAction SilentlyContinue)) {
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString("https://community.chocolatey.org/install.ps1"))
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
}
Write-Ok "choco ready"

Write-Step "Install Git, Git LFS, 7-Zip"
choco install git git-lfs 7zip -y --no-progress
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
git lfs install --system
Write-Ok "git $(git --version)"

Write-Step "Install Unity Hub"
if (-not (Test-Path $HubExe)) {
    choco install unity-hub -y --no-progress
}
if (Test-Path $HubExe) { Write-Ok "Unity Hub at $HubExe" } else { Write-Warn2 "Unity Hub not found after install. Install it from https://unity.com/download" }

if (-not $SkipUnityEditor) {
    Write-Step "Unity $UnityVersion + Android module"
    if (Test-Path $UnityExe) {
        Write-Ok "Editor already present: $UnityExe"
    } else {
        Write-Warn2 "Unity Hub must be signed in with a license on this VM before a headless editor install works."
        Write-Host "  1. Open Unity Hub, sign in, activate a Personal/Plus/Pro license."
        Write-Host "  2. Then run:"
        Write-Host "     & `"$HubExe`" -- --headless install --version $UnityVersion --module android --module windows-il2cpp --childModules"
        Write-Host "  If headless install fails, use Hub UI:"
        Write-Host "     Installs -> Install Editor -> $UnityVersion"
        Write-Host "     Modules: Android Build Support, OpenJDK, Android SDK & NDK Tools, Windows Build Support (IL2CPP)"
    }
}

Write-Step "Create work folders"
New-Item -ItemType Directory -Force -Path $WorkRoot | Out-Null
New-Item -ItemType Directory -Force -Path $RunnerRoot | Out-Null
Write-Ok $WorkRoot

Write-Step "GitHub Actions runner"
if (-not (Test-Path "$RunnerRoot\config.cmd")) {
    $ver = "2.328.0"
    $zip = "$RunnerRoot\actions-runner-win-x64-$ver.zip"
    $uri = "https://github.com/actions/runner/releases/download/v$ver/actions-runner-win-x64-$ver.zip"
    Write-Host "  Downloading $uri"
    Invoke-WebRequest -Uri $uri -OutFile $zip
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zip, $RunnerRoot)
    Remove-Item $zip -Force
    Write-Ok "Runner extracted to $RunnerRoot"
} else {
    Write-Ok "Runner already extracted"
}

if (-not $GitHubToken) {
    Write-Warn2 "No -GitHubToken passed. Generate one (expires ~1h) at:"
    Write-Host "  $RepoUrl/settings/actions/runners/new"
    Write-Host "  Then rerun:"
    Write-Host "  .\Setup-PitStrikerBuildRunner.ps1 -GitHubToken YOUR_TOKEN"
} elseif (-not (Test-Path "$RunnerRoot\.runner")) {
    Push-Location $RunnerRoot
    try {
        & .\config.cmd --unattended --url $RepoUrl --token $GitHubToken --name $RunnerName --labels "self-hosted,windows" --work "$WorkRoot" --runasservice
        & .\svc.cmd install
        & .\svc.cmd start
        Write-Ok "Runner registered as Windows service: $RunnerName"
    } finally {
        Pop-Location
    }
} else {
    Write-Ok "Runner already configured. Starting service if needed."
    Push-Location $RunnerRoot
    try { & .\svc.cmd start } catch { }
    Pop-Location
}

Write-Step "Final checklist"
$unityOk = Test-Path $UnityExe
$runnerOk = Test-Path "$RunnerRoot\.runner"
Write-Host ("  Unity {0,-8} {1}" -f $UnityVersion, $(if ($unityOk) { "FOUND" } else { "MISSING - install via Hub" }))
Write-Host ("  Runner config     {0}" -f $(if ($runnerOk) { "FOUND" } else { "MISSING - pass -GitHubToken" }))
Write-Host ("  Expected Unity    {0}" -f $UnityExe)
Write-Host ""
Write-Host "After Unity is installed and the runner is Online in GitHub:"
Write-Host "  Repo -> Actions -> Build and Upload Android APK -> Run workflow"
Write-Host "  Branch: dev-isotrophic"
Write-Host "  Runner type: self-hosted (Windows PC)"
Write-Host ""
Write-Host "Security: restrict Azure NSG inbound 3389 to your home/office IP. Do not leave RDP open to 0.0.0.0/0."
