[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$sdkVersion = '8.0.427'
$source = "https://builds.dotnet.microsoft.com/dotnet/Sdk/$sdkVersion/dotnet-sdk-$sdkVersion-win-x64.zip"
$metadataSource = 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json'
$expectedSha512 = 'a4dd6eb2bf2a2f6fb10997d3e044a229b23ad96a5c86d20494687e34ee73b537d7bd14488ce14cded6917f6778da4d66360e2734020b7ef487d0f29ab144489d'
$downloadDir = Join-Path $taskRoot 'tools\downloads'
$sdkDir = Join-Path $taskRoot 'tools\dotnet'
$archive = Join-Path $downloadDir "dotnet-sdk-$sdkVersion-win-x64.zip"
$dotnet = Join-Path $sdkDir 'dotnet.exe'

if (Test-Path -LiteralPath $dotnet) {
    $existingVersion = (& $dotnet --version).Trim()
    if ($LASTEXITCODE -eq 0 -and $existingVersion -eq $sdkVersion) {
        Write-Output "Local .NET SDK $sdkVersion is already available."
        return
    }
    throw "A different or incomplete SDK exists at $sdkDir; it will not be overwritten."
}
if ((Test-Path -LiteralPath $sdkDir) -and @(Get-ChildItem -LiteralPath $sdkDir -Force).Count -gt 0) {
    throw "Nonempty SDK directory $sdkDir will not be overwritten."
}
New-Item -ItemType Directory -Force -Path $downloadDir,$sdkDir | Out-Null
if (-not (Test-Path -LiteralPath $archive)) {
    $partial = "$archive.partial"
    if (Test-Path -LiteralPath $partial) {
        throw "Previous partial download retained at $partial; inspect it before retrying."
    }
    Invoke-WebRequest -Uri $source -OutFile $partial
    $actualHash = (Get-FileHash -LiteralPath $partial -Algorithm SHA512).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedSha512) { throw 'SDK SHA-512 verification failed; download retained for inspection.' }
    Move-Item -LiteralPath $partial -Destination $archive
}
$actualHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash.ToLowerInvariant()
if ($actualHash -ne $expectedSha512) { throw 'Existing SDK archive does not match the pinned official SHA-512.' }
[pscustomobject]@{
    component = '.NET SDK'
    version = $sdkVersion
    source = $source
    metadata_source = $metadataSource
    verified_utc = [DateTime]::UtcNow.ToString('o')
    sha512 = $actualHash
    sha256 = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    bytes = (Get-Item -LiteralPath $archive).Length
} | ConvertTo-Json | Set-Content (Join-Path $downloadDir 'dotnet-sdk-source.json') -Encoding utf8
Expand-Archive -LiteralPath $archive -DestinationPath $sdkDir
& $dotnet --version
if ($LASTEXITCODE -ne 0) { throw 'Extracted SDK could not start.' }
