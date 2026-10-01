# Compatibility entry point for the wheel disposable installer fixture.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$PackageDirectory)
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot '../../components/wheel/tools/testing/Test-Installer.ps1') @PSBoundParameters
