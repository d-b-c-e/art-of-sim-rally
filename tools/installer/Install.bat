@echo off
setlocal
title art of rally triple-screen - installer
set "PSModulePath="
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" %*
set "installerExit=%ERRORLEVEL%"
echo.
pause
exit /b %installerExit%
