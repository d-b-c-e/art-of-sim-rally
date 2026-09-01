@echo off
REM Double-click this to remove art of sim rally. Settings and key bindings are
REM left alone, so reinstalling picks up where you left off.
title art of sim rally - uninstaller
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" -Uninstall
echo.
pause
