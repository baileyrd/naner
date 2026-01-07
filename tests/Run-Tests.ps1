<#
.SYNOPSIS
    Test runner script for Naner PowerShell modules.

.DESCRIPTION
    Runs all Pester tests for Naner modules with code coverage reporting.
    Installs Pester if not already installed.

.PARAMETER Path
    Specific test file or directory to run. Defaults to all tests.

.PARAMETER CodeCoverage
    Enable code coverage analysis.

.PARAMETER Output
    Output format: Normal, Detailed, Minimal. Default is Detailed.

.EXAMPLE
    .\Run-Tests.ps1

.EXAMPLE
    .\Run-Tests.ps1 -Path .\unit\Common.Tests.ps1

.EXAMPLE
    .\Run-Tests.ps1 -CodeCoverage

.NOTES
    Requires Pester 5.x or higher.
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$Path = "",

    [Parameter()]
    [switch]$CodeCoverage,

    [Parameter()]
    [ValidateSet("Normal", "Detailed", "Minimal")]
    [string]$Output = "Detailed"
)

# Ensure we're in the tests directory
$testsRoot = $PSScriptRoot
$projectRoot = Split-Path $testsRoot -Parent

Write-Host "=== Naner Test Runner ===" -ForegroundColor Cyan
Write-Host "Project Root: $projectRoot" -ForegroundColor Gray
Write-Host "Tests Root: $testsRoot" -ForegroundColor Gray
Write-Host ""

# Check if Pester is installed
$pesterModule = Get-Module -ListAvailable -Name Pester | Where-Object { $_.Version -ge [Version]"5.0.0" }

if (-not $pesterModule) {
    Write-Host "[!] Pester 5.x or higher is not installed." -ForegroundColor Yellow
    Write-Host "[*] Installing Pester from PowerShell Gallery..." -ForegroundColor Cyan

    try {
        Install-Module -Name Pester -MinimumVersion 5.0.0 -Scope CurrentUser -Force -SkipPublisherCheck
        Write-Host "[OK] Pester installed successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "[✗] Failed to install Pester: $_" -ForegroundColor Red
        exit 1
    }
}

# Import Pester
Import-Module Pester -MinimumVersion 5.0.0

# Determine test path
$testPath = if ($Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) {
        $Path
    } else {
        Join-Path $testsRoot $Path
    }
} else {
    Join-Path $testsRoot "unit"
}

if (-not (Test-Path $testPath)) {
    Write-Host "[✗] Test path not found: $testPath" -ForegroundColor Red
    exit 1
}

Write-Host "[*] Running tests from: $testPath" -ForegroundColor Cyan
Write-Host ""

# Configure Pester
$pesterConfig = New-PesterConfiguration

$pesterConfig.Run.Path = $testPath
$pesterConfig.Run.PassThru = $true
$pesterConfig.Output.Verbosity = $Output

if ($CodeCoverage) {
    Write-Host "[*] Code coverage enabled" -ForegroundColor Cyan

    # Set up code coverage paths
    $coveragePaths = @(
        (Join-Path $projectRoot "src\powershell\Common.psm1"),
        (Join-Path $projectRoot "src\powershell\Naner.Vendors.psm1"),
        (Join-Path $projectRoot "src\powershell\Naner.Archives.psm1")
    )

    $pesterConfig.CodeCoverage.Enabled = $true
    $pesterConfig.CodeCoverage.Path = $coveragePaths
    $pesterConfig.CodeCoverage.OutputPath = Join-Path $testsRoot "coverage.xml"
    $pesterConfig.CodeCoverage.OutputFormat = "JaCoCo"
}

# Run tests
try {
    $result = Invoke-Pester -Configuration $pesterConfig

    Write-Host ""
    Write-Host "=== Test Summary ===" -ForegroundColor Cyan
    Write-Host "Total Tests: $($result.TotalCount)" -ForegroundColor Gray
    Write-Host "Passed: $($result.PassedCount)" -ForegroundColor Green
    Write-Host "Failed: $($result.FailedCount)" -ForegroundColor $(if ($result.FailedCount -gt 0) { "Red" } else { "Gray" })
    Write-Host "Skipped: $($result.SkippedCount)" -ForegroundColor Yellow
    Write-Host "Duration: $($result.Duration)" -ForegroundColor Gray

    if ($CodeCoverage -and $result.CodeCoverage) {
        Write-Host ""
        Write-Host "=== Code Coverage ===" -ForegroundColor Cyan

        $coverage = $result.CodeCoverage
        $coveragePercent = if ($coverage.NumberOfCommandsAnalyzed -gt 0) {
            [math]::Round(($coverage.NumberOfCommandsExecuted / $coverage.NumberOfCommandsAnalyzed) * 100, 2)
        } else {
            0
        }

        Write-Host "Commands Analyzed: $($coverage.NumberOfCommandsAnalyzed)" -ForegroundColor Gray
        Write-Host "Commands Executed: $($coverage.NumberOfCommandsExecuted)" -ForegroundColor Gray
        Write-Host "Commands Missed: $($coverage.NumberOfCommandsMissed)" -ForegroundColor Gray
        Write-Host "Coverage: $coveragePercent%" -ForegroundColor $(if ($coveragePercent -ge 80) { "Green" } elseif ($coveragePercent -ge 60) { "Yellow" } else { "Red" })

        if ($coverage.MissedCommands) {
            Write-Host ""
            Write-Host "Missed Commands:" -ForegroundColor Yellow
            $coverage.MissedCommands | Select-Object -First 10 | ForEach-Object {
                Write-Host "  $($_.File):$($_.Line) - $($_.Command)" -ForegroundColor Gray
            }

            if ($coverage.MissedCommands.Count -gt 10) {
                Write-Host "  ... and $($coverage.MissedCommands.Count - 10) more" -ForegroundColor Gray
            }
        }

        Write-Host ""
        Write-Host "Coverage report saved to: $($pesterConfig.CodeCoverage.OutputPath.Value)" -ForegroundColor Gray
    }

    Write-Host ""

    # Exit with appropriate code
    if ($result.FailedCount -gt 0) {
        Write-Host "[✗] Tests FAILED" -ForegroundColor Red
        exit 1
    } else {
        Write-Host "[OK] All tests PASSED" -ForegroundColor Green
        exit 0
    }
}
catch {
    Write-Host "[✗] Test execution failed: $_" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}
