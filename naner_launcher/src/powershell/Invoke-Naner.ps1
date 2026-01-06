<#
.SYNOPSIS
    Naner terminal launcher with vendored dependencies.

.DESCRIPTION
    Launches Windows Terminal with a unified environment containing PowerShell,
    Git, and Unix tools from vendored dependencies.

.PARAMETER Profile
    The profile to launch. Defaults to the DefaultProfile in naner.json.

.PARAMETER StartingDirectory
    The starting directory for the terminal session.

.PARAMETER ConfigPath
    Path to naner.json configuration file.

.PARAMETER Debug
    Enable debug output.

.EXAMPLE
    .\Invoke-Naner.ps1
    
.EXAMPLE
    .\Invoke-Naner.ps1 -Profile Bash -StartingDirectory "C:\Projects"
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$Profile,
    
    [Parameter()]
    [string]$StartingDirectory,
    
    [Parameter()]
    [string]$ConfigPath,
    
    [Parameter()]
    [switch]$Debug
)

$ErrorActionPreference = "Stop"

#region Helper Functions

function Write-DebugInfo {
    param([string]$Message)
    if ($Debug) {
        Write-Host "[DEBUG] $Message" -ForegroundColor Yellow
    }
}

function Find-NanerRoot {
    <#
    .SYNOPSIS
        Locates the Naner root directory by traversing up from the script location.
    #>
    $currentPath = $PSScriptRoot
    $maxDepth = 10
    $depth = 0
    
    Write-DebugInfo "Starting search from: $currentPath"
    
    while ($depth -lt $maxDepth) {
        Write-DebugInfo "Checking depth $depth : $currentPath"
        
        # Check for marker directories
        $binPath = Join-Path $currentPath "bin"
        $vendorPath = Join-Path $currentPath "vendor"
        $configPath = Join-Path $currentPath "config"
        
        if ((Test-Path $binPath) -and (Test-Path $vendorPath) -and (Test-Path $configPath)) {
            Write-DebugInfo "Found Naner root at: $currentPath"
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

function Expand-NanerPath {
    <#
    .SYNOPSIS
        Expands paths containing %NANER_ROOT% and environment variables.
    #>
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

function Get-NanerConfig {
    <#
    .SYNOPSIS
        Loads and validates the Naner configuration.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$ConfigPath,
        
        [Parameter(Mandatory)]
        [string]$NanerRoot
    )
    
    if (-not (Test-Path $ConfigPath)) {
        throw "Configuration file not found: $ConfigPath"
    }
    
    Write-DebugInfo "Loading configuration from: $ConfigPath"
    
    $configJson = Get-Content $ConfigPath -Raw -Encoding UTF8
    $config = $configJson | ConvertFrom-Json
    
    # Validate vendor paths
    if ($config.VendorPaths) {
        Write-DebugInfo "Validating vendor paths..."
        
        foreach ($property in $config.VendorPaths.PSObject.Properties) {
            $expandedPath = Expand-NanerPath -Path $property.Value -NanerRoot $NanerRoot
            
            if (-not (Test-Path $expandedPath)) {
                Write-Warning "Vendor path not found: $($property.Name) = $expandedPath"
            }
            else {
                Write-DebugInfo "  ✓ $($property.Name): $expandedPath"
            }
        }
    }
    
    return $config
}

function Build-UnifiedPath {
    <#
    .SYNOPSIS
        Builds the unified PATH environment variable.
    #>
    param(
        [Parameter(Mandatory)]
        [object]$Config,
        
        [Parameter(Mandatory)]
        [string]$NanerRoot
    )
    
    $pathComponents = @()
    
    if ($Config.Environment.PathPrecedence) {
        Write-DebugInfo "Building unified PATH..."
        
        foreach ($pathEntry in $Config.Environment.PathPrecedence) {
            $expandedPath = Expand-NanerPath -Path $pathEntry -NanerRoot $NanerRoot
            
            if (Test-Path $expandedPath) {
                $pathComponents += $expandedPath
                Write-DebugInfo "  Added: $expandedPath"
            }
            else {
                Write-DebugInfo "  Skipped (not found): $expandedPath"
            }
        }
    }
    
    # Add system PATH if configured
    if ($Config.Environment.InheritSystemPath -or 
        -not $Config.Advanced.PSObject.Properties['InheritSystemPath']) {
        $systemPath = [System.Environment]::GetEnvironmentVariable("PATH", "Machine")
        $userPath = [System.Environment]::GetEnvironmentVariable("PATH", "User")
        
        if ($systemPath) {
            $pathComponents += $systemPath
            Write-DebugInfo "  Added: System PATH"
        }
        
        if ($userPath) {
            $pathComponents += $userPath
            Write-DebugInfo "  Added: User PATH"
        }
    }
    
    return ($pathComponents -join ";")
}

function Get-ShellCommand {
    <#
    .SYNOPSIS
        Determines the shell command based on profile configuration.
    #>
    param(
        [Parameter(Mandatory)]
        [object]$ProfileConfig,
        
        [Parameter(Mandatory)]
        [object]$Config,
        
        [Parameter(Mandatory)]
        [string]$NanerRoot
    )
    
    # Check for custom shell first
    if ($ProfileConfig.CustomShell -and $ProfileConfig.CustomShell.ExecutablePath) {
        $shellPath = Expand-NanerPath -Path $ProfileConfig.CustomShell.ExecutablePath -NanerRoot $NanerRoot
        $shellArgs = if ($ProfileConfig.CustomShell.Arguments) { $ProfileConfig.CustomShell.Arguments } else { "" }
        
        Write-DebugInfo "Using custom shell: $shellPath $shellArgs"
        return @{
            Path = $shellPath
            Arguments = $shellArgs
        }
    }
    
    # Use shell type from profile
    switch ($ProfileConfig.Shell) {
        "PowerShell" {
            $pwshPath = Expand-NanerPath -Path $Config.VendorPaths.PowerShell -NanerRoot $NanerRoot
            Write-DebugInfo "Using PowerShell: $pwshPath"
            return @{
                Path = $pwshPath
                Arguments = "-NoExit -Command `"& { Set-Location '$StartingDirectory' }`""
            }
        }
        
        "Bash" {
            $bashPath = Expand-NanerPath -Path $Config.VendorPaths.GitBash -NanerRoot $NanerRoot
            Write-DebugInfo "Using Bash: $bashPath"
            return @{
                Path = $bashPath
                Arguments = "--login -i"
            }
        }
        
        "CMD" {
            Write-DebugInfo "Using CMD"
            return @{
                Path = "cmd.exe"
                Arguments = "/k"
            }
        }
        
        default {
            throw "Unknown shell type: $($ProfileConfig.Shell)"
        }
    }
}

#endregion

#region Main Execution

try {
    Write-Host "Naner Terminal Launcher" -ForegroundColor Cyan
    Write-Host "========================" -ForegroundColor Cyan
    Write-Host ""
    
    # Locate Naner root
    $nanerRoot = Find-NanerRoot
    Write-Host "Naner Root: $nanerRoot" -ForegroundColor Green
    
    # Determine config path
    if (-not $ConfigPath) {
        $ConfigPath = Join-Path $nanerRoot "config\naner.json"
    }
    
    # Load configuration
    $config = Get-NanerConfig -ConfigPath $ConfigPath -NanerRoot $nanerRoot
    Write-Host "Configuration: $ConfigPath" -ForegroundColor Green
    
    # Determine profile
    if (-not $Profile) {
        $Profile = $config.DefaultProfile
        Write-DebugInfo "Using default profile: $Profile"
    }
    
    # Get profile configuration
    $profileConfig = $config.Profiles.$Profile
    if (-not $profileConfig) {
        # Check custom profiles
        $profileConfig = $config.CustomProfiles.$Profile
        
        if (-not $profileConfig) {
            throw "Profile not found: $Profile"
        }
    }
    
    Write-Host "Profile: $($profileConfig.Name)" -ForegroundColor Green
    
    # Determine starting directory
    if (-not $StartingDirectory) {
        if ($profileConfig.StartingDirectory) {
            $StartingDirectory = Expand-NanerPath -Path $profileConfig.StartingDirectory -NanerRoot $nanerRoot
        }
        else {
            $StartingDirectory = $env:USERPROFILE
        }
    }
    
    Write-Host "Starting Directory: $StartingDirectory" -ForegroundColor Green
    Write-Host ""
    
    # Build unified PATH
    $unifiedPath = Build-UnifiedPath -Config $config -NanerRoot $nanerRoot
    
    # Get shell command
    $shellCommand = Get-ShellCommand -ProfileConfig $profileConfig -Config $config -NanerRoot $nanerRoot
    
    # Get Windows Terminal path
    $wtPath = Expand-NanerPath -Path $config.VendorPaths.WindowsTerminal -NanerRoot $nanerRoot
    
    if (-not (Test-Path $wtPath)) {
        throw "Windows Terminal not found: $wtPath. Run Setup-NanerVendor.ps1 first."
    }
    
    # Build Windows Terminal command
    $wtArgs = @()
    
    # Add profile name
    if ($config.WindowsTerminal.TabTitle) {
        $tabTitle = $config.WindowsTerminal.TabTitle
        $wtArgs += "--title"
        $wtArgs += "`"$tabTitle`""
    }
    
    # Add starting directory
    $wtArgs += "-d"
    $wtArgs += "`"$StartingDirectory`""
    
    # Build environment variables
    $envVars = @{
        "PATH" = $unifiedPath
        "NANER_ROOT" = $nanerRoot
    }
    
    # Add custom environment variables from config
    if ($config.Environment.EnvironmentVariables) {
        foreach ($property in $config.Environment.EnvironmentVariables.PSObject.Properties) {
            $expandedValue = Expand-NanerPath -Path $property.Value -NanerRoot $nanerRoot
            $envVars[$property.Name] = $expandedValue
        }
    }
    
    # Build command line
    $fullCommand = "`"$($shellCommand.Path)`" $($shellCommand.Arguments)"
    
    # Add the -- separator before shell command
    $wtArgs += "--"
    $wtArgs += $shellCommand.Path
    
    if ($shellCommand.Arguments) {
        $wtArgs += $shellCommand.Arguments
    }
    
    Write-DebugInfo "Windows Terminal: $wtPath"
    Write-DebugInfo "Arguments: $($wtArgs -join ' ')"
    Write-DebugInfo "Environment: $($envVars.Keys -join ', ')"
    
    # Set environment variables for the process
    foreach ($key in $envVars.Keys) {
        [System.Environment]::SetEnvironmentVariable($key, $envVars[$key], "Process")
    }
    
    # Launch Windows Terminal
    Write-Host "Launching Windows Terminal..." -ForegroundColor Cyan
    
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $wtPath
    $psi.Arguments = $wtArgs -join ' '
    $psi.UseShellExecute = $false
    
    # Apply environment variables
    foreach ($key in $envVars.Keys) {
        $psi.EnvironmentVariables[$key] = $envVars[$key]
    }
    
    $process = [System.Diagnostics.Process]::Start($psi)
    
    Write-Host "✓ Launched successfully!" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Error: $_" -ForegroundColor Red
    
    if ($Debug) {
        Write-Host ""
        Write-Host "Stack Trace:" -ForegroundColor Yellow
        Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    }
    
    exit 1
}

#endregion
