@echo off
cd /d "%~dp0"
echo Titlovi.com (Unofficial) - Jellyfin build
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-JellyfinTitlovi.ps1"
echo.
if errorlevel 1 (
  echo Build did not complete successfully. See output\build.log.
) else (
  echo Build completed successfully.
)
echo.
pause
