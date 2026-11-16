<#
.SYNOPSIS
    Unified CLI runner for the SIGA Opaque-Box E2E Test Suite.

.DESCRIPTION
    Executes the automated E2E integration test suite for SIGA targeting tests/SIGA.E2ETests/SIGA.E2ETests.csproj.
    Supports running all tests or filtering by tier (Tier1, Tier2, Tier3, Tier4).

.PARAMETER Tier
    Optional tier filter: Tier1 (Feature Coverage), Tier2 (Boundary Cases), Tier3 (Cross-Feature), Tier4 (Real-World Scenarios).

.PARAMETER Logger
    Test logger specification. Defaults to "console;verbosity=normal".

.EXAMPLE
    .\test-e2e.ps1
    .\test-e2e.ps1 -Tier Tier1
    .\test-e2e.ps1 -Tier Tier2
    .\test-e2e.ps1 -Tier Tier3
    .\test-e2e.ps1 -Tier Tier4
#>

[CmdletBinding()]
param(
    [ValidateSet("Tier1", "Tier2", "Tier3", "Tier4", "All")]
    [string]$Tier = "All",

    [string]$Logger = "console;verbosity=normal",

    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$scriptRoot = $PSScriptRoot
if (-not $scriptRoot) {
    $scriptRoot = (Get-Location).Path
}

$testProjectPath = Join-Path $scriptRoot "tests\SIGA.E2ETests\SIGA.E2ETests.csproj"

if (-not (Test-Path $testProjectPath)) {
    Write-Error "E2E Test project not found at: $testProjectPath"
    exit 1
}

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "       SIGA End-to-End Automated Test Suite Runner              " -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "Project:  $testProjectPath"
Write-Host "Tier:     $Tier"
Write-Host "Time:     $([DateTime]::UtcNow.ToString('yyyy-MM-dd HH:mm:ss')) UTC"
Write-Host "================================================================" -ForegroundColor Cyan

$testArgs = @("test", $testProjectPath, "--logger", $Logger)

if ($NoBuild) {
    $testArgs += "--no-build"
}

if ($Tier -ne "All") {
    $testArgs += @("--filter", "Category=$Tier")
}

$startTime = [System.Diagnostics.Stopwatch]::StartNew()

try {
    & dotnet @testArgs
    $exitCode = $LASTEXITCODE
}
catch {
    Write-Error "Failed to execute dotnet test: $_"
    $exitCode = 1
}

$startTime.Stop()
$elapsed = $startTime.Elapsed

Write-Host "----------------------------------------------------------------" -ForegroundColor Cyan
Write-Host "Execution Time: $($elapsed.TotalSeconds.ToString('F2')) seconds"
if ($exitCode -eq 0) {
    Write-Host "Result: ALL TESTS PASSED (Exit Code: 0)" -ForegroundColor Green
} else {
    Write-Host "Result: TEST FAILURES DETECTED (Exit Code: $exitCode)" -ForegroundColor Red
}
Write-Host "================================================================" -ForegroundColor Cyan

exit $exitCode
