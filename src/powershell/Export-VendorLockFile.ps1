<#
.SYNOPSIS
    Exports vendor versions to a lock file for reproducible installations.

.DESCRIPTION
    Creates a vendors.lock.json file containing the exact versions of all
    installed vendor dependencies. This lock file can be used to ensure
    consistent installations across different environments.

.PARAMETER ConfigPath
    Path to vendors.json configuration file. Defaults to config/vendors.json.

.PARAMETER VendorDir
    Path to vendor directory. Defaults to vendor/ in Naner root.

.PARAMETER OutputPath
    Path to output lock file. Defaults to config/vendors.lock.json.

.PARAMETER IncludeHashes
    Include SHA256 file hashes for verification (slower but more secure).

.EXAMPLE
    .\Export-VendorLockFile.ps1

.EXAMPLE
    .\Export-VendorLockFile.ps1 -IncludeHashes

.EXAMPLE
    .\Export-VendorLockFile.ps1 -OutputPath ".\my-vendors.lock.json"

.NOTES
    This script should be run after vendor installation to capture current versions.
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$ConfigPath = "",

    [Parameter()]
    [string]$VendorDir = "",

    [Parameter()]
    [string]$OutputPath = "",

    [Parameter()]
    [switch]$IncludeHashes
)

# Import Common module
$commonModule = Join-Path $PSScriptRoot "Common.psm1"
if (Test-Path $commonModule) {
    Import-Module $commonModule -Force
}

# Import Vendors module
$vendorsModule = Join-Path $PSScriptRoot "Naner.Vendors.psm1"
if (Test-Path $vendorsModule) {
    Import-Module $vendorsModule -Force
}

# Find Naner root
try {
    $nanerRoot = Find-NanerRoot -StartPath $PSScriptRoot
} catch {
    Write-Failure "Could not find Naner root: $_"
    exit 1
}

# Set defaults
if (-not $ConfigPath) {
    $ConfigPath = Join-Path $nanerRoot "config\vendors.json"
}

if (-not $VendorDir) {
    $VendorDir = Join-Path $nanerRoot "vendor"
}

if (-not $OutputPath) {
    $OutputPath = Join-Path $nanerRoot "config\vendors.lock.json"
}

Write-Status "Exporting vendor lock file..."
Write-Info "Config: $ConfigPath"
Write-Info "Vendor Directory: $VendorDir"
Write-Info "Output: $OutputPath"

# Load vendor configuration
try {
    $vendors = Get-VendorConfiguration -ConfigPath $ConfigPath
} catch {
    Write-Failure "Failed to load vendor configuration: $_"
    exit 1
}

# Build lock file data
$lockData = @{
    version = "1.0.0"
    generated = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    nanerVersion = "1.0.0"  # TODO: Get from project version
    platform = @{
        os = $PSVersionTable.OS
        psVersion = $PSVersionTable.PSVersion.ToString()
        architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    }
    vendors = @{}
}

foreach ($vendorId in $vendors.Keys) {
    $vendor = $vendors[$vendorId]

    Write-Status "Processing $vendorId..."

    $vendorPath = Join-Path $VendorDir $vendor.extractDir

    if (-not (Test-Path $vendorPath)) {
        Write-Info "  Skipping (not installed)"
        continue
    }

    # Get release information
    try {
        $releaseInfo = Get-VendorRelease -Vendor $vendor

        $vendorLock = @{
            name = $vendor.name
            version = $releaseInfo.Version
            url = $releaseInfo.Url
            fileName = $releaseInfo.FileName
            size = $releaseInfo.Size
            installed = $true
            installedDate = (Get-Item $vendorPath).CreationTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
            extractDir = $vendor.extractDir
        }

        # Add file hash if requested
        if ($IncludeHashes) {
            Write-Info "  Calculating hash (this may take a moment)..."

            # Look for downloaded archive in common locations
            $possibleArchives = @(
                (Join-Path $nanerRoot "downloads\$($releaseInfo.FileName)"),
                (Join-Path $VendorDir "$($releaseInfo.FileName)")
            )

            $archivePath = $possibleArchives | Where-Object { Test-Path $_ } | Select-Object -First 1

            if ($archivePath) {
                $hash = Get-FileHash -Path $archivePath -Algorithm SHA256
                $vendorLock.sha256 = $hash.Hash
                Write-Info "  SHA256: $($hash.Hash)"
            } else {
                Write-Info "  Hash unavailable (archive not found)"
            }
        }

        $lockData.vendors[$vendorId] = $vendorLock
        Write-Success "  Locked: $($vendor.name) v$($releaseInfo.Version)"

    } catch {
        Write-Warning "  Failed to get release info: $_"
        Write-Info "  Recording as installed without version"

        $vendorLock = @{
            name = $vendor.name
            version = "unknown"
            installed = $true
            installedDate = (Get-Item $vendorPath).CreationTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
            extractDir = $vendor.extractDir
        }

        $lockData.vendors[$vendorId] = $vendorLock
    }
}

# Write lock file
try {
    $lockJson = $lockData | ConvertTo-Json -Depth 10
    Set-Content -Path $OutputPath -Value $lockJson -Encoding UTF8

    Write-Success "Lock file created successfully"
    Write-Info "Location: $OutputPath"
    Write-Info "Locked vendors: $($lockData.vendors.Count)"

} catch {
    Write-Failure "Failed to write lock file: $_"
    exit 1
}

# Display summary
Write-Host ""
Write-Host "=== Lock File Summary ===" -ForegroundColor Cyan
Write-Host "Generated: $($lockData.generated)" -ForegroundColor Gray
Write-Host "Platform: $($lockData.platform.os)" -ForegroundColor Gray
Write-Host "Locked Vendors:" -ForegroundColor Gray

foreach ($vendorId in $lockData.vendors.Keys) {
    $v = $lockData.vendors[$vendorId]
    Write-Host "  - $($v.name) v$($v.version)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "To use this lock file for reproducible installations:" -ForegroundColor Cyan
Write-Host "  .\Setup-NanerVendor.ps1 -UseLockFile" -ForegroundColor Yellow
