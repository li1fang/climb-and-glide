[CmdletBinding()]
param([string]$Editor = 'C:\Unity\6000.3.28f1\Editor\Unity.exe')
Write-Warning 'A1 evidence is archived; current source is A2. Running the current A2 trial.'
& (Join-Path $PSScriptRoot 'run-a2.ps1') -Editor $Editor
