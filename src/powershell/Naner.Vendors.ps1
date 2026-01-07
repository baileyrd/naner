<#
.SYNOPSIS
    Naner Vendor Management Module

.DESCRIPTION
    This module provides vendor dependency management functionality for Naner.
    It handles loading vendor configurations from JSON, orchestrating installations,
    and executing vendor-specific PostInstall logic.

.NOTES
    Part of the Naner portable development environment.
    Requires: Common.psm1, Naner.Archives.psm1

    Module Dependencies:
    - Common.psm1 - Logging and utility functions
    - Naner.Archives.psm1 - Archive extraction functions
#>

# Import Common module for logging
$commonModule = Join-Path $PSScriptRoot "Common.psm1"
if (Test-Path $commonModule) {
    Import-Module $commonModule -Force
}

# Import Archives module for extraction
$archivesModule = Join-Path $PSScriptRoot "Naner.Archives.psm1"
if (Test-Path $archivesModule) {
    Import-Module $archivesModule -Force
}

#region Configuration Management

function Get-VendorConfiguration {
    <#
    .SYNOPSIS
        Loads vendor configuration from vendors.json.

    .DESCRIPTION
        Reads and parses the vendors.json configuration file, validating
        against the schema and returning vendor definitions.

    .PARAMETER ConfigPath
        Path to the vendors.json configuration file.

    .PARAMETER VendorId
        Optional vendor ID to retrieve. If not specified, returns all vendors.

    .OUTPUTS
        Hashtable or array of vendor configurations.

    .EXAMPLE
        $vendors = Get-VendorConfiguration -ConfigPath "C:\naner\config\vendors.json"

    .EXAMPLE
        $rustVendor = Get-VendorConfiguration -ConfigPath "C:\naner\config\vendors.json" -VendorId "Rust"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ConfigPath,

        [Parameter()]
        [string]$VendorId
    )

    if (-not (Test-Path $ConfigPath)) {
        throw "Vendor configuration not found: $ConfigPath"
    }

    try {
        $config = Get-Content $ConfigPath -Raw | ConvertFrom-Json

        if (-not $config.vendors) {
            throw "Invalid vendor configuration: 'vendors' section not found"
        }

        # Convert PSCustomObject to hashtable for easier manipulation
        $vendors = @{}
        $config.vendors.PSObject.Properties | ForEach-Object {
            $vendors[$_.Name] = $_.Value
        }

        if ($VendorId) {
            if ($vendors.ContainsKey($VendorId)) {
                return $vendors[$VendorId]
            } else {
                throw "Vendor not found: $VendorId"
            }
        }

        return $vendors
    }
    catch {
        throw "Failed to load vendor configuration: $_"
    }
}

#endregion

#region Release Information

function Get-VendorRelease {
    <#
    .SYNOPSIS
        Retrieves release information for a vendor.

    .DESCRIPTION
        Fetches the latest release information based on the vendor's
        releaseSource configuration. Supports GitHub API, web scraping,
        static URLs, and Go API.

    .PARAMETER Vendor
        Vendor configuration object.

    .OUTPUTS
        Hashtable with Version, Url, FileName, and Size.

    .EXAMPLE
        $release = Get-VendorRelease -Vendor $vendorConfig
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$Vendor
    )

    $releaseSource = $Vendor.releaseSource

    try {
        switch ($releaseSource.type) {
            "github" {
                return Get-GitHubRelease -ReleaseSource $releaseSource
            }
            "web-scrape" {
                return Get-WebScrapedRelease -ReleaseSource $releaseSource
            }
            "static" {
                return Get-StaticRelease -ReleaseSource $releaseSource
            }
            "golang-api" {
                return Get-GoRelease -ReleaseSource $releaseSource
            }
            default {
                throw "Unsupported release source type: $($releaseSource.type)"
            }
        }
    }
    catch {
        if ($releaseSource.fallback) {
            if (Get-Command Write-Warning -ErrorAction SilentlyContinue) {
                Write-Warning "Failed to get latest release: $_"
                Write-Warning "Using fallback version: $($releaseSource.fallback.version)"
            }
            return @{
                Version = $releaseSource.fallback.version
                Url = $releaseSource.fallback.url
                FileName = $releaseSource.fallback.fileName
                Size = $releaseSource.fallback.size
            }
        }
        throw
    }
}

function Get-GitHubRelease {
    <#
    .SYNOPSIS
        Gets release information from GitHub API.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$ReleaseSource
    )

    $apiUrl = "https://api.github.com/repos/$($ReleaseSource.repo)/releases/latest"

    try {
        $response = Invoke-RestMethod -Uri $apiUrl -Headers @{
            "User-Agent" = "Naner-Vendor-Setup"
        }

        $asset = $response.assets | Where-Object {
            $_.name -like $ReleaseSource.assetPattern
        } | Select-Object -First 1

        if (-not $asset) {
            throw "No matching asset found for pattern: $($ReleaseSource.assetPattern)"
        }

        return @{
            Version = $response.tag_name -replace '^v', ''
            Url = $asset.browser_download_url
            FileName = $asset.name
            Size = "~$([math]::Round($asset.size / 1MB))"
        }
    }
    catch {
        throw "GitHub API request failed: $_"
    }
}

function Get-WebScrapedRelease {
    <#
    .SYNOPSIS
        Gets release information by scraping a web page.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$ReleaseSource
    )

    try {
        $response = Invoke-WebRequest -Uri $ReleaseSource.url -UseBasicParsing

        if ($response.Content -match $ReleaseSource.pattern) {
            $fileName = $matches[1]
            $version = if ($matches.Count -gt 2) { $matches[2] } else { "latest" }

            # Construct full URL if needed
            $downloadUrl = if ($fileName -match '^https?://') {
                $fileName
            } else {
                $baseUrl = [Uri]$ReleaseSource.url
                "$($baseUrl.Scheme)://$($baseUrl.Host)$(Split-Path $baseUrl.AbsolutePath -Parent)/$fileName"
            }

            return @{
                Version = $version
                Url = $downloadUrl
                FileName = $fileName
                Size = $ReleaseSource.size
            }
        }

        throw "Pattern not found in web page: $($ReleaseSource.pattern)"
    }
    catch {
        throw "Web scraping failed: $_"
    }
}

function Get-StaticRelease {
    <#
    .SYNOPSIS
        Returns static release information.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$ReleaseSource
    )

    return @{
        Version = $ReleaseSource.version
        Url = $ReleaseSource.url
        FileName = $ReleaseSource.fileName
        Size = $ReleaseSource.size
    }
}

function Get-GoRelease {
    <#
    .SYNOPSIS
        Gets Go release information from golang.org API.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$ReleaseSource
    )

    try {
        $response = Invoke-RestMethod -Uri $ReleaseSource.url

        # Get the latest stable release for Windows amd64
        $release = $response | Where-Object {
            $_.stable -eq $true -and
            $_.files | Where-Object {
                $_.os -eq "windows" -and
                $_.arch -eq "amd64" -and
                $_.kind -eq "archive"
            }
        } | Select-Object -First 1

        if (-not $release) {
            throw "No matching Go release found"
        }

        $file = $release.files | Where-Object {
            $_.os -eq "windows" -and
            $_.arch -eq "amd64" -and
            $_.kind -eq "archive"
        } | Select-Object -First 1

        return @{
            Version = $release.version
            Url = "https://go.dev/dl/$($file.filename)"
            FileName = $file.filename
            Size = "~$([math]::Round($file.size / 1MB))"
        }
    }
    catch {
        throw "Go API request failed: $_"
    }
}

#endregion

#region Vendor Installation

function Install-VendorPackage {
    <#
    .SYNOPSIS
        Installs a vendor package.

    .DESCRIPTION
        Orchestrates the complete vendor installation process:
        1. Gets release information
        2. Downloads the package
        3. Extracts to destination
        4. Runs PostInstall function

    .PARAMETER VendorId
        The vendor identifier (e.g., "PowerShell", "Rust").

    .PARAMETER Vendor
        Vendor configuration object.

    .PARAMETER DownloadDir
        Directory for downloaded files.

    .PARAMETER VendorDir
        Base directory for vendor installations.

    .PARAMETER SkipDownload
        Skip downloading if file already exists.

    .PARAMETER ForceDownload
        Force re-download even if file exists.

    .OUTPUTS
        Boolean indicating success.

    .EXAMPLE
        $success = Install-VendorPackage -VendorId "PowerShell" -Vendor $config -DownloadDir "C:\downloads" -VendorDir "C:\vendor"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$VendorId,

        [Parameter(Mandatory)]
        [PSCustomObject]$Vendor,

        [Parameter(Mandatory)]
        [string]$DownloadDir,

        [Parameter(Mandatory)]
        [string]$VendorDir,

        [Parameter()]
        [switch]$SkipDownload,

        [Parameter()]
        [switch]$ForceDownload
    )

    try {
        # Get release information
        $releaseInfo = Get-VendorRelease -Vendor $Vendor

        $downloadPath = Join-Path $DownloadDir $releaseInfo.FileName
        $extractPath = Join-Path $VendorDir $Vendor.extractDir

        # Check if already installed
        if ((Test-Path $extractPath) -and -not $ForceDownload -and $SkipDownload) {
            if (Get-Command Write-Info -ErrorAction SilentlyContinue) {
                Write-Info "Already installed, skipping..."
            }
            return $true
        }

        # Download
        if (-not (Test-Path $downloadPath) -or $ForceDownload) {
            if (Get-Command Write-Info -ErrorAction SilentlyContinue) {
                Write-Info "Downloading $($Vendor.name) v$($releaseInfo.Version) ($($releaseInfo.Size) MB)..."
                Write-Info "From: $($releaseInfo.Url)"
            }

            $success = Get-FileWithProgress -Url $releaseInfo.Url -OutFile $downloadPath

            if (-not $success) {
                if (Get-Command Write-Failure -ErrorAction SilentlyContinue) {
                    Write-Failure "Failed to download $($Vendor.name)"
                }
                return $false
            }

            if (Get-Command Write-Success -ErrorAction SilentlyContinue) {
                Write-Success "Downloaded: $($releaseInfo.FileName)"
            }
        } else {
            if (Get-Command Write-Info -ErrorAction SilentlyContinue) {
                Write-Info "Using cached download: $($releaseInfo.FileName)"
            }
        }

        # Extract based on installType
        if ($Vendor.installType -eq "installer") {
            # For installers, just copy to extract path
            if (-not (Test-Path $extractPath)) {
                New-Item -ItemType Directory -Path $extractPath -Force | Out-Null
            }
            Copy-Item $downloadPath $extractPath -Force
        } else {
            # Standard archive extraction
            if (Get-Command Write-Status -ErrorAction SilentlyContinue) {
                Write-Status "Extracting $($Vendor.name)..."
            }

            $success = Expand-VendorArchive -ArchivePath $downloadPath -DestinationPath $VendorDir -ExtractDir $Vendor.extractDir

            if (-not $success) {
                if (Get-Command Write-Failure -ErrorAction SilentlyContinue) {
                    Write-Failure "Failed to extract $($Vendor.name)"
                }
                return $false
            }

            if (Get-Command Write-Success -ErrorAction SilentlyContinue) {
                Write-Success "Extracted to: $extractPath"
            }
        }

        # Run PostInstall function if specified
        if ($Vendor.postInstallFunction) {
            $functionName = "Initialize-$VendorId"

            if (Get-Command $functionName -ErrorAction SilentlyContinue) {
                if (Get-Command Write-Status -ErrorAction SilentlyContinue) {
                    Write-Status "Running PostInstall for $($Vendor.name)..."
                }

                & $functionName -ExtractPath $extractPath -Vendor $Vendor
            } else {
                if (Get-Command Write-Warning -ErrorAction SilentlyContinue) {
                    Write-Warning "PostInstall function not found: $functionName"
                }
            }
        }

        return $true
    }
    catch {
        if (Get-Command Write-Failure -ErrorAction SilentlyContinue) {
            Write-Failure "Installation failed for $($Vendor.name): $_"
        }
        return $false
    }
}

#endregion

#region Vendor PostInstall Functions

function Initialize-SevenZip {
    <#
    .SYNOPSIS
        PostInstall configuration for 7-Zip.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ExtractPath,

        [Parameter()]
        [PSCustomObject]$Vendor
    )

    Write-Info "7-Zip extraction completed"

    # Verify 7z.exe exists
    $sevenZipExe = Join-Path $ExtractPath "7z.exe"
    if (Test-Path $sevenZipExe) {
        Write-Success "7-Zip ready: $sevenZipExe"
    } else {
        Write-Warning "7z.exe not found at expected location"
    }
}

function Initialize-PowerShell {
    <#
    .SYNOPSIS
        PostInstall configuration for PowerShell.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ExtractPath,

        [Parameter()]
        [PSCustomObject]$Vendor
    )

    # Create a pwsh.bat wrapper for easier PATH usage
    $wrapperContent = @"
@echo off
"%~dp0pwsh.exe" %*
"@
    $wrapperPath = Join-Path $ExtractPath "pwsh.bat"
    Set-Content -Path $wrapperPath -Value $wrapperContent -Encoding ASCII
}

function Initialize-WindowsTerminal {
    <#
    .SYNOPSIS
        PostInstall configuration for Windows Terminal.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ExtractPath,

        [Parameter()]
        [PSCustomObject]$Vendor
    )

    Write-Status "Configuring Windows Terminal..."

    # The zip file is already extracted by Expand-VendorArchive
    # Handle nested directory structure (e.g., terminal-1.23.13503.0 folder)
    $subDirs = Get-ChildItem $ExtractPath -Directory -ErrorAction SilentlyContinue

    # If there's exactly one subdirectory and it looks like a version folder, move contents up
    if ($subDirs.Count -eq 1 -and $subDirs[0].Name -match '^terminal-[\d\.]+$') {
        Write-Info "Found nested directory: $($subDirs[0].Name), moving contents up..."
        $nestedDir = $subDirs[0].FullName

        # Move contents up one level
        Get-ChildItem $nestedDir | Move-Item -Destination $ExtractPath -Force

        # Remove the now-empty nested directory
        Remove-Item $nestedDir -Force -ErrorAction SilentlyContinue
    }

    # Verify critical files exist
    $criticalFiles = @("wt.exe", "WindowsTerminal.exe", "OpenConsole.exe")

    Write-Info "Verifying critical files:"
    $allPresent = $true
    foreach ($file in $criticalFiles) {
        $found = Get-ChildItem $ExtractPath -Filter $file -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            $relativePath = $found.FullName.Replace($ExtractPath, "").TrimStart('\')
            Write-Info "  [OK] $file at .\$relativePath"
        } else {
            Write-Info "  [MISSING] $file NOT FOUND!"
            $allPresent = $false
        }
    }

    if (-not $allPresent) {
        Write-Failure "Some critical files are missing!"
    }

    # Create .portable file to enable portable mode
    $portableFile = Join-Path $ExtractPath ".portable"
    New-Item -Path $portableFile -ItemType File -Force | Out-Null
    Write-Info "Created .portable file for portable mode"

    # Count what we have
    $fileCount = (Get-ChildItem $ExtractPath -Recurse -File -ErrorAction SilentlyContinue).Count
    $dirCount = (Get-ChildItem $ExtractPath -Recurse -Directory -ErrorAction SilentlyContinue).Count

    Write-Success "Windows Terminal configured successfully"
    Write-Info "  Files: $fileCount | Directories: $dirCount"
}

function Initialize-MSYS2 {
    <#
    .SYNOPSIS
        PostInstall configuration for MSYS2.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ExtractPath,

        [Parameter()]
        [PSCustomObject]$Vendor
    )

    Write-Status "Configuring MSYS2..."

    # Initialize MSYS2
    $msys2Shell = Join-Path $ExtractPath "msys2_shell.cmd"

    if (Test-Path $msys2Shell) {
        Write-Info "Initializing MSYS2 (this may take a few minutes)..."

        # First run to initialize
        & $msys2Shell -defterm -no-start -c "exit" 2>&1 | Out-Null
        Start-Sleep -Seconds 2

        # Update package database
        Write-Info "Updating package database..."
        & $msys2Shell -defterm -no-start -c "pacman -Sy --noconfirm" 2>&1 | Out-Null

        # Install packages from vendor configuration
        if ($Vendor.packages) {
            Write-Info "Installing packages: $($Vendor.packages -join ', ')..."
            $packageList = $Vendor.packages -join " "
            & $msys2Shell -defterm -no-start -c "pacman -S --noconfirm $packageList" 2>&1 | Out-Null
            Write-Success "MSYS2 configured with packages"
        }
    } else {
        Write-Failure "MSYS2 shell script not found"
    }
}

function Initialize-NodeJS {
    <#
    .SYNOPSIS
        PostInstall configuration for Node.js.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ExtractPath,

        [Parameter()]
        [PSCustomObject]$Vendor
    )

    Write-Status "Configuring Node.js..."

    # Node.js zip contains a versioned folder (e.g., node-v20.11.0-win-x64)
    # Move contents up one level
    $subDirs = Get-ChildItem $ExtractPath -Directory -ErrorAction SilentlyContinue

    if ($subDirs.Count -eq 1 -and $subDirs[0].Name -match '^node-v[\d\.]+-win-x64$') {
        Write-Info "Found nested directory: $($subDirs[0].Name), moving contents up..."
        $nestedDir = $subDirs[0].FullName

        # Move contents up one level
        Get-ChildItem $nestedDir | Move-Item -Destination $ExtractPath -Force

        # Remove the now-empty nested directory
        Remove-Item $nestedDir -Force -ErrorAction SilentlyContinue
    }

    # Verify Node.js executables exist
    $nodeExe = Join-Path $ExtractPath "node.exe"
    $npmCmd = Join-Path $ExtractPath "npm.cmd"

    if (Test-Path $nodeExe) {
        # Get Node.js version
        $nodeVersion = & $nodeExe --version 2>&1
        Write-Success "Node.js installed: $nodeVersion"
    } else {
        Write-Warning "node.exe not found at expected location"
    }

    if (Test-Path $npmCmd) {
        # Get npm version
        $npmVersion = & $nodeExe (Join-Path $ExtractPath "node_modules\npm\bin\npm-cli.js") --version 2>&1
        Write-Success "npm installed: v$npmVersion"
    } else {
        Write-Warning "npm not found at expected location"
    }

    # Configure npm to use portable directories
    Write-Info "Configuring npm for portable usage..."

    # Set npm prefix to portable location (for global packages)
    $npmPrefix = Join-Path (Split-Path (Split-Path $ExtractPath -Parent) -Parent) "home\.npm-global"
    $npmCache = Join-Path (Split-Path (Split-Path $ExtractPath -Parent) -Parent) "home\.npm-cache"

    # Create directories
    New-Item -ItemType Directory -Force -Path $npmPrefix | Out-Null
    New-Item -ItemType Directory -Force -Path $npmCache | Out-Null

    # Configure npm
    & $nodeExe (Join-Path $ExtractPath "node_modules\npm\bin\npm-cli.js") config set prefix $npmPrefix --location=user 2>&1 | Out-Null
    & $nodeExe (Join-Path $ExtractPath "node_modules\npm\bin\npm-cli.js") config set cache $npmCache --location=user 2>&1 | Out-Null

    Write-Success "Node.js configured for portable usage"
    Write-Info "  Global packages: $npmPrefix"
    Write-Info "  npm cache: $npmCache"
}

function Initialize-Miniconda {
    <#
    .SYNOPSIS
        PostInstall configuration for Miniconda (Python).
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ExtractPath,

        [Parameter()]
        [PSCustomObject]$Vendor
    )

    Write-Status "Installing Miniconda..."

    # Miniconda comes as an .exe installer
    $installerPath = Join-Path $ExtractPath "Miniconda3-latest-Windows-x86_64.exe"

    if (Test-Path $installerPath) {
        Write-Info "Running Miniconda silent installation (this may take a few minutes)..."

        # Silent install arguments
        $installArgs = @(
            "/InstallationType=JustMe",
            "/RegisterPython=0",
            "/S",
            "/D=$ExtractPath"
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
    $pythonExe = Join-Path $ExtractPath "python.exe"
    $condaExe = Join-Path $ExtractPath "Scripts\conda.exe"

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

    $nanerRoot = Split-Path (Split-Path $ExtractPath -Parent) -Parent
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

function Initialize-Go {
    <#
    .SYNOPSIS
        PostInstall configuration for Go.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ExtractPath,

        [Parameter()]
        [PSCustomObject]$Vendor
    )

    # Go extracts to a 'go' subdirectory
    $goRoot = $ExtractPath
    $goExe = Join-Path $goRoot "bin\go.exe"

    if (Test-Path $goExe) {
        Write-Info "  [OK] Go installed successfully"

        # Configure portable GOPATH
        $goPath = Join-Path (Split-Path (Split-Path $ExtractPath -Parent) -Parent) "home\go"
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

function Initialize-Rust {
    <#
    .SYNOPSIS
        PostInstall configuration for Rust via rustup.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ExtractPath,

        [Parameter()]
        [PSCustomObject]$Vendor
    )

    Write-Status "Installing Rust via rustup..."

    # rustup-init.exe is the installer
    $rustupInit = Join-Path $ExtractPath "rustup-init.exe"

    if (Test-Path $rustupInit) {
        Write-Info "Running rustup installation (this may take a few minutes)..."

        # Configure portable Cargo/Rustup home
        $nanerRoot = Split-Path (Split-Path $ExtractPath -Parent) -Parent
        $cargoHome = Join-Path $nanerRoot "home\.cargo"
        $rustupHome = Join-Path $nanerRoot "home\.rustup"

        # Create directories
        New-Item -ItemType Directory -Path $cargoHome -Force | Out-Null
        New-Item -ItemType Directory -Path $rustupHome -Force | Out-Null

        # Set environment variables for installation
        $env:CARGO_HOME = $cargoHome
        $env:RUSTUP_HOME = $rustupHome

        # Get installer args from vendor configuration
        $installArgs = if ($Vendor.installerArgs) {
            $Vendor.installerArgs
        } else {
            @(
                "-y",
                "--default-toolchain", "stable",
                "--profile", "default",
                "--no-modify-path"
            )
        }

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

function Initialize-Ruby {
    <#
    .SYNOPSIS
        PostInstall configuration for Ruby.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ExtractPath,

        [Parameter()]
        [PSCustomObject]$Vendor
    )

    # Ruby extracts to rubyinstaller-<version>-x64 directory
    $rubyDir = Get-ChildItem -Path $ExtractPath -Directory | Where-Object { $_.Name -match 'rubyinstaller' } | Select-Object -First 1
    if ($rubyDir) {
        $actualRubyPath = $rubyDir.FullName

        # Move contents up one level
        Get-ChildItem -Path $actualRubyPath | Move-Item -Destination $ExtractPath -Force
        Remove-Item -Path $actualRubyPath -Force
    }

    $rubyExe = Join-Path $ExtractPath "bin\ruby.exe"
    $gemExe = Join-Path $ExtractPath "bin\gem.cmd"

    if ((Test-Path $rubyExe) -and (Test-Path $gemExe)) {
        Write-Info "  [OK] Ruby installed successfully"

        # Configure portable gem home
        $gemHome = Join-Path (Split-Path (Split-Path $ExtractPath -Parent) -Parent) "home\.gem"
        if (-not (Test-Path $gemHome)) {
            New-Item -ItemType Directory -Path $gemHome -Force | Out-Null
        }

        # Create .gemrc for portable configuration
        $homeDir = Split-Path (Split-Path $ExtractPath -Parent) -Parent | Join-Path -ChildPath "home"
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
        $gemPath = Join-Path $ExtractPath 'bin\gem'
        $gemVersion = & $rubyExe $gemPath --version
        Write-Info "  Ruby: $rubyVersion"
        Write-Info "  Gem: $gemVersion"
        Write-Info "  GEM_HOME: $gemHome"

        # Install bundler if not present
        Write-Info "  Installing bundler..."
        $env:GEM_HOME = $gemHome
        $env:GEM_PATH = $gemHome
        & $rubyExe $gemPath install bundler --no-document 2>&1 | Out-Null
        Write-Success "  Bundler installed"
    } else {
        Write-Warning "Ruby executables not found at expected location"
    }
}

#endregion

# Export module functions
# Export-ModuleMember removed - this is a .ps1 file for dot-sourcing, not a module
# All functions are automatically available when dot-sourced
