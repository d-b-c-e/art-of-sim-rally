@echo off
REM Builds dinput-enum.exe (x64). Standalone - sets up MSVC itself.
setlocal enabledelayedexpansion
set "HERE=%~dp0"
set "OUT=%HERE%build"
if not exist "%OUT%" mkdir "%OUT%"
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "!VSWHERE!" ( echo ERROR: vswhere.exe not found & exit /b 1 )
for /f "usebackq tokens=*" %%i in (`"!VSWHERE!" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "VSPATH=%%i"
if not defined VSPATH ( echo ERROR: MSVC x64 toolset not found & exit /b 1 )
call "!VSPATH!\VC\Auxiliary\Build\vcvars64.bat" >nul
cl.exe /nologo /O2 /EHsc /W4 /DNDEBUG /Fo"%OUT%\\" /Fe"%OUT%\dinput-enum.exe" "%HERE%dinput-enum.cpp"
exit /b %errorlevel%
