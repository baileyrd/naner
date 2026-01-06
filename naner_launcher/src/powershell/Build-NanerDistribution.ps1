<#
.SYNOPSIS
    Builds Naner distribution packages.

.DESCRIPTION
    Creates portable and installer distributions of Naner with all vendor dependencies.
    Handles size optimization, license compliance, and packaging.

.PARAMETER Version
    Version string for the release (e.g., "1.0.0").

.PARAMETER OutputDir
    Directory to place the built packages.

.PARAMETER SkipOptimization
    Skip size optimization steps.

.PARAMETER CreatePortable
    Create portable ZIP package.

.PARAMETER CreateInstaller
    Create MSI installer package (requires WiX Toolset).

.EXAMPLE
    .\Build-NanerDistribution.ps1 -Version "1.0.0" -CreatePortable
    
.EXAMPLE
    .\Build-NanerDistribution.ps1 -Version "1.0.0" -CreatePortable -CreateInstaller
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,
    
    [Parameter()]
    [string]$OutputDir = ".\dist",
    
    [Parameter()]
    [switch]$SkipOptimization,
    
    [Parameter()]
    [switch]$CreatePortable,
    
    [Parameter()]
    [switch]$CreateInstaller
)

$ErrorActionPreference = "Stop"

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

function Get-DirectorySize {
    param([string]$Path)
    
    $size = (Get-ChildItem $Path -Recurse -File | Measure-Object -Property Length -Sum).Sum
    return [math]::Round($size / 1MB, 2)
}

# Validate inputs
if (-not $CreatePortable -and -not $CreateInstaller) {
    Write-Status "No package type specified. Creating portable package by default."
    $CreatePortable = $true
}

# Determine Naner root
$nanerRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent

if (-not (Test-Path $nanerRoot)) {
    throw "Naner root not found: $nanerRoot"
}

Write-Status "Building Naner v$Version"
Write-Info "Source: $nanerRoot"
Write-Host ""

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null
}

$OutputDir = Resolve-Path $OutputDir

# Create build directory
$buildDir = Join-Path $OutputDir "build"
$stagingDir = Join-Path $buildDir "naner"

if (Test-Path $buildDir) {
    Write-Status "Cleaning previous build..."
    Remove-Item $buildDir -Recurse -Force
}

New-Item -Path $stagingDir -ItemType Directory -Force | Out-Null

# Copy Naner files
Write-Status "Copying Naner files..."

$foldersToInclude = @("bin", "config", "icons", "vendor", "opt")

foreach ($folder in $foldersToInclude) {
    $sourcePath = Join-Path $nanerRoot $folder
    $destPath = Join-Path $stagingDir $folder
    
    if (Test-Path $sourcePath) {
        Copy-Item $sourcePath -Destination $destPath -Recurse -Force
        $size = Get-DirectorySize $destPath
        Write-Info "Copied $folder/ ($size MB)"
    }
    else {
        Write-Info "Skipped $folder/ (not found)"
    }
}

# Copy root scripts
Write-Status "Copying launcher scripts..."
$scriptsToInclude = @(
    "Invoke-Naner.ps1",
    "Setup-NanerVendor.ps1",
    "Manage-NanerVendor.ps1"
)

foreach ($script in $scriptsToInclude) {
    $sourcePath = Join-Path $nanerRoot $script
    if (Test-Path $sourcePath) {
        Copy-Item $sourcePath -Destination $stagingDir -Force
        Write-Info "Copied $script"
    }
}

Write-Success "Files copied successfully"
Write-Host ""

# Size before optimization
$sizeBefore = Get-DirectorySize $stagingDir
Write-Status "Size before optimization: $sizeBefore MB"

# Optimization
if (-not $SkipOptimization) {
    Write-Status "Optimizing package size..."
    
    # Remove MSYS2 package cache
    $msys2Cache = Join-Path $stagingDir "vendor\msys64\var\cache"
    if (Test-Path $msys2Cache) {
        Remove-Item $msys2Cache -Recurse -Force -ErrorAction SilentlyContinue
        Write-Info "Removed MSYS2 package cache"
    }
    
    # Remove documentation
    $docsToRemove = @(
        "vendor\msys64\usr\share\doc",
        "vendor\msys64\usr\share\man",
        "vendor\msys64\usr\share\info",
        "vendor\msys64\mingw64\share\doc",
        "vendor\msys64\mingw64\share\man"
    )
    
    foreach ($docPath in $docsToRemove) {
        $fullPath = Join-Path $stagingDir $docPath
        if (Test-Path $fullPath) {
            Remove-Item $fullPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Info "Removed documentation: $docPath"
        }
    }
    
    # Remove download cache
    $downloadCache = Join-Path $stagingDir "vendor\.downloads"
    if (Test-Path $downloadCache) {
        Remove-Item $downloadCache -Recurse -Force -ErrorAction SilentlyContinue
        Write-Info "Removed download cache"
    }
    
    # Remove Git GUI (most users won't need it)
    $gitGui = Join-Path $stagingDir "vendor\msys64\mingw64\share\git-gui"
    if (Test-Path $gitGui) {
        Remove-Item $gitGui -Recurse -Force -ErrorAction SilentlyContinue
        Write-Info "Removed Git GUI"
    }
    
    $sizeAfter = Get-DirectorySize $stagingDir
    $savings = $sizeBefore - $sizeAfter
    
    Write-Success "Optimization complete"
    Write-Info "Size after optimization: $sizeAfter MB"
    Write-Info "Space saved: $savings MB"
    Write-Host ""
}

# Create license and attribution files
Write-Status "Creating license files..."

$readmeContent = @"
Naner Terminal Launcher v$Version
================================

A modern, portable terminal environment for Windows.

Quick Start
-----------
1. Extract this archive to any location
2. Run Invoke-Naner.ps1
3. Enjoy a unified environment with PowerShell, Git, and Unix tools!

Documentation
-------------
For detailed documentation, see README-VENDOR.md

Support
-------
Report issues: [Your repository URL]

License
-------
See LICENSE.txt and ATTRIBUTION.txt for licensing information.
"@

$attributionContent = @"
Naner Third-Party Attribution
==============================

Naner bundles the following open-source software:

PowerShell
----------
Copyright (c) Microsoft Corporation
License: MIT License
Source: https://github.com/PowerShell/PowerShell

Windows Terminal
----------------
Copyright (c) Microsoft Corporation
License: MIT License
Source: https://github.com/microsoft/terminal

MSYS2
-----
Various copyrights and licenses (GPL, BSD, MIT, etc.)
License: See individual packages
Source: https://github.com/msys2/msys2

Git for Windows
---------------
Copyright (c) Linus Torvalds and others
License: GPL v2
Source: https://gitforwindows.org/

Full license texts are available in the vendor/ subdirectories.
"@

Set-Content -Path (Join-Path $buildDir "README.txt") -Value $readmeContent -Encoding UTF8
Set-Content -Path (Join-Path $buildDir "ATTRIBUTION.txt") -Value $attributionContent -Encoding UTF8

Write-Success "License files created"
Write-Host ""

# Create portable package
if ($CreatePortable) {
    Write-Status "Creating portable package..."
    
    $zipName = "naner-portable-v$Version.zip"
    $zipPath = Join-Path $OutputDir $zipName
    
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    
    # Use .NET compression for better compatibility
    Add-Type -Assembly System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($buildDir, $zipPath, "Optimal", $false)
    
    $zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
    
    Write-Success "Portable package created: $zipName ($zipSize MB)"
    Write-Info "Location: $zipPath"
    Write-Host ""
}

# Create installer package
if ($CreateInstaller) {
    Write-Status "Creating installer package..."
    
    # Check for WiX Toolset
    $wixPath = "${env:ProgramFiles(x86)}\WiX Toolset v3.11\bin\candle.exe"
    
    if (-not (Test-Path $wixPath)) {
        Write-Warning "WiX Toolset not found. Skipping installer creation."
        Write-Info "Install WiX from: https://wixtoolset.org/"
    }
    else {
        Write-Info "WiX Toolset found"
        Write-Info "Installer creation not yet implemented"
        Write-Info "TODO: Create WiX configuration and build MSI"
    }
    
    Write-Host ""
}

# Build summary
Write-Success "Build complete!"
Write-Host ""
Write-Status "Build Summary:"
Write-Info "Version: $Version"
Write-Info "Output directory: $OutputDir"

if ($CreatePortable) {
    Write-Info "Portable package: naner-portable-v$Version.zip"
}

if ($CreateInstaller) {
    Write-Info "Installer: (not yet implemented)"
}

Write-Host ""
Write-Status "Next steps:"
Write-Info "1. Test the portable package"
Write-Info "2. Create release notes"
Write-Info "3. Upload to release platform"
Write-Info "4. Update documentation"
