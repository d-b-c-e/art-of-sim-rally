@echo off
setlocal
REM Double-click this to install art of sim rally.
REM
REM -ExecutionPolicy Bypass is needed because a script downloaded from the
REM internet is blocked by default, and telling users to run Unblock-File is a
REM good way to lose them.
title art of sim rally - installer
REM Use Windows PowerShell's built-in modules even when called from PowerShell 7.
set "PSModulePath="
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" %*
set "installerExit=%ERRORLEVEL%"
echo.
pause
exit /b %installerExit%
