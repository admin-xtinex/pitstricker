$ErrorActionPreference = 'Stop'
$cleanupRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$cleanupManifest = Get-Content -LiteralPath (Join-Path $cleanupRoot 'pitstricker/Library/cleanup-manifest.json') -Raw | ConvertFrom-Json
if (!(Test-Path -LiteralPath $cleanupManifest.archive)) { throw 'Recovery archive missing' }
$cleanupFiles = @($cleanupManifest.files)
foreach ($cleanupFile in $cleanupFiles) {
    $cleanupAbsolute = [IO.Path]::GetFullPath($cleanupFile)
    if (!$cleanupAbsolute.StartsWith($cleanupRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Outside project: $cleanupAbsolute" }
    if ((Get-Item -LiteralPath $cleanupAbsolute).PSIsContainer) { throw "Expected file: $cleanupAbsolute" }
}
foreach ($cleanupFile in $cleanupFiles) {
    # Keep folder identity when some referenced children survive pruning.
    if ($cleanupFile.EndsWith('.meta')) {
        $cleanupFolder = $cleanupFile.Substring(0, $cleanupFile.Length - 5)
        if (Test-Path -LiteralPath $cleanupFolder -PathType Container) {
            $survivors = @(Get-ChildItem -LiteralPath $cleanupFolder -File -Recurse | Where-Object { $_.FullName -notin $cleanupFiles })
            if ($survivors.Count -gt 0) { continue }
        }
    }
    Remove-Item -LiteralPath $cleanupFile -Force
}
# Only remove empty directories belonging to the cleanup set.
$cleanupDirectories = $cleanupFiles | ForEach-Object { Split-Path -Parent $_ } | Sort-Object -Unique | Sort-Object Length -Descending
foreach ($cleanupDirectory in $cleanupDirectories) {
    if ($cleanupDirectory.StartsWith($cleanupRoot + '\', [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $cleanupDirectory)) {
        if (@(Get-ChildItem -LiteralPath $cleanupDirectory -Force).Count -eq 0) { Remove-Item -LiteralPath $cleanupDirectory }
    }
}
Write-Output "Cleaned $($cleanupFiles.Count) archived files. Recovery: $($cleanupManifest.archive)"
