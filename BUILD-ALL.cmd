@echo off
setlocal
cd /d "%~dp0"

echo Building Titlovi.com (Unofficial) for Emby...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0src\Emby\Build-EmbyTitlovi.ps1"
if errorlevel 1 goto :failed

echo.
echo Building Titlovi.com (Unofficial) for Jellyfin...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0src\Jellyfin\Build-JellyfinTitlovi.ps1"
if errorlevel 1 goto :failed

echo.
echo Both builds completed successfully.
pause
exit /b 0

:failed
echo.
echo At least one build failed. Check the corresponding output\build.log.
pause
exit /b 1
