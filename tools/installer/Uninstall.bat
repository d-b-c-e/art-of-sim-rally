@echo off
setlocal
REM Double-click this to remove art of sim rally. Settings and key bindings are
REM left alone, so reinstalling picks up where you left off.
title art of sim rally - uninstaller
set "PSModulePath="
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" -Uninstall %*
set "installerExit=%ERRORLEVEL%"
echo.
pause
exit /b %installerExit%
