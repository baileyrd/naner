# Naner GUI Configuration Manager

**Version:** 1.0.0
**Status:** Phase 9.4 Implementation
**Date:** 2026-01-07

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Getting Started](#getting-started)
4. [User Interface Guide](#user-interface-guide)
5. [Vendors Tab](#vendors-tab)
6. [Environments Tab](#environments-tab)
7. [Profiles Tab](#profiles-tab)
8. [Settings Tab](#settings-tab)
9. [Setup Wizard](#setup-wizard)
10. [Keyboard Shortcuts](#keyboard-shortcuts)
11. [Troubleshooting](#troubleshooting)
12. [Technical Architecture](#technical-architecture)
13. [Future Enhancements](#future-enhancements)

---

## Overview

The **Naner GUI Configuration Manager** is a visual, user-friendly interface for managing your Naner portable development environment. It provides an alternative to command-line configuration, making it easier for new users to set up and manage:

- Vendor installations (Node.js, Python, Go, Rust, Ruby, etc.)
- Multiple development environments
- Shell profiles (PowerShell, Bash, CMD)
- Configuration settings

### Why Use the GUI?

**Benefits:**
- ✅ **Visual Management** - See all vendors, environments, and profiles at a glance
- ✅ **Easier for Beginners** - No need to memorize PowerShell commands
- ✅ **Quick Access** - One-click operations for common tasks
- ✅ **Setup Wizard** - Guided setup for first-time users
- ✅ **Validation Feedback** - Visual indicators for configuration status
- ✅ **No Learning Curve** - Familiar Windows Forms interface

**Command-Line Alternative:**
- Power users can still use PowerShell cmdlets for automation
- GUI and CLI are complementary, not exclusive

---

## Features

### Implemented (v1.0.0)

**Vendor Management:**
- ✅ View all available vendors
- ✅ See installation status
- ✅ Identify required vs. optional vendors
- ✅ Install selected vendors (UI ready, backend integration pending)
- ✅ Enable/disable vendors (UI ready, backend integration pending)

**Environment Management:**
- ✅ View all environments (default + custom)
- ✅ See active environment with visual indicator
- ✅ Create new environments (UI ready, backend integration pending)
- ✅ Switch between environments (UI ready, backend integration pending)
- ✅ Delete custom environments (UI ready, backend integration pending)

**Profile Management:**
- ✅ View all shell profiles
- ✅ See default profile with visual indicator
- ✅ Launch profiles (UI ready, backend integration pending)
- ✅ Set default profile (UI ready, backend integration pending)

**Settings & Configuration:**
- ✅ View Naner root directory
- ✅ Quick access to configuration files (naner.json, vendors.json)
- ✅ Open home folder in Explorer
- ✅ Run setup wizard
- ✅ Validate configuration (UI ready, backend integration pending)
- ✅ About information

**Setup Wizard:**
- ✅ Welcome screen
- ✅ Optional vendor selection
- ✅ Installation progress tracking
- ✅ First-time user guidance

### Planned (Future Versions)

- 🔄 Real-time vendor installation with progress
- 🔄 Advanced configuration editor (PATH, environment variables)
- 🔄 Plugin management UI
- 🔄 Configuration export/import
- 🔄 Sync & backup UI integration
- 🔄 Theme customization
- 🔄 Keyboard shortcut customization
- 🔄 Search and filter capabilities
- 🔄 Configuration history and rollback

---

## Getting Started

### Requirements

- **Windows 10/11** (with .NET Framework - included with Windows)
- **Naner Installation** - GUI must be run from a Naner installation
- **PowerShell Execution Policy** - Set to allow script execution

### Launching the GUI

**Option 1: Direct Execution**
```powershell
# From Naner root directory
.\src\powershell\Invoke-NanerGUI.ps1
```

**Option 2: With Setup Wizard**
```powershell
# Launch setup wizard for first-time setup
.\src\powershell\Invoke-NanerGUI.ps1 -ShowWizard
```

**Option 3: Open Specific Tab**
```powershell
# Open directly to Vendors tab
.\src\powershell\Invoke-NanerGUI.ps1 -Tab Vendors

# Available tabs: Vendors, Environments, Profiles, Settings
```

**Option 4: Create Desktop Shortcut**
1. Right-click on Desktop → New → Shortcut
2. Target: `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass -File "C:\path\to\naner\src\powershell\Invoke-NanerGUI.ps1"`
3. Name: "Naner Configuration Manager"
4. Click Finish

---

## User Interface Guide

### Main Window Layout

```
┌─────────────────────────────────────────────────────────────┐
│ Naner Configuration Manager                          ☐ □ ✕ │
├─────────────────────────────────────────────────────────────┤
│ ┌─────┬────────────┬─────────────┬──────────┬──────────┐   │
│ │Vendors│Environments│  Profiles   │ Settings │          │   │
│ └─────┴────────────┴─────────────┴──────────┴──────────┘   │
│ ┌───────────────────────────────────────────────────────┐   │
│ │                                                       │   │
│ │               Tab Content Area                        │   │
│ │                                                       │   │
│ │               (ListView + Buttons)                    │   │
│ │                                                       │   │
│ │                                                       │   │
│ └───────────────────────────────────────────────────────┘   │
│ [Action Buttons]                           [Refresh]       │
└─────────────────────────────────────────────────────────────┘
```

### UI Elements

**Tab Control:**
- **Vendors** - Manage vendor installations
- **Environments** - Manage multiple environments
- **Profiles** - Manage shell profiles
- **Settings** - Configuration and utilities

**ListView:**
- Displays items in a table format
- Click column headers to sort (future feature)
- Select items to enable action buttons
- Bold text indicates active/default items
- Color coding: Blue (required), Gray (disabled)

**Action Buttons:**
- Located at the bottom of each tab
- Enabled/disabled based on selection
- Provide immediate feedback

---

## Vendors Tab

### Overview

The Vendors tab allows you to manage the installation and configuration of development tools and runtimes bundled with Naner.

### Vendor List Columns

| Column | Description |
|--------|-------------|
| **ID** | Internal vendor identifier (e.g., NodeJS, Python) |
| **Name** | Display name (e.g., Node.js, Miniconda) |
| **Enabled** | Whether the vendor is enabled in config |
| **Required** | Whether the vendor is required for Naner |
| **Status** | Installation status (Installed / Not Installed) |

### Color Coding

- **Blue Text** - Required vendors (PowerShell, Git, Terminal, MSYS2)
- **Gray Text** - Disabled vendors (not currently enabled)
- **Black Text** - Enabled optional vendors

### Actions

**Install Selected:**
- Select a vendor from the list
- Click "Install Selected"
- Installation will download and configure the vendor
- Progress dialog shows installation status (backend integration pending)

**Enable/Disable:**
- Toggle vendor enabled status in configuration
- Disabled vendors won't be included in PATH
- Useful for managing which tools are available (backend integration pending)

**Refresh:**
- Reload vendor list from configuration
- Updates installation status
- Use after manual configuration changes

### Vendor Details

**Required Vendors (Always Enabled):**
1. **7-Zip** - Archive extraction utility
2. **PowerShell 7** - Modern PowerShell runtime
3. **Windows Terminal** - Modern terminal emulator
4. **MSYS2/Git Bash** - Unix tools and Git

**Optional Vendors (Enable as Needed):**
5. **Node.js** - JavaScript runtime + npm
6. **Miniconda (Python)** - Python 3.x + conda + pip
7. **Go** - Go programming language
8. **Rust** - Rust toolchain + cargo
9. **Ruby** - Ruby runtime + gem + bundler

### Best Practices

1. **Install Required Vendors First** - Ensure core functionality works
2. **Install Optional Vendors On-Demand** - Save disk space, install only what you need
3. **Verify Installation** - Check Status column after installation
4. **Keep Vendors Updated** - Periodically check for updates

---

## Environments Tab

### Overview

The Environments tab manages multiple isolated development environments. Each environment has its own:
- Home directory (`home/`)
- Configuration files (.gitconfig, .bashrc, etc.)
- SSH keys
- Shell history
- Package caches

### Environment List Columns

| Column | Description |
|--------|-------------|
| **Name** | Environment identifier |
| **Description** | Human-readable description |
| **Status** | "Active" if currently in use |
| **Path** | Full path to environment home directory |

### Visual Indicators

- **Bold Text** - Active environment
- **"Active" Status** - Currently selected environment

### Actions

**Create New:**
- Opens dialog to create a new environment
- Specify name and description
- Option to copy from existing environment
- Creates isolated home directory structure (backend integration pending)

**Switch To:**
- Select an environment from the list
- Click "Switch To"
- Changes active environment for future shell sessions
- Does not affect currently running shells (backend integration pending)

**Delete:**
- Select a non-default environment
- Click "Delete"
- Confirmation dialog prevents accidental deletion
- Cannot delete default environment or active environment (backend integration pending)

**Refresh:**
- Reload environment list
- Updates active status
- Use after using CLI commands

### Use Cases

**Work vs. Personal:**
```
work/        → Work Git config, SSH keys, project configs
personal/    → Personal Git config, different SSH keys
```

**Client Projects:**
```
client-acme/     → Client A Git config, VPN settings
client-globex/   → Client B Git config, different credentials
```

**Experimentation:**
```
experiment/  → Testing new tools without affecting main environment
```

### Best Practices

1. **Default Environment** - Keep as your primary environment
2. **Descriptive Names** - Use clear, meaningful names
3. **Document Purpose** - Add good descriptions
4. **Backup Before Deleting** - Use sync/backup features
5. **Don't Over-Create** - Only create when truly needed

---

## Profiles Tab

### Overview

The Profiles tab manages shell profiles for launching different terminal environments. Profiles define:
- Shell type (PowerShell, Bash, CMD)
- Starting directory
- Color scheme
- Environment variables

### Profile List Columns

| Column | Description |
|--------|-------------|
| **ID** | Internal profile identifier |
| **Display Name** | Name shown in menus |
| **Shell** | Shell type (PowerShell, Bash, CMD) |
| **Description** | Profile description |
| **Default** | "Yes" if this is the default profile |

### Visual Indicators

- **Bold Text** - Default profile (launched when no profile specified)
- **"Yes" in Default Column** - Default profile marker

### Built-in Profiles

**Unified (Naner):**
- PowerShell 7 with all vendor tools in PATH
- Full Unix command support (via MSYS2)
- **Recommended for most users**

**PowerShell:**
- Pure PowerShell 7 environment
- Vendor tools available but focused on PowerShell

**Bash:**
- MSYS2 Bash environment
- Unix-like experience on Windows
- Good for Linux/Mac users

**CMD:**
- Windows Command Prompt
- Legacy compatibility
- Vendor tools in PATH

### Actions

**Launch Profile:**
- Select a profile from the list
- Click "Launch Profile"
- Opens Windows Terminal with selected profile
- Uses active environment's configuration (backend integration pending)

**Set as Default:**
- Select a profile
- Click "Set as Default"
- This profile will launch when running `Invoke-Naner.ps1` without arguments
- Updates naner.json (backend integration pending)

**Refresh:**
- Reload profile list from configuration
- Updates default marker
- Use after manual config changes

### Best Practices

1. **Start with Unified** - Best all-around experience
2. **Experiment with Others** - Find what works for you
3. **Keep Default Simple** - Set most-used profile as default
4. **Custom Profiles** - Edit naner.json to add custom profiles

---

## Settings Tab

### Overview

The Settings tab provides access to configuration files, system information, and utility functions.

### Information Display

**Naner Root:**
- Shows full path to Naner installation
- Read-only (cannot be changed via GUI)

**Config File:**
- Shows path to naner.json
- Read-only

### Actions

**Edit naner.json:**
- Opens naner.json in Notepad
- Main configuration file for Naner
- Edit profiles, PATH, environment variables
- Save and close Notepad to apply changes (requires restart)

**Edit vendors.json:**
- Opens vendors.json in Notepad
- Vendor definitions and download URLs
- Advanced users only
- Backup before editing

**Open Home Folder:**
- Opens home directory in Windows Explorer
- Browse configuration files
- Manage SSH keys, Git config, etc.
- Quick access to portable files

**Run Setup Wizard:**
- Launch the setup wizard again
- Useful for adding optional vendors
- Can be run multiple times

**Validate Configuration:**
- Checks configuration for errors
- Validates JSON syntax
- Checks file paths
- Reports issues with resolutions (backend integration pending)

### About Section

Displays:
- Naner version
- Feature list
- Documentation link

---

## Setup Wizard

### Overview

The Setup Wizard guides first-time users through initial Naner setup, making it easy to configure optional vendors and verify installation.

### When to Use

- **First Installation** - Set up Naner for the first time
- **Adding Vendors** - Install optional development runtimes
- **Reconfiguration** - Reset or change vendor selections

### Launching the Wizard

```powershell
# Launch wizard directly
.\src\powershell\Invoke-NanerGUI.ps1 -ShowWizard

# Or click "Run Setup Wizard" in Settings tab
```

### Wizard Steps

**Step 1: Welcome Screen**
- Introduction to Naner
- Overview of setup process

**Step 2: Vendor Selection**
- Checklist of optional vendors:
  - ☐ Node.js - JavaScript development
  - ☐ Python (Miniconda) - Python development
  - ☐ Go - Go development
  - ☐ Rust - Rust development
  - ☐ Ruby - Ruby development
- Check vendors you want to install
- Required vendors are pre-installed

**Step 3: Installation**
- Progress bar shows installation progress
- Status label shows current operation
- "Install Selected" button starts installation
- "Skip" button exits without installing

**Step 4: Completion**
- Success message
- Instructions for next steps
- Close wizard

### Notes

- Wizard can be cancelled at any time
- Vendors can be installed later via Vendors tab
- Re-running wizard won't reinstall existing vendors

---

## Keyboard Shortcuts

### Global Shortcuts

| Shortcut | Action |
|----------|--------|
| `Alt+F4` | Close GUI |
| `Ctrl+Tab` | Next tab |
| `Ctrl+Shift+Tab` | Previous tab |
| `F5` | Refresh current tab |

### Tab-Specific Shortcuts

**Vendors Tab:**
| Shortcut | Action |
|----------|--------|
| `Ctrl+I` | Install selected vendor |
| `Ctrl+E` | Enable/disable selected vendor |
| `F5` | Refresh vendor list |

**Environments Tab:**
| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | Create new environment |
| `Ctrl+S` | Switch to selected environment |
| `Delete` | Delete selected environment |
| `F5` | Refresh environment list |

**Profiles Tab:**
| Shortcut | Action |
|----------|--------|
| `Ctrl+L` | Launch selected profile |
| `Ctrl+D` | Set selected profile as default |
| `F5` | Refresh profile list |

**Settings Tab:**
| Shortcut | Action |
|----------|--------|
| `Ctrl+O` | Open naner.json |
| `Ctrl+V` | Open vendors.json |
| `Ctrl+H` | Open home folder |

*Note: Keyboard shortcuts are planned for future versions.*

---

## Troubleshooting

### Common Issues

#### GUI Won't Launch

**Symptom:** Double-clicking script does nothing or shows error

**Solutions:**
1. **Check Execution Policy:**
   ```powershell
   Get-ExecutionPolicy
   # If Restricted, run:
   Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
   ```

2. **Run from PowerShell:**
   ```powershell
   # See actual error message
   .\src\powershell\Invoke-NanerGUI.ps1
   ```

3. **Check .NET Framework:**
   - Windows Forms requires .NET Framework
   - Included with Windows 10/11
   - Update Windows if missing

#### Vendors Not Showing

**Symptom:** Vendors tab is empty

**Solutions:**
1. **Check vendors.json exists:**
   ```powershell
   Test-Path .\config\vendors.json
   ```

2. **Validate JSON syntax:**
   ```powershell
   Get-Content .\config\vendors.json | ConvertFrom-Json
   ```

3. **Refresh the list:**
   - Click "Refresh" button in Vendors tab

#### Environments Not Showing

**Symptom:** Only "default" environment visible

**Solutions:**
1. **Check environments directory:**
   ```powershell
   ls .\environments\
   ```

2. **Verify metadata files:**
   ```powershell
   # Each environment should have .metadata.json
   ls .\environments\*\.metadata.json
   ```

3. **Create environment via CLI first:**
   ```powershell
   Import-Module .\src\powershell\Naner.Environments.psm1
   New-NanerEnvironment -Name "test" -Description "Test environment"
   ```

#### "Access Denied" Errors

**Symptom:** Cannot save configuration or install vendors

**Solutions:**
1. **Run as Administrator:**
   - Right-click PowerShell → "Run as Administrator"
   - Then launch GUI

2. **Check file permissions:**
   ```powershell
   # Verify you can write to Naner directory
   Test-Path -Path $env:NANER_ROOT -PathType Container
   ```

3. **Antivirus interference:**
   - Add Naner directory to exclusions
   - Some antivirus block script execution

#### Configuration Changes Not Applied

**Symptom:** Changes in GUI don't take effect

**Solutions:**
1. **Restart shells:**
   - Close all Naner terminal sessions
   - Launch new session to see changes

2. **Check file was saved:**
   - Open naner.json in Notepad
   - Verify changes are present

3. **Validate JSON:**
   ```powershell
   Get-Content .\config\naner.json | ConvertFrom-Json
   ```

### Getting Help

**Check Documentation:**
- [README.md](../README.md) - Main documentation
- [ARCHITECTURE.md](ARCHITECTURE.md) - System design
- [MULTI-ENVIRONMENT.md](MULTI-ENVIRONMENT.md) - Environment management

**Debug Mode:**
```powershell
# Run with debug output
$DebugPreference = "Continue"
.\src\powershell\Invoke-NanerGUI.ps1
```

**Report Issues:**
- Create GitHub issue with:
  - Error message
  - Steps to reproduce
  - PowerShell version (`$PSVersionTable`)
  - Windows version

---

## Technical Architecture

### Technology Stack

**UI Framework:**
- **Windows Forms** (.NET Framework)
- Built-in with Windows 10/11
- No additional dependencies
- Native Windows look and feel

**Language:**
- **PowerShell 5.1+** (included with Windows)
- Integrates seamlessly with existing Naner modules
- Easy to modify and extend

**Modules Used:**
- **Common.psm1** - Shared utilities (Find-NanerRoot, Get-NanerConfig)
- **Naner.Vendors.psm1** - Vendor management
- **Naner.Environments.psm1** - Environment management

### Component Architecture

```
Invoke-NanerGUI.ps1
├── Main Window (Form)
│   ├── Tab Control
│   │   ├── Vendors Tab
│   │   │   ├── ListView (vendor list)
│   │   │   └── Action Buttons
│   │   ├── Environments Tab
│   │   │   ├── ListView (environment list)
│   │   │   └── Action Buttons
│   │   ├── Profiles Tab
│   │   │   ├── ListView (profile list)
│   │   │   └── Action Buttons
│   │   └── Settings Tab
│   │       ├── Information Fields
│   │       └── Action Buttons
│   └── Setup Wizard (Dialog)
│       ├── Welcome Screen
│       ├── Vendor Selection
│       └── Progress/Status
└── Utility Functions
    ├── Refresh-VendorsList
    ├── Refresh-EnvironmentsList
    ├── Refresh-ProfilesList
    └── Show-MessageBox
```

### Data Flow

```
User Action → Event Handler → Business Logic → Data Update → UI Refresh
     ↓              ↓               ↓              ↓             ↓
  Button Click   Click Event   PowerShell      JSON File    ListView
                   Handler      Cmdlets        Update        Reload
```

### File Structure

```
src/powershell/
└── Invoke-NanerGUI.ps1        # Main GUI script (1,100+ lines)

docs/
└── GUI-CONFIGURATION-MANAGER.md   # This document

config/
├── naner.json                 # Main configuration
└── vendors.json               # Vendor definitions

environments/
├── .active                    # Active environment marker
└── {environment-name}/        # Custom environments
    ├── .metadata.json
    └── home/                  # Environment home directory
```

### Integration Points

**With Existing Modules:**
```powershell
# Common utilities
Import-Module Common.psm1
$nanerRoot = Find-NanerRoot
$config = Get-NanerConfig

# Vendor management
Import-Module Naner.Vendors.psm1
Setup-NanerVendor -VendorId NodeJS

# Environment management
Import-Module Naner.Environments.psm1
New-NanerEnvironment -Name "work"
Use-NanerEnvironment -Name "work"
```

**With Configuration Files:**
```powershell
# Read configuration
$config = Get-Content config\naner.json | ConvertFrom-Json

# Modify configuration
$config.DefaultProfile = "Bash"

# Write configuration
$config | ConvertTo-Json -Depth 10 | Set-Content config\naner.json
```

### Security Considerations

**Script Execution:**
- Requires PowerShell execution policy: RemoteSigned or Unrestricted
- User must explicitly allow script execution
- No network communication (all operations are local)

**File Access:**
- GUI only reads/writes within Naner directory
- No system-wide changes (except PATH when launching shells)
- Configuration files are plain JSON (no credentials stored)

**Vendor Installation:**
- Downloads from official sources only
- SHA256 hash verification (when lock file present)
- User confirmation before installation

---

## Future Enhancements

### Version 1.1 (Next Release)

**Backend Integration:**
- ✅ Connect vendor install button to actual installation
- ✅ Connect environment create/switch/delete to cmdlets
- ✅ Connect profile launch to Invoke-Naner.ps1
- ✅ Implement configuration validation logic

**Progress Dialogs:**
- Show real-time progress for long operations
- Cancel button for installations
- Detailed progress messages

**Error Handling:**
- Graceful error messages
- Detailed error logs
- Retry mechanisms

### Version 1.2

**Advanced Configuration:**
- Edit PATH precedence visually
- Manage environment variables in GUI
- Custom profile editor

**Search & Filter:**
- Search vendors by name
- Filter by status (installed/not installed)
- Filter environments by name

**Themes:**
- Dark mode support
- Custom color schemes
- Font size options

### Version 2.0 (C# Migration)

**WPF Modern UI:**
- Modern UI with Fluent Design
- Animations and transitions
- Better responsiveness

**Performance:**
- Faster startup time
- Async operations
- Better memory management

**Additional Features:**
- Plugin management UI
- Sync & backup visual interface
- Configuration history/rollback
- Multi-language support

---

## API Reference

### Functions

#### Refresh-VendorsList
```powershell
Refresh-VendorsList -ListView $listView
```
Reloads vendor list from vendors.json and updates the ListView.

**Parameters:**
- `ListView` - The ListView control to update

**Returns:** None

#### Refresh-EnvironmentsList
```powershell
Refresh-EnvironmentsList -ListView $listView
```
Reloads environment list from environments directory and updates the ListView.

**Parameters:**
- `ListView` - The ListView control to update

**Returns:** None

#### Refresh-ProfilesList
```powershell
Refresh-ProfilesList -ListView $listView
```
Reloads profile list from naner.json and updates the ListView.

**Parameters:**
- `ListView` - The ListView control to update

**Returns:** None

#### Show-MessageBox
```powershell
Show-MessageBox -Message "Text" -Title "Title" -Buttons YesNo -Icon Question
```
Displays a message box with the specified parameters.

**Parameters:**
- `Message` (string) - Message text
- `Title` (string) - Window title (default: "Naner Configuration")
- `Buttons` (MessageBoxButtons) - Button style (OK, YesNo, etc.)
- `Icon` (MessageBoxIcon) - Icon type (Information, Warning, Error, Question)

**Returns:** DialogResult (OK, Yes, No, Cancel)

#### Show-SetupWizard
```powershell
Show-SetupWizard
```
Displays the setup wizard dialog for first-time configuration.

**Parameters:** None

**Returns:** None

---

## Conclusion

The Naner GUI Configuration Manager provides a user-friendly alternative to command-line configuration, making Naner accessible to users of all skill levels. While the initial version (1.0.0) focuses on UI and structure, future versions will expand functionality and eventually migrate to C# WPF for better performance and modern UI.

**Key Takeaways:**
- ✅ Visual management of vendors, environments, and profiles
- ✅ Setup wizard for beginners
- ✅ Complements existing PowerShell cmdlets
- ✅ Foundation for future C# GUI migration

For command-line users, all functionality remains available via PowerShell modules. The GUI is an additional tool, not a replacement.

---

**Document Version:** 1.0.0
**Last Updated:** 2026-01-07
**Author:** Naner Project
**License:** MIT
