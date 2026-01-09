<#
.SYNOPSIS
    Imports and validates a vendor lock file.

.DESCRIPTION
    Reads a vendors.lock.json file and returns the locked vendor configuration.
    Can be used to install exact versions for reproducible environments.

.PARAMETER LockFilePath
    Path to vendors.lock.json file. Defaults to config/vendors.lock.json.

.PARAMETER ValidateHashes
    Validate SHA256 hashes of downloaded files (if included in lock file).

.PARAMETER ShowSummary
    Display a summary of locked vendors.

.OUTPUTS
    PSCustomObject containing lock file data.

.EXAMPLE
    $lockData = .\Import-VendorLockFile.ps1

.EXAMPLE
    $lockData = .\Import-VendorLockFile.ps1 -ShowSummary

.EXAMPLE
    $lockData = .\Import-VendorLockFile.ps1 -LockFilePath ".\my-vendors.lock.json"

.NOTES
    Lock file format:
    {
      "version": "1.0.0",
      "generated": "2024-01-07T10:00:00Z",
      "vendors": {
        "PowerShell": {
          "version": "7.4.6",
          "url": "https://...",
          "fileName": "...",
          "sha256": "..."
        }
      }
    }
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$LockFilePath = "",

    [Parameter()]
    [switch]$ValidateHashes,

    [Parameter()]
    [switch]$ShowSummary
)

# Import Common module
$commonModule = Join-Path $PSScriptRoot "Common.psm1"
if (Test-Path $commonModule) {
    Import-Module $commonModule -Force
}

# Find Naner root
try {
    $nanerRoot = Find-NanerRoot -StartPath $PSScriptRoot
} catch {
    Write-Failure "Could not find Naner root: $_"
    exit 1
}

# Set default lock file path
if (-not $LockFilePath) {
    $LockFilePath = Join-Path $nanerRoot "config\vendors.lock.json"
}

# Check if lock file exists
if (-not (Test-Path $LockFilePath)) {
    Write-Failure "Lock file not found: $LockFilePath"
    Write-Info "Create a lock file with: .\Export-VendorLockFile.ps1"
    exit 1
}

Write-Status "Loading vendor lock file..."
Write-Info "Path: $LockFilePath"

# Load lock file
try {
    $lockContent = Get-Content $LockFilePath -Raw -Encoding UTF8
    $lockData = $lockContent | ConvertFrom-Json
} catch {
    Write-Failure "Failed to parse lock file: $_"
    exit 1
}

# Validate lock file structure
if (-not $lockData.version) {
    Write-Failure "Invalid lock file: missing version field"
    exit 1
}

if (-not $lockData.vendors) {
    Write-Failure "Invalid lock file: missing vendors field"
    exit 1
}

Write-Success "Lock file loaded successfully"

# Display summary if requested
if ($ShowSummary) {
    Write-Host ""
    Write-Host "=== Vendor Lock File Summary ===" -ForegroundColor Cyan
    Write-Host "Version: $($lockData.version)" -ForegroundColor Gray
    Write-Host "Generated: $($lockData.generated)" -ForegroundColor Gray

    if ($lockData.platform) {
        Write-Host "Platform:" -ForegroundColor Gray
        Write-Host "  OS: $($lockData.platform.os)" -ForegroundColor Gray
        Write-Host "  PowerShell: $($lockData.platform.psVersion)" -ForegroundColor Gray
        Write-Host "  Architecture: $($lockData.platform.architecture)" -ForegroundColor Gray
    }

    Write-Host ""
    Write-Host "Locked Vendors ($($lockData.vendors.PSObject.Properties.Count)):" -ForegroundColor Cyan

    $lockData.vendors.PSObject.Properties | ForEach-Object {
        $vendorId = $_.Name
        $vendor = $_.Value

        $hashInfo = if ($vendor.sha256) { " [SHA256: $($vendor.sha256.Substring(0,8))...]" } else { "" }

        Write-Host "  $($vendor.name):" -ForegroundColor Yellow
        Write-Host "    Version: $($vendor.version)" -ForegroundColor Gray
        Write-Host "    File: $($vendor.fileName)$hashInfo" -ForegroundColor Gray

        if ($vendor.installed) {
            Write-Host "    Installed: $($vendor.installedDate)" -ForegroundColor Gray
        }
    }

    Write-Host ""
}

# Validate hashes if requested
if ($ValidateHashes) {
    Write-Status "Validating file hashes..."

    $downloadDir = Join-Path $nanerRoot "downloads"
    $vendorDir = Join-Path $nanerRoot "vendor"

    $validated = 0
    $missing = 0
    $failed = 0

    $lockData.vendors.PSObject.Properties | ForEach-Object {
        $vendorId = $_.Name
        $vendor = $_.Value

        if ($vendor.sha256 -and $vendor.fileName) {
            # Look for file
            $possiblePaths = @(
                (Join-Path $downloadDir $vendor.fileName),
                (Join-Path $vendorDir $vendor.fileName)
            )

            $filePath = $possiblePaths | Where-Object { Test-Path $_ } | Select-Object -First 1

            if ($filePath) {
                Write-Info "Validating $($vendor.fileName)..."

                $hash = Get-FileHash -Path $filePath -Algorithm SHA256

                if ($hash.Hash -eq $vendor.sha256) {
                    Write-Success "  Hash valid"
                    $validated++
                } else {
                    Write-Failure "  Hash mismatch!"
                    Write-Info "    Expected: $($vendor.sha256)"
                    Write-Info "    Actual:   $($hash.Hash)"
                    $failed++
                }
            } else {
                Write-Info "  File not found (will be downloaded): $($vendor.fileName)"
                $missing++
            }
        }
    }

    Write-Host ""
    Write-Host "Hash Validation Summary:" -ForegroundColor Cyan
    Write-Host "  Validated: $validated" -ForegroundColor Green
    Write-Host "  Missing: $missing" -ForegroundColor Yellow
    Write-Host "  Failed: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Gray" })

    if ($failed -gt 0) {
        Write-Failure "Hash validation failed"
        exit 1
    }
}

# Return lock data for use in other scripts
return $lockData
