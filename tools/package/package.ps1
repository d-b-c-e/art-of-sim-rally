# Compatibility entry point; wheel implementation lives in components/wheel.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Version)
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot '../../components/wheel/tools/package/package.ps1') @PSBoundParameters
