@echo off
setlocal
cd /d "%~dp0\..\.."
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Run-GenerateVillageArena.ps1"
echo.
pause
