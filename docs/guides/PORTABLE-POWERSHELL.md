# Portable PowerShell Configuration Guide

Naner provides a fully portable PowerShell environment with custom profiles, modules, and scripts that travel with your installation.

## Overview

When you launch PowerShell in Naner, the following are automatically configured:
- **Profile Location**: `%NANER_ROOT%\home\.config\powershell\profile.ps1`
- **Modules Path**: `%NANER_ROOT%\home\Documents\PowerShell\Modules\`
- **Scripts Path**: `%NANER_ROOT%\home\Documents\PowerShell\Scripts\` (added to PATH)
- **Home Directory**: `%NANER_ROOT%\home` (affects all tools)

This means your PowerShell configuration, modules, and scripts are completely portable across machines.

## Quick Start

### 1. Launch PowerShell in Naner

```cmd
naner.bat
```

Or:
```powershell
.\src\powershell\Invoke-Naner.ps1 -Profile PowerShell
```

### 2. Verify Portable Configuration

From within Naner PowerShell:
```powershell
# Check profile location
$PROFILE
# Output: C:\path\to\naner_launcher\home\.config\powershell\profile.ps1

# Check module path
$env:PSModulePath -split ';' | Select-Object -First 1
# Output: C:\path\to\naner_launcher\home\Documents\PowerShell\Modules

# Or use built-in helper
Get-NanerInfo
```

### 3. Customize Your Profile

Edit the profile to add your customizations:
```powershell
# From within Naner
notepad $PROFILE

# Or use VS Code
code $PROFILE
```

## Directory Structure

```
home/
├── .config/
│   └── powershell/
│       └── profile.ps1          # Your PowerShell profile
├── Documents/
│   └── PowerShell/
│       ├── Modules/             # Portable modules
│       │   ├── .gitkeep
│       │   ├── posh-git/        # Example module
│       │   └── PSReadLine/      # Example module
│       ├── Scripts/             # Custom scripts (in PATH)
│       │   ├── .gitkeep
│       │   └── MyScript.ps1     # Example script
│       └── README.md
└── .local/share/powershell/     # History, cache (gitignored)
```

## Installing Modules

### Method 1: Install-Module (Recommended)

PowerShell will automatically install to the portable location:

```powershell
# Install a module
Install-Module -Name posh-git -Scope CurrentUser

# Installed to: %NANER_ROOT%\home\Documents\PowerShell\Modules\posh-git\
```

The `-Scope CurrentUser` flag ensures modules install to your portable home directory.

### Method 2: Manual Installation

Download and extract modules manually:

1. Download module from [PowerShell Gallery](https://www.powershellgallery.com/)
2. Extract to `home\Documents\PowerShell\Modules\ModuleName\`
3. Verify structure:
   ```
   Modules/
   └── ModuleName/
       ├── ModuleName.psd1
       ├── ModuleName.psm1
       └── ...
   ```

### Popular Modules to Install

```powershell
# Git integration and prompt customization
Install-Module posh-git -Scope CurrentUser

# Enhanced command-line editing
Install-Module PSReadLine -Scope CurrentUser -Force -SkipPublisherCheck

# File type icons in directory listings
Install-Module Terminal-Icons -Scope CurrentUser

# JSON manipulation
Install-Module powershell-yaml -Scope CurrentUser

# HTTP testing
Install-Module Pester -Scope CurrentUser
```

## Profile Configuration

The included profile (`home\.config\powershell\profile.ps1`) provides:

### Built-in Features

1. **Custom Prompt** - Shows current directory and Git branch
2. **Git Shortcuts**:
   - `gs` → `git status`
   - `ga` → `git add`
   - `gc` → `git commit`
   - `gp` → `git push`
   - `gl` → `git pull`
   - `gd` → `git diff`
   - `glog` → `git log --oneline --graph`

3. **Docker Shortcuts** (if Docker installed):
   - `dps` → `docker ps`
   - `di` → `docker images`
   - `dex` → `docker exec -it`

4. **Utility Functions**:
   - `Get-NanerInfo` - Display Naner environment information
   - `Invoke-NanerSetup` - Quick access to vendor setup
   - `Test-NanerInstall` - Run installation tests

5. **Enhanced Aliases**:
   - `ll` → `Get-ChildItem`
   - `which` → `Get-Command`

### Customizing the Profile

Add your own customizations to the profile:

```powershell
# Open profile for editing
notepad $PROFILE

# Add custom aliases
Set-Alias -Name np -Value notepad
Set-Alias -Name code -Value "C:\Program Files\Microsoft VS Code\Code.exe"

# Add custom functions
function Get-PublicIP {
    (Invoke-WebRequest -Uri "https://api.ipify.org").Content
}

function New-GitRepo {
    param([string]$Name)
    git init
    git add .
    git commit -m "Initial commit"
    gh repo create $Name --public --source . --push
}

# Change prompt color
function prompt {
    Write-Host "[Naner] " -NoNewline -ForegroundColor Magenta
    Write-Host (Get-Location) -NoNewline -ForegroundColor Green
    return "> "
}

# Auto-load modules
Import-Module posh-git
Import-Module Terminal-Icons
```

## Creating Custom Scripts

Scripts in `Documents\PowerShell\Scripts\` are automatically in your PATH.

### Example: Create a Utility Script

**File:** `home\Documents\PowerShell\Scripts\New-Component.ps1`

```powershell
<#
.SYNOPSIS
    Creates a new React component with boilerplate code.

.PARAMETER Name
    The name of the component.

.EXAMPLE
    New-Component -Name Button
#>
param(
    [Parameter(Mandatory)]
    [string]$Name
)

$componentDir = "src/components/$Name"
New-Item -ItemType Directory -Path $componentDir -Force

$content = @"
import React from 'react';
import './$Name.css';

export const $Name = () => {
  return (
    <div className="$($Name.ToLower())">
      $Name Component
    </div>
  );
};
"@

Set-Content -Path "$componentDir/$Name.jsx" -Value $content

Write-Host "Created component: $componentDir/$Name.jsx" -ForegroundColor Green
```

**Usage:**
```powershell
# From anywhere in your project
New-Component -Name Button
# Creates: src/components/Button/Button.jsx
```

## Environment Variables

Naner automatically sets these for PowerShell:

| Variable | Value | Purpose |
|----------|-------|---------|
| `$PROFILE` | `%NANER_ROOT%\home\.config\powershell\profile.ps1` | Profile location |
| `$env:PSModulePath` | Includes portable modules | Module search path |
| `$env:HOME` | `%NANER_ROOT%\home` | Home directory |
| `$env:NANER_ROOT` | Naner installation path | Root directory |

## Advanced Configuration

### Oh My Posh Integration

Install [Oh My Posh](https://ohmyposh.dev/) for beautiful terminal themes:

1. **Install Oh My Posh:**
   ```powershell
   winget install JanDeDobbeleer.OhMyPosh
   ```

2. **Add to profile:**
   ```powershell
   # Edit profile
   notepad $PROFILE

   # Add this line
   oh-my-posh init pwsh --config "$env:POSH_THEMES_PATH\paradox.omp.json" | Invoke-Expression
   ```

3. **Choose a theme:**
   ```powershell
   # Browse themes
   Get-ChildItem $env:POSH_THEMES_PATH

   # Preview a theme
   oh-my-posh config export --config "$env:POSH_THEMES_PATH\jandedobbeleer.omp.json"
   ```

### PSReadLine Configuration

Enhance command-line editing:

```powershell
# Add to profile
if (Get-Module -ListAvailable -Name PSReadLine) {
    Import-Module PSReadLine

    # Prediction settings
    Set-PSReadLineOption -PredictionSource History
    Set-PSReadLineOption -PredictionViewStyle ListView

    # Color scheme
    Set-PSReadLineOption -Colors @{
        Command = 'Cyan'
        Parameter = 'Green'
        String = 'Yellow'
    }

    # Key bindings
    Set-PSReadLineKeyHandler -Key UpArrow -Function HistorySearchBackward
    Set-PSReadLineKeyHandler -Key DownArrow -Function HistorySearchForward
    Set-PSReadLineKeyHandler -Key Tab -Function MenuComplete
}
```

### Conditional Module Loading

Load modules only when needed for faster startup:

```powershell
# Instead of: Import-Module HeavyModule

# Create a function that imports on first use
function Use-HeavyModule {
    if (-not (Get-Module HeavyModule)) {
        Import-Module HeavyModule
    }
}

# Or lazy-load with an alias
Set-Alias heavy-command -Value {
    Import-Module HeavyModule
    heavy-command @args
}.GetNewClosure()
```

## Portability

### What's Portable ✅

- PowerShell profile (`.config\powershell\profile.ps1`)
- Custom modules (`Documents\PowerShell\Modules\`)
- Custom scripts (`Documents\PowerShell\Scripts\`)
- PSReadLine settings (if configured in profile)
- Oh My Posh themes (if stored in `$env:HOME`)

### What's NOT Portable ❌

- Modules installed to system locations (`C:\Program Files\PowerShell\Modules\`)
- Windows-specific absolute paths
- Modules requiring admin privileges
- System-wide PowerShell settings

### Best Practices for Portability

1. **Always use environment variables:**
   ```powershell
   # Good
   $configPath = Join-Path $env:HOME ".config\myapp\config.json"

   # Bad
   $configPath = "C:\Users\YourName\.config\myapp\config.json"
   ```

2. **Install modules with `-Scope CurrentUser`:**
   ```powershell
   Install-Module ModuleName -Scope CurrentUser
   ```

3. **Use relative paths in scripts:**
   ```powershell
   # Good
   $scriptPath = Join-Path $PSScriptRoot "helpers\helper.ps1"

   # Bad
   $scriptPath = "C:\Users\YourName\scripts\helper.ps1"
   ```

## Troubleshooting

### Profile Not Loading

**Symptom:** Custom aliases and functions not available

**Solution:**
```powershell
# Check profile path
$PROFILE
# Should be: C:\path\to\naner_launcher\home\.config\powershell\profile.ps1

# Test if profile exists
Test-Path $PROFILE

# Manually load profile
. $PROFILE
```

### Modules Not Found

**Symptom:** `Import-Module` fails

**Solution:**
```powershell
# Check module path
$env:PSModulePath -split ';'
# Should include: C:\path\to\naner_launcher\home\Documents\PowerShell\Modules

# List installed modules
Get-Module -ListAvailable

# Reinstall module
Install-Module ModuleName -Scope CurrentUser -Force
```

### Scripts Not in PATH

**Symptom:** Can't run custom scripts

**Solution:**
```powershell
# Check if scripts directory is in PATH
$env:PATH -split ';' | Select-String "Scripts"

# Manually add to PATH (temporary)
$env:PATH = "$(Join-Path $env:HOME 'Documents\PowerShell\Scripts');$env:PATH"

# Permanent fix: restart PowerShell session
```

### Execution Policy Errors

**Symptom:** "cannot be loaded because running scripts is disabled"

**Solution:**
```powershell
# Check current policy
Get-ExecutionPolicy

# Set for current user
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Or bypass for specific script
PowerShell -ExecutionPolicy Bypass -File script.ps1
```

### Slow Profile Loading

**Symptom:** PowerShell takes several seconds to start

**Solution:**
```powershell
# Measure profile load time
Measure-Command { . $PROFILE }

# Disable profile temporarily
pwsh -NoProfile

# Optimize profile:
# 1. Remove heavy Import-Module commands
# 2. Use lazy loading
# 3. Cache expensive operations
```

## Git Integration

### Using posh-git

Install and configure Git integration:

```powershell
# Install
Install-Module posh-git -Scope CurrentUser

# Add to profile
Import-Module posh-git

# Customize prompt
$GitPromptSettings.DefaultPromptAbbreviateHomeDirectory = $true
$GitPromptSettings.DefaultPromptPrefix.Text = '[Naner] '
```

### Git Credential Storage

Store Git credentials in the portable home:

```powershell
# Configure Git to use portable credential helper
git config --global credential.helper store
git config --global credential.helper "store --file $env:HOME/.git-credentials"
```

## Testing Your Setup

Run these commands to verify everything is working:

```powershell
# 1. Check Naner environment
Get-NanerInfo

# 2. Test module loading
Import-Module posh-git -ErrorAction SilentlyContinue
Get-Module posh-git

# 3. Test PATH
Get-Command -CommandType Application | Where-Object { $_.Source -like "*naner*" }

# 4. Test Git
gs  # Should run 'git status'

# 5. Verify profile loaded
Get-Command Get-NanerInfo
```

Expected output should show all Naner paths and modules.

## Security Notes

### What's Gitignored

The following are automatically excluded from git:

- ❌ Module cache files (`*.nupkg`, `.local/share/powershell/`)
- ❌ PowerShell history
- ❌ Downloaded module packages
- ❌ Temporary cache files

### What's Tracked

- ✅ Your profile (`profile.ps1`)
- ✅ Custom scripts (`Scripts/*.ps1`)
- ✅ Directory structure (`.gitkeep` files)

**Note:** Installed modules are gitignored by default. Document required modules in your profile or a `requirements.txt` file.

## Example Workflows

### Workflow 1: Development Environment Setup

```powershell
# Install development modules
Install-Module posh-git -Scope CurrentUser
Install-Module PSReadLine -Scope CurrentUser
Install-Module Terminal-Icons -Scope CurrentUser

# Create custom scripts
New-Item -Path "$env:HOME\Documents\PowerShell\Scripts\dev.ps1" -Value @'
function Start-DevEnv {
    code .
    npm run dev
}
'@

# Update profile to auto-load modules
Add-Content $PROFILE @'
Import-Module posh-git
Import-Module Terminal-Icons
'@
```

### Workflow 2: Server Administration

```powershell
# Create admin functions
New-Item -Path "$env:HOME\Documents\PowerShell\Scripts\admin.ps1" -Value @'
function Get-ServerHealth {
    $cpu = Get-Counter '\Processor(_Total)\% Processor Time'
    $mem = Get-Counter '\Memory\Available MBytes'
    [PSCustomObject]@{
        CPU = [math]::Round($cpu.CounterSamples.CookedValue, 2)
        Memory = [math]::Round($mem.CounterSamples.CookedValue, 2)
    }
}
'@
```

## Related Documentation

- [PORTABLE-SSH.md](PORTABLE-SSH.md) - Portable SSH configuration
- [home/Documents/PowerShell/README.md](../home/Documents/PowerShell/README.md) - Detailed PowerShell docs
- [config/naner.json](../config/naner.json) - Environment configuration

## Resources

- [PowerShell Gallery](https://www.powershellgallery.com/) - Module repository
- [PowerShell Documentation](https://docs.microsoft.com/powershell/) - Official docs
- [Oh My Posh](https://ohmyposh.dev/) - Terminal theming
- [PSReadLine](https://github.com/PowerShell/PSReadLine) - Enhanced editing
- [posh-git](https://github.com/dahlbyk/posh-git) - Git integration
