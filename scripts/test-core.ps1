[CmdletBinding()]
param([switch]$Trial)

$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$sdkRoot = Join-Path $taskRoot 'tools\dotnet'
$dotnet = Join-Path $sdkRoot 'dotnet.exe'
$project = Join-Path $taskRoot 'tests\CoreChecks.csproj'
$compatibilityProject = Join-Path $taskRoot 'tests\UnityCoreCompatibility.csproj'
if (-not (Test-Path -LiteralPath $dotnet)) {
    throw 'Local .NET SDK is missing. See docs/SETUP.md; this script never installs tools or contacts devices.'
}

# Keep the SDK home, first-run state and NuGet cache within the project.
$saved = @{}
$settings = @{
    DOTNET_ROOT = $sdkRoot
    DOTNET_CLI_HOME = (Join-Path $taskRoot 'tools\dotnet-home')
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    DOTNET_NOLOGO = '1'
    DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
    DOTNET_MULTILEVEL_LOOKUP = '0'
    NUGET_PACKAGES = (Join-Path $taskRoot 'tools\nuget-cache')
}
try {
    foreach ($name in $settings.Keys) {
        $saved[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $settings[$name], 'Process')
    }
    & $dotnet restore $compatibilityProject --configfile (Join-Path $taskRoot 'tests\NuGet.Config') --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "Core compatibility restore failed with exit code $LASTEXITCODE" }
    & $dotnet build $compatibilityProject --configuration Release --no-restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "Core compatibility build failed with exit code $LASTEXITCODE" }
    & $dotnet restore $project --configfile (Join-Path $taskRoot 'tests\NuGet.Config') --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "Core restore failed with exit code $LASTEXITCODE" }
    $runArguments = @('run', '--project', $project, '--configuration', 'Release', '--no-restore')
    if ($Trial) {
        $trialPath = Join-Path $taskRoot ('artifacts\trial-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
        $runArguments += @('--', '--trial', $trialPath)
    }
    & $dotnet @runArguments
    if ($LASTEXITCODE -ne 0) { throw "Core checks failed with exit code $LASTEXITCODE" }
} finally {
    foreach ($name in $saved.Keys) {
        [Environment]::SetEnvironmentVariable($name, $saved[$name], 'Process')
    }
}
