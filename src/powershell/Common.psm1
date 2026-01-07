<#
.SYNOPSIS
    Common utility functions for Naner scripts.

.DESCRIPTION
    This module provides shared functions used across multiple Naner PowerShell scripts
    to follow DRY principles and maintain consistency.
#>

#region Logging Functions

function Write-Status {
    <#
    .SYNOPSIS
        Writes a status message in cyan.
    #>
    param([string]$Message)
    Write-Host "[*] $Message" -ForegroundColor Cyan
}

function Write-Success {
    <#
    .SYNOPSIS
        Writes a success message in green.
    #>
    param([string]$Message)
    Write-Host "[✓] $Message" -ForegroundColor Green
}

function Write-Failure {
    <#
    .SYNOPSIS
        Writes a failure message in red.
    #>
    param([string]$Message)
    Write-Host "[✗] $Message" -ForegroundColor Red
}

function Write-Info {
    <#
    .SYNOPSIS
        Writes an info message in gray.
    #>
    param([string]$Message)
    Write-Host "    $Message" -ForegroundColor Gray
}

function Write-DebugInfo {
    <#
    .SYNOPSIS
        Writes debug information when debug mode is enabled.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$Message,

        [Parameter()]
        [bool]$DebugMode = $false
    )

    if ($DebugMode) {
        Write-Host "[DEBUG] $Message" -ForegroundColor Yellow
    }
}

#endregion

#region Path Functions

function Find-NanerRoot {
    <#
    .SYNOPSIS
        Locates the Naner root directory by traversing up from the script location.

    .PARAMETER StartPath
        The path to start searching from. Defaults to $PSScriptRoot of the calling script.

    .PARAMETER MaxDepth
        Maximum number of parent directories to traverse.

    .OUTPUTS
        System.String - The absolute path to Naner root directory.

    .EXAMPLE
        $nanerRoot = Find-NanerRoot

    .EXAMPLE
        $nanerRoot = Find-NanerRoot -StartPath "C:\custom\path"
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter()]
        [string]$StartPath = $PSScriptRoot,

        [Parameter()]
        [int]$MaxDepth = 10
    )

    # If calling from another script, try to use its PSScriptRoot
    if (-not $StartPath -or $StartPath -eq $PSScriptRoot) {
        # Get the caller's script root
        $callerScript = Get-PSCallStack | Select-Object -Skip 1 -First 1
        if ($callerScript.ScriptName) {
            $StartPath = Split-Path $callerScript.ScriptName -Parent
        }
    }

    $currentPath = $StartPath
    $depth = 0

    Write-Verbose "Starting search from: $currentPath"

    while ($depth -lt $MaxDepth) {
        Write-Verbose "Checking depth $depth : $currentPath"

        # Check for marker directories
        $binPath = Join-Path $currentPath "bin"
        $vendorPath = Join-Path $currentPath "vendor"
        $configPath = Join-Path $currentPath "config"

        if ((Test-Path $binPath) -and (Test-Path $vendorPath) -and (Test-Path $configPath)) {
            Write-Verbose "Found Naner root at: $currentPath"
            return $currentPath
        }

        $parentPath = Split-Path $currentPath -Parent
        if (-not $parentPath -or $parentPath -eq $currentPath) {
            break
        }

        $currentPath = $parentPath
        $depth++
    }

    throw "Could not locate Naner root directory. Ensure bin/, vendor/, and config/ folders exist."
}

function Get-NanerRootSimple {
    <#
    .SYNOPSIS
        Gets Naner root using simple parent directory navigation.
        This is faster but less robust than Find-NanerRoot.

    .PARAMETER ScriptRoot
        The $PSScriptRoot of the calling script.

    .OUTPUTS
        System.String - The absolute path to Naner root directory.

    .EXAMPLE
        $nanerRoot = Get-NanerRootSimple -ScriptRoot $PSScriptRoot
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$ScriptRoot
    )

    return Split-Path (Split-Path $ScriptRoot -Parent) -Parent
}

function Expand-NanerPath {
    <#
    .SYNOPSIS
        Expands paths containing %NANER_ROOT% and environment variables.

    .PARAMETER Path
        The path string to expand.

    .PARAMETER NanerRoot
        The Naner root directory path.

    .OUTPUTS
        System.String - The expanded path.

    .EXAMPLE
        $expanded = Expand-NanerPath -Path "%NANER_ROOT%\vendor\pwsh" -NanerRoot "C:\naner"
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$NanerRoot
    )

    # Replace %NANER_ROOT% first
    $expanded = $Path -replace '%NANER_ROOT%', $NanerRoot

    # Handle Windows-style environment variables (%VAR%)
    $expanded = [System.Environment]::ExpandEnvironmentVariables($expanded)

    # Handle PowerShell-style environment variables ($env:VAR)
    if ($expanded -match '\$env:(\w+)') {
        $matches | ForEach-Object {
            $varName = $_.Groups[1].Value
            $varValue = [System.Environment]::GetEnvironmentVariable($varName)
            if ($varValue) {
                $expanded = $expanded -replace "\`$env:$varName", $varValue
            }
        }
    }

    return $expanded
}

#endregion

#region Configuration Functions

function Get-NanerConfig {
    <#
    .SYNOPSIS
        Loads and validates the Naner configuration file.

    .PARAMETER ConfigPath
        Path to naner.json configuration file.

    .PARAMETER NanerRoot
        The Naner root directory (for path expansion).

    .PARAMETER ValidateVendorPaths
        Whether to validate vendor paths exist.

    .OUTPUTS
        PSCustomObject - The parsed configuration object.

    .EXAMPLE
        $config = Get-NanerConfig -ConfigPath "C:\naner\config\naner.json" -NanerRoot "C:\naner"
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [Parameter(Mandatory)]
        [string]$ConfigPath,

        [Parameter(Mandatory)]
        [string]$NanerRoot,

        [Parameter()]
        [switch]$ValidateVendorPaths
    )

    if (-not (Test-Path $ConfigPath)) {
        throw "Configuration file not found: $ConfigPath"
    }

    Write-Verbose "Loading configuration from: $ConfigPath"

    try {
        $configJson = Get-Content $ConfigPath -Raw -Encoding UTF8
        $config = $configJson | ConvertFrom-Json
    }
    catch {
        throw "Failed to parse configuration file: $_"
    }

    # Validate vendor paths if requested
    if ($ValidateVendorPaths -and $config.VendorPaths) {
        Write-Verbose "Validating vendor paths..."

        foreach ($property in $config.VendorPaths.PSObject.Properties) {
            $expandedPath = Expand-NanerPath -Path $property.Value -NanerRoot $NanerRoot

            if (-not (Test-Path $expandedPath)) {
                Write-Warning "Vendor path not found: $($property.Name) = $expandedPath"
            }
            else {
                Write-Verbose "  ✓ $($property.Name): $expandedPath"
            }
        }
    }

    return $config
}

#endregion

#region GitHub API Functions

function Get-LatestGitHubRelease {
    <#
    .SYNOPSIS
        Fetches the latest release information from a GitHub repository.

    .PARAMETER Repo
        GitHub repository in format "owner/repo".

    .PARAMETER AssetPattern
        Wildcard pattern to match release assets.

    .PARAMETER FallbackUrl
        Fallback download URL if API call fails.

    .OUTPUTS
        Hashtable with Version, Url, FileName, and Size properties.

    .EXAMPLE
        $release = Get-LatestGitHubRelease -Repo "PowerShell/PowerShell" -AssetPattern "*win-x64.zip"
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
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
        Write-Verbose "Fetching latest release from $Repo..."

        # GitHub API call with User-Agent (required by GitHub)
        $headers = @{
            "User-Agent" = "Naner-Setup-Script"
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
            Write-Verbose "Using fallback URL..."
            $fileName = [System.IO.Path]::GetFileName($FallbackUrl)

            return @{
                Version = "latest"
                Url = $FallbackUrl
                FileName = $fileName
                Size = "Unknown"
            }
        }

        throw "No fallback URL available and GitHub API call failed"
    }
}

#endregion

# Export functions
Export-ModuleMember -Function @(
    'Write-Status',
    'Write-Success',
    'Write-Failure',
    'Write-Info',
    'Write-DebugInfo',
    'Find-NanerRoot',
    'Get-NanerRootSimple',
    'Expand-NanerPath',
    'Get-NanerConfig',
    'Get-LatestGitHubRelease'
)
