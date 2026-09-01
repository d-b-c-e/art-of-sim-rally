@echo off
REM Double-click this to install art of sim rally.
REM
REM -ExecutionPolicy Bypass is needed because a script downloaded from the
REM internet is blocked by default, and telling users to run Unblock-File is a
REM good way to lose them.
title art of sim rally - installer
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
echo.
pause
