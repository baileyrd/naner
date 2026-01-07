<#
.SYNOPSIS
    Downloads and configures vendor dependencies for Naner.

.DESCRIPTION
    This script downloads and sets up vendor dependencies from vendors.json configuration:
    - PowerShell 7.x (latest)
    - Windows Terminal (latest)
    - MSYS2 (with Git and essential tools)
    - Optional: NodeJS, Miniconda, Go, Rust, Ruby

    All dependencies are installed to the vendor/ directory and configured
    for a unified PATH environment.

    DYNAMIC VERSION FETCHING:
    This script automatically fetches the latest releases from official sources
    based on vendors.json configuration:
    - GitHub API (PowerShell, Windows Terminal, NodeJS, Ruby)
    - Web scraping (MSYS2, 7-Zip)
    - Static URLs (Rust, Miniconda)
    - Go API (Go language)

    Fallback URLs are provided in vendors.json for all vendors in case API calls
    fail due to rate limiting or network issues.

.PARAMETER NanerRoot
    The root directory of the Naner installation. Defaults to script's grandparent directory.

.PARAMETER SkipDownload
    Skip downloading if files already exist.

.PARAMETER ForceDownload
    Force re-download even if files exist.

.PARAMETER VendorId
    Install only a specific vendor by ID (e.g., "PowerShell", "Rust").

.EXAMPLE
    .\Setup-NanerVendor.ps1

.EXAMPLE
    .\Setup-NanerVendor.ps1 -NanerRoot "C:\MyNaner" -ForceDownload

.EXAMPLE
    .\Setup-NanerVendor.ps1 -VendorId "Rust"

.NOTES
    Network Requirements:
    - Internet connection required for downloads
    - GitHub API access (api.github.com)
    - MSYS2 repository access (repo.msys2.org)

    GitHub API Rate Limits:
    - Unauthenticated: 60 requests/hour per IP
    - If rate limited, fallback URLs will be used

    Configuration:
    - Vendor definitions: config/vendors.json
    - Vendor schema: config/vendors-schema.json
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$NanerRoot,

    [Parameter()]
    [switch]$SkipDownload,

    [Parameter()]
    [switch]$ForceDownload,

    [Parameter()]
    [string]$VendorId
)

$ErrorActionPreference = "Stop"

# Import required modules
$commonModule = Join-Path $PSScriptRoot "Common.psm1"
if (-not (Test-Path $commonModule)) {
    throw "Common.psm1 module not found at: $commonModule`nThis module is required for Setup-NanerVendor.ps1 to function."
}

$archivesModule = Join-Path $PSScriptRoot "Naner.Archives.psm1"
if (-not (Test-Path $archivesModule)) {
    throw "Naner.Archives.psm1 module not found at: $archivesModule`nThis module is required for Setup-NanerVendor.ps1 to function."
}

$vendorsModule = Join-Path $PSScriptRoot "Naner.Vendors.psm1"
if (-not (Test-Path $vendorsModule)) {
    throw "Naner.Vendors.psm1 module not found at: $vendorsModule`nThis module is required for Setup-NanerVendor.ps1 to function."
}

try {
    # Use dot-sourcing instead of Import-Module to ensure functions are available in script scope
    . $commonModule
    . $archivesModule
    . $vendorsModule
}
catch {
    Write-Host "ERROR loading modules: $_"
    Write-Host "Stack trace: $($_.ScriptStackTrace)"
    exit 1
}

# Determine Naner root
if (-not $NanerRoot) {
    # Calculate Naner root (script is in src/powershell, so go up two levels)
    $NanerRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
}

if (-not (Test-Path $NanerRoot)) {
    Write-Failure "Naner root directory not found: $NanerRoot"
    exit 1
}

Write-Status "Naner root: $NanerRoot"

# Create directory structure
$vendorDir = Join-Path $NanerRoot "vendor"
$optDir = Join-Path $NanerRoot "opt"
$downloadDir = Join-Path $vendorDir ".downloads"

Write-Status "Creating directory structure..."
@($vendorDir, $optDir, $downloadDir) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -Path $_ -ItemType Directory -Force | Out-Null
        Write-Info "Created: $_"
    }
}

# Load vendor configuration from JSON
$configPath = Join-Path $NanerRoot "config\vendors.json"
if (-not (Test-Path $configPath)) {
    Write-Failure "Vendor configuration not found: $configPath"
    exit 1
}

Write-Status "Loading vendor configuration from vendors.json..."

try {
    $vendors = Get-VendorConfiguration -ConfigPath $configPath
}
catch {
    Write-Failure "Failed to load vendor configuration: $_"
    exit 1
}

# Filter vendors based on VendorId parameter
if ($VendorId) {
    if ($vendors.ContainsKey($VendorId)) {
        $vendors = @{ $VendorId = $vendors[$VendorId] }
        Write-Info "Installing single vendor: $VendorId"
    } else {
        Write-Failure "Vendor not found: $VendorId"
        Write-Info "Available vendors: $($vendors.Keys -join ', ')"
        exit 1
    }
}

# Filter to only enabled vendors (unless VendorId is specified)
if (-not $VendorId) {
    $enabledVendors = @{}
    foreach ($key in $vendors.Keys) {
        $vendor = $vendors[$key]
        if ($vendor.enabled -eq $true) {
            $enabledVendors[$key] = $vendor
        }
    }
    $vendors = $enabledVendors
    Write-Info "Found $($vendors.Count) enabled vendors"
}

# Process dependencies in order (respecting dependencies array)
$installed = @{}
$toInstall = New-Object System.Collections.Generic.List[string]
foreach ($key in $vendors.Keys) {
    $toInstall.Add($key)
}

function Install-VendorWithDependencies {
    param(
        [string]$VendorId,
        [hashtable]$AllVendors,
        [hashtable]$Installed
    )

    # Already installed
    if ($Installed.ContainsKey($VendorId)) {
        return $true
    }

    $vendor = $AllVendors[$VendorId]

    # Install dependencies first
    if ($vendor.dependencies -and $vendor.dependencies.Count -gt 0) {
        foreach ($dep in $vendor.dependencies) {
            if (-not $Installed.ContainsKey($dep)) {
                Write-Info "$VendorId requires $dep, installing dependency first..."
                $depSuccess = Install-VendorWithDependencies -VendorId $dep -AllVendors $AllVendors -Installed $Installed
                if (-not $depSuccess) {
                    Write-Warning "Failed to install dependency: $dep"
                    return $false
                }
            }
        }
    }

    # Install this vendor
    Write-Host ""
    Write-Status "Processing: $($vendor.name)"

    $success = Install-VendorPackage `
        -VendorId $VendorId `
        -Vendor $vendor `
        -DownloadDir $downloadDir `
        -VendorDir $vendorDir `
        -SkipDownload:$SkipDownload `
        -ForceDownload:$ForceDownload

    if ($success) {
        $Installed[$VendorId] = $true
        Write-Success "$($vendor.name) installed successfully"
    } else {
        Write-Failure "$($vendor.name) installation failed"
        return $false
    }

    return $true
}

# Install all vendors
Write-Status "Setting up vendor dependencies..."
Write-Host ""

$installResults = @{}
foreach ($vendorId in $toInstall) {
    $success = Install-VendorWithDependencies -VendorId $vendorId -AllVendors $vendors -Installed $installed
    $installResults[$vendorId] = $success
}

# Create vendor manifest
Write-Status "Creating vendor manifest..."
$manifest = @{
    Version = "1.0.0"
    Created = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    ConfigVersion = "1.0.0"
    Dependencies = @{}
}

foreach ($vendorId in $vendors.Keys) {
    $vendor = $vendors[$vendorId]
    if ($installResults[$vendorId]) {
        $extractPath = Join-Path $vendorDir $vendor.extractDir
        $manifest.Dependencies[$vendorId] = @{
            Name = $vendor.name
            Description = $vendor.description
            ExtractDir = $vendor.extractDir
            Enabled = $vendor.enabled
            Required = $vendor.required
            InstalledDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            ReleaseSource = $vendor.releaseSource.type
        }
    }
}

$manifestPath = Join-Path $vendorDir "vendor-manifest.json"
$manifest | ConvertTo-Json -Depth 10 | Set-Content $manifestPath -Encoding UTF8
Write-Success "Manifest created: $manifestPath"

Write-Host ""
Write-Success "Vendor setup complete!"
Write-Host ""

# Display installed vendors
$successCount = ($installResults.Values | Where-Object { $_ -eq $true }).Count
$totalCount = $installResults.Count

Write-Info "Installation Summary: $successCount/$totalCount succeeded"
Write-Host ""

if ($successCount -gt 0) {
    Write-Info "Installed vendors:"
    foreach ($vendorId in $installResults.Keys) {
        if ($installResults[$vendorId]) {
            $vendor = $vendors[$vendorId]
            $vendorPath = Join-Path $vendorDir $vendor.extractDir
            Write-Info "  $($vendor.name): $vendorPath"
        }
    }
    Write-Host ""
}

if ($successCount -lt $totalCount) {
    Write-Warning "Some vendors failed to install. Check output above for details."
    Write-Host ""
}

Write-Info "Next steps:"
Write-Info "  1. Update naner.json to use vendor paths"
Write-Info "  2. Test with: .\Invoke-Naner.ps1"
