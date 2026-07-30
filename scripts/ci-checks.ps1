<#
.SYNOPSIS
    Lightweight CI-ish checks for DairyDNA (spec 013): build+test, a basic
    secret scan over tracked files, and confirmation a health-check test
    exists.

.DESCRIPTION
    Intended to be runnable locally or from a minimal CI workflow
    (.github/workflows/ci.yml calls the same dotnet commands). Does not spin
    up Aspire/Docker - uses UseInMemoryDatabase for anything that needs a DB.

.PARAMETER SkipTests
    Skip `dotnet test` (useful if you only want the secret scan).

.PARAMETER TestOutputPath
    Optional directory to pass to `dotnet test --results-directory`.

.EXAMPLE
    ./scripts/ci-checks.ps1

.EXAMPLE
    ./scripts/ci-checks.ps1 -TestOutputPath C:\Temp\dairydna-test-out
#>
[CmdletBinding()]
param(
    [switch]$SkipTests,
    [string]$TestOutputPath
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$failed = $false

function Write-Section($text) {
    Write-Host ""
    Write-Host "== $text ==" -ForegroundColor Cyan
}

# --- 1. Build + test -------------------------------------------------------
if (-not $SkipTests) {
    Write-Section "dotnet build"
    dotnet build "$repoRoot/DairyDNA.sln" --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[FAIL] dotnet build failed (exit $LASTEXITCODE)." -ForegroundColor Red
        $failed = $true
    } else {
        Write-Host "[PASS] dotnet build" -ForegroundColor Green
    }

    Write-Section "dotnet test DairyDNA.sln"
    $testArgs = @("test", "$repoRoot/DairyDNA.sln", "--no-build", "--nologo")
    if ($TestOutputPath) { $testArgs += @("--results-directory", $TestOutputPath) }
    dotnet @testArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[FAIL] dotnet test failed (exit $LASTEXITCODE). If this is a locked-file build issue, stop any" -ForegroundColor Red
        Write-Host "       running 'dotnet run'/debug sessions and retry; otherwise inspect the failing test output above." -ForegroundColor Red
        $failed = $true
    } else {
        Write-Host "[PASS] dotnet test DairyDNA.sln" -ForegroundColor Green
    }
} else {
    Write-Host "Skipping build/test (-SkipTests)."
}

# --- 2. Basic secret scan over tracked files --------------------------------
Write-Section "Secret scan (tracked files)"

# Deliberately simple pattern set - not a replacement for a real scanner
# (e.g. gitleaks), just a cheap guardrail per spec 013 ("don't add heavy
# gitleaks unless trivial").
$secretPatterns = @(
    @{ Name = "AWS Access Key ID"; Pattern = "AKIA[0-9A-Z]{16}" },
    @{ Name = "Generic API key assignment"; Pattern = "(?i)(api[_-]?key|secret|password|passwd|token)\s*[:=]\s*[\x22\x27][A-Za-z0-9/+_\-]{16,}[\x22\x27]" },
    @{ Name = "Private key header"; Pattern = "-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----" },
    @{ Name = "SQL Server connection string password"; Pattern = "(?i)Password\s*=\s*[^;\s]{6,};" }
)

Push-Location $repoRoot
try {
    $trackedFiles = git ls-files
} finally {
    Pop-Location
}

$secretHits = @()
foreach ($file in $trackedFiles) {
    $fullPath = Join-Path $repoRoot $file
    if (-not (Test-Path $fullPath -PathType Leaf)) { continue }
    if ($file -like "scripts/ci-checks.ps1") { continue } # this file documents the patterns above
    $ext = [System.IO.Path]::GetExtension($file).ToLowerInvariant()
    if ($ext -in @(".dll", ".exe", ".pdb", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".zip")) { continue }

    try {
        $content = Get-Content -Raw -ErrorAction Stop -LiteralPath $fullPath
    } catch {
        continue # binary or unreadable file; skip
    }
    if ($null -eq $content) { continue }

    foreach ($p in $secretPatterns) {
        if ($content -match $p.Pattern) {
            $secretHits += "$file -- $($p.Name)"
        }
    }
}

if ($secretHits.Count -gt 0) {
    Write-Host "[FAIL] Possible secrets found in tracked files:" -ForegroundColor Red
    $secretHits | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $failed = $true
} else {
    Write-Host "[PASS] No obvious secret patterns found in $($trackedFiles.Count) tracked files." -ForegroundColor Green
}

# Also flag any tracked .env file (should never be committed).
$envFiles = $trackedFiles | Where-Object { $_ -match "(^|/)\.env($|\.)" }
if ($envFiles) {
    Write-Host "[FAIL] Tracked .env file(s) found (should be gitignored):" -ForegroundColor Red
    $envFiles | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $failed = $true
} else {
    Write-Host "[PASS] No tracked .env files." -ForegroundColor Green
}

# --- 3. Confirm a health-check test exists ----------------------------------
Write-Section "Health-check test presence"

$healthTestFile = Join-Path $repoRoot "tests/DairyDNA.IntegrationTests/ApiTests.cs"
if ((Test-Path $healthTestFile) -and ((Get-Content -Raw $healthTestFile) -match "Health_returns_ok")) {
    Write-Host "[PASS] Health_returns_ok test found in $healthTestFile" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Could not find a health-check test (expected Health_returns_ok in $healthTestFile)." -ForegroundColor Red
    $failed = $true
}

# --- Summary -----------------------------------------------------------------
Write-Section "Summary"
if ($failed) {
    Write-Host "One or more CI checks FAILED." -ForegroundColor Red
    exit 1
}
Write-Host "All CI checks passed." -ForegroundColor Green
exit 0
