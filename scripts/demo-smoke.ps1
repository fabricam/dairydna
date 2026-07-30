<#
.SYNOPSIS
    Smoke-checks a running DairyDNA API (spec 013).

.DESCRIPTION
    Hits /health once and (optionally) exercises generate -> optimize via the
    one-shot /api/demo/bootstrap endpoint, reporting pass/fail and timings.
    Does not start or require Aspire - point it at any already-running API
    (standalone or the 'api' resource endpoint from an Aspire run).

.PARAMETER BaseUrl
    Base URL of the running API. Defaults to the standalone dev default.

.PARAMETER Bootstrap
    Also call POST /api/demo/bootstrap to verify generate+optimize succeed.

.EXAMPLE
    ./scripts/demo-smoke.ps1

.EXAMPLE
    ./scripts/demo-smoke.ps1 -BaseUrl http://localhost:5199 -Bootstrap
#>
[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5114",
    [switch]$Bootstrap
)

$ErrorActionPreference = "Stop"
$failed = $false

function Test-Step($name, [scriptblock]$action) {
    try {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $result = & $action
        $sw.Stop()
        Write-Host "[PASS] $name ($([math]::Round($sw.Elapsed.TotalMilliseconds, 1)) ms)" -ForegroundColor Green
        return $result
    } catch {
        Write-Host "[FAIL] $name : $($_.Exception.Message)" -ForegroundColor Red
        $script:failed = $true
        return $null
    }
}

Write-Host "DairyDNA demo smoke check against $BaseUrl"

Test-Step "GET /health" {
    $r = Invoke-RestMethod -Uri "$BaseUrl/health" -TimeoutSec 20
    if ($r.status -ne "Healthy") { throw "status was '$($r.status)', expected 'Healthy'." }
}

if ($Bootstrap) {
    $boot = Test-Step "POST /api/demo/bootstrap" {
        Invoke-RestMethod -Uri "$BaseUrl/api/demo/bootstrap" -Method Post -Body "{}" -ContentType "application/json" -TimeoutSec 40
    }
    if ($boot) {
        Write-Host "  generationId=$($boot.generationId) status=$($boot.generationStatus) optimizationStatus=$($boot.optimizationStatus) objective=$($boot.objectiveValue)"
        if ($boot.generationStatus -ne "Completed") {
            Write-Host "[FAIL] generation status was '$($boot.generationStatus)', expected 'Completed'." -ForegroundColor Red
            $failed = $true
        }
    }
}

if ($failed) {
    Write-Host ""
    Write-Host "Smoke check FAILED. Is the API running? Try: dotnet run --project src/DairyDNA.Api (UseInMemoryDatabase=true) or scripts/demo-start.ps1 -InMemory" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Smoke check passed." -ForegroundColor Green
exit 0
