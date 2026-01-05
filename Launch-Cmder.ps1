#Requires -Version 5.1
<#
.SYNOPSIS
    Cmder Windows Terminal Launcher - PowerShell Edition
    
.DESCRIPTION
    A launcher that mimics Cmder.exe functionality but uses Windows Terminal instead of ConEmu.
    This is the rapid prototype version - will be ported to C# for production.
    
.PARAMETER StartDir
    Directory to start the terminal in. Defaults to current directory.
    
.PARAMETER Profile
    Windows Terminal profile to use. Defaults to "Cmder".
    
.PARAMETER Task
    Alias for Profile parameter (for Cmder compatibility).
    
.PARAMETER Config
    Path to custom Cmder configuration root.
    
.PARAMETER Icon
    Path to custom icon (for future GUI implementation).
    
.PARAMETER Single
    Open in existing Windows Terminal window if possible.
    
.PARAMETER Register
    Register Cmder in Windows Explorer context menu. Values: USER, ALL
    
.PARAMETER Unregister
    Unregister Cmder from Windows Explorer context menu. Values: USER, ALL
    
.PARAMETER New
    Force new Windows Terminal window.
    
.PARAMETER Verbose
    Show detailed logging information.
    
.EXAMPLE
    .\Launch-Cmder.ps1
    Launches Windows Terminal with Cmder profile in current directory.
    
.EXAMPLE
    .\Launch-Cmder.ps1 -StartDir "C:\Projects"
    Launches in C:\Projects directory.
    
.EXAMPLE
    .\Launch-Cmder.ps1 -Profile "PowerShell" -Single
    Opens PowerShell profile in existing window.
    
.EXAMPLE
    .\Launch-Cmder.ps1 -Register USER
    Registers "Cmder Here" in Windows Explorer context menu.
    
.NOTES
    Version: 1.0.0-prototype
    Author: Hybrid Development Approach
    Purpose: Rapid prototype for feature validation
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$StartDir,
    
    [Parameter()]
    [string]$Profile = "Cmder",
    
    [Parameter()]
    [string]$Task,
    
    [Parameter()]
    [Alias("c")]
    [string]$Config,
    
    [Parameter()]
    [string]$Icon,
    
    [Parameter()]
    [switch]$Single,
    
    [Parameter()]
    [switch]$New,
    
    [Parameter()]
    [ValidateSet("USER", "ALL")]
    [string]$Register,
    
    [Parameter()]
    [ValidateSet("USER", "ALL")]
    [string]$Unregister
)

# ==============================================================================
# CONFIGURATION
# ==============================================================================

$ErrorActionPreference = "Stop"
$Script:Version = "1.0.0-prototype"

# Determine Cmder root directory
$Script:CmderRoot = if ($Config) {
    $Config
} else {
    # Assume script is in launcher/powershell subdirectory
    $scriptPath = $PSScriptRoot
    if ($scriptPath -match 'src[\\/]powershell$') {
        Split-Path (Split-Path $scriptPath -Parent) -Parent
    } else {
        $scriptPath
    }
}

# ==============================================================================
# LOGGING
# ==============================================================================

function Write-Log {
    param(
        [string]$Message,
        [ValidateSet("Info", "Success", "Warning", "Error")]
        [string]$Level = "Info"
    )
    
    if (-not $VerbosePreference -and $Level -eq "Info") { return }
    
    $colors = @{
        Info    = "Cyan"
        Success = "Green"
        Warning = "Yellow"
        Error   = "Red"
    }
    
    $prefix = @{
        Info    = "[INFO]"
        Success = "[OK]"
        Warning = "[WARN]"
        Error   = "[ERROR]"
    }
    
    Write-Host "$($prefix[$Level]) $Message" -ForegroundColor $colors[$Level]
}

# ==============================================================================
# ENVIRONMENT SETUP
# ==============================================================================

function Initialize-CmderEnvironment {
    Write-Log "Initializing Cmder environment..."
    
    # Set Cmder environment variables
    $env:CMDER_ROOT = $Script:CmderRoot
    $env:CMDER_USER_CONFIG = Join-Path $Script:CmderRoot "config"
    $env:CMDER_USER_BIN = Join-Path $Script:CmderRoot "bin"
    
    Write-Log "CMDER_ROOT: $env:CMDER_ROOT"
    Write-Log "CMDER_USER_CONFIG: $env:CMDER_USER_CONFIG"
    Write-Log "CMDER_USER_BIN: $env:CMDER_USER_BIN"
    
    # Create directories if they don't exist
    @($env:CMDER_USER_CONFIG, $env:CMDER_USER_BIN) | ForEach-Object {
        if (-not (Test-Path $_)) {
            New-Item -ItemType Directory -Path $_ -Force | Out-Null
            Write-Log "Created directory: $_"
        }
    }
}

# ==============================================================================
# WINDOWS TERMINAL DETECTION
# ==============================================================================

function Find-WindowsTerminal {
    Write-Log "Searching for Windows Terminal..."
    
    # Common installation paths
    $searchPaths = @(
        # Windows Store version
        "$env:LOCALAPPDATA\Microsoft\WindowsApps\wt.exe",
        
        # Direct install
        "$env:PROGRAMFILES\WindowsApps\Microsoft.WindowsTerminal_*\wt.exe",
        
        # Preview version
        "$env:LOCALAPPDATA\Microsoft\WindowsApps\wtd.exe",
        
        # Portable version
        "$env:PROGRAMFILES\WindowsTerminal\wt.exe"
    )
    
    foreach ($path in $searchPaths) {
        # Handle wildcards
        if ($path -like "*`**") {
            $resolved = Get-Item $path -ErrorAction SilentlyContinue | 
                        Sort-Object LastWriteTime -Descending | 
                        Select-Object -First 1
            if ($resolved) {
                Write-Log "Found Windows Terminal at: $($resolved.FullName)" -Level Success
                return $resolved.FullName
            }
        } else {
            if (Test-Path $path) {
                Write-Log "Found Windows Terminal at: $path" -Level Success
                return $path
            }
        }
    }
    
    # Try PATH
    $wtPath = Get-Command wt.exe -ErrorAction SilentlyContinue
    if ($wtPath) {
        Write-Log "Found Windows Terminal in PATH: $($wtPath.Source)" -Level Success
        return $wtPath.Source
    }
    
    return $null
}

# ==============================================================================
# WINDOWS TERMINAL CONFIGURATION
# ==============================================================================

function Get-WindowsTerminalSettingsPath {
    # Windows Terminal stores settings in LocalState
    $basePath = "$env:LOCALAPPDATA\Packages"
    
    $wtPackages = @(
        "Microsoft.WindowsTerminal_8wekyb3d8bbwe",
        "Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe"
    )
    
    foreach ($package in $wtPackages) {
        $settingsPath = Join-Path $basePath "$package\LocalState\settings.json"
        if (Test-Path $settingsPath) {
            return $settingsPath
        }
    }
    
    return $null
}

function Test-CmderProfile {
    $settingsPath = Get-WindowsTerminalSettingsPath
    if (-not $settingsPath) {
        Write-Log "Windows Terminal settings not found" -Level Warning
        return $false
    }
    
    Write-Log "Checking for Cmder profile in: $settingsPath"
    
    try {
        $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
        
        $hasCmderProfile = $settings.profiles.list | 
                          Where-Object { $_.name -eq "Cmder" } | 
                          Measure-Object | 
                          Select-Object -ExpandProperty Count
        
        return $hasCmderProfile -gt 0
    } catch {
        Write-Log "Failed to parse settings.json: $_" -Level Warning
        return $false
    }
}

function Add-CmderProfile {
    Write-Log "Adding Cmder profile to Windows Terminal..." -Level Info
    
    $settingsPath = Get-WindowsTerminalSettingsPath
    if (-not $settingsPath) {
        Write-Log "Cannot add profile: Windows Terminal settings not found" -Level Error
        return $false
    }
    
    try {
        # Backup original settings
        $backupPath = "$settingsPath.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        Copy-Item $settingsPath $backupPath
        Write-Log "Created backup: $backupPath"
        
        # Read and parse settings
        $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
        
        # Create Cmder profile
        $cmderProfile = @{
            name              = "Cmder"
            commandline       = "cmd.exe /k $env:CMDER_ROOT\vendor\init.bat"
            startingDirectory = "%USERPROFILE%"
            icon              = "$env:CMDER_ROOT\icons\cmder.ico"
            colorScheme       = "Campbell"
            hidden            = $false
        }
        
        # Add to profiles list
        $profilesList = @($settings.profiles.list)
        $profilesList += $cmderProfile
        $settings.profiles.list = $profilesList
        
        # Save settings
        $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding UTF8
        
        Write-Log "Cmder profile added successfully!" -Level Success
        return $true
        
    } catch {
        Write-Log "Failed to add Cmder profile: $_" -Level Error
        return $false
    }
}

# ==============================================================================
# SHELL INTEGRATION (CONTEXT MENU)
# ==============================================================================

function Register-CmderShellIntegration {
    param(
        [ValidateSet("USER", "ALL")]
        [string]$Scope = "USER"
    )
    
    $hive = if ($Scope -eq "ALL") { "HKLM" } else { "HKCU" }
    $basePath = if ($Scope -eq "ALL") {
        "HKEY_LOCAL_MACHINE\SOFTWARE\Classes"
    } else {
        "HKEY_CURRENT_USER\SOFTWARE\Classes"
    }
    
    Write-Log "Registering Cmder shell integration ($Scope)..."
    
    # Check admin rights if installing for ALL
    if ($Scope -eq "ALL") {
        $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
        if (-not $isAdmin) {
            Write-Log "Administrator rights required for ALL scope" -Level Error
            return $false
        }
    }
    
    $launcherPath = Join-Path $Script:CmderRoot "Cmder.exe"
    $iconPath = Join-Path $Script:CmderRoot "icons\cmder.ico"
    
    # Registry entries to create
    $entries = @(
        @{
            Path    = "$basePath\Directory\Background\shell\Cmder"
            Name    = "(Default)"
            Value   = "Cmder Here"
            Type    = "String"
        },
        @{
            Path    = "$basePath\Directory\Background\shell\Cmder"
            Name    = "Icon"
            Value   = $iconPath
            Type    = "String"
        },
        @{
            Path    = "$basePath\Directory\Background\shell\Cmder\command"
            Name    = "(Default)"
            Value   = "`"$launcherPath`" `"%V`""
            Type    = "String"
        },
        @{
            Path    = "$basePath\Directory\shell\Cmder"
            Name    = "(Default)"
            Value   = "Cmder Here"
            Type    = "String"
        },
        @{
            Path    = "$basePath\Directory\shell\Cmder\command"
            Name    = "(Default)"
            Value   = "`"$launcherPath`" `"%1`""
            Type    = "String"
        }
    )
    
    try {
        foreach ($entry in $entries) {
            # Create path if it doesn't exist
            if (-not (Test-Path "Registry::$($entry.Path)")) {
                New-Item -Path "Registry::$($entry.Path)" -Force | Out-Null
            }
            
            # Set value
            Set-ItemProperty -Path "Registry::$($entry.Path)" -Name $entry.Name -Value $entry.Value -Type $entry.Type
            Write-Log "Created: $($entry.Path)\$($entry.Name)"
        }
        
        Write-Log "Shell integration registered successfully!" -Level Success
        return $true
        
    } catch {
        Write-Log "Failed to register shell integration: $_" -Level Error
        return $false
    }
}

function Unregister-CmderShellIntegration {
    param(
        [ValidateSet("USER", "ALL")]
        [string]$Scope = "USER"
    )
    
    $basePath = if ($Scope -eq "ALL") {
        "HKEY_LOCAL_MACHINE\SOFTWARE\Classes"
    } else {
        "HKEY_CURRENT_USER\SOFTWARE\Classes"
    }
    
    Write-Log "Unregistering Cmder shell integration ($Scope)..."
    
    $pathsToRemove = @(
        "$basePath\Directory\Background\shell\Cmder",
        "$basePath\Directory\shell\Cmder"
    )
    
    try {
        foreach ($path in $pathsToRemove) {
            if (Test-Path "Registry::$path") {
                Remove-Item -Path "Registry::$path" -Recurse -Force
                Write-Log "Removed: $path"
            }
        }
        
        Write-Log "Shell integration unregistered successfully!" -Level Success
        return $true
        
    } catch {
        Write-Log "Failed to unregister shell integration: $_" -Level Error
        return $false
    }
}

# ==============================================================================
# LAUNCHER
# ==============================================================================

function Start-WindowsTerminal {
    param(
        [string]$WindowsTerminalPath,
        [string]$ProfileName,
        [string]$Directory,
        [bool]$OpenInExisting
    )
    
    Write-Log "Launching Windows Terminal..."
    Write-Log "Profile: $ProfileName"
    Write-Log "Directory: $Directory"
    
    # Build command arguments
    $wtArgs = @()
    
    # Window handling
    if ($OpenInExisting) {
        $wtArgs += "-w", "0"  # Use existing window
    } else {
        $wtArgs += "new-tab"  # Or use 'new-tab' for new window
    }
    
    # Profile
    $wtArgs += "-p", "`"$ProfileName`""
    
    # Starting directory
    if ($Directory) {
        $wtArgs += "-d", "`"$Directory`""
    }
    
    Write-Log "Arguments: $($wtArgs -join ' ')"
    
    try {
        Start-Process -FilePath $WindowsTerminalPath -ArgumentList $wtArgs
        Write-Log "Windows Terminal launched successfully!" -Level Success
        return $true
    } catch {
        Write-Log "Failed to launch Windows Terminal: $_" -Level Error
        return $false
    }
}

# ==============================================================================
# MAIN
# ==============================================================================

function Main {
    Write-Host ""
    Write-Host "Cmder Windows Terminal Launcher v$Script:Version" -ForegroundColor Cyan
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Handle registration/unregistration
    if ($Register) {
        Initialize-CmderEnvironment
        $success = Register-CmderShellIntegration -Scope $Register
        exit ($success ? 0 : 1)
    }
    
    if ($Unregister) {
        $success = Unregister-CmderShellIntegration -Scope $Unregister
        exit ($success ? 0 : 1)
    }
    
    # Normal launch flow
    Initialize-CmderEnvironment
    
    # Find Windows Terminal
    $wtPath = Find-WindowsTerminal
    if (-not $wtPath) {
        Write-Host ""
        Write-Host "ERROR: Windows Terminal not found!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please install Windows Terminal from:" -ForegroundColor Yellow
        Write-Host "  - Microsoft Store: https://aka.ms/terminal" -ForegroundColor Yellow
        Write-Host "  - GitHub: https://github.com/microsoft/terminal/releases" -ForegroundColor Yellow
        Write-Host ""
        exit 1
    }
    
    # Check for Cmder profile
    if (-not (Test-CmderProfile)) {
        Write-Log "Cmder profile not found in Windows Terminal" -Level Warning
        Write-Host ""
        $response = Read-Host "Would you like to create a Cmder profile? (Y/N)"
        if ($response -eq 'Y' -or $response -eq 'y') {
            Add-CmderProfile
        }
    }
    
    # Determine starting directory
    $targetDir = if ($StartDir) {
        $StartDir
    } else {
        $PWD.Path
    }
    
    # Use Task parameter if provided (Cmder compatibility)
    $targetProfile = if ($Task) { $Task } else { $Profile }
    
    # Launch Windows Terminal
    $success = Start-WindowsTerminal `
        -WindowsTerminalPath $wtPath `
        -ProfileName $targetProfile `
        -Directory $targetDir `
        -OpenInExisting $Single
    
    if ($success) {
        Write-Host ""
        Write-Host "✓ Launched successfully!" -ForegroundColor Green
        Write-Host ""
    }
    
    exit ($success ? 0 : 1)
}

# Run main function
Main
