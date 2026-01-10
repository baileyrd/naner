# Archived PowerShell Source Code

This directory contains the original PowerShell implementation of Naner from before v1.0.0.

## Historical Context

These files were the core of Naner's PowerShell-based implementation, which was replaced by a pure C# implementation in v1.0.0 (January 2026).

## Key Files

**Core Modules:**
- `Common.psm1` / `Common.ps1` - Common utilities and functions
- `ErrorCodes.psm1` - Error code definitions
- `Naner.Vendors.psm1` - Vendor download and setup
- `Naner.Archives.psm1` - Archive extraction utilities
- `Naner.Environments.psm1` - Environment management
- `Naner.Plugins.psm1` - Plugin system

**Main Scripts:**
- `Invoke-Naner.ps1` - Main launcher script
- `Invoke-NanerGUI.ps1` - GUI configuration manager (experimental)
- `Setup-NanerVendor.ps1` - Vendor setup script
- `Test-NanerInstallation.ps1` - Installation testing

**Utilities:**
- `Backup-NanerConfig.ps1` - Configuration backup
- `Restore-NanerConfig.ps1` - Configuration restore
- `Sync-NanerConfig.ps1` - Configuration sync
- `Export-VendorLockFile.ps1` - Vendor lock file export
- `Import-VendorLockFile.ps1` - Vendor lock file import
- `Manage-NanerVendor.ps1` - Vendor management
- `New-NanerProject.ps1` - Project template creation
- `Show-TerminalStructure.ps1` - Display directory structure
- `Test-WindowsTerminalLaunch.ps1` - Terminal launch testing
- `Validate-WindowsTerminal.ps1` - Terminal validation

## Why Archived?

The PowerShell implementation was replaced with C# for several reasons:
1. Better performance (no PowerShell startup overhead)
2. Single self-contained executable (11 MB)
3. No external dependencies
4. Easier debugging and testing
5. Better error handling and diagnostics
6. More professional CLI experience

## Current Implementation

The current C# implementation is located in `src/csharp/` and provides all the same functionality with significant improvements.

See [CSHARP-MIGRATION-ROADMAP.md](../CSHARP-MIGRATION-ROADMAP.md) for details on the migration.

## Historical Value

These files are preserved for:
- Historical reference
- Understanding the evolution of the project
- Potential future PowerShell module extraction
- Learning from design decisions

**Note:** This code is no longer maintained and should not be used for new installations.
