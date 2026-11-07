[CmdletBinding()]
param([string]$Editor = 'C:\Unity\6000.3.28f1\Editor\Unity.exe', [string]$UnityCli, [switch]$SkipBuild, [switch]$LeftTarget, [ValidateSet('None','Startup','Runtime')][string]$Stall = 'None')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root ('artifacts\a2-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $out -Force | Out-Null
$project = Join-Path $root 'game'
$cli = if ($UnityCli) { $UnityCli } else { Join-Path $root 'tools\unity-cli\unity.exe' }
if (!(Get-Command node -ErrorAction SilentlyContinue)) { throw 'Node.js is required for trial analysis. See docs/SETUP.md.' }
if (!$SkipBuild) {
if (!(Test-Path -LiteralPath $Editor)) { throw "Editor missing: $Editor" }
if (!(Test-Path -LiteralPath $cli)) { throw 'Unity CLI missing; supply -UnityCli or install locally. See docs/SETUP.md.' }
& $cli build $project --editor-path $Editor --target StandaloneWindows64 --execute-method Climb.EditorTools.DemoSceneBuilder.BuildWindows --log-file "$out\build.log" --no-tail --format json
if ($LASTEXITCODE -ne 0) { throw "Unity build failed; see $out\build.log" }
}
$exe = Join-Path $project 'Builds\Windows\ClimbDemo.exe'
if (!(Test-Path $exe)) { throw 'Build output missing' }
@{ injectedStall = $Stall; coreSha256 = (Get-FileHash -LiteralPath (Join-Path $project 'Builds\Windows\ClimbDemo_Data\Managed\Climb.Core.dll') -Algorithm SHA256).Hash; exeSha256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash; managedSha256 = (Get-FileHash -LiteralPath (Join-Path $project 'Builds\Windows\ClimbDemo_Data\Managed\Assembly-CSharp.dll') -Algorithm SHA256).Hash; target = $(if ($LeftTarget) { 'B' } else { 'A' }); startedUtc = [DateTime]::UtcNow.ToString('o') } | ConvertTo-Json | Set-Content "$out\run.json"
# A rendered application is intentional here; do not use -nographics on the player.
$extra = if ($LeftTarget) { '--trial-left' } else { '--trial-default' }
$fault = if ($Stall -eq 'Startup') { '--trial-startup-stall' } elseif ($Stall -eq 'Runtime') { '--trial-runtime-stall' } else { '--trial-no-stall' }
$player = Start-Process -FilePath $exe -ArgumentList @('--auto-trial',$extra,$fault,'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',('"'+$out+'\player.log"')) -PassThru
$deadline = [DateTime]::UtcNow.AddSeconds(300)
while (!$player.WaitForExit(1000)) {
    if ([DateTime]::UtcNow -gt $deadline) { throw "Player still running; inspect log before retrying. PID $($player.Id)" }
}
$match = Select-String -Path "$out\player.log" -Pattern '^A2 output: (.+)$' | Select-Object -Last 1
if (!$match) { throw 'Player did not report its capture directory' }
$capturePath = $match.Matches[0].Groups[1].Value.Trim()
Copy-Item -LiteralPath $capturePath -Destination "$out\captures" -Recurse
if ($player.ExitCode -ne 0) { throw "Player failed: $($player.ExitCode); evidence retained in $out\captures" }
& node (Join-Path $root 'scripts\analyze-a2.mjs') "$out\captures" "$out\player.log"
if ($LASTEXITCODE -ne 0) { throw "A2 evidence checks failed: $out" }
Write-Output "A2 automated checks passed: $out. Inspect the screenshots before visual acceptance."
