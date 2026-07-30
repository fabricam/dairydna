<#
.SYNOPSIS
    One-command local demo bring-up for DairyDNA (spec 013 FR-001).

.DESCRIPTION
    Restores/builds the solution if needed, then runs the Aspire AppHost -
    THE one documented command for the flagship demo path (generate ->
    dashboard -> optimize -> scenarios -> replay -> visuals). Prints the
    resulting URLs and points at the presenter script.

    This script does not run in CI: it launches a long-lived interactive
    process (Aspire dashboard + api + web) intended for a human to click
    around in. For CI/smoke checks use scripts/demo-smoke.ps1 or
    scripts/ci-checks.ps1 instead.

.PARAMETER SkipBuild
    Skip the restore/build step (use if you just built already).

.PARAMETER InMemory
    Instead of Aspire (which needs Docker Desktop for SQL Server), run the
    API standalone with UseInMemoryDatabase=true and the Web app pointed at
    it. Useful when Docker isn't available.

.EXAMPLE
    ./scripts/demo-start.ps1

.EXAMPLE
    ./scripts/demo-start.ps1 -InMemory
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$InMemory
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

function Write-Section($text) {
    Write-Host ""
    Write-Host "== $text ==" -ForegroundColor Cyan
}

Write-Section "DairyDNA demo start"
Write-Host "Repo root: $repoRoot"
Write-Host "Honesty boundary: local-dev demo only, unauthenticated, synthetic data."
Write-Host "See docs/demo/presenter-script.md for the full walkthrough."

if (-not $SkipBuild) {
    Write-Section "Restore + build (skip with -SkipBuild)"
    dotnet restore "$repoRoot/DairyDNA.sln"
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed (exit $LASTEXITCODE)." }
    dotnet build "$repoRoot/DairyDNA.sln" --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)." }
}

if ($InMemory) {
    Write-Section "Starting API (in-memory DB) - no Docker required"
    Write-Host "API:  http://localhost:5114  (GET /health, POST /api/generation-runs, ...)"
    Write-Host "Web needs to be started separately and pointed at the API (see README):"
    Write-Host "  dotnet run --project src/DairyDNA.Web"
    Write-Host ""
    Write-Host "Port already in use? Edit src/DairyDNA.Api/Properties/launchSettings.json" -ForegroundColor Yellow
    Write-Host "  (applicationUrl) to pick a free port, or stop the conflicting process." -ForegroundColor Yellow
    Write-Host ""
    $env:UseInMemoryDatabase = "true"
    dotnet run --project "$repoRoot/src/DairyDNA.Api" --no-build
    exit $LASTEXITCODE
}

Write-Section "Starting Aspire AppHost (requires Docker Desktop for SQL Server)"
Write-Host "This is THE one command for the full demo - it starts SQL Server, the API, and the Web app."
Write-Host ""
Write-Host "Watch the console for the Aspire dashboard URL (default https://localhost:17266"
Write-Host "or http://localhost:15110), then open the 'api' and 'web' resource endpoints from there."
Write-Host ""
Write-Host "If Docker Desktop is not running:" -ForegroundColor Yellow
Write-Host "  - Start Docker Desktop and re-run this script, OR" -ForegroundColor Yellow
Write-Host "  - Re-run with -InMemory to skip SQL Server entirely." -ForegroundColor Yellow
Write-Host ""
Write-Host "Port conflicts (Aspire dashboard 15110/17266, api 5114/7122, web 5152/7032,"
Write-Host "OTLP 19290/21063): stop the conflicting process, or edit the relevant"
Write-Host "Properties/launchSettings.json applicationUrl." -ForegroundColor Yellow
Write-Host ""
Write-Host "Locked .dll/.exe on build? Stop any previous 'dotnet run'/debug session" -ForegroundColor Yellow
Write-Host "  (Task Manager -> dotnet.exe / DairyDNA.*.exe) before rebuilding." -ForegroundColor Yellow
Write-Host ""
Write-Host "Next: follow docs/demo/presenter-script.md once the dashboard/web/api are up." -ForegroundColor Green
Write-Host ""

dotnet run --project "$repoRoot/src/DairyDNA.AppHost"
exit $LASTEXITCODE
