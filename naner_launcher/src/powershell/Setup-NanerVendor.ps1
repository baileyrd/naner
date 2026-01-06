<#
.SYNOPSIS
    Downloads and configures vendor dependencies for Naner.

.DESCRIPTION
    This script downloads and sets up the required vendor dependencies:
    - PowerShell 7.x (latest)
    - Windows Terminal (latest)
    - MSYS2 (with Git and essential tools)
    
    All dependencies are installed to the vendor/ directory and configured
    for a unified PATH environment.

.PARAMETER NanerRoot
    The root directory of the Naner installation. Defaults to script's grandparent directory.

.PARAMETER SkipDownload
    Skip downloading if files already exist.

.PARAMETER ForceDownload
    Force re-download even if files exist.

.EXAMPLE
    .\Setup-NanerVendor.ps1
    
.EXAMPLE
    .\Setup-NanerVendor.ps1 -NanerRoot "C:\MyNaner" -ForceDownload
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$NanerRoot,
    
    [Parameter()]
    [switch]$SkipDownload,
    
    [Parameter()]
    [switch]$ForceDownload
)

$ErrorActionPreference = "Stop"

# Color output functions
function Write-Status {
    param([string]$Message)
    Write-Host "[*] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[✓] $Message" -ForegroundColor Green
}

function Write-Failure {
    param([string]$Message)
    Write-Host "[✗] $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "    $Message" -ForegroundColor Gray
}

# Determine Naner root
if (-not $NanerRoot) {
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

# Vendor configuration
$vendorConfig = @{
    PowerShell = @{
        Name = "PowerShell"
        Version = "latest"
        Url = "https://github.com/PowerShell/PowerShell/releases/latest/download/PowerShell-7.4.6-win-x64.zip"
        FileName = "PowerShell-win-x64.zip"
        ExtractDir = "powershell"
        PostInstall = {
            param($extractPath)
            # Create a pwsh.bat wrapper for easier PATH usage
            $wrapperContent = @"
@echo off
"%~dp0pwsh.exe" %*
"@
            $wrapperPath = Join-Path $extractPath "pwsh.bat"
            Set-Content -Path $wrapperPath -Value $wrapperContent -Encoding ASCII
        }
    }
    
    WindowsTerminal = @{
        Name = "Windows Terminal"
        Version = "latest"
        Url = "https://github.com/microsoft/terminal/releases/latest/download/Microsoft.WindowsTerminal_Win10_1.21.2361.0_8wekyb3d8bbwe.msixbundle"
        FileName = "WindowsTerminal.msixbundle"
        ExtractDir = "terminal"
        PostInstall = {
            param($extractPath)
            Write-Status "Extracting Windows Terminal from MSIX bundle..."
            
            # Extract the msixbundle (it's a zip file)
            $bundlePath = Join-Path $downloadDir $vendorConfig.WindowsTerminal.FileName
            $tempExtract = Join-Path $downloadDir "wt_temp"
            
            if (Test-Path $tempExtract) {
                Remove-Item $tempExtract -Recurse -Force
            }
            
            Expand-Archive -Path $bundlePath -DestinationPath $tempExtract -Force
            
            # Find the x64 msix package
            $x64Package = Get-ChildItem $tempExtract -Filter "*x64*.msix" | Select-Object -First 1
            
            if ($x64Package) {
                Write-Info "Found x64 package: $($x64Package.Name)"
                
                # Extract the x64 msix package
                $msixExtract = Join-Path $tempExtract "msix_contents"
                Expand-Archive -Path $x64Package.FullName -DestinationPath $msixExtract -Force
                
                # Copy necessary files to vendor directory
                $filesToCopy = @("wt.exe", "WindowsTerminal.exe", "OpenConsole.exe", "*.dll")
                
                foreach ($pattern in $filesToCopy) {
                    Get-ChildItem $msixExtract -Filter $pattern -Recurse | ForEach-Object {
                        Copy-Item $_.FullName -Destination $extractPath -Force
                        Write-Info "Copied: $($_.Name)"
                    }
                }
                
                Write-Success "Windows Terminal extracted successfully"
            } else {
                Write-Failure "Could not find x64 package in bundle"
            }
            
            # Cleanup temp extraction
            Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    
    MSYS2 = @{
        Name = "MSYS2"
        Version = "latest"
        Url = "https://repo.msys2.org/distrib/x86_64/msys2-base-x86_64-20240727.tar.xz"
        FileName = "msys2-base.tar.xz"
        ExtractDir = "msys64"
        PostInstall = {
            param($extractPath)
            Write-Status "Configuring MSYS2..."
            
            # Initialize MSYS2
            $msys2Shell = Join-Path $extractPath "msys2_shell.cmd"
            
            if (Test-Path $msys2Shell) {
                Write-Info "Initializing MSYS2 (this may take a few minutes)..."
                
                # First run to initialize
                & $msys2Shell -defterm -no-start -c "exit" 2>&1 | Out-Null
                Start-Sleep -Seconds 2
                
                # Update package database
                Write-Info "Updating package database..."
                & $msys2Shell -defterm -no-start -c "pacman -Sy --noconfirm" 2>&1 | Out-Null
                
                # Install essential packages
                Write-Info "Installing essential packages (git, make, gcc, etc.)..."
                $packages = @(
                    "git",
                    "make",
                    "mingw-w64-x86_64-gcc",
                    "mingw-w64-x86_64-make",
                    "diffutils",
                    "patch",
                    "tar",
                    "zip",
                    "unzip"
                )
                
                $packageList = $packages -join " "
                & $msys2Shell -defterm -no-start -c "pacman -S --noconfirm $packageList" 2>&1 | Out-Null
                
                Write-Success "MSYS2 configured with essential packages"
            } else {
                Write-Failure "MSYS2 shell script not found"
            }
        }
    }
}

# Function to download file with progress
function Download-FileWithProgress {
    param(
        [string]$Url,
        [string]$OutFile
    )
    
    try {
        $webClient = New-Object System.Net.WebClient
        
        $webClient.DownloadFile($Url, $OutFile)
        $webClient.Dispose()
        
        return $true
    }
    catch {
        Write-Failure "Download failed: $_"
        return $false
    }
}

# Function to extract archive
function Extract-Archive {
    param(
        [string]$ArchivePath,
        [string]$DestinationPath
    )
    
    $extension = [System.IO.Path]::GetExtension($ArchivePath).ToLower()
    
    if ($extension -eq ".zip") {
        Expand-Archive -Path $ArchivePath -DestinationPath $DestinationPath -Force
    }
    elseif ($extension -eq ".xz") {
        # For .tar.xz files, we need 7-zip or tar
        if (Get-Command tar -ErrorAction SilentlyContinue) {
            & tar -xf $ArchivePath -C $DestinationPath
        }
        else {
            Write-Failure "tar command not found. Please install 7-Zip or ensure tar is in PATH."
            return $false
        }
    }
    elseif ($extension -eq ".msixbundle" -or $extension -eq ".msix") {
        # Handle MSIX in PostInstall
        return $true
    }
    else {
        Write-Failure "Unsupported archive format: $extension"
        return $false
    }
    
    return $true
}

# Process each vendor dependency
Write-Status "Setting up vendor dependencies..."
Write-Host ""

foreach ($key in $vendorConfig.Keys) {
    $config = $vendorConfig[$key]
    
    Write-Status "Processing: $($config.Name)"
    
    $downloadPath = Join-Path $downloadDir $config.FileName
    $extractPath = Join-Path $vendorDir $config.ExtractDir
    
    # Check if already installed
    if ((Test-Path $extractPath) -and (-not $ForceDownload)) {
        if ($SkipDownload) {
            Write-Info "Already installed, skipping..."
            continue
        }
        
        $response = Read-Host "Already installed. Re-download? (y/N)"
        if ($response -ne "y" -and $response -ne "Y") {
            Write-Info "Skipping..."
            continue
        }
    }
    
    # Download
    if (-not (Test-Path $downloadPath) -or $ForceDownload) {
        Write-Info "Downloading from: $($config.Url)"
        $success = Download-FileWithProgress -Url $config.Url -OutFile $downloadPath
        
        if (-not $success) {
            Write-Failure "Failed to download $($config.Name)"
            continue
        }
        
        Write-Success "Downloaded: $($config.FileName)"
    }
    else {
        Write-Info "Using cached download: $($config.FileName)"
    }
    
    # Extract
    Write-Info "Extracting to: $extractPath"
    
    if (Test-Path $extractPath) {
        Remove-Item $extractPath -Recurse -Force
    }
    
    New-Item -Path $extractPath -ItemType Directory -Force | Out-Null
    
    $extractSuccess = Extract-Archive -ArchivePath $downloadPath -DestinationPath $extractPath
    
    if (-not $extractSuccess) {
        Write-Failure "Failed to extract $($config.Name)"
        continue
    }
    
    # Handle nested extraction (e.g., msys2-base.tar.xz creates msys64 folder)
    if ($key -eq "MSYS2") {
        $nestedMsys64 = Join-Path $extractPath "msys64"
        if (Test-Path $nestedMsys64) {
            # Move contents up one level
            Get-ChildItem $nestedMsys64 | Move-Item -Destination $extractPath -Force
            Remove-Item $nestedMsys64 -Force
        }
    }
    
    Write-Success "Extracted successfully"
    
    # Run post-install actions
    if ($config.PostInstall) {
        & $config.PostInstall $extractPath
    }
    
    Write-Host ""
}

# Create vendor manifest
Write-Status "Creating vendor manifest..."
$manifest = @{
    Version = "1.0.0"
    Created = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Dependencies = @{}
}

foreach ($key in $vendorConfig.Keys) {
    $config = $vendorConfig[$key]
    $manifest.Dependencies[$key] = @{
        Name = $config.Name
        Version = $config.Version
        ExtractDir = $config.ExtractDir
    }
}

$manifestPath = Join-Path $vendorDir "vendor-manifest.json"
$manifest | ConvertTo-Json -Depth 10 | Set-Content $manifestPath -Encoding UTF8
Write-Success "Manifest created: $manifestPath"

Write-Host ""
Write-Success "Vendor setup complete!"
Write-Host ""
Write-Info "Vendored tools:"
Write-Info "  PowerShell: $(Join-Path $vendorDir 'powershell\pwsh.exe')"
Write-Info "  Windows Terminal: $(Join-Path $vendorDir 'terminal\wt.exe')"
Write-Info "  MSYS2: $(Join-Path $vendorDir 'msys64\usr\bin')"
Write-Host ""
Write-Info "Next steps:"
Write-Info "  1. Update naner.json to use vendor paths"
Write-Info "  2. Test with: .\Invoke-Naner.ps1"
