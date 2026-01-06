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
    
    DYNAMIC VERSION FETCHING:
    This script automatically fetches the latest releases from official sources:
    - PowerShell: GitHub API (PowerShell/PowerShell)
    - Windows Terminal: GitHub API (microsoft/terminal)
    - MSYS2: Web scraping (repo.msys2.org)
    
    Fallback URLs are provided in case API calls fail due to rate limiting
    or network issues. The script will use the fallback URLs and continue.

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
    
.NOTES
    Network Requirements:
    - Internet connection required for downloads
    - GitHub API access (api.github.com)
    - MSYS2 repository access (repo.msys2.org)
    
    GitHub API Rate Limits:
    - Unauthenticated: 60 requests/hour per IP
    - If rate limited, fallback URLs will be used
    
    Total Download Size: ~550MB
    - PowerShell: ~100MB
    - Windows Terminal: ~50MB
    - MSYS2: ~400MB
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

# Function to get latest release info from GitHub
function Get-LatestGitHubRelease {
    param(
        [Parameter(Mandatory)]
        [string]$Repo,
        
        [Parameter(Mandatory)]
        [string]$AssetPattern,
        
        [Parameter()]
        [string]$FallbackUrl = ""
    )
    
    try {
        $apiUrl = "https://api.github.com/repos/$Repo/releases/latest"
        Write-Info "Fetching latest release from $Repo..."
        
        # GitHub API call with User-Agent (required by GitHub)
        $headers = @{
            "User-Agent" = "Naner-Vendor-Setup"
            "Accept" = "application/vnd.github.v3+json"
        }
        
        $release = Invoke-RestMethod -Uri $apiUrl -Headers $headers -TimeoutSec 30
        
        $asset = $release.assets | Where-Object { $_.name -like $AssetPattern } | Select-Object -First 1
        
        if (-not $asset) {
            throw "Could not find asset matching pattern: $AssetPattern"
        }
        
        return @{
            Version = $release.tag_name
            Url = $asset.browser_download_url
            FileName = $asset.name
            Size = [math]::Round($asset.size / 1MB, 2)
        }
    }
    catch {
        Write-Warning "Failed to fetch release info from GitHub API: $_"
        
        if ($FallbackUrl) {
            Write-Info "Using fallback URL..."
            $fileName = [System.IO.Path]::GetFileName($FallbackUrl)
            
            return @{
                Version = "latest"
                Url = $FallbackUrl
                FileName = $fileName
                Size = "Unknown"
            }
        }
        
        Write-Failure "No fallback URL available"
        return $null
    }
}

# Function to get latest MSYS2 release
function Get-LatestMSYS2Release {
    param(
        [Parameter()]
        [string]$FallbackUrl = "https://repo.msys2.org/distrib/x86_64/msys2-base-x86_64-20240727.tar.xz"
    )
    
    try {
        $baseUrl = "https://repo.msys2.org/distrib/x86_64/"
        Write-Info "Fetching latest MSYS2 release..."
        
        # Fetch the directory listing
        $response = Invoke-WebRequest -Uri $baseUrl -UseBasicParsing -TimeoutSec 30
        
        # Parse HTML to find base packages (not sfx)
        $pattern = 'href="(msys2-base-x86_64-(\d{8})\.tar\.xz)"'
        $matches = [regex]::Matches($response.Content, $pattern)
        
        if ($matches.Count -eq 0) {
            throw "Could not find MSYS2 base package"
        }
        
        # Get the most recent (they're named with dates YYYYMMDD)
        $latest = $matches | Sort-Object { $_.Groups[2].Value } -Descending | Select-Object -First 1
        
        $fileName = $latest.Groups[1].Value
        $version = $latest.Groups[2].Value
        
        return @{
            Version = $version
            Url = $baseUrl + $fileName
            FileName = $fileName
            Size = "~400"  # Approximate
        }
    }
    catch {
        Write-Warning "Failed to fetch MSYS2 release info: $_"
        
        if ($FallbackUrl) {
            Write-Info "Using fallback URL..."
            $fileName = [System.IO.Path]::GetFileName($FallbackUrl)
            $version = if ($fileName -match '(\d{8})') { $matches[1] } else { "latest" }
            
            return @{
                Version = $version
                Url = $FallbackUrl
                FileName = $fileName
                Size = "~400"
            }
        }
        
        Write-Failure "No fallback URL available"
        return $null
    }
}

# Vendor configuration - ORDER MATTERS: 7-Zip must be first!
$vendorConfig = [ordered]@{
    SevenZip = @{
        Name = "7-Zip"
        ExtractDir = "7zip"
        GetLatestRelease = {
            # 7-Zip doesn't use GitHub releases API, scrape from official site
            try {
                $downloadPage = "https://www.7-zip.org/download.html"
                Write-Info "Fetching latest 7-Zip version..."
                
                $response = Invoke-WebRequest -Uri $downloadPage -UseBasicParsing -TimeoutSec 30
                
                # Find the x64 MSI download link
                $pattern = 'href="([^"]*7z(\d+)-x64\.msi)"'
                $match = [regex]::Match($response.Content, $pattern)
                
                if ($match.Success) {
                    $fileName = $match.Groups[1].Value
                    $version = $match.Groups[2].Value
                    
                    # Version format: 2408 = 24.08
                    $versionFormatted = "$($version.Substring(0,2)).$($version.Substring(2))"
                    
                    return @{
                        Version = $versionFormatted
                        Url = "https://www.7-zip.org/$fileName"
                        FileName = [System.IO.Path]::GetFileName($fileName)
                        Size = "~2"
                    }
                }
                
                throw "Could not parse 7-Zip version from download page"
            }
            catch {
                Write-Warning "Failed to fetch 7-Zip release info: $_"
                # Fallback to a recent stable version
                $fallbackUrl = "https://www.7-zip.org/a/7z2408-x64.msi"
                return @{
                    Version = "24.08"
                    Url = $fallbackUrl
                    FileName = "7z2408-x64.msi"
                    Size = "~2"
                }
            }
        }
        PostInstall = {
            param($extractPath)
            Write-Info "7-Zip extraction completed"
            
            # Verify 7z.exe exists
            $sevenZipExe = Join-Path $extractPath "7z.exe"
            if (Test-Path $sevenZipExe) {
                Write-Success "7-Zip ready: $sevenZipExe"
            } else {
                Write-Warning "7z.exe not found at expected location"
            }
        }
    }
    
    PowerShell = @{
        Name = "PowerShell"
        ExtractDir = "powershell"
        GetLatestRelease = {
            # Fallback to a recent stable version if API fails
            $fallbackUrl = "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/PowerShell-7.4.6-win-x64.zip"
            return Get-LatestGitHubRelease -Repo "PowerShell/PowerShell" -AssetPattern "*win-x64.zip" -FallbackUrl $fallbackUrl
        }
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
        ExtractDir = "terminal"
        GetLatestRelease = {
            # Fallback to a recent stable version if API fails
            $fallbackUrl = "https://github.com/microsoft/terminal/releases/download/v1.21.2361.0/Microsoft.WindowsTerminal_1.21.2361.0_x64.zip"
            return Get-LatestGitHubRelease -Repo "microsoft/terminal" -AssetPattern "*.msixbundle" -FallbackUrl $fallbackUrl
        }
        PostInstall = {
            param($extractPath)
            Write-Status "Extracting Windows Terminal from MSIX bundle..."
            
            # Use vendored 7-Zip
            $sevenZip = Join-Path $vendorDir "7zip\7z.exe"
            
            # Extract the msixbundle (it's a zip file)
            $bundlePath = Join-Path $downloadDir $releaseInfo.FileName
            $tempExtract = Join-Path $downloadDir "wt_temp"
            
            if (Test-Path $tempExtract) {
                Remove-Item $tempExtract -Recurse -Force
            }
            
            New-Item -Path $tempExtract -ItemType Directory -Force | Out-Null
            
            # Extract bundle
            Write-Info "Extracting MSIX bundle..."
            & $sevenZip x "$bundlePath" -o"$tempExtract" -y | Out-Null
            
            # Find the x64 msix package
            $x64Package = Get-ChildItem $tempExtract -Filter "*x64*.msix" -ErrorAction SilentlyContinue | Select-Object -First 1
            
            if (-not $x64Package) {
                # Try without architecture filter
                $x64Package = Get-ChildItem $tempExtract -Filter "*.msix" -ErrorAction SilentlyContinue | 
                    Where-Object { $_.Name -match "x64|win10" } | 
                    Select-Object -First 1
            }
            
            if ($x64Package) {
                Write-Info "Found package: $($x64Package.Name)"
                
                # Extract the x64 msix package
                $msixExtract = Join-Path $tempExtract "msix_contents"
                New-Item -Path $msixExtract -ItemType Directory -Force | Out-Null
                
                Write-Info "Extracting MSIX package..."
                & $sevenZip x "$($x64Package.FullName)" -o"$msixExtract" -y | Out-Null
                
                # Copy ALL files from the MSIX to destination (more reliable than selective copy)
                Write-Info "Copying Windows Terminal files..."
                
                # Get all files, excluding some unnecessary ones
                $excludePatterns = @(
                    "AppxManifest.xml",
                    "AppxSignature.p7x",
                    "*.pri",
                    "[Content_Types].xml",
                    "AppxBlockMap.xml"
                )
                
                $filesToCopy = Get-ChildItem $msixExtract -File -Recurse | 
                    Where-Object { 
                        $file = $_
                        $exclude = $false
                        foreach ($pattern in $excludePatterns) {
                            if ($file.Name -like $pattern) {
                                $exclude = $true
                                break
                            }
                        }
                        -not $exclude
                    }
                
                $copiedCount = 0
                foreach ($file in $filesToCopy) {
                    $destPath = Join-Path $extractPath $file.Name
                    Copy-Item $file.FullName -Destination $destPath -Force -ErrorAction SilentlyContinue
                    $copiedCount++
                }
                
                Write-Info "Copied $copiedCount files"
                
                # Verify critical files exist
                $criticalFiles = @("wt.exe", "WindowsTerminal.exe", "OpenConsole.exe")
                $missing = @()
                
                foreach ($file in $criticalFiles) {
                    if (-not (Test-Path (Join-Path $extractPath $file))) {
                        $missing += $file
                    }
                }
                
                if ($missing.Count -gt 0) {
                    Write-Failure "Missing critical files: $($missing -join ', ')"
                    Write-Info "Attempting deeper search..."
                    
                    # Try to find files in subdirectories
                    foreach ($file in $missing) {
                        $found = Get-ChildItem $msixExtract -Filter $file -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
                        if ($found) {
                            Copy-Item $found.FullName -Destination (Join-Path $extractPath $file) -Force
                            Write-Info "Found and copied: $file"
                        }
                    }
                }
                
                # Create .portable file to enable portable mode
                $portableFile = Join-Path $extractPath ".portable"
                New-Item -Path $portableFile -ItemType File -Force | Out-Null
                Write-Info "Created .portable file for portable mode"
                
                # Count final files
                $finalFileCount = (Get-ChildItem $extractPath -File).Count
                Write-Success "Windows Terminal extracted successfully ($finalFileCount files)"
                
                # List what we have
                Write-Info "Key files present:"
                foreach ($file in $criticalFiles) {
                    $filePath = Join-Path $extractPath $file
                    if (Test-Path $filePath) {
                        $size = [math]::Round((Get-Item $filePath).Length / 1KB, 2)
                        Write-Info "  ✓ $file ($size KB)"
                    } else {
                        Write-Info "  ✗ $file (MISSING)"
                    }
                }
                
            } else {
                Write-Failure "Could not find x64 MSIX package in bundle"
                Write-Info "Available packages:"
                Get-ChildItem $tempExtract -Filter "*.msix" | ForEach-Object {
                    Write-Info "  - $($_.Name)"
                }
            }
            
            # Cleanup temp extraction
            Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    
    MSYS2 = @{
        Name = "MSYS2"
        ExtractDir = "msys64"
        GetLatestRelease = {
            # Fallback to a known stable version if scraping fails
            $fallbackUrl = "https://repo.msys2.org/distrib/x86_64/msys2-base-x86_64-20240727.tar.xz"
            return Get-LatestMSYS2Release -FallbackUrl $fallbackUrl
        }
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
        [string]$OutFile,
        [int]$MaxRetries = 3
    )
    
    $attempt = 0
    $success = $false
    
    while ($attempt -lt $MaxRetries -and -not $success) {
        $attempt++
        
        try {
            if ($attempt -gt 1) {
                Write-Info "Retry attempt $attempt of $MaxRetries..."
                Start-Sleep -Seconds 2
            }
            
            # Use .NET WebClient for better progress support
            $webClient = New-Object System.Net.WebClient
            
            # Register progress event
            $progressEventHandler = {
                param($sender, $e)
                $percent = [int]($e.BytesReceived / $e.TotalBytesToReceive * 100)
                Write-Progress -Activity "Downloading" -Status "$percent% Complete" -PercentComplete $percent
            }
            
            Register-ObjectEvent -InputObject $webClient -EventName DownloadProgressChanged -SourceIdentifier WebClient.DownloadProgressChanged -Action $progressEventHandler | Out-Null
            
            # Start download
            $downloadTask = $webClient.DownloadFileTaskAsync($Url, $OutFile)
            $downloadTask.Wait()
            
            # Cleanup
            Unregister-Event -SourceIdentifier WebClient.DownloadProgressChanged -ErrorAction SilentlyContinue
            $webClient.Dispose()
            Write-Progress -Activity "Downloading" -Completed
            
            $success = $true
        }
        catch {
            Write-Warning "Download attempt $attempt failed: $($_.Exception.Message)"
            
            if ($attempt -lt $MaxRetries) {
                Write-Info "Retrying..."
            }
            else {
                Write-Failure "Download failed after $MaxRetries attempts: $_"
            }
            
            # Cleanup on error
            Unregister-Event -SourceIdentifier WebClient.DownloadProgressChanged -ErrorAction SilentlyContinue
            
            # Remove partial download
            if (Test-Path $OutFile) {
                Remove-Item $OutFile -Force -ErrorAction SilentlyContinue
            }
        }
    }
    
    return $success
}

# Function to extract archive
function Extract-Archive {
    param(
        [string]$ArchivePath,
        [string]$DestinationPath,
        [string]$VendorDir = ""
    )
    
    $extension = [System.IO.Path]::GetExtension($ArchivePath).ToLower()
    
    if ($extension -eq ".zip") {
        Expand-Archive -Path $ArchivePath -DestinationPath $DestinationPath -Force
    }
    elseif ($extension -eq ".msi") {
        # Extract MSI using msiexec (Windows built-in)
        Write-Info "Extracting MSI using msiexec..."
        
        # Create temp directory for extraction
        $tempExtract = Join-Path ([System.IO.Path]::GetTempPath()) "naner_msi_$([Guid]::NewGuid())"
        New-Item -Path $tempExtract -ItemType Directory -Force | Out-Null
        
        # Use msiexec to extract files (administrative install)
        $msiArgs = @(
            "/a",
            "`"$ArchivePath`"",
            "/qn",
            "TARGETDIR=`"$tempExtract`""
        )
        
        $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $msiArgs -Wait -PassThru -NoNewWindow
        
        if ($process.ExitCode -eq 0) {
            # Find the Files directory (MSI extracts to Files/ProgramFiles/7-Zip)
            $filesDir = Get-ChildItem $tempExtract -Recurse -Directory | 
                Where-Object { $_.Name -eq "7-Zip" } | 
                Select-Object -First 1
            
            if ($filesDir) {
                # Copy extracted files to destination
                Copy-Item "$($filesDir.FullName)\*" -Destination $DestinationPath -Recurse -Force
            }
            else {
                # Fallback: copy everything from temp
                Copy-Item "$tempExtract\*" -Destination $DestinationPath -Recurse -Force
            }
            
            # Cleanup temp directory
            Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue
        }
        else {
            Write-Failure "MSI extraction failed with exit code: $($process.ExitCode)"
            Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue
            return $false
        }
    }
    elseif ($extension -eq ".xz") {
        # For .tar.xz files, we need special handling on Windows
        
        # First, check if we have vendored 7-Zip
        $vendoredSevenZip = if ($VendorDir) { Join-Path $VendorDir "7zip\7z.exe" } else { "" }
        
        if ($vendoredSevenZip -and (Test-Path $vendoredSevenZip)) {
            Write-Info "Using vendored 7-Zip for extraction..."
            
            # Extract .xz to get .tar
            $tarPath = $ArchivePath -replace '\.xz$', ''
            & $vendoredSevenZip x "$ArchivePath" -o"$(Split-Path $ArchivePath)" -y | Out-Null
            
            if ($LASTEXITCODE -ne 0) {
                Write-Failure "Failed to decompress .xz file"
                return $false
            }
            
            # Extract .tar to destination
            & $vendoredSevenZip x "$tarPath" -o"$DestinationPath" -y | Out-Null
            
            if ($LASTEXITCODE -ne 0) {
                Write-Failure "Failed to extract .tar file"
                Remove-Item $tarPath -Force -ErrorAction SilentlyContinue
                return $false
            }
            
            # Cleanup intermediate .tar file
            Remove-Item $tarPath -Force -ErrorAction SilentlyContinue
        }
        # Try system 7-Zip
        else {
            $sevenZipPaths = @(
                "${env:ProgramFiles}\7-Zip\7z.exe",
                "${env:ProgramFiles(x86)}\7-Zip\7z.exe",
                "$env:ProgramData\chocolatey\bin\7z.exe"
            )
            
            $sevenZip = $sevenZipPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
            
            if ($sevenZip) {
                Write-Info "Using system 7-Zip for extraction..."
                
                # Extract .xz to get .tar
                $tarPath = $ArchivePath -replace '\.xz$', ''
                & $sevenZip x "$ArchivePath" -o"$(Split-Path $ArchivePath)" -y | Out-Null
                
                if ($LASTEXITCODE -ne 0) {
                    Write-Failure "Failed to decompress .xz file"
                    return $false
                }
                
                # Extract .tar to destination
                & $sevenZip x "$tarPath" -o"$DestinationPath" -y | Out-Null
                
                if ($LASTEXITCODE -ne 0) {
                    Write-Failure "Failed to extract .tar file"
                    Remove-Item $tarPath -Force -ErrorAction SilentlyContinue
                    return $false
                }
                
                # Cleanup intermediate .tar file
                Remove-Item $tarPath -Force -ErrorAction SilentlyContinue
            }
            # Try tar with Unix-style path (last resort)
            elseif (Get-Command tar -ErrorAction SilentlyContinue) {
                Write-Info "Using tar for extraction..."
                Write-Warning "This may fail on some systems. Consider letting setup complete 7-Zip installation first."
                
                # Create destination directory first
                New-Item -Path $DestinationPath -ItemType Directory -Force | Out-Null
                
                # Change to destination directory and extract
                $currentDir = Get-Location
                Set-Location $DestinationPath
                
                & tar -xf "$ArchivePath" 2>&1 | Out-Null
                $tarExitCode = $LASTEXITCODE
                
                Set-Location $currentDir
                
                if ($tarExitCode -ne 0) {
                    Write-Failure "tar extraction failed"
                    return $false
                }
            }
            else {
                Write-Failure "Cannot extract .tar.xz files - 7-Zip not available"
                Write-Host ""
                Write-Host "This shouldn't happen as 7-Zip is downloaded first." -ForegroundColor Yellow
                Write-Host "Try re-running the setup script." -ForegroundColor Yellow
                return $false
            }
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
    
    # Fetch latest release information
    Write-Info "Fetching latest release information..."
    $releaseInfo = & $config.GetLatestRelease
    
    if (-not $releaseInfo) {
        Write-Failure "Failed to get release information for $($config.Name)"
        continue
    }
    
    Write-Info "Latest version: $($releaseInfo.Version)"
    Write-Info "Download URL: $($releaseInfo.Url)"
    Write-Info "File size: ~$($releaseInfo.Size) MB"
    
    $downloadPath = Join-Path $downloadDir $releaseInfo.FileName
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
        Write-Info "Downloading from: $($releaseInfo.Url)"
        $success = Download-FileWithProgress -Url $releaseInfo.Url -OutFile $downloadPath
        
        if (-not $success) {
            Write-Failure "Failed to download $($config.Name)"
            continue
        }
        
        Write-Success "Downloaded: $($releaseInfo.FileName)"
    }
    else {
        Write-Info "Using cached download: $($releaseInfo.FileName)"
    }
    
    # Extract
    Write-Info "Extracting to: $extractPath"
    
    if (Test-Path $extractPath) {
        Remove-Item $extractPath -Recurse -Force
    }
    
    New-Item -Path $extractPath -ItemType Directory -Force | Out-Null
    
    $extractSuccess = Extract-Archive -ArchivePath $downloadPath -DestinationPath $extractPath -VendorDir $vendorDir
    
    if (-not $extractSuccess) {
        Write-Failure "Failed to extract $($config.Name)"
        continue
    }
    
    # Handle nested extraction (e.g., msys2-base.tar.xz creates msys64 folder)
    if ($key -eq "MSYS2") {
        $nestedMsys64 = Join-Path $extractPath "msys64"
        if (Test-Path $nestedMsys64) {
            Write-Info "Moving MSYS2 files from nested directory..."
            # Move contents up one level
            Get-ChildItem $nestedMsys64 | Move-Item -Destination $extractPath -Force
            Remove-Item $nestedMsys64 -Force
        }
        
        # Verify critical files exist
        $msys2Shell = Join-Path $extractPath "msys2_shell.cmd"
        if (-not (Test-Path $msys2Shell)) {
            Write-Failure "MSYS2 extraction appears incomplete - msys2_shell.cmd not found"
            Write-Info "Expected location: $msys2Shell"
            Write-Info "Contents of $extractPath :"
            Get-ChildItem $extractPath -Name | ForEach-Object { Write-Info "  $_" }
            continue
        }
    }
    
    Write-Success "Extracted successfully"
    
    # Run post-install actions
    if ($config.PostInstall) {
        & $config.PostInstall $extractPath
    }
    
    # Store release info for manifest
    $config.ReleaseInfo = $releaseInfo
    
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
    if ($config.ReleaseInfo) {
        $manifest.Dependencies[$key] = @{
            Name = $config.Name
            Version = $config.ReleaseInfo.Version
            ExtractDir = $config.ExtractDir
            DownloadUrl = $config.ReleaseInfo.Url
            FileName = $config.ReleaseInfo.FileName
            InstalledDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        }
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
