#Requires -Version 5.1
<#
.SYNOPSIS
    Naner Windows Terminal Launcher - PowerShell Edition
    
.DESCRIPTION
    A launcher that mimics Naner.exe functionality but uses Windows Terminal instead of ConEmu.
    This is the rapid prototype version - will be ported to C# for production.
    
.PARAMETER StartDir
    Directory to start the terminal in. Defaults to current directory.
    
.PARAMETER Profile
    Windows Terminal profile to use. Defaults to "Naner".
    
.PARAMETER Task
    Alias for Profile parameter (for Naner compatibility).
    
.PARAMETER Config
    Path to custom Naner configuration root.
    
.PARAMETER Icon
    Path to custom icon (for future GUI implementation).
    
.PARAMETER Single
    Open in existing Windows Terminal window if possible.
    
.PARAMETER Register
    Register Naner in Windows Explorer context menu. Values: USER, ALL
    
.PARAMETER Unregister
    Unregister Naner from Windows Explorer context menu. Values: USER, ALL
    
.PARAMETER New
    Force new Windows Terminal window.
    
.PARAMETER Verbose
    Show detailed logging information.
    
.EXAMPLE
    .\Launch-Naner.ps1
    Launches Windows Terminal with Naner profile in current directory.
    
.EXAMPLE
    .\Launch-Naner.ps1 -StartDir "C:\Projects"
    Launches in C:\Projects directory.
    
.EXAMPLE
    .\Launch-Naner.ps1 -Profile "PowerShell" -Single
    Opens PowerShell profile in existing window.
    
.EXAMPLE
    .\Launch-Naner.ps1 -Register USER
    Registers "Naner Here" in Windows Explorer context menu.
    
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
    [string]$Profile = "Naner",
    
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

# Determine Naner root directory
$Script:NanerRoot = if ($Config) {
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

function Initialize-NanerEnvironment {
    Write-Log "Initializing Naner environment..."
    
    # Set Naner environment variables
    $env:Naner_ROOT = $Script:NanerRoot
    $env:Naner_USER_CONFIG = Join-Path $Script:NanerRoot "config"
    $env:Naner_USER_BIN = Join-Path $Script:NanerRoot "bin"
    
    Write-Log "Naner_ROOT: $env:Naner_ROOT"
    Write-Log "Naner_USER_CONFIG: $env:Naner_USER_CONFIG"
    Write-Log "Naner_USER_BIN: $env:Naner_USER_BIN"
    
    # Create directories if they don't exist
    @($env:Naner_USER_CONFIG, $env:Naner_USER_BIN) | ForEach-Object {
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

function Test-NanerProfile {
    $settingsPath = Get-WindowsTerminalSettingsPath
    if (-not $settingsPath) {
        Write-Log "Windows Terminal settings not found" -Level Warning
        return $false
    }
    
    Write-Log "Checking for Naner profile in: $settingsPath"
    
    try {
        $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
        
        $hasNanerProfile = $settings.profiles.list | 
                          Where-Object { $_.name -eq "Naner" } | 
                          Measure-Object | 
                          Select-Object -ExpandProperty Count
        
        return $hasNanerProfile -gt 0
    } catch {
        Write-Log "Failed to parse settings.json: $_" -Level Warning
        return $false
    }
}

function Add-NanerProfile {
    Write-Log "Adding Naner profile to Windows Terminal..." -Level Info
    
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
        
        # Create Naner profile
        $NanerProfile = @{
            name              = "Naner"
            commandline       = "cmd.exe /k $env:Naner_ROOT\vendor\init.bat"
            startingDirectory = "%USERPROFILE%"
            icon              = "$env:Naner_ROOT\icons\Naner.ico"
            colorScheme       = "Campbell"
            hidden            = $false
        }
        
        # Add to profiles list
        $profilesList = @($settings.profiles.list)
        $profilesList += $NanerProfile
        $settings.profiles.list = $profilesList
        
        # Save settings
        $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath -Encoding UTF8
        
        Write-Log "Naner profile added successfully!" -Level Success
        return $true
        
    } catch {
        Write-Log "Failed to add Naner profile: $_" -Level Error
        return $false
    }
}

# ==============================================================================
# SHELL INTEGRATION (CONTEXT MENU)
# ==============================================================================

function Register-NanerShellIntegration {
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
    
    Write-Log "Registering Naner shell integration ($Scope)..."
    
    # Check admin rights if installing for ALL
    if ($Scope -eq "ALL") {
        $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
        if (-not $isAdmin) {
            Write-Log "Administrator rights required for ALL scope" -Level Error
            return $false
        }
    }
    
    $launcherPath = Join-Path $Script:NanerRoot "Naner.exe"
    $iconPath = Join-Path $Script:NanerRoot "icons\Naner.ico"
    
    # Registry entries to create
    $entries = @(
        @{
            Path    = "$basePath\Directory\Background\shell\Naner"
            Name    = "(Default)"
            Value   = "Naner Here"
            Type    = "String"
        },
        @{
            Path    = "$basePath\Directory\Background\shell\Naner"
            Name    = "Icon"
            Value   = $iconPath
            Type    = "String"
        },
        @{
            Path    = "$basePath\Directory\Background\shell\Naner\command"
            Name    = "(Default)"
            Value   = "`"$launcherPath`" `"%V`""
            Type    = "String"
        },
        @{
            Path    = "$basePath\Directory\shell\Naner"
            Name    = "(Default)"
            Value   = "Naner Here"
            Type    = "String"
        },
        @{
            Path    = "$basePath\Directory\shell\Naner\command"
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

function Unregister-NanerShellIntegration {
    param(
        [ValidateSet("USER", "ALL")]
        [string]$Scope = "USER"
    )
    
    $basePath = if ($Scope -eq "ALL") {
        "HKEY_LOCAL_MACHINE\SOFTWARE\Classes"
    } else {
        "HKEY_CURRENT_USER\SOFTWARE\Classes"
    }
    
    Write-Log "Unregistering Naner shell integration ($Scope)..."
    
    $pathsToRemove = @(
        "$basePath\Directory\Background\shell\Naner",
        "$basePath\Directory\shell\Naner"
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
    Write-Host "Naner Windows Terminal Launcher v$Script:Version" -ForegroundColor Cyan
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Handle registration/unregistration
    if ($Register) {
        Initialize-NanerEnvironment
        $success = Register-NanerShellIntegration -Scope $Register
        exit ($success ? 0 : 1)
    }
    
    if ($Unregister) {
        $success = Unregister-NanerShellIntegration -Scope $Unregister
        exit ($success ? 0 : 1)
    }
    
    # Normal launch flow
    Initialize-NanerEnvironment
    
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
    
    # Check for Naner profile
    if (-not (Test-NanerProfile)) {
        Write-Log "Naner profile not found in Windows Terminal" -Level Warning
        Write-Host ""
        $response = Read-Host "Would you like to create a Naner profile? (Y/N)"
        if ($response -eq 'Y' -or $response -eq 'y') {
            Add-NanerProfile
        }
    }
    
    # Determine starting directory
    $targetDir = if ($StartDir) {
        $StartDir
    } else {
        $PWD.Path
    }
    
    # Use Task parameter if provided (Naner compatibility)
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
