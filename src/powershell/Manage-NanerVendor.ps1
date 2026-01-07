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

# Import common utilities - REQUIRED
$commonModule = Join-Path $PSScriptRoot "Common.psm1"
if (-not (Test-Path $commonModule)) {
    throw "Common.psm1 module not found at: $commonModule`nThis module is required for Manage-NanerVendor.ps1 to function."
}

Import-Module $commonModule -Force

# Determine Naner root
$nanerRoot = Get-NanerRootSimple -ScriptRoot $PSScriptRoot
$vendorDir = Join-Path $nanerRoot "vendor"
$manifestPath = Join-Path $vendorDir "vendor-manifest.json"

# NOTE: Get-LatestGitHubRelease is now provided by Common.psm1
# For compatibility, create alias for old function name
if (Get-Command Get-LatestGitHubRelease -ErrorAction SilentlyContinue) {
    New-Alias -Name Get-GitHubLatestRelease -Value Get-LatestGitHubRelease -Force
}

# Helper function for MSYS2
function Get-MSYS2LatestRelease {
    try {
        $baseUrl = "https://repo.msys2.org/distrib/x86_64/"
        $response = Invoke-WebRequest -Uri $baseUrl -UseBasicParsing
        
        $pattern = 'href="(msys2-base-x86_64-(\d{8})\.tar\.xz)"'
        $matches = [regex]::Matches($response.Content, $pattern)
        
        if ($matches.Count -gt 0) {
            $latest = $matches | Sort-Object { $_.Groups[2].Value } -Descending | Select-Object -First 1
            $fileName = $latest.Groups[1].Value
            $version = $latest.Groups[2].Value
            
            return @{
                Version = $version
                Url = $baseUrl + $fileName
                FileName = $fileName
            }
        }
        return $null
    }
    catch {
        return $null
    }
}

# Version sources
$versionSources = @{
    SevenZip = @{
        Type = "Direct"
        GetLatestUrl = {
            try {
                $downloadPage = "https://www.7-zip.org/download.html"
                $response = Invoke-WebRequest -Uri $downloadPage -UseBasicParsing
                
                $pattern = 'href="([^"]*7z(\d+)-x64\.msi)"'
                $match = [regex]::Match($response.Content, $pattern)
                
                if ($match.Success) {
                    $fileName = $match.Groups[1].Value
                    $version = $match.Groups[2].Value
                    $versionFormatted = "$($version.Substring(0,2)).$($version.Substring(2))"
                    
                    return @{
                        Version = $versionFormatted
                        Url = "https://www.7-zip.org/$fileName"
                        FileName = [System.IO.Path]::GetFileName($fileName)
                    }
                }
            }
            catch { }
            return $null
        }
    }
    
    PowerShell = @{
        Type = "GitHub"
        Repo = "PowerShell/PowerShell"
        GetLatestUrl = {
            return Get-GitHubLatestRelease -Repo "PowerShell/PowerShell" -AssetPattern "*win-x64.zip"
        }
    }
    
    WindowsTerminal = @{
        Type = "GitHub"
        Repo = "microsoft/terminal"
        GetLatestUrl = {
            return Get-GitHubLatestRelease -Repo "microsoft/terminal" -AssetPattern "*.msixbundle"
        }
    }
    
    MSYS2 = @{
        Type = "Direct"
        GetLatestUrl = {
            return Get-MSYS2LatestRelease
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
