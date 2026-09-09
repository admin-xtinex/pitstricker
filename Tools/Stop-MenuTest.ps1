$ErrorActionPreference = 'Stop'
$testProcesses = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'Unity.exe' -and $_.CommandLine -like '*PitStriker.EditorTools.MenuFlowChecks.Run*' }
foreach ($testProcess in $testProcesses) {
    Write-Output "Stopping interrupted UI test process $($testProcess.ProcessId)"
    Stop-Process -Id $testProcess.ProcessId -Force
}
