#Requires -Version 5.1
<#
.SYNOPSIS
  Finds Blender on Windows and generates the Kerala village arena FBX for Unity.

.DESCRIPTION
  Blender is usually not on PATH, so `blender` fails in PowerShell.
  This script locates blender.exe, runs generate_arena.py headless, and
  writes the FBX into the real Unity project folder.

.EXAMPLE
  Right-click Run-GenerateVillageArena.cmd -> Run
  or in PowerShell:
    Set-ExecutionPolicy Bypass -Scope Process -Force
    .\Tools\Blender\Run-GenerateVillageArena.ps1
#>
[CmdletBinding()]
param(
    [string]$Map = "rough_soil",
    [double]$PitSpacing = 12,
    [double]$PitRadius = 0.18
)

$ErrorActionPreference = "Stop"

function Write-Step($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Write-Ok($msg) { Write-Host "  OK  $msg" -ForegroundColor Green }
function Write-Fail($msg) { Write-Host "  FAIL $msg" -ForegroundColor Red }

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..\..")).Path
$Generator = Join-Path $ScriptDir "generate_arena.py"
$UnityFbxDir = Join-Path $RepoRoot "pitstricker\Assets\_Project\Art\Environments\Village\Generated"
$BlendOut = Join-Path $RepoRoot "Art\Generated\pit_striker_rough_soil.blend"
$FbxOut = Join-Path $UnityFbxDir "PitStriker_rough_soil.fbx"
$LogFile = Join-Path $RepoRoot "Art\Generated\blender_generate.log"

if (-not (Test-Path $Generator)) {
    throw "generate_arena.py not found at $Generator"
}

New-Item -ItemType Directory -Force -Path (Split-Path $BlendOut) | Out-Null
New-Item -ItemType Directory -Force -Path $UnityFbxDir | Out-Null

function Find-Blender {
    $hits = @()

    $cmd = Get-Command blender.exe -ErrorAction SilentlyContinue
    if ($cmd) { $hits += $cmd.Source }

    $roots = @(
        ${env:ProgramFiles},
        ${env:ProgramFiles(x86)},
        "$env:LOCALAPPDATA\Programs"
    ) | Where-Object { $_ }

    foreach ($root in $roots) {
        $hub = Join-Path $root "Blender Foundation"
        if (Test-Path $hub) {
            $hits += Get-ChildItem -Path $hub -Recurse -Filter blender.exe -ErrorAction SilentlyContinue |
                Select-Object -ExpandProperty FullName
        }
    }

    $steam = Join-Path ${env:ProgramFiles(x86)} "Steam\steamapps\common\Blender\blender.exe"
    if (Test-Path $steam) { $hits += $steam }

    $hits | Where-Object { $_ -and (Test-Path $_) } | Select-Object -Unique
}

Write-Step "Repo"
Write-Host "  $RepoRoot"
Write-Host "  map=$Map spacing=$PitSpacing radius=$PitRadius"

Write-Step "Find Blender"
$blenders = @(Find-Blender)
if ($blenders.Count -eq 0) {
    Write-Fail "blender.exe not found."
    Write-Host ""
    Write-Host "Install Blender 4.x from https://www.blender.org/download/"
    Write-Host "Default location looks like:"
    Write-Host "  C:\Program Files\Blender Foundation\Blender 4.2\blender.exe"
    Write-Host ""
    Write-Host "After install, run this script again. You do NOT need to add Blender to PATH."
    exit 2
}

$BlenderExe = $blenders[0]
Write-Ok $BlenderExe

Write-Step "Generate village arena (headless, no Blender window)"
$pyArgs = @(
    "--background",
    "--python", $Generator,
    "--",
    "--map", $Map,
    "--pit-spacing", "$PitSpacing",
    "--pit-radius", "$PitRadius",
    "--output", $BlendOut,
    "--export-fbx", $FbxOut
)

& $BlenderExe @pyArgs *>&1 | Tee-Object -FilePath $LogFile
$code = $LASTEXITCODE
if ($code -ne 0) {
    Write-Fail "Blender exited with code $code"
    Write-Host "Log: $LogFile"
    exit $code
}

Write-Step "Outputs"
if (Test-Path $FbxOut) {
    Write-Ok "FBX  $FbxOut"
} else {
    Write-Fail "FBX was not written. See $LogFile"
    exit 3
}
if (Test-Path $BlendOut) { Write-Ok "BLEND $BlendOut" }
Write-Ok "LOG   $LogFile"

Write-Host ""
Write-Host "Next in Unity:"
Write-Host "  1. Open C:\Users\Tisan\Documents\pitstricker\pitstricker"
Write-Host "  2. Wait for PitStriker_rough_soil.fbx to import"
Write-Host "  3. Scene SC_Village_Graphics_Test: disable old ground/house, drop in the new FBX"
Write-Host "  4. Do not move pit objects. Keep house off the left play edge."
