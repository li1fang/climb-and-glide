[CmdletBinding()]
param([switch]$Probe)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$exe=Join-Path $root 'game/Builds/WindowsVR/ClimbDemo.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'VR build missing; use Climb > Build PC VR Test or the CLI BuildPcVr entry in docs/PCVR_PLAN.zh-CN.md.' }
$runtime=(Get-ItemProperty 'HKLM:\SOFTWARE\Khronos\OpenXR\1' -ErrorAction SilentlyContinue).ActiveRuntime
if (!$Probe -and (!$runtime -or !(Test-Path -LiteralPath $runtime))) {
    throw 'No active OpenXR runtime. Install/sign in to Meta Horizon Link, set its OpenXR runtime active, connect Quest Link, then retry. See docs/PCVR_PLAN.zh-CN.md.'
}
$out=Join-Path $root ('artifacts/pcvr-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $out -Force | Out-Null
@{ utc=[DateTime]::UtcNow.ToString('o'); runtime=$runtime; probe=[bool]$Probe; managedSha256=(Get-FileHash (Join-Path $root 'game/Builds/WindowsVR/ClimbDemo_Data/Managed/Assembly-CSharp.dll')).Hash } | ConvertTo-Json | Set-Content "$out/launch.json"
$mode=if($Probe){'--vr-probe'}else{'--vr'}
$player=Start-Process -FilePath $exe -ArgumentList @($mode,'-force-d3d11','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',('"'+$out+'\player.log"')) -PassThru
if (!$Probe) { Write-Output "PC VR requested, not yet validated. PID $($player.Id); log: $out/player.log"; return }
if (!$player.WaitForExit(45000)) { throw "Probe still running: PID $($player.Id). Inspect the log before retrying." }
$line=Select-String "$out/player.log" -Pattern '^PCVR output: (.+)$' | Select-Object -Last 1
if($line){Copy-Item -LiteralPath $line.Matches[0].Groups[1].Value.Trim() -Destination "$out/captures" -Recurse}
Write-Output "Probe exit $($player.ExitCode); evidence: $out (0 = tracked inputs, 4 = unavailable; neither proves visual comfort)."
if($player.ExitCode -ne 0){exit $player.ExitCode}
