# Naner Multi-Environment Support

**Version:** 1.0
**Date:** 2026-01-07
**Status:** Production Ready

---

## Overview

Naner's Multi-Environment Support allows you to create and manage multiple isolated development environments within a single Naner installation. Each environment has its own:

- Home directory with separate configurations
- Git identity and SSH keys
- Shell configurations (.bashrc, PowerShell profile)
- Editor settings (VS Code, vim)
- Development tool configurations (npm, pip, cargo, etc.)
- Package caches and installed packages

This enables you to maintain separate configurations for different contexts without interfering with each other.

---

## Table of Contents

- [Overview](#overview)
- [Use Cases](#use-cases)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Command Reference](#command-reference)
- [Common Workflows](#common-workflows)
- [Advanced Usage](#advanced-usage)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)
- [Technical Details](#technical-details)

---

## Use Cases

### 1. Work vs. Personal Development

Separate your work projects from personal projects with different:
- Git identities (work email vs. personal email)
- SSH keys (work GitHub vs. personal GitHub)
- Tool versions and configurations
- Project templates and preferences

```powershell
# Create work environment
New-NanerEnvironment -Name "work" -Description "Work projects with company credentials"

# Create personal environment
New-NanerEnvironment -Name "personal" -Description "Personal open-source projects"
```

### 2. Client-Specific Configurations

Maintain separate environments for different clients:

```powershell
New-NanerEnvironment -Name "client-acme" -Description "ACME Corp projects"
New-NanerEnvironment -Name "client-globex" -Description "Globex Inc projects"
```

Each client environment can have:
- Client-specific Git credentials
- Client-specific SSH keys
- Custom aliases and scripts
- Project-specific tools

### 3. Testing and Experimentation

Create temporary environments for testing new tools or configurations:

```powershell
New-NanerEnvironment -Name "experiment" -Description "Testing new configurations"

# When done
Remove-NanerEnvironment -Name "experiment" -Force
```

### 4. Team Collaboration

Share environment configurations across team members:

```powershell
# Export your team's standard environment
Export-VendorLockFile -OutputPath "team-env.lock.json"

# Team members create environment from template
New-NanerEnvironment -Name "team-standard" -CopyFrom "default"
```

---

## Quick Start

### Creating Your First Environment

```powershell
# Create a new environment
New-NanerEnvironment -Name "my-env" -Description "My custom environment"

# Switch to it
Use-NanerEnvironment -Name "my-env"

# Launch Naner with this environment
.\Invoke-Naner.ps1
```

### Copying an Existing Environment

```powershell
# Copy from the default environment
New-NanerEnvironment -Name "work" -CopyFrom "default"

# Now customize the work environment
Use-NanerEnvironment -Name "work"
```

### Switching Between Environments

```powershell
# List all environments
Get-NanerEnvironment

# Switch to work environment
Use-NanerEnvironment -Name "work"

# Launch with work environment (uses active environment by default)
.\Invoke-Naner.ps1

# Or launch with a specific environment
.\Invoke-Naner.ps1 -Environment "personal"
```

### Viewing Environment Details

```powershell
# List all environments
Get-NanerEnvironment

# Get details about a specific environment
Get-NanerEnvironment -Name "work"

# Check which environment is currently active
Get-ActiveNanerEnvironment
```

---

## Architecture

### Directory Structure

```
naner/
├── home/                          # Default environment
│   ├── .gitconfig
│   ├── .bashrc
│   ├── .ssh/
│   └── ...
│
├── home/environments/             # Custom environments
│   ├── work/                      # Work environment
│   │   ├── .naner-env.json       # Environment metadata
│   │   ├── .gitconfig            # Work-specific Git config
│   │   ├── .bashrc               # Work-specific Bash config
│   │   ├── .ssh/                 # Work SSH keys
│   │   └── ...
│   │
│   └── personal/                  # Personal environment
│       ├── .naner-env.json
│       ├── .gitconfig
│       └── ...
│
└── config/
    └── active-environment.txt     # Tracks the active environment
```

### Environment Metadata

Each environment has a `.naner-env.json` file that stores metadata:

```json
{
  "name": "work",
  "description": "Work projects",
  "createdAt": "2026-01-07T10:30:00Z",
  "version": "1.0",
  "copiedFrom": "default"
}
```

### Environment Variables

When you launch Naner with an environment, the following environment variables are set:

- `HOME`: Points to the environment's home directory
- `NANER_ENVIRONMENT`: Name of the active environment
- `NANER_ROOT`: Naner root directory (unchanged)
- All other paths in `naner.json` that reference `%NANER_ROOT%\home` are automatically adjusted

---

## Command Reference

### New-NanerEnvironment

Creates a new environment.

**Syntax:**
```powershell
New-NanerEnvironment
    -Name <String>
    [-Description <String>]
    [-CopyFrom <String>]
    [-NanerRoot <String>]
```

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-Name` | Yes | Name of the environment (alphanumeric, dash, underscore) |
| `-Description` | No | Human-readable description |
| `-CopyFrom` | No | Name of existing environment to copy from |
| `-NanerRoot` | No | Naner root path (auto-detected if omitted) |

**Examples:**

```powershell
# Basic creation
New-NanerEnvironment -Name "dev"

# With description
New-NanerEnvironment -Name "work" -Description "Work projects"

# Copy from default
New-NanerEnvironment -Name "personal" -CopyFrom "default"

# Copy from another custom environment
New-NanerEnvironment -Name "client-b" -CopyFrom "client-a"
```

**What Gets Copied:**

When using `-CopyFrom`, the following items are copied:
- `.gitconfig` - Git configuration
- `.bashrc`, `.bash_profile`, `.bash_aliases` - Bash configuration
- `.ssh/config` - SSH configuration (NOT private keys)
- `.config/` - Application configurations
- `.vscode/` - VS Code settings
- `Documents/PowerShell/` - PowerShell profile and modules

**What Doesn't Get Copied:**
- SSH private keys (security)
- Package caches (`.npm-cache`, `.conda/pkgs`, etc.)
- Installed packages
- Build artifacts

---

### Use-NanerEnvironment

Sets the active environment.

**Syntax:**
```powershell
Use-NanerEnvironment
    -Name <String>
    [-NanerRoot <String>]
```

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-Name` | Yes | Name of the environment to activate |
| `-NanerRoot` | No | Naner root path (auto-detected if omitted) |

**Examples:**

```powershell
# Switch to work environment
Use-NanerEnvironment -Name "work"

# Switch back to default
Use-NanerEnvironment -Name "default"
```

**Notes:**
- The active environment is stored in `config/active-environment.txt`
- When you run `.\Invoke-Naner.ps1` without `-Environment`, it uses the active environment
- You can override the active environment with `.\Invoke-Naner.ps1 -Environment "name"`

---

### Get-NanerEnvironment

Lists all environments or gets details about a specific one.

**Syntax:**
```powershell
Get-NanerEnvironment
    [-Name <String>]
    [-NanerRoot <String>]
```

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-Name` | No | Name of specific environment to view |
| `-NanerRoot` | No | Naner root path (auto-detected if omitted) |

**Examples:**

```powershell
# List all environments
Get-NanerEnvironment

# Output:
# Naner Environments
# ==================
#
#   default - Base environment
# * work - Work projects
#   personal - Personal projects
#
# * = Active environment

# Get details about specific environment
Get-NanerEnvironment -Name "work"

# Output:
# Environment: work
# Path: C:\naner\home\environments\work
# Active: True
# Description: Work projects
# Created: 2026-01-07T10:30:00Z
```

---

### Get-ActiveNanerEnvironment

Returns the name of the currently active environment.

**Syntax:**
```powershell
Get-ActiveNanerEnvironment
    [-NanerRoot <String>]
```

**Returns:** String (environment name)

**Example:**

```powershell
$activeEnv = Get-ActiveNanerEnvironment
Write-Host "Current environment: $activeEnv"
```

---

### Remove-NanerEnvironment

Deletes an environment and all its files.

**Syntax:**
```powershell
Remove-NanerEnvironment
    -Name <String>
    [-Force]
    [-NanerRoot <String>]
```

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-Name` | Yes | Name of the environment to remove |
| `-Force` | No | Skip confirmation prompt |
| `-NanerRoot` | No | Naner root path (auto-detected if omitted) |

**Examples:**

```powershell
# Remove with confirmation
Remove-NanerEnvironment -Name "old-project"

# Remove without confirmation
Remove-NanerEnvironment -Name "temp" -Force
```

**Restrictions:**
- Cannot remove the `default` environment
- Cannot remove the currently active environment (switch away first)

---

## Common Workflows

### Workflow 1: Setting Up Work and Personal Environments

```powershell
# Step 1: Create work environment from default
New-NanerEnvironment -Name "work" -CopyFrom "default" -Description "Work projects"

# Step 2: Switch to work environment
Use-NanerEnvironment -Name "work"

# Step 3: Launch Naner and configure work-specific settings
.\Invoke-Naner.ps1

# In the terminal, configure work Git identity
git config --global user.name "John Doe"
git config --global user.email "john.doe@company.com"

# Step 4: Create personal environment
New-NanerEnvironment -Name "personal" -CopyFrom "default" -Description "Personal projects"

# Step 5: Switch to personal
Use-NanerEnvironment -Name "personal"

# Step 6: Configure personal settings
.\Invoke-Naner.ps1
git config --global user.name "John Doe"
git config --global user.email "john@example.com"

# Step 7: Switch between them as needed
Use-NanerEnvironment -Name "work"
.\Invoke-Naner.ps1

Use-NanerEnvironment -Name "personal"
.\Invoke-Naner.ps1
```

---

### Workflow 2: Client Project Isolation

```powershell
# Create client environment
New-NanerEnvironment -Name "client-acme" -Description "ACME Corp projects"
Use-NanerEnvironment -Name "client-acme"

# Launch and configure client-specific tools
.\Invoke-Naner.ps1

# In terminal:
# - Set up client SSH keys
# - Configure client Git credentials
# - Install client-specific npm packages
# - Set up client-specific aliases

# When switching clients
Use-NanerEnvironment -Name "client-globex"
.\Invoke-Naner.ps1
```

---

### Workflow 3: Experimentation and Testing

```powershell
# Create experimental environment
New-NanerEnvironment -Name "test-new-tools" -Description "Testing experimental setup"

# Switch to it
Use-NanerEnvironment -Name "test-new-tools"

# Launch and experiment
.\Invoke-Naner.ps1

# Try new configurations, install test packages, etc.
# If it works, copy settings to your main environment
# If it fails, just delete it

# Clean up when done
Use-NanerEnvironment -Name "default"
Remove-NanerEnvironment -Name "test-new-tools" -Force
```

---

### Workflow 4: Team Environment Template

```powershell
# Team lead sets up standard environment
New-NanerEnvironment -Name "team-standard" -Description "Team standard configuration"
Use-NanerEnvironment -Name "team-standard"

# Configure team settings:
# - Team Git hooks
# - Code formatting rules
# - Standard aliases
# - Required tools

# Export the configuration
Backup-NanerConfig -BackupPath "team-standard-backup" -Compress

# Team members restore from backup
Restore-NanerConfig -BackupPath "team-standard-backup.zip"
New-NanerEnvironment -Name "my-work" -CopyFrom "team-standard"
```

---

## Advanced Usage

### Scripting with Environments

```powershell
# Get all environments programmatically
$activeEnv = Get-ActiveNanerEnvironment
$envPath = Join-Path $env:NANER_ROOT "home\environments"

# Loop through environments
Get-ChildItem -Path $envPath -Directory | ForEach-Object {
    Write-Host "Environment: $($_.Name)"

    # Read metadata
    $metadataPath = Join-Path $_.FullName ".naner-env.json"
    if (Test-Path $metadataPath) {
        $metadata = Get-Content $metadataPath | ConvertFrom-Json
        Write-Host "  Description: $($metadata.description)"
    }
}
```

### Environment-Specific Launch Scripts

Create shortcuts for specific environments:

**launch-work.ps1:**
```powershell
Use-NanerEnvironment -Name "work"
.\Invoke-Naner.ps1 -Profile "PowerShell"
```

**launch-personal.ps1:**
```powershell
Use-NanerEnvironment -Name "personal"
.\Invoke-Naner.ps1 -Profile "Bash"
```

### Checking Environment in Scripts

```powershell
# In a script, check which environment is active
$currentEnv = Get-ActiveNanerEnvironment

if ($currentEnv -eq "work") {
    Write-Host "Work environment - using company proxy"
    # Set proxy
}
elseif ($currentEnv -eq "personal") {
    Write-Host "Personal environment - no proxy"
}
```

### Environment-Specific Aliases

Add to your PowerShell profile:

```powershell
# home/environments/work/Documents/PowerShell/profile.ps1
function goto-work-repo {
    Set-Location "C:\work\main-repo"
}

Set-Alias work goto-work-repo
```

---

## Troubleshooting

### Issue: "Environment 'xyz' not found"

**Cause:** The environment doesn't exist or was deleted.

**Solution:**
```powershell
# List all environments
Get-NanerEnvironment

# Create the environment if needed
New-NanerEnvironment -Name "xyz"
```

---

### Issue: "Cannot remove the currently active environment"

**Cause:** You're trying to delete the environment you're currently using.

**Solution:**
```powershell
# Switch to a different environment first
Use-NanerEnvironment -Name "default"

# Then remove the environment
Remove-NanerEnvironment -Name "xyz" -Force
```

---

### Issue: Environment configurations not applying

**Cause:** The HOME environment variable might not be set correctly.

**Solution:**
```powershell
# Launch with DebugMode to see environment variables
.\Invoke-Naner.ps1 -Environment "work" -DebugMode

# Check if HOME is pointing to the correct directory
```

---

### Issue: SSH keys not working in custom environment

**Cause:** SSH private keys are not copied when using `-CopyFrom` for security reasons.

**Solution:**
```powershell
# Manually copy SSH keys to the new environment
$sourceKeys = Join-Path $env:NANER_ROOT "home\.ssh"
$destKeys = Join-Path $env:NANER_ROOT "home\environments\work\.ssh"

Copy-Item "$sourceKeys\id_rsa" $destKeys
Copy-Item "$sourceKeys\id_rsa.pub" $destKeys

# Or generate new SSH keys for the environment
ssh-keygen -t rsa -b 4096 -f "$destKeys\id_rsa"
```

---

### Issue: Package installations not isolated

**Cause:** Some package managers install globally by default.

**Solution:** Ensure environment variables are set correctly in `naner.json`:
- `NPM_CONFIG_PREFIX` → `%NANER_ROOT%\home\.npm-global`
- `GOPATH` → `%NANER_ROOT%\home\go`
- `CARGO_HOME` → `%NANER_ROOT%\home\.cargo`
- `GEM_HOME` → `%NANER_ROOT%\home\.gem`

These are automatically adjusted per environment.

---

## Best Practices

### 1. Use Descriptive Names and Descriptions

```powershell
# Good
New-NanerEnvironment -Name "work-backend" -Description "Backend API development at XYZ Corp"

# Not as good
New-NanerEnvironment -Name "env1"
```

### 2. Create Environments from Templates

Instead of starting from scratch, copy from an existing environment:

```powershell
New-NanerEnvironment -Name "new-client" -CopyFrom "default"
```

### 3. Document Your Environments

Add a README.md in each environment's home directory:

```powershell
Use-NanerEnvironment -Name "work"
$readmePath = Join-Path $env:HOME "README.md"

@"
# Work Environment

## Purpose
Backend development for XYZ Corp projects

## Git Identity
- Name: John Doe
- Email: john.doe@xyzcorp.com

## SSH Keys
- GitHub: id_rsa_work
- GitLab: id_rsa_gitlab

## Notes
- Uses company proxy
- NPM registry: https://registry.xyzcorp.com
"@ | Set-Content $readmePath
```

### 4. Regularly Back Up Environments

```powershell
# Back up specific environment
Use-NanerEnvironment -Name "work"
Backup-NanerConfig -BackupPath "work-backup-$(Get-Date -Format 'yyyy-MM-dd')" -Compress
```

### 5. Clean Up Unused Environments

```powershell
# Remove old/unused environments
Remove-NanerEnvironment -Name "old-project" -Force
```

### 6. Use Environment-Specific Launch Scripts

Create desktop shortcuts or scripts for quick access:

```powershell
# Create launch-work.bat
@echo off
cd C:\naner
powershell -ExecutionPolicy Bypass -Command "Use-NanerEnvironment -Name 'work'; .\Invoke-Naner.ps1"
```

### 7. Keep Default Environment Clean

Use the `default` environment as a template. Don't pollute it with project-specific configurations.

### 8. Name Environments Consistently

Use a naming convention:
- `work-frontend`
- `work-backend`
- `personal-oss`
- `personal-learning`
- `client-acme`
- `client-globex`

---

## Technical Details

### Environment Resolution Order

1. Command-line parameter: `.\Invoke-Naner.ps1 -Environment "work"`
2. Active environment file: `config\active-environment.txt`
3. Default: `default` environment

### Environment Variable Replacement

When launching with an environment, Naner replaces path variables:

| Original | Replacement (for "work" environment) |
|----------|--------------------------------------|
| `%NANER_ROOT%\home` | `%NANER_ROOT%\home\environments\work` |
| `HOME` | Points to environment's home directory |

### Files Created

Each environment creates:

```
home/environments/<name>/
├── .naner-env.json          # Metadata
├── .gitconfig              # Git configuration
├── .bashrc                 # Bash configuration
├── .ssh/                   # SSH configuration
│   └── config
├── .config/                # Application configs
│   ├── powershell/
│   └── windows-terminal/
├── Documents/              # PowerShell modules
│   └── PowerShell/
├── .vscode/                # VS Code settings
├── go/                     # Go workspace
│   ├── bin/
│   ├── pkg/
│   └── src/
├── .cargo/                 # Rust/Cargo home
├── .rustup/                # Rustup home
├── .npm-global/            # npm global packages
├── .npm-cache/             # npm cache
├── .gem/                   # Ruby gems
├── .conda/                 # Conda packages
│   ├── pkgs/
│   └── envs/
└── .local/                 # Python user packages
```

### Performance Considerations

- Each environment duplicates configuration files (~1-10 MB)
- Package caches and installed packages can grow large
- Consider using symlinks for large shared resources
- Clean up unused environments regularly

---

## Migration from Single Environment

If you've been using Naner without environments, your current setup is the `default` environment.

To migrate to multi-environment:

```powershell
# Your current setup is already the "default" environment
# Just create new environments as needed

# Create a work environment
New-NanerEnvironment -Name "work" -CopyFrom "default"

# Customize the work environment
Use-NanerEnvironment -Name "work"
.\Invoke-Naner.ps1

# Your original setup remains intact as "default"
Use-NanerEnvironment -Name "default"
```

---

## Related Documentation

- [Portable Git Configuration](PORTABLE-GIT.md)
- [Portable SSH](PORTABLE-SSH.md)
- [Portable PowerShell](PORTABLE-POWERSHELL.md)
- [Sync & Backup](SYNC-BACKUP.md) - Backing up environments
- [Capability Roadmap](CAPABILITY-ROADMAP.md) - Phase 9.2

---

## Feedback and Support

- GitHub Issues: https://github.com/anthropics/naner/issues
- Feature Requests: Tag with `enhancement` and `multi-environment`

---

**Last Updated:** 2026-01-07
**Version:** 1.0
**Status:** Production Ready
