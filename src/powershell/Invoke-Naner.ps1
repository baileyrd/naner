<#
.SYNOPSIS
    Naner terminal launcher with vendored dependencies.

.DESCRIPTION
    Launches Windows Terminal with a unified environment containing PowerShell,
    Git, and Unix tools from vendored dependencies.

.PARAMETER Profile
    The profile to launch. Defaults to the DefaultProfile in naner.json.

.PARAMETER Environment
    The environment to use. Defaults to the active environment or "default".

.PARAMETER StartingDirectory
    The starting directory for the terminal session.

.PARAMETER ConfigPath
    Path to naner.json configuration file.

.PARAMETER DebugMode
    Enable debug output.

.EXAMPLE
    .\Invoke-Naner.ps1

.EXAMPLE
    .\Invoke-Naner.ps1 -Profile Bash -StartingDirectory "C:\Projects"

.EXAMPLE
    .\Invoke-Naner.ps1 -Environment "work"

.EXAMPLE
    .\Invoke-Naner.ps1 -DebugMode
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$Profile,

    [Parameter()]
    [string]$Environment,

    [Parameter()]
    [string]$StartingDirectory,

    [Parameter()]
    [string]$ConfigPath,

    [Parameter()]
    [switch]$DebugMode
)

$ErrorActionPreference = "Stop"

# Import common utilities - REQUIRED
$commonModule = Join-Path $PSScriptRoot "Common.psm1"
if (-not (Test-Path $commonModule)) {
    throw "Common.psm1 module not found at: $commonModule`nThis module is required for Invoke-Naner.ps1 to function."
}

Import-Module $commonModule -Force

# Import environment management module
$envModule = Join-Path $PSScriptRoot "Naner.Environments.psm1"
if (Test-Path $envModule) {
    Import-Module $envModule -Force
}

#region Helper Functions

function Write-DebugInfo {
    param([string]$Message)
    if ($DebugMode) {
        Write-Host "[DEBUG] $Message" -ForegroundColor Yellow
    }
}

# Note: Find-NanerRoot, Expand-NanerPath, and Get-NanerConfig are now imported from Common.psm1

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

    # Determine active environment
    if (-not $Environment) {
        if (Get-Command Get-ActiveNanerEnvironment -ErrorAction SilentlyContinue) {
            $Environment = Get-ActiveNanerEnvironment -NanerRoot $nanerRoot
        }
        else {
            $Environment = "default"
        }
    }

    Write-Host "Environment: $Environment" -ForegroundColor Green

    # Set HOME environment variable based on environment
    if ($Environment -eq "default") {
        $homeDir = Join-Path $nanerRoot "home"
    }
    else {
        $homeDir = Join-Path $nanerRoot "home\environments\$Environment"

        if (-not (Test-Path $homeDir)) {
            throw "Environment '$Environment' not found at: $homeDir`nCreate it with: New-NanerEnvironment -Name '$Environment'"
        }
    }

    Write-DebugInfo "HOME directory: $homeDir"
    
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
        "NANER_ENVIRONMENT" = $Environment
    }

    # Add custom environment variables from config
    if ($config.Environment.EnvironmentVariables) {
        foreach ($property in $config.Environment.EnvironmentVariables.PSObject.Properties) {
            # Special handling for HOME - use environment-specific directory
            if ($property.Name -eq "HOME") {
                $envVars["HOME"] = $homeDir
                Write-DebugInfo "Set HOME to: $homeDir"
            }
            else {
                # Replace %NANER_ROOT%\home with actual home directory for environment
                $value = $property.Value
                if ($value -match '%NANER_ROOT%\\home') {
                    $value = $value -replace '%NANER_ROOT%\\home', $homeDir
                }

                $expandedValue = Expand-NanerPath -Path $value -NanerRoot $nanerRoot
                $envVars[$property.Name] = $expandedValue
                Write-DebugInfo "Set $($property.Name) to: $expandedValue"
            }
        }
    }

    # Ensure HOME is set correctly
    if (-not $envVars.ContainsKey("HOME")) {
        $envVars["HOME"] = $homeDir
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
    
    # Launch Windows Terminal
    Write-Host "Launching Windows Terminal..." -ForegroundColor Cyan
    
    try {
        # Use Start-Process with environment variables
        $startArgs = @{
            FilePath = $wtPath
            ArgumentList = $wtArgs
            PassThru = $true
        }
        
        # Set environment variables for this process (inherited by child)
        foreach ($key in $envVars.Keys) {
            [System.Environment]::SetEnvironmentVariable($key, $envVars[$key], "Process")
        }
        
        $process = Start-Process @startArgs
        
        # Give it a moment to start
        Start-Sleep -Milliseconds 500
        
        # Check if process started successfully
        if ($process -and -not $process.HasExited) {
            Write-Host "✓ Launched successfully! (PID: $($process.Id))" -ForegroundColor Green
        }
        elseif ($process -and $process.HasExited) {
            Write-Host "⚠ Process started but exited immediately (Exit Code: $($process.ExitCode))" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "Troubleshooting:" -ForegroundColor Cyan
            Write-Host "  1. Run: .\Test-WindowsTerminalLaunch.ps1" -ForegroundColor Gray
            Write-Host "  2. Try: .\Invoke-Naner.ps1 -DebugMode" -ForegroundColor Gray
            Write-Host "  3. Check Task Manager for WindowsTerminal.exe" -ForegroundColor Gray
        }
        else {
            Write-Host "✓ Launched successfully!" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "✗ Failed to launch Windows Terminal" -ForegroundColor Red
        Write-Host "Error: $_" -ForegroundColor Red
        throw
    }
}
catch {
    Write-Host ""
    Write-Host "Error: $_" -ForegroundColor Red
    
    if ($DebugMode) {
        Write-Host ""
        Write-Host "Stack Trace:" -ForegroundColor Yellow
        Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    }
    
    exit 1
}

#endregion
