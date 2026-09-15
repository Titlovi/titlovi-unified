@echo off
cd /d "%~dp0"
echo Titlovi.com (Unofficial) - Emby build
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-EmbyTitlovi.ps1"
echo.
if errorlevel 1 (
  echo Build did not complete successfully. See the message above.
) else (
  echo Build completed successfully.
)
echo.
pause
