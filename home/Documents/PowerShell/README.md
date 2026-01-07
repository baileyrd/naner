# Portable PowerShell Configuration

This directory contains your portable PowerShell profile, modules, and scripts for Naner.

## Directory Structure

```
Documents/PowerShell/
├── Modules/              # PowerShell modules (auto-loaded)
│   └── .gitkeep
├── Scripts/              # Custom scripts (added to PATH)
│   └── .gitkeep
└── README.md            # This file
```

## PowerShell Profile Location

The PowerShell profile for Naner is located at:
```
%NANER_ROOT%\home\.config\powershell\profile.ps1
```

This profile is automatically loaded when you start PowerShell in Naner.

## Installing Modules

### Method 1: Install to Portable Location

```powershell
# Install to the portable modules directory
Install-Module -Name ModuleName -Scope CurrentUser

# The module will be installed to:
# %NANER_ROOT%\home\Documents\PowerShell\Modules\
```

### Method 2: Manual Installation

1. Download module from [PowerShell Gallery](https://www.powershellgallery.com/)
2. Extract to `Documents\PowerShell\Modules\ModuleName\`
3. Module structure should be:
   ```
   Modules/
   └── ModuleName/
       ├── ModuleName.psd1
       ├── ModuleName.psm1
       └── ...
   ```

## Recommended Modules

### posh-git (Git Integration)
```powershell
Install-Module posh-git -Scope CurrentUser
```
Adds Git status to your prompt and tab completion for Git commands.

### PSReadLine (Enhanced Editing)
```powershell
Install-Module PSReadLine -Scope CurrentUser -Force -SkipPublisherCheck
```
Provides syntax highlighting, command history, and IntelliSense.

### Terminal-Icons (File Icons)
```powershell
Install-Module -Name Terminal-Icons -Scope CurrentUser
```
Shows file type icons in directory listings.

### Oh-My-Posh (Terminal Theming)
```powershell
# Install Oh-My-Posh (requires winget or manual install)
winget install JanDeDobbeleer.OhMyPosh

# Then add to profile.ps1:
oh-my-posh init pwsh --config "$env:POSH_THEMES_PATH\paradox.omp.json" | Invoke-Expression
```

## Adding Custom Scripts

Place any `.ps1` script in `Documents\PowerShell\Scripts\` and it will be available from anywhere:

**Example:** Create `Documents\PowerShell\Scripts\MyScript.ps1`:
```powershell
# MyScript.ps1
param([string]$Name = "World")
Write-Host "Hello, $Name!" -ForegroundColor Green
```

**Usage:**
```powershell
# From anywhere in the terminal
MyScript -Name "Naner"
# Output: Hello, Naner!
```

## Customizing Your Profile

Edit `%NANER_ROOT%\home\.config\powershell\profile.ps1` to customize:

### Add Custom Aliases
```powershell
Set-Alias -Name np -Value notepad
Set-Alias -Name ll -Value Get-ChildItem
```

### Add Custom Functions
```powershell
function Get-Weather {
    param([string]$City = "London")
    curl "wttr.in/$City"
}
```

### Change Prompt
```powershell
function prompt {
    $location = Get-Location
    Write-Host "PS " -NoNewline -ForegroundColor Cyan
    Write-Host "$location" -NoNewline -ForegroundColor Green
    return "> "
}
```

### Load Modules Automatically
```powershell
# Add to profile.ps1
Import-Module posh-git
Import-Module Terminal-Icons
```

## Environment Variables

Naner automatically sets these PowerShell-specific variables:

| Variable | Value | Purpose |
|----------|-------|---------|
| `$PROFILE` | `%HOME%\.config\powershell\profile.ps1` | PowerShell profile location |
| `$env:PSModulePath` | Includes portable modules path | Module search path |
| `$env:HOME` | `%NANER_ROOT%\home` | User home directory |

## Profile Loading Order

PowerShell loads profiles in this order:
1. All Users, All Hosts
2. All Users, Current Host
3. Current User, All Hosts (← **Naner uses this**)
4. Current User, Current Host

Naner sets the profile to:
```
%NANER_ROOT%\home\.config\powershell\profile.ps1
```

## Git Integration

The included profile has Git shortcuts:

```powershell
gs          # git status
ga <files>  # git add
gc -m "msg" # git commit
gp          # git push
gl          # git pull
gd          # git diff
glog        # git log --oneline --graph
```

## Checking Your Setup

Run this command to verify PowerShell is using the portable configuration:

```powershell
Get-NanerInfo
```

Output should show:
```
Naner Root:    C:\path\to\naner_launcher
Home:          C:\path\to\naner_launcher\home
Profile:       C:\path\to\naner_launcher\home\.config\powershell\profile.ps1
PSModulePath:  C:\path\to\naner_launcher\home\Documents\PowerShell\Modules
               ...
```

## Portability

### What's Portable
✅ PowerShell profile (`.config/powershell/profile.ps1`)
✅ Custom modules (`Documents/PowerShell/Modules/`)
✅ Custom scripts (`Documents/PowerShell/Scripts/`)
✅ Module configurations (if stored in `$env:HOME`)

### What's NOT Portable
❌ Modules installed to system locations
❌ Windows-specific paths in profile
❌ Modules that depend on absolute paths

### Making Modules Portable

Always use environment variables in your profile:
```powershell
# Good - uses portable paths
$configPath = Join-Path $env:HOME ".config\myapp\config.json"

# Bad - hardcoded path
$configPath = "C:\Users\YourName\.config\myapp\config.json"
```

## Git Ignore

The following are gitignored for privacy/size:
- Module cache files (`*.nupkg`, `*.zip`)
- PowerShell history (`.local/share/powershell/PSReadLine/`)

Your custom scripts and profile ARE tracked in git by default.

## Troubleshooting

### Profile Not Loading

Check if profile is set correctly:
```powershell
$PROFILE
# Should output: C:\path\to\naner_launcher\home\.config\powershell\profile.ps1
```

### Modules Not Found

Verify module path:
```powershell
$env:PSModulePath -split ';'
# Should include: C:\path\to\naner_launcher\home\Documents\PowerShell\Modules
```

### Scripts Not in PATH

Check PATH includes scripts directory:
```powershell
$env:PATH -split ';' | Select-String "Scripts"
```

### Permission Errors

PowerShell execution policy may block scripts:
```powershell
# Check current policy
Get-ExecutionPolicy

# Set for current user (recommended)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

## Advanced Configuration

### Using Multiple Profiles

You can create conditional logic in your profile:

```powershell
# Different behavior for Windows vs Linux
if ($IsWindows) {
    # Windows-specific config
} elseif ($IsLinux) {
    # Linux-specific config (WSL)
}

# Different behavior based on PS version
if ($PSVersionTable.PSVersion.Major -ge 7) {
    # PowerShell 7+ features
}
```

### Performance Optimization

If your profile is slow to load:

```powershell
# Measure profile load time
Measure-Command { . $PROFILE }

# Lazy-load heavy modules
function Get-HeavyModule {
    if (-not (Get-Module HeavyModule)) {
        Import-Module HeavyModule
    }
    HeavyModule @args
}
```

## Resources

- [PowerShell Gallery](https://www.powershellgallery.com/) - Find modules
- [PowerShell Documentation](https://docs.microsoft.com/powershell/) - Official docs
- [Oh My Posh Themes](https://ohmyposh.dev/docs/themes) - Terminal themes
- [PSReadLine Guide](https://github.com/PowerShell/PSReadLine) - Enhanced editing

## Related Documentation

- [../.ssh/README.md](../.ssh/README.md) - Portable SSH configuration
- [../../docs/PORTABLE-SSH.md](../../docs/PORTABLE-SSH.md) - SSH setup guide
- [../../config/naner.json](../../config/naner.json) - Naner configuration
