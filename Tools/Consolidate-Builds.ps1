$ErrorActionPreference = 'Stop'
$cleanupRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$cleanupSource = [IO.Path]::GetFullPath((Join-Path $cleanupRoot 'pitstricker/Builds/Android'))
$cleanupTarget = [IO.Path]::GetFullPath((Join-Path $cleanupRoot 'Builds/Android'))
if (!$cleanupSource.StartsWith($cleanupRoot + '\') -or !$cleanupTarget.StartsWith($cleanupRoot + '\')) { throw 'Outside workspace' }
if (Test-Path -LiteralPath $cleanupSource) {
    foreach ($buildFile in Get-ChildItem -LiteralPath $cleanupSource) {
        if (Test-Path -LiteralPath (Join-Path $cleanupTarget $buildFile.Name)) { throw 'Destination already exists' }
        Move-Item -LiteralPath $buildFile.FullName -Destination $cleanupTarget
    }
    Remove-Item -LiteralPath $cleanupSource
    Remove-Item -LiteralPath (Join-Path $cleanupRoot 'pitstricker/Builds')
}
