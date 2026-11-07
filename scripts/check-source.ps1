[CmdletBinding()]
param([switch]$RequireLicense)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $paths = @(& git -c core.quotepath=false ls-files --cached --others --exclude-standard | Sort-Object -Unique)
    if ($LASTEXITCODE -ne 0 -or $paths.Count -eq 0) { throw 'Could not enumerate source candidates.' }
    $issues = [System.Collections.Generic.List[string]]::new()
    $inventory = @()
    foreach ($path in $paths) {
        if ($path -notmatch '^(\.gitignore|\.gitattributes|README\.md|AGENTS\.md|CONTRIBUTING\.md|THIRD_PARTY_NOTICES\.md|LICENSE|docs/.+|scripts/.+|tests/.+|game/(Assets|Packages|ProjectSettings)/.+)$') {
            $issues.Add("Unexpected publication path: $path")
        }
        if ($path -match '(^|/)(Library|PackageCache|UserSettings|Logs|tools|artifacts|bin|obj)(/|$)' -or $path -match '\.(exe|dll|zip|ulf|ulic|pem|key|pfx|log)$') {
            $issues.Add("Excluded material in source candidates: $path")
        }
        if (!(Test-Path -LiteralPath $path -PathType Leaf)) { $issues.Add("Missing candidate: $path"); continue }
        $file = Get-Item -LiteralPath $path
        if ($file.Attributes -band [IO.FileAttributes]::ReparsePoint) { $issues.Add("Link requires review: $path"); continue }
        if ($file.Length -gt 2MB) { $issues.Add("Large file requires review: $path"); continue }
        $body = Get-Content -LiteralPath $path -Raw
        # Never echo matched content: a finding may itself be a credential.
        $patterns = @('-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----', '(?:ghp_|github_pat_)[A-Za-z0-9_]{20,}', 'sk-[A-Za-z0-9_-]{24,}', '(?i)(?:password|access_token|api_key)\s*[:=]\s*["''][A-Za-z0-9_+/=-]{16,}', '(?i)C:[/\\]Users[/\\][^/\\\s]+')
        foreach ($pattern in $patterns) {
            if ($body -match $pattern) { $issues.Add("Sensitive-looking content requires review: $path"); break }
        }
        $inventory += [pscustomobject]@{ path=$path; bytes=$file.Length; sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
    if ($RequireLicense -and !(Test-Path -LiteralPath LICENSE -PathType Leaf)) { $issues.Add('LICENSE is not yet selected.') }
    $out = Join-Path $root 'artifacts/source-review'
    New-Item -ItemType Directory -Path $out -Force | Out-Null
    [pscustomobject]@{ generatedUtc=[DateTime]::UtcNow.ToString('o'); scope='Git tracked and untracked nonignored source candidates; heuristic review, not a guarantee'; fileCount=$inventory.Count; totalBytes=($inventory | Measure-Object bytes -Sum).Sum; issues=@($issues.ToArray()); files=$inventory } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $out 'manifest.json') -Encoding utf8
    if ($issues.Count -gt 0) { $issues | Write-Output; throw 'Source review failed; inspect artifacts/source-review/manifest.json.' }
    Write-Output "Source inventory checks passed: $($inventory.Count) files. Manifest: artifacts/source-review/manifest.json. License/publication approval is separate."
} finally { Pop-Location }
