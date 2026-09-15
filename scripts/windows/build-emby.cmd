@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build.ps1" -Target Emby
exit /b %ERRORLEVEL%
