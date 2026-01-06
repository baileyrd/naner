<#
.SYNOPSIS
    Naner launcher for Windows Terminal - PowerShell implementation

.DESCRIPTION
    This script launches Windows Terminal with Cmder-like environment setup.
    It provides the core functionality needed for a Cmder replacement using Windows Terminal.

.PARAMETER Profile
    Windows Terminal profile to launch (default: uses DefaultProfile from config)

.PARAMETER Task
    Legacy Cmder task name for compatibility

.PARAMETER Config
    Path to Naner configuration directory (default: %NANER_ROOT%\config)

.PARAMETER Register
    Register Naner integration (context menu, etc.)

.PARAMETER Unregister
    Unregister Naner integration

.PARAMETER StartDir
    Starting directory for the terminal

.EXAMPLE
    .\Launch-Naner.ps1
    Launch with default profile from config

.EXAMPLE
    .\Launch-Naner.ps1 -Profile "Command Prompt"
    Launch with Command Prompt profile

.EXAMPLE
    .\Launch-Naner.ps1 -StartDir "C:\Projects"
    Launch in specific directory with default profile
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Profile = "",
    
    [Parameter()]
    [string]$Task,
    
    [Parameter()]
    [string]$Config,
    
    [Parameter()]
    [string]$StartDir,
    
    [Parameter()]
    [switch]$Register,
    
    [Parameter()]
    [switch]$Unregister
)

# Script metadata
$Script:Version = "1.0.0"
$Script:Name = "Naner Launcher"

#region Configuration Management

function Get-NanerRoot {
    <#
    .SYNOPSIS
        Get the Naner root directory
    #>
    
    # Check if NANER_ROOT is set
    if ($env:NANER_ROOT) {
        return $env:NANER_ROOT
    }
    
    # Search up the directory tree for config folder
    $currentPath = Split-Path -Parent $PSCommandPath
    $maxLevels = 5  # Don't search too far up
    
    for ($i = 0; $i -lt $maxLevels; $i++) {
        $configPath = Join-Path $currentPath "config"
        
        if (Test-Path $configPath) {
            Write-Verbose "Found Naner root at: $currentPath"
            return $currentPath
        }
        
        # Move up one level
        $parentPath = Split-Path -Parent $currentPath
        
        # Stop if we've reached the root or can't go further
        if (-not $parentPath -or $parentPath -eq $currentPath) {
            break
        }
        
        $currentPath = $parentPath
    }
    
    Write-Error "Could not determine Naner root directory. Please set NANER_ROOT environment variable."
    return $null
}

function Get-NanerConfig {
    <#
    .SYNOPSIS
        Load Naner configuration
    #>
    param(
        [string]$ConfigPath
    )
    
    $nanerRoot = Get-NanerRoot
    if (-not $nanerRoot) {
        return $null
    }
    
    # Determine config path
    if (-not $ConfigPath) {
        $ConfigPath = Join-Path $nanerRoot "config"
    }
    
    # Load user-settings.json if it exists
    $settingsFile = Join-Path $ConfigPath "user-settings.json"
    $settings = @{
        NanerRoot = $nanerRoot
        ConfigPath = $ConfigPath
        DefaultProfile = "PowerShell"
        StartupDir = $null
        WindowsTerminalPath = $null
        CustomProfiles = @{}
    }
    
    if (Test-Path $settingsFile) {
        try {
            $userSettings = Get-Content $settingsFile -Raw | ConvertFrom-Json
            
            # Merge user settings with environment variable expansion
            if ($userSettings.DefaultProfile) {
                $settings.DefaultProfile = $userSettings.DefaultProfile
            }
            if ($userSettings.StartupDir) {
                $settings.StartupDir = Expand-EnvironmentVariables $userSettings.StartupDir
            }
            if ($userSettings.WindowsTerminalPath) {
                $settings.WindowsTerminalPath = Expand-EnvironmentVariables $userSettings.WindowsTerminalPath
            }
            if ($userSettings.CustomProfiles) {
                # Process custom profiles
                $customProfiles = @{}
                Write-Verbose "Loading custom profiles from configuration..."
                
                foreach ($prop in $userSettings.CustomProfiles.PSObject.Properties) {
                    $profileName = $prop.Name
                    $profileConfig = $prop.Value
                    
                    Write-Verbose "  Loading profile: $profileName"
                    
                    $customProfiles[$profileName] = @{
                        ShellPath = if ($profileConfig.ShellPath) { 
                            $expandedPath = Expand-EnvironmentVariables $profileConfig.ShellPath
                            Write-Verbose "    ShellPath: $expandedPath"
                            $expandedPath
                        } else { 
                            $null 
                        }
                        Arguments = if ($profileConfig.Arguments) { 
                            Write-Verbose "    Arguments: $($profileConfig.Arguments)"
                            $profileConfig.Arguments 
                        } else { 
                            $null 
                        }
                        Title = if ($profileConfig.Title) { 
                            Write-Verbose "    Title: $($profileConfig.Title)"
                            $profileConfig.Title 
                        } else { 
                            $profileName 
                        }
                    }
                }
                $settings.CustomProfiles = $customProfiles
                Write-Verbose "Loaded $($customProfiles.Count) custom profile(s)"
            }
        }
        catch {
            Write-Warning "Failed to load user settings: $_"
        }
    }
    
    return $settings
}

function Expand-EnvironmentVariables {
    <#
    .SYNOPSIS
        Expand environment variables in a string
    .DESCRIPTION
        Supports both Windows (%VAR%) and PowerShell ($env:VAR) style variables
    #>
    param(
        [string]$InputString
    )
    
    if ([string]::IsNullOrEmpty($InputString)) {
        return $InputString
    }
    
    # Expand Windows-style variables (%VAR%)
    $expanded = [System.Environment]::ExpandEnvironmentVariables($InputString)
    
    # Also support PowerShell-style variables ($env:VAR)
    # Match $env:VARIABLENAME pattern
    $pattern = '\$env:([A-Za-z_][A-Za-z0-9_]*)'
    $matches = [regex]::Matches($expanded, $pattern)
    
    foreach ($match in $matches) {
        $varName = $match.Groups[1].Value
        $varValue = [System.Environment]::GetEnvironmentVariable($varName)
        if ($varValue) {
            $expanded = $expanded -replace [regex]::Escape($match.Value), $varValue
        }
    }
    
    return $expanded
}

#endregion

#region Windows Terminal Detection

function Get-WindowsTerminalPath {
    <#
    .SYNOPSIS
        Find Windows Terminal executable
    #>
    param(
        [string]$CustomPath
    )
    
    # Check custom path first if provided
    if ($CustomPath) {
        if (Test-Path $CustomPath) {
            Write-Verbose "Using custom Windows Terminal path: $CustomPath"
            return $CustomPath
        }
        else {
            Write-Warning "Custom Windows Terminal path not found: $CustomPath"
        }
    }
    
    # Check common locations
    $locations = @(
        "${env:LOCALAPPDATA}\Microsoft\WindowsApps\wt.exe",
        "${env:ProgramFiles}\WindowsApps\Microsoft.WindowsTerminal_*\wt.exe",
        "${env:ProgramFiles}\WindowsApps\Microsoft.WindowsTerminalPreview_*\wt.exe"
    )
    
    foreach ($location in $locations) {
        $resolved = Resolve-Path $location -ErrorAction SilentlyContinue
        if ($resolved) {
            return $resolved.Path | Select-Object -First 1
        }
    }
    
    # Try PATH
    $wtPath = Get-Command wt.exe -ErrorAction SilentlyContinue
    if ($wtPath) {
        return $wtPath.Source
    }
    
    return $null
}

function Test-WindowsTerminalInstalled {
    <#
    .SYNOPSIS
        Check if Windows Terminal is installed
    #>
    param(
        [string]$CustomPath
    )
    
    $wtPath = Get-WindowsTerminalPath -CustomPath $CustomPath
    if ($wtPath) {
        Write-Verbose "Windows Terminal found at: $wtPath"
        return $true
    }
    
    Write-Error "Windows Terminal is not installed. Please install it from the Microsoft Store or specify WindowsTerminalPath in config/user-settings.json"
    return $false
}

#endregion

#region Environment Setup

function Initialize-NanerEnvironment {
    <#
    .SYNOPSIS
        Set up Naner environment variables
    #>
    param(
        [hashtable]$Config
    )
    
    # Set core environment variables
    $env:NANER_ROOT = $Config.NanerRoot
    $env:NANER_VERSION = $Script:Version
    
    # Add Naner bin to PATH if it exists
    $binPath = Join-Path $Config.NanerRoot "bin"
    if (Test-Path $binPath) {
        if ($env:PATH -notlike "*$binPath*") {
            $env:PATH = "$binPath;$env:PATH"
        }
    }
    
    # Add vendor bin to PATH if it exists
    $vendorBinPath = Join-Path $Config.NanerRoot "vendor\bin"
    if (Test-Path $vendorBinPath) {
        if ($env:PATH -notlike "*$vendorBinPath*") {
            $env:PATH = "$vendorBinPath;$env:PATH"
        }
    }
    
    Write-Verbose "Naner environment initialized"
}

#endregion

#region Profile Management

function Get-ProfileMapping {
    <#
    .SYNOPSIS
        Map legacy Cmder task names to Windows Terminal profiles
    #>
    param(
        [string]$TaskName
    )
    
    $mappings = @{
        "PowerShell" = "PowerShell"
        "cmd" = "Command Prompt"
        "bash" = "Git Bash"
        "wsl" = "Ubuntu"
        "ubuntu" = "Ubuntu"
    }
    
    if ($mappings.ContainsKey($TaskName)) {
        return $mappings[$TaskName]
    }
    
    return $TaskName
}

function Build-WindowsTerminalArgs {
    <#
    .SYNOPSIS
        Build Windows Terminal command line arguments
    #>
    param(
        [string]$Profile,
        [string]$StartDirectory,
        [hashtable]$Config,
        [hashtable]$CustomProfileConfig
    )
    
    $args = @()
    
    # Check if using a custom profile with custom shell
    if ($CustomProfileConfig -and $CustomProfileConfig.ShellPath) {
        # Use custom shell path with proper Windows Terminal syntax
        # Format: wt.exe [options] -- command args
        
        # Add title if specified
        if ($CustomProfileConfig.Title) {
            $args += "--title"
            $args += "`"$($CustomProfileConfig.Title)`""
        }
        
        # Add starting directory
        if ($StartDirectory) {
            $args += "-d"
            $args += "`"$StartDirectory`""
        }
        elseif ($Config.StartupDir) {
            $args += "-d"
            $args += "`"$($Config.StartupDir)`""
        }
        
        # Add the separator to indicate start of command
        $args += "--"
        
        # Add the custom shell command
        $args += "`"$($CustomProfileConfig.ShellPath)`""
        
        # Add shell arguments if specified
        if ($CustomProfileConfig.Arguments) {
            $args += $CustomProfileConfig.Arguments
        }
    }
    else {
        # Use standard profile
        if ($Profile) {
            $args += "-p"
            $args += "`"$Profile`""
        }
        
        # Add starting directory
        if ($StartDirectory) {
            $args += "-d"
            $args += "`"$StartDirectory`""
        }
        elseif ($Config.StartupDir) {
            $args += "-d"
            $args += "`"$($Config.StartupDir)`""
        }
    }
    
    return $args -join " "
}


#endregion

#region Launch Logic

function Start-WindowsTerminal {
    <#
    .SYNOPSIS
        Launch Windows Terminal with specified configuration
    #>
    param(
        [string]$Profile,
        [string]$Task,
        [string]$StartDir,
        [hashtable]$Config
    )
    
    Write-Verbose "=== Start-WindowsTerminal ==="
    Write-Verbose "Profile parameter: '$Profile'"
    Write-Verbose "Task parameter: '$Task'"
    Write-Verbose "StartDir parameter: '$StartDir'"
    
    # Verify Windows Terminal is installed
    if (-not (Test-WindowsTerminalInstalled -CustomPath $Config.WindowsTerminalPath)) {
        return $false
    }
    
    # Determine profile to use
    $targetProfile = $Profile
    if ($Task) {
        $targetProfile = Get-ProfileMapping -TaskName $Task
        Write-Verbose "Mapped task '$Task' to profile '$targetProfile'"
    }
    
    # Use default profile if none specified (handle empty string or null)
    if ([string]::IsNullOrWhiteSpace($targetProfile)) {
        $targetProfile = $Config.DefaultProfile
        Write-Verbose "No profile specified, using default from config: '$targetProfile'"
    }
    else {
        Write-Verbose "Using specified profile: '$targetProfile'"
    }
    
    # Check if this is a custom profile
    $customProfileConfig = $null
    if ($Config.CustomProfiles) {
        Write-Verbose "Custom profiles available: $($Config.CustomProfiles.Keys -join ', ')"
        
        if ($Config.CustomProfiles.ContainsKey($targetProfile)) {
            $customProfileConfig = $Config.CustomProfiles[$targetProfile]
            Write-Verbose "*** Using custom profile configuration for '$targetProfile' ***"
            Write-Verbose "  Shell: $($customProfileConfig.ShellPath)"
            if ($customProfileConfig.Arguments) {
                Write-Verbose "  Args: $($customProfileConfig.Arguments)"
            }
            if ($customProfileConfig.Title) {
                Write-Verbose "  Title: $($customProfileConfig.Title)"
            }
        }
        else {
            Write-Verbose "Profile '$targetProfile' not found in custom profiles, using as standard profile"
        }
    }
    else {
        Write-Verbose "No custom profiles defined"
    }
    
    # Determine starting directory
    $startDirectory = $StartDir
    if (-not $startDirectory -and $Config.StartupDir) {
        $startDirectory = $Config.StartupDir
    }
    if ($startDirectory) {
        Write-Verbose "Starting directory: $startDirectory"
    }
    
    # Build arguments
    $wtArgs = Build-WindowsTerminalArgs -Profile $targetProfile -StartDirectory $startDirectory -Config $Config -CustomProfileConfig $customProfileConfig
    
    # Get Windows Terminal path
    $wtPath = Get-WindowsTerminalPath -CustomPath $Config.WindowsTerminalPath
    
    # Launch Windows Terminal
    Write-Verbose "=== Launching Windows Terminal ==="
    Write-Verbose "Command: $wtPath $wtArgs"
    
    $success = $false
    try {
        Start-Process -FilePath $wtPath -ArgumentList $wtArgs
        $success = $true
        Write-Verbose "Successfully launched Windows Terminal"
    }
    catch {
        Write-Error "Failed to launch Windows Terminal: $_"
        $success = $false
    }
    
    return $success
}

#endregion

#region Registration

function Register-NanerProfile {
    <#
    .SYNOPSIS
        Register Naner with Windows (context menu integration, etc.)
    #>
    
    Write-Host "Registering Naner integration..."
    
    # Get script path
    $scriptPath = $PSCommandPath
    $nanerRoot = Get-NanerRoot
    
    if (-not $nanerRoot) {
        Write-Error "Cannot register: Naner root not found"
        return $false
    }
    
    # Registry path for context menu
    $registryPath = "HKCU:\Software\Classes\Directory\Background\shell\Naner"
    
    $success = $false
    try {
        # Create registry keys
        if (-not (Test-Path $registryPath)) {
            New-Item -Path $registryPath -Force | Out-Null
        }
        
        Set-ItemProperty -Path $registryPath -Name "(Default)" -Value "Open Naner here"
        Set-ItemProperty -Path $registryPath -Name "Icon" -Value "$nanerRoot\icons\naner.ico"
        
        # Create command subkey
        $commandPath = "$registryPath\command"
        if (-not (Test-Path $commandPath)) {
            New-Item -Path $commandPath -Force | Out-Null
        }
        
        $commandValue = "powershell.exe -ExecutionPolicy Bypass -File `"$scriptPath`" -StartDir `"%V`""
        Set-ItemProperty -Path $commandPath -Name "(Default)" -Value $commandValue
        
        Write-Host "[OK] Naner registered successfully" -ForegroundColor Green
        Write-Host "  Right-click in any folder and select 'Open Naner here'"
        
        $success = $true
    }
    catch {
        Write-Error "Failed to register Naner: $_"
        $success = $false
    }
    
    return $success
}

function Unregister-NanerProfile {
    <#
    .SYNOPSIS
        Unregister Naner integration
    #>
    
    Write-Host "Unregistering Naner integration..."
    
    $registryPath = "HKCU:\Software\Classes\Directory\Background\shell\Naner"
    
    $success = $false
    try {
        if (Test-Path $registryPath) {
            Remove-Item -Path $registryPath -Recurse -Force
            Write-Host "[OK] Naner unregistered successfully" -ForegroundColor Green
            $success = $true
        }
        else {
            Write-Host "Naner was not registered" -ForegroundColor Yellow
            $success = $true
        }
    }
    catch {
        Write-Error "Failed to unregister Naner: $_"
        $success = $false
    }
    
    return $success
}

#endregion

#region Main Entry Point

function Main {
    <#
    .SYNOPSIS
        Main entry point
    #>
    
    Write-Verbose "Naner Launcher v$($Script:Version)"
    
    # Handle registration commands
    if ($Register) {
        $success = Register-NanerProfile
        if ($success) { exit 0 } else { exit 1 }
    }
    
    if ($Unregister) {
        $success = Unregister-NanerProfile
        if ($success) { exit 0 } else { exit 1 }
    }
    
    # Load configuration
    $config = Get-NanerConfig -ConfigPath $Config
    if (-not $config) {
        Write-Error "Failed to load configuration"
        exit 1
    }
    
    # Initialize environment
    Initialize-NanerEnvironment -Config $config
    
    # Launch Windows Terminal
    $success = Start-WindowsTerminal -Profile $Profile -Task $Task -StartDir $StartDir -Config $config
    
    if ($success) {
        exit 0
    }
    else {
        exit 1
    }
}

#endregion

# Execute main function
Main