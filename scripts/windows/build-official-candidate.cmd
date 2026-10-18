@echo off
setlocal
where py >nul 2>&1
if %ERRORLEVEL%==0 (
  py -3 scripts\migration\build-official-candidate.py
) else (
  python scripts\migration\build-official-candidate.py
)
exit /b %ERRORLEVEL%
