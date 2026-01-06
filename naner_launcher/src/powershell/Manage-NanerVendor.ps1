<#
.SYNOPSIS
    Manages vendor dependency versions and updates.

.DESCRIPTION
    Checks for updates to vendor dependencies and can download/install them.
    Maintains a version history and allows rollback if needed.

.PARAMETER CheckUpdates
    Check for available updates without installing.

.PARAMETER Update
    Update specified vendor dependencies.

.PARAMETER ListVersions
    List installed versions from manifest.

.PARAMETER Backup
    Create backup before updating.

.EXAMPLE
    .\Manage-NanerVendor.ps1 -CheckUpdates
    
.EXAMPLE
    .\Manage-NanerVendor.ps1 -Update PowerShell -Backup
#>

[CmdletBinding()]
param(
    [Parameter()]
    [switch]$CheckUpdates,
    
    [Parameter()]
    [string[]]$Update,
    
    [Parameter()]
    [switch]$ListVersions,
    
    [Parameter()]
    [switch]$Backup
)

$ErrorActionPreference = "Stop"

# Determine Naner root
$nanerRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$vendorDir = Join-Path $nanerRoot "vendor"
$manifestPath = Join-Path $vendorDir "vendor-manifest.json"

function Write-Status {
    param([string]$Message)
    Write-Host "[*] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[✓] $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "    $Message" -ForegroundColor Gray
}

# Version sources
$versionSources = @{
    PowerShell = @{
        Type = "GitHub"
        Repo = "PowerShell/PowerShell"
        GetLatestUrl = {
            $releases = Invoke-RestMethod "https://api.github.com/repos/PowerShell/PowerShell/releases/latest"
            $asset = $releases.assets | Where-Object { $_.name -like "*win-x64.zip" } | Select-Object -First 1
            return @{
                Version = $releases.tag_name
                Url = $asset.browser_download_url
                FileName = $asset.name
            }
        }
    }
    
    WindowsTerminal = @{
        Type = "GitHub"
        Repo = "microsoft/terminal"
        GetLatestUrl = {
            $releases = Invoke-RestMethod "https://api.github.com/repos/microsoft/terminal/releases/latest"
            $asset = $releases.assets | Where-Object { $_.name -like "*.msixbundle" } | Select-Object -First 1
            return @{
                Version = $releases.tag_name
                Url = $asset.browser_download_url
                FileName = $asset.name
            }
        }
    }
    
    MSYS2 = @{
        Type = "Direct"
        GetLatestUrl = {
            # MSYS2 uses a dated versioning scheme
            $baseUrl = "https://repo.msys2.org/distrib/x86_64/"
            
            # Scrape the directory listing to find latest
            $page = Invoke-WebRequest $baseUrl
            $links = $page.Links | Where-Object { $_.href -like "msys2-base-x86_64-*.tar.xz" }
            $latest = $links | Sort-Object href -Descending | Select-Object -First 1
            
            if ($latest) {
                $fileName = $latest.href
                $version = $fileName -replace 'msys2-base-x86_64-', '' -replace '.tar.xz', ''
                
                return @{
                    Version = $version
                    Url = "$baseUrl$fileName"
                    FileName = $fileName
                }
            }
            
            return $null
        }
    }
}

if ($ListVersions) {
    Write-Status "Installed Vendor Versions"
    Write-Host ""
    
    if (Test-Path $manifestPath) {
        $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
        
        Write-Info "Manifest created: $($manifest.Created)"
        Write-Host ""
        
        foreach ($dep in $manifest.Dependencies.PSObject.Properties) {
            Write-Host "  $($dep.Value.Name)" -ForegroundColor Yellow
            Write-Info "Version: $($dep.Value.Version)"
            Write-Info "Path: $(Join-Path $vendorDir $dep.Value.ExtractDir)"
            Write-Host ""
        }
    }
    else {
        Write-Info "No manifest found. Run Setup-NanerVendor.ps1 first."
    }
    
    exit 0
}

if ($CheckUpdates) {
    Write-Status "Checking for updates..."
    Write-Host ""
    
    foreach ($dep in $versionSources.Keys) {
        Write-Host "  $dep" -ForegroundColor Yellow
        
        try {
            $latestInfo = & $versionSources[$dep].GetLatestUrl
            
            if ($latestInfo) {
                Write-Info "Latest version: $($latestInfo.Version)"
                Write-Info "Download URL: $($latestInfo.Url)"
                
                # Check installed version
                if (Test-Path $manifestPath) {
                    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
                    $installed = $manifest.Dependencies.$dep.Version
                    
                    if ($installed -ne $latestInfo.Version) {
                        Write-Host "    UPDATE AVAILABLE!" -ForegroundColor Green
                        Write-Info "Installed: $installed"
                    }
                    else {
                        Write-Info "Up to date"
                    }
                }
            }
            else {
                Write-Info "Could not determine latest version"
            }
        }
        catch {
            Write-Info "Error checking: $_"
        }
        
        Write-Host ""
    }
    
    exit 0
}

if ($Update) {
    Write-Status "Updating vendor dependencies: $($Update -join ', ')"
    
    # TODO: Implement update logic
    # - Backup current installation if -Backup
    # - Download new version
    # - Extract and configure
    # - Update manifest
    
    Write-Info "Update functionality coming soon!"
    Write-Info "For now, run Setup-NanerVendor.ps1 -ForceDownload"
    
    exit 0
}

# Default: show help
Write-Host @"
Naner Vendor Management
=======================

Usage:
  .\Manage-NanerVendor.ps1 -ListVersions        # Show installed versions
  .\Manage-NanerVendor.ps1 -CheckUpdates        # Check for available updates
  .\Manage-NanerVendor.ps1 -Update <dep>        # Update specific dependency

Examples:
  .\Manage-NanerVendor.ps1 -ListVersions
  .\Manage-NanerVendor.ps1 -CheckUpdates
  .\Manage-NanerVendor.ps1 -Update PowerShell -Backup

"@
