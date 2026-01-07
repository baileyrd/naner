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

# Import common utilities - REQUIRED
$commonModule = Join-Path $PSScriptRoot "Common.psm1"
if (-not (Test-Path $commonModule)) {
    throw "Common.psm1 module not found at: $commonModule`nThis module is required for Setup-NanerVendor.ps1 to function."
}

Import-Module $commonModule -Force

# Determine Naner root
if (-not $NanerRoot) {
    $NanerRoot = Get-NanerRootSimple -ScriptRoot $PSScriptRoot
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

# NOTE: Get-LatestGitHubRelease is now provided by Common.psm1

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
            return Get-LatestGitHubRelease -Repo "microsoft/terminal" -AssetPattern "*_x64.zip" -FallbackUrl $fallbackUrl
        }
        PostInstall = {
            param($extractPath)
            Write-Status "Configuring Windows Terminal..."

            # The zip file is already extracted by Expand-VendorArchive
            # Handle nested directory structure (e.g., terminal-1.23.13503.0 folder)
            $subDirs = Get-ChildItem $extractPath -Directory -ErrorAction SilentlyContinue

            # If there's exactly one subdirectory and it looks like a version folder, move contents up
            if ($subDirs.Count -eq 1 -and $subDirs[0].Name -match '^terminal-[\d\.]+$') {
                Write-Info "Found nested directory: $($subDirs[0].Name), moving contents up..."
                $nestedDir = $subDirs[0].FullName

                # Move contents up one level
                Get-ChildItem $nestedDir | Move-Item -Destination $extractPath -Force

                # Remove the now-empty nested directory
                Remove-Item $nestedDir -Force -ErrorAction SilentlyContinue
            }

            # Verify critical files exist
            $criticalFiles = @("wt.exe", "WindowsTerminal.exe", "OpenConsole.exe")

            Write-Info "Verifying critical files:"
            $allPresent = $true
            foreach ($file in $criticalFiles) {
                $found = Get-ChildItem $extractPath -Filter $file -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
                if ($found) {
                    $relativePath = $found.FullName.Replace($extractPath, "").TrimStart('\')
                    Write-Info "  ✓ $file at .\$relativePath"
                } else {
                    Write-Info "  ✗ $file NOT FOUND!"
                    $allPresent = $false
                }
            }

            if (-not $allPresent) {
                Write-Failure "Some critical files are missing!"
            }

            # Create .portable file to enable portable mode
            $portableFile = Join-Path $extractPath ".portable"
            New-Item -Path $portableFile -ItemType File -Force | Out-Null
            Write-Info "Created .portable file for portable mode"

            # Count what we have
            $fileCount = (Get-ChildItem $extractPath -Recurse -File -ErrorAction SilentlyContinue).Count
            $dirCount = (Get-ChildItem $extractPath -Recurse -Directory -ErrorAction SilentlyContinue).Count

            Write-Success "Windows Terminal configured successfully"
            Write-Info "  Files: $fileCount | Directories: $dirCount"
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

    NodeJS = @{
        Name = "Node.js"
        ExtractDir = "nodejs"
        GetLatestRelease = {
            # Fallback to a recent LTS version if API fails
            $fallbackUrl = "https://nodejs.org/dist/v20.11.0/node-v20.11.0-win-x64.zip"
            return Get-LatestGitHubRelease -Repo "nodejs/node" -AssetPattern "*win-x64.zip" -FallbackUrl $fallbackUrl
        }
        PostInstall = {
            param($extractPath)
            Write-Status "Configuring Node.js..."

            # Node.js zip contains a versioned folder (e.g., node-v20.11.0-win-x64)
            # Move contents up one level
            $subDirs = Get-ChildItem $extractPath -Directory -ErrorAction SilentlyContinue

            if ($subDirs.Count -eq 1 -and $subDirs[0].Name -match '^node-v[\d\.]+-win-x64$') {
                Write-Info "Found nested directory: $($subDirs[0].Name), moving contents up..."
                $nestedDir = $subDirs[0].FullName

                # Move contents up one level
                Get-ChildItem $nestedDir | Move-Item -Destination $extractPath -Force

                # Remove the now-empty nested directory
                Remove-Item $nestedDir -Force -ErrorAction SilentlyContinue
            }

            # Verify Node.js executables exist
            $nodeExe = Join-Path $extractPath "node.exe"
            $npmCmd = Join-Path $extractPath "npm.cmd"
            $npxCmd = Join-Path $extractPath "npx.cmd"

            if (Test-Path $nodeExe) {
                # Get Node.js version
                $nodeVersion = & $nodeExe --version 2>&1
                Write-Success "Node.js installed: $nodeVersion"
            } else {
                Write-Warning "node.exe not found at expected location"
            }

            if (Test-Path $npmCmd) {
                # Get npm version
                $npmVersion = & $nodeExe (Join-Path $extractPath "node_modules\npm\bin\npm-cli.js") --version 2>&1
                Write-Success "npm installed: v$npmVersion"
            } else {
                Write-Warning "npm not found at expected location"
            }

            # Configure npm to use portable directories
            Write-Info "Configuring npm for portable usage..."

            # Set npm prefix to portable location (for global packages)
            $npmPrefix = Join-Path (Split-Path (Split-Path $extractPath -Parent) -Parent) "home\.npm-global"
            $npmCache = Join-Path (Split-Path (Split-Path $extractPath -Parent) -Parent) "home\.npm-cache"

            # Create directories
            New-Item -ItemType Directory -Force -Path $npmPrefix | Out-Null
            New-Item -ItemType Directory -Force -Path $npmCache | Out-Null

            # Configure npm
            & $nodeExe (Join-Path $extractPath "node_modules\npm\bin\npm-cli.js") config set prefix $npmPrefix --location=user 2>&1 | Out-Null
            & $nodeExe (Join-Path $extractPath "node_modules\npm\bin\npm-cli.js") config set cache $npmCache --location=user 2>&1 | Out-Null

            Write-Success "Node.js configured for portable usage"
            Write-Info "  Global packages: $npmPrefix"
            Write-Info "  npm cache: $npmCache"
        }
    }

    Miniconda = @{
        Name = "Miniconda (Python)"
        ExtractDir = "miniconda"
        GetLatestRelease = {
            # Miniconda3 latest version (Python 3.11+)
            $fallbackUrl = "https://repo.anaconda.com/miniconda/Miniconda3-latest-Windows-x86_64.exe"

            return @{
                Version = "latest"
                Url = $fallbackUrl
                FileName = "Miniconda3-latest-Windows-x86_64.exe"
                Size = "~50"
            }
        }
        PostInstall = {
            param($extractPath)
            Write-Status "Installing Miniconda..."

            # Miniconda comes as an .exe installer
            $installerPath = Join-Path $extractPath "Miniconda3-latest-Windows-x86_64.exe"

            if (Test-Path $installerPath) {
                Write-Info "Running Miniconda silent installation (this may take a few minutes)..."

                # Silent install arguments
                $installArgs = @(
                    "/InstallationType=JustMe",
                    "/RegisterPython=0",
                    "/S",
                    "/D=$extractPath"
                )

                # Run installer
                $process = Start-Process -FilePath $installerPath -ArgumentList $installArgs -Wait -PassThru

                if ($process.ExitCode -eq 0) {
                    Write-Success "Miniconda installed successfully"
                    Remove-Item $installerPath -Force -ErrorAction SilentlyContinue
                } else {
                    Write-Warning "Miniconda installation returned exit code: $($process.ExitCode)"
                }
            }

            # Verify executables
            $pythonExe = Join-Path $extractPath "python.exe"
            $condaExe = Join-Path $extractPath "Scripts\conda.exe"

            if (Test-Path $pythonExe) {
                $pythonVersion = & $pythonExe --version 2>&1
                Write-Success "Python installed: $pythonVersion"
            }

            if (Test-Path $condaExe) {
                $condaVersion = & $condaExe --version 2>&1
                Write-Success "Conda installed: $condaVersion"
            }

            # Configure conda for portable usage
            Write-Info "Configuring conda for portable usage..."

            $nanerRoot = Split-Path (Split-Path $extractPath -Parent) -Parent
            $condaPkgs = Join-Path $nanerRoot "home\.conda\pkgs"
            $condaEnvs = Join-Path $nanerRoot "home\.conda\envs"

            New-Item -ItemType Directory -Force -Path $condaPkgs | Out-Null
            New-Item -ItemType Directory -Force -Path $condaEnvs | Out-Null

            if (Test-Path $condaExe) {
                & $condaExe config --add pkgs_dirs $condaPkgs 2>&1 | Out-Null
                & $condaExe config --add envs_dirs $condaEnvs 2>&1 | Out-Null
                & $condaExe config --set auto_activate_base false 2>&1 | Out-Null

                Write-Success "Conda configured for portable usage"
                Write-Info "  Package cache: $condaPkgs"
                Write-Info "  Environments: $condaEnvs"
            }
        }
    }

    Go = @{
        Name = "Go"
        ExtractDir = "go"
        GetLatestRelease = {
            try {
                # Get latest stable Go version from golang.org
                $goDownloadPage = Invoke-WebRequest -Uri "https://go.dev/dl/?mode=json" -UseBasicParsing
                $releases = $goDownloadPage.Content | ConvertFrom-Json

                # Find latest stable Windows AMD64 zip
                $latestRelease = $releases | Where-Object { $_.stable -eq $true } | Select-Object -First 1
                $asset = $latestRelease.files | Where-Object { $_.os -eq "windows" -and $_.arch -eq "amd64" -and $_.kind -eq "archive" } | Select-Object -First 1

                if ($asset) {
                    return @{
                        Version = $latestRelease.version
                        Url = "https://go.dev/dl/$($asset.filename)"
                        FileName = $asset.filename
                        Size = [math]::Round($asset.size / 1MB, 2)
                    }
                }
            } catch {
                Write-Warning "Could not fetch latest Go release: $_"
            }

            # Fallback to known stable version
            $fallbackVersion = "go1.21.6"
            $fallbackFile = "$fallbackVersion.windows-amd64.zip"
            return @{
                Version = $fallbackVersion
                Url = "https://go.dev/dl/$fallbackFile"
                FileName = $fallbackFile
                Size = "~140"
            }
        }
        PostInstall = {
            param($extractPath)

            # Go extracts to a 'go' subdirectory
            $goRoot = $extractPath
            $goExe = Join-Path $goRoot "bin\go.exe"

            if (Test-Path $goExe) {
                Write-Info "  ✓ Go installed successfully"

                # Configure portable GOPATH
                $goPath = Join-Path (Split-Path (Split-Path $extractPath -Parent) -Parent) "home\go"
                if (-not (Test-Path $goPath)) {
                    New-Item -ItemType Directory -Path $goPath -Force | Out-Null
                    New-Item -ItemType Directory -Path (Join-Path $goPath "bin") -Force | Out-Null
                    New-Item -ItemType Directory -Path (Join-Path $goPath "pkg") -Force | Out-Null
                    New-Item -ItemType Directory -Path (Join-Path $goPath "src") -Force | Out-Null
                }

                # Display version
                $version = & $goExe version
                Write-Info "  Version: $version"
                Write-Info "  GOROOT: $goRoot"
                Write-Info "  GOPATH: $goPath"
            } else {
                Write-Warning "Go executable not found at expected location"
            }
        }
    }

    Rust = @{
        Name = "Rust"
        ExtractDir = "rust"
        GetLatestRelease = {
            # Use rustup-init.exe for Rust installation
            return @{
                Version = "latest"
                Url = "https://static.rust-lang.org/rustup/dist/x86_64-pc-windows-msvc/rustup-init.exe"
                FileName = "rustup-init.exe"
                Size = "~10"
            }
        }
        PostInstall = {
            param($extractPath)
            Write-Status "Installing Rust via rustup..."

            # rustup-init.exe is the installer
            $rustupInit = Join-Path $extractPath "rustup-init.exe"

            if (Test-Path $rustupInit) {
                Write-Info "Running rustup installation (this may take a few minutes)..."

                # Configure portable Cargo/Rustup home
                $nanerRoot = Split-Path (Split-Path $extractPath -Parent) -Parent
                $cargoHome = Join-Path $nanerRoot "home\.cargo"
                $rustupHome = Join-Path $nanerRoot "home\.rustup"

                # Create directories
                New-Item -ItemType Directory -Path $cargoHome -Force | Out-Null
                New-Item -ItemType Directory -Path $rustupHome -Force | Out-Null

                # Set environment variables for installation
                $env:CARGO_HOME = $cargoHome
                $env:RUSTUP_HOME = $rustupHome

                # Silent install arguments for rustup
                $installArgs = @(
                    "-y",
                    "--default-toolchain", "stable",
                    "--profile", "default",
                    "--no-modify-path"
                )

                # Run installer
                $process = Start-Process -FilePath $rustupInit -ArgumentList $installArgs -Wait -PassThru -NoNewWindow

                if ($process.ExitCode -eq 0) {
                    Write-Success "Rust installed successfully via rustup"
                    Remove-Item $rustupInit -Force -ErrorAction SilentlyContinue
                } else {
                    Write-Warning "Rustup installation returned exit code: $($process.ExitCode)"
                }

                # Create config.toml for portable registry
                $cargoConfig = Join-Path $cargoHome "config.toml"
                if (-not (Test-Path $cargoConfig)) {
                    $configContent = @"
# Portable Cargo configuration for Naner

[build]
# Number of parallel jobs, defaults to # of CPUs
# jobs = 1

[term]
# Verbosity
# verbose = false
# color = 'auto'
"@
                    Set-Content -Path $cargoConfig -Value $configContent -Encoding UTF8
                }

                # Verify executables
                $cargoExe = Join-Path $cargoHome "bin\cargo.exe"
                $rustcExe = Join-Path $cargoHome "bin\rustc.exe"
                $rustupExe = Join-Path $cargoHome "bin\rustup.exe"

                if (Test-Path $cargoExe) {
                    $cargoVersion = & $cargoExe --version 2>&1
                    Write-Success "Cargo: $cargoVersion"
                }

                if (Test-Path $rustcExe) {
                    $rustcVersion = & $rustcExe --version 2>&1
                    Write-Success "Rustc: $rustcVersion"
                }

                if (Test-Path $rustupExe) {
                    $rustupVersion = & $rustupExe --version 2>&1
                    Write-Success "Rustup: $rustupVersion"
                }

                Write-Info "  CARGO_HOME: $cargoHome"
                Write-Info "  RUSTUP_HOME: $rustupHome"
            } else {
                Write-Warning "rustup-init.exe not found at expected location"
            }
        }
    }

    Ruby = @{
        Name = "Ruby"
        ExtractDir = "ruby"
        GetLatestRelease = {
            try {
                # Get latest Ruby version from RubyInstaller GitHub releases
                $apiUrl = "https://api.github.com/repos/oneclick/rubyinstaller2/releases/latest"
                $release = Invoke-RestMethod -Uri $apiUrl -Headers @{ "User-Agent" = "Naner-Setup" }

                # Find Ruby+Devkit x64 7z archive
                $asset = $release.assets | Where-Object {
                    $_.name -match 'rubyinstaller-devkit-.*-x64\.7z$'
                } | Select-Object -First 1

                if ($asset) {
                    # Extract version from filename (e.g., rubyinstaller-devkit-3.2.3-1-x64.7z)
                    if ($asset.name -match 'rubyinstaller-devkit-([0-9.]+(?:-\d+)?)-x64\.7z') {
                        $version = $matches[1]
                    } else {
                        $version = $release.tag_name -replace '^RubyInstaller-', ''
                    }

                    return @{
                        Version = $version
                        Url = $asset.browser_download_url
                        FileName = $asset.name
                        Size = [math]::Round($asset.size / 1MB, 2)
                    }
                }
            } catch {
                Write-Warning "Could not fetch latest Ruby release: $_"
            }

            # Fallback to known stable version
            $fallbackVersion = "3.2.3-1"
            $fallbackFile = "rubyinstaller-devkit-$fallbackVersion-x64.7z"
            return @{
                Version = $fallbackVersion
                Url = "https://github.com/oneclick/rubyinstaller2/releases/download/RubyInstaller-$fallbackVersion/$fallbackFile"
                FileName = $fallbackFile
                Size = "~180"
            }
        }
        PostInstall = {
            param($extractPath)

            # Ruby extracts to rubyinstaller-<version>-x64 directory
            $rubyDir = Get-ChildItem -Path $extractPath -Directory | Where-Object { $_.Name -match 'rubyinstaller' } | Select-Object -First 1
            if ($rubyDir) {
                $actualRubyPath = $rubyDir.FullName

                # Move contents up one level
                Get-ChildItem -Path $actualRubyPath | Move-Item -Destination $extractPath -Force
                Remove-Item -Path $actualRubyPath -Force
            }

            $rubyExe = Join-Path $extractPath "bin\ruby.exe"
            $gemExe = Join-Path $extractPath "bin\gem.cmd"

            if ((Test-Path $rubyExe) -and (Test-Path $gemExe)) {
                Write-Info "  ✓ Ruby installed successfully"

                # Configure portable gem home
                $gemHome = Join-Path (Split-Path (Split-Path $extractPath -Parent) -Parent) "home\.gem"
                if (-not (Test-Path $gemHome)) {
                    New-Item -ItemType Directory -Path $gemHome -Force | Out-Null
                }

                # Create .gemrc for portable configuration
                $homeDir = Split-Path (Split-Path $extractPath -Parent) -Parent | Join-Path -ChildPath "home"
                $gemrc = Join-Path $homeDir ".gemrc"
                if (-not (Test-Path $gemrc)) {
                    $gemrcContent = @"
# Portable gem configuration for Naner
gem: --no-document
install: --env-shebang
update: --env-shebang
"@
                    Set-Content -Path $gemrc -Value $gemrcContent -Encoding UTF8
                }

                # Display version
                $rubyVersion = & $rubyExe --version
                $gemVersion = & $rubyExe (Join-Path $extractPath "bin\gem") --version
                Write-Info "  Ruby: $rubyVersion"
                Write-Info "  Gem: $gemVersion"
                Write-Info "  GEM_HOME: $gemHome"

                # Install bundler if not present
                Write-Info "  Installing bundler..."
                $env:GEM_HOME = $gemHome
                $env:GEM_PATH = $gemHome
                & $rubyExe (Join-Path $extractPath "bin\gem") install bundler --no-document 2>&1 | Out-Null
                Write-Success "  Bundler installed"
            } else {
                Write-Warning "Ruby executables not found at expected location"
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
            $webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36")

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

# Helper function to get available 7-Zip executable
function Get-SevenZipPath {
    param(
        [string]$VendorDir = ""
    )

    # First, check if we have vendored 7-Zip
    $vendoredSevenZip = if ($VendorDir) { Join-Path $VendorDir "7zip\7z.exe" } else { "" }

    if ($vendoredSevenZip -and (Test-Path $vendoredSevenZip)) {
        return @{
            Path = $vendoredSevenZip
            Source = "vendored"
        }
    }

    # Try system 7-Zip
    $sevenZipPaths = @(
        "${env:ProgramFiles}\7-Zip\7z.exe",
        "${env:ProgramFiles(x86)}\7-Zip\7z.exe",
        "$env:ProgramData\chocolatey\bin\7z.exe"
    )

    $sevenZip = $sevenZipPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($sevenZip) {
        return @{
            Path = $sevenZip
            Source = "system"
        }
    }

    return $null
}

# Helper function to expand archive using 7-Zip
function Expand-ArchiveWith7Zip {
    param(
        [string]$ArchivePath,
        [string]$DestinationPath,
        [string]$VendorDir = ""
    )

    $sevenZipInfo = Get-SevenZipPath -VendorDir $VendorDir

    if (-not $sevenZipInfo) {
        return $false
    }

    Write-Info "Using $($sevenZipInfo.Source) 7-Zip for extraction..."
    $sevenZip = $sevenZipInfo.Path

    $extension = [System.IO.Path]::GetExtension($ArchivePath).ToLower()

    if ($extension -eq ".xz") {
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
    else {
        # Standard extraction for other formats
        & $sevenZip x "$ArchivePath" -o"$DestinationPath" -y | Out-Null

        if ($LASTEXITCODE -ne 0) {
            Write-Failure "7-Zip extraction failed"
            return $false
        }
    }

    return $true
}

# Function to expand vendor archive files
function Expand-VendorArchive {
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
        # For .tar.xz files, try 7-Zip first, then fallback to tar
        $success = Expand-ArchiveWith7Zip -ArchivePath $ArchivePath -DestinationPath $DestinationPath -VendorDir $VendorDir

        if (-not $success) {
            # Try tar with Unix-style path (last resort)
            if (Get-Command tar -ErrorAction SilentlyContinue) {
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
    
    $extractSuccess = Expand-VendorArchive -ArchivePath $downloadPath -DestinationPath $extractPath -VendorDir $vendorDir
    
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