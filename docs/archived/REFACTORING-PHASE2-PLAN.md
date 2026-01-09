# PowerShell Refactoring - Phase 2 Implementation Plan
## Modularization & Externalization

**Date Created:** 2026-01-07
**Status:** In Progress (Naner.Archives.psm1 Complete)
**Estimated Effort Remaining:** 8-10 hours

---

## Phase 2 Overview

Phase 2 focuses on creating specialized modules and externalizing hardcoded configuration to improve flexibility and reduce the size of Setup-NanerVendor.ps1.

### Goals
1. ✅ Create Naner.Archives.psm1 - Extract archive handling logic
2. ⏳ Create Naner.Vendors.psm1 - Extract vendor orchestration logic
3. ⏳ Externalize vendor config to config/vendors.json
4. ⏳ Refactor Setup-NanerVendor.ps1 to use new modules
5. ⏳ Test and validate the refactored system

---

## Completed Work

### ✅ Naner.Archives.psm1 Created

**Location:** [src/powershell/Naner.Archives.psm1](../src/powershell/Naner.Archives.psm1)

**Functions Exported:**
1. `Get-FileWithProgress` - Downloads files with progress and retry logic
   - Renamed from `Download-FileWithProgress` to use approved PowerShell verb
   - Includes Chrome User-Agent header
   - Automatic retry with configurable max attempts

2. `Get-SevenZipPath` - Locates 7-Zip executable
   - Checks vendored 7-Zip first
   - Falls back to system installations
   - Returns path and source information

3. `Expand-ArchiveWith7Zip` - Extracts archives using 7-Zip
   - Handles .tar.xz two-stage extraction
   - Supports all 7-Zip formats
   - Automatic cleanup of intermediate files

4. `Expand-VendorArchive` - Universal archive extraction
   - Supports .zip (built-in), .msi (msiexec), .tar.xz (7-Zip/tar)
   - Intelligent format detection
   - Graceful fallback mechanisms

**Benefits:**
- Reusable archive functions across all scripts
- Centralized download and extraction logic
- Easier to test and maintain
- ~200 lines extracted from Setup-NanerVendor.ps1

---

## Remaining Tasks

### Task 1: Create Naner.Vendors.psm1

**Purpose:** Extract vendor-specific setup logic into reusable functions.

**Proposed Structure:**
```powershell
# Naner.Vendors.psm1

function Get-VendorConfiguration {
    # Load vendors.json and return vendor configurations
}

function Invoke-VendorRelease {
    # Execute the GetLatestRelease logic for a vendor
    # Supports: GitHub API, web scraping, static URLs
}

function Install-VendorPackage {
    # Generic vendor installation orchestration:
    # 1. Get release info
    # 2. Download package
    # 3. Extract to destination
    # 4. Run post-install
}

function Initialize-SevenZipVendor {
    # PostInstall logic for 7-Zip
}

function Initialize-PowerShellVendor {
    # PostInstall logic for PowerShell
}

function Initialize-WindowsTerminalVendor {
    # PostInstall logic for Windows Terminal
}

function Initialize-MSYS2Vendor {
    # PostInstall logic for MSYS2
}

function Initialize-NodeJSVendor {
    # PostInstall logic for Node.js
}

function Initialize-MinicondaVendor {
    # PostInstall logic for Miniconda
}

function Initialize-GoVendor {
    # PostInstall logic for Go
}

function Initialize-RustVendor {
    # PostInstall logic for Rust
}

function Initialize-RubyVendor {
    # PostInstall logic for Ruby
}
```

**Estimated Effort:** 3-4 hours

---

### Task 2: Externalize Vendor Configuration

**Create:** `config/vendors.json`

**Proposed Schema:**
```json
{
  "$schema": "./vendors-schema.json",
  "vendors": [
    {
      "id": "SevenZip",
      "name": "7-Zip",
      "extractDir": "7zip",
      "dependencies": [],
      "enabled": true,
      "releaseSource": {
        "type": "web-scrape",
        "url": "https://www.7-zip.org/download.html",
        "pattern": "href=\"([^\"]*7z(\\d+)-x64\\.msi)\"",
        "fallback": {
          "version": "24.08",
          "url": "https://www.7-zip.org/a/7z2408-x64.msi",
          "fileName": "7z2408-x64.msi",
          "size": "~2"
        }
      },
      "postInstall": "Initialize-SevenZipVendor"
    },
    {
      "id": "PowerShell",
      "name": "PowerShell",
      "extractDir": "powershell",
      "dependencies": ["SevenZip"],
      "enabled": true,
      "releaseSource": {
        "type": "github",
        "repo": "PowerShell/PowerShell",
        "assetPattern": "*win-x64.zip",
        "fallback": {
          "version": "7.4.6",
          "url": "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/PowerShell-7.4.6-win-x64.zip",
          "fileName": "PowerShell-7.4.6-win-x64.zip",
          "size": "~100"
        }
      },
      "postInstall": "Initialize-PowerShellVendor"
    },
    {
      "id": "WindowsTerminal",
      "name": "Windows Terminal",
      "extractDir": "terminal",
      "dependencies": ["SevenZip"],
      "enabled": true,
      "releaseSource": {
        "type": "github",
        "repo": "microsoft/terminal",
        "assetPattern": "*_x64.zip",
        "fallback": {
          "version": "1.21.2361.0",
          "url": "https://github.com/microsoft/terminal/releases/download/v1.21.2361.0/Microsoft.WindowsTerminal_1.21.2361.0_x64.zip",
          "fileName": "Microsoft.WindowsTerminal_1.21.2361.0_x64.zip",
          "size": "~50"
        }
      },
      "postInstall": "Initialize-WindowsTerminalVendor"
    },
    {
      "id": "MSYS2",
      "name": "MSYS2",
      "extractDir": "msys64",
      "dependencies": ["SevenZip"],
      "enabled": true,
      "releaseSource": {
        "type": "web-scrape",
        "url": "https://repo.msys2.org/distrib/x86_64/",
        "pattern": "href=\"(msys2-base-x86_64-(\\d{8})\\.tar\\.xz)\"",
        "fallback": {
          "version": "20240727",
          "url": "https://repo.msys2.org/distrib/x86_64/msys2-base-x86_64-20240727.tar.xz",
          "fileName": "msys2-base-x86_64-20240727.tar.xz",
          "size": "~400"
        }
      },
      "postInstall": "Initialize-MSYS2Vendor",
      "packages": [
        "git",
        "make",
        "mingw-w64-x86_64-gcc",
        "mingw-w64-x86_64-make",
        "diffutils",
        "patch",
        "tar",
        "zip",
        "unzip"
      ]
    },
    {
      "id": "NodeJS",
      "name": "Node.js",
      "extractDir": "nodejs",
      "dependencies": ["SevenZip"],
      "enabled": false,
      "releaseSource": {
        "type": "github",
        "repo": "nodejs/node",
        "assetPattern": "*win-x64.zip",
        "fallback": {
          "version": "20.11.0",
          "url": "https://nodejs.org/dist/v20.11.0/node-v20.11.0-win-x64.zip",
          "fileName": "node-v20.11.0-win-x64.zip",
          "size": "~30"
        }
      },
      "postInstall": "Initialize-NodeJSVendor"
    },
    {
      "id": "Miniconda",
      "name": "Miniconda (Python)",
      "extractDir": "miniconda",
      "dependencies": [],
      "enabled": false,
      "releaseSource": {
        "type": "static",
        "url": "https://repo.anaconda.com/miniconda/Miniconda3-latest-Windows-x86_64.exe",
        "fileName": "Miniconda3-latest-Windows-x86_64.exe",
        "size": "~50",
        "version": "latest"
      },
      "postInstall": "Initialize-MinicondaVendor"
    },
    {
      "id": "Go",
      "name": "Go",
      "extractDir": "go",
      "dependencies": [],
      "enabled": false,
      "releaseSource": {
        "type": "golang-api",
        "url": "https://go.dev/dl/?mode=json",
        "fallback": {
          "version": "go1.21.6",
          "url": "https://go.dev/dl/go1.21.6.windows-amd64.zip",
          "fileName": "go1.21.6.windows-amd64.zip",
          "size": "~140"
        }
      },
      "postInstall": "Initialize-GoVendor"
    },
    {
      "id": "Rust",
      "name": "Rust",
      "extractDir": "rust",
      "dependencies": [],
      "enabled": false,
      "releaseSource": {
        "type": "static",
        "url": "https://static.rust-lang.org/rustup/dist/x86_64-pc-windows-msvc/rustup-init.exe",
        "fileName": "rustup-init.exe",
        "size": "~10",
        "version": "latest"
      },
      "postInstall": "Initialize-RustVendor"
    },
    {
      "id": "Ruby",
      "name": "Ruby",
      "extractDir": "ruby",
      "dependencies": [],
      "enabled": false,
      "releaseSource": {
        "type": "github",
        "repo": "oneclick/rubyinstaller2",
        "assetPattern": "rubyinstaller-devkit-*-x64.7z",
        "fallback": {
          "version": "3.2.3-1",
          "url": "https://github.com/oneclick/rubyinstaller2/releases/download/RubyInstaller-3.2.3-1/rubyinstaller-devkit-3.2.3-1-x64.7z",
          "fileName": "rubyinstaller-devkit-3.2.3-1-x64.7z",
          "size": "~180"
        }
      },
      "postInstall": "Initialize-RubyVendor"
    }
  ]
}
```

**Benefits:**
- Easy to add/remove/modify vendors without code changes
- Users can disable unwanted vendors via `"enabled": false`
- Clear dependency tracking
- Centralized fallback URLs
- Self-documenting configuration

**Estimated Effort:** 2-3 hours

---

### Task 3: Refactor Setup-NanerVendor.ps1

**New Structure:**
```powershell
# Setup-NanerVendor.ps1 (Orchestrator)

[CmdletBinding()]
param(
    [string]$NanerRoot,
    [switch]$SkipDownload,
    [switch]$ForceDownload,
    [string[]]$Vendors  # NEW: Allow selecting specific vendors
)

# Import modules
Import-Module (Join-Path $PSScriptRoot "Common.psm1") -Force
Import-Module (Join-Path $PSScriptRoot "Naner.Archives.psm1") -Force
Import-Module (Join-Path $PSScriptRoot "Naner.Vendors.psm1") -Force

# Determine Naner root
$nanerRoot = Get-NanerRootSimple -ScriptRoot $PSScriptRoot

# Load vendor configuration
$vendorConfigPath = Join-Path $nanerRoot "config\vendors.json"
$vendorConfig = Get-VendorConfiguration -ConfigPath $vendorConfigPath

# Filter vendors if specified
if ($Vendors) {
    $vendorConfig = $vendorConfig | Where-Object { $_.id -in $Vendors }
}

# Filter enabled vendors
$vendorConfig = $vendorConfig | Where-Object { $_.enabled -eq $true }

# Process each vendor
foreach ($vendor in $vendorConfig) {
    Write-Status "Processing: $($vendor.name)"

    # Install vendor package
    $success = Install-VendorPackage `
        -Vendor $vendor `
        -DownloadDir $downloadDir `
        -VendorDir $vendorDir `
        -SkipDownload:$SkipDownload `
        -ForceDownload:$ForceDownload

    if (-not $success) {
        Write-Warning "Failed to install $($vendor.name)"
    }
}

# Create manifest
# ... (simplified manifest creation)
```

**New Features:**
- Select specific vendors: `.\Setup-NanerVendor.ps1 -Vendors "PowerShell","WindowsTerminal"`
- Automatic dependency resolution
- Much smaller file (~200 lines vs 1,170 lines)

**Estimated Effort:** 3-4 hours

---

### Task 4: Testing & Validation

**Test Plan:**
1. Test vendor installation with new modules
2. Verify all PostInstall functions work correctly
3. Test dependency resolution (7-Zip must install before others)
4. Test selective vendor installation
5. Verify fallback URLs work when API fails
6. Test with both enabled and disabled vendors
7. Validate manifest generation

**Test Commands:**
```powershell
# Test full installation
.\Setup-NanerVendor.ps1 -ForceDownload

# Test selective installation
.\Setup-NanerVendor.ps1 -Vendors "PowerShell","WindowsTerminal" -ForceDownload

# Test skip download
.\Setup-NanerVendor.ps1 -SkipDownload

# Test with disabled vendor (should skip)
# Edit vendors.json to disable a vendor first
.\Setup-NanerVendor.ps1
```

**Estimated Effort:** 2 hours

---

## Expected Benefits

### Before (Current State)
- Setup-NanerVendor.ps1: 1,170 lines
- Hardcoded vendor configurations
- Difficult to add new vendors
- Cannot selectively install vendors
- PostInstall logic embedded in hashtables

### After (Phase 2 Complete)
- Setup-NanerVendor.ps1: ~200 lines (83% reduction)
- JSON-based vendor configuration
- Adding vendors: Edit JSON + create Initialize function
- Selective installation via command-line
- Reusable, testable modules

### Metrics
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Setup-NanerVendor.ps1 size | 1,170 lines | ~200 lines | **83% smaller** |
| Time to add vendor | 20-30 min | 5-10 min | **60% faster** |
| Vendor selection | Not possible | Command-line | **New feature** |
| Code reusability | Low | High | **Modular** |
| Testability | Difficult | Easy | **Improved** |

---

## Implementation Checklist

- [x] Create Naner.Archives.psm1
  - [x] Get-FileWithProgress
  - [x] Get-SevenZipPath
  - [x] Expand-ArchiveWith7Zip
  - [x] Expand-VendorArchive

- [ ] Create Naner.Vendors.psm1
  - [ ] Get-VendorConfiguration
  - [ ] Invoke-VendorRelease
  - [ ] Install-VendorPackage
  - [ ] Initialize-* functions (9 vendors)

- [ ] Create config/vendors.json
  - [ ] Define JSON schema
  - [ ] Add all 9 vendor configurations
  - [ ] Set reasonable defaults for enabled/disabled

- [ ] Refactor Setup-NanerVendor.ps1
  - [ ] Import new modules
  - [ ] Load JSON configuration
  - [ ] Simplify orchestration logic
  - [ ] Remove old functions (now in modules)

- [ ] Testing
  - [ ] Full installation test
  - [ ] Selective vendor test
  - [ ] Dependency resolution test
  - [ ] Fallback URL test
  - [ ] Enabled/disabled vendor test

- [ ] Documentation
  - [ ] Update Phase 2 summary
  - [ ] Document vendors.json schema
  - [ ] Update Setup-NanerVendor.ps1 examples
  - [ ] Add vendor addition guide

---

## Next Steps

1. **Continue implementation** - Create Naner.Vendors.psm1 with PostInstall functions
2. **Create vendors.json** - Externalize configuration
3. **Refactor Setup-NanerVendor.ps1** - Use new modules
4. **Test thoroughly** - Ensure no regressions
5. **Commit Phase 2** - Create comprehensive commit message

---

## Notes

- Phase 1 eliminated ~225 lines of duplicate code
- Phase 2 will further reduce Setup-NanerVendor.ps1 by ~970 lines
- Total reduction: ~1,195 lines removed, ~630 lines added to modules
- Net improvement: **560 lines removed** with **better organization**

---

**Status:** Naner.Archives.psm1 complete, ready to continue with Naner.Vendors.psm1
**Next Action:** Create Naner.Vendors.psm1 and begin vendor PostInstall extraction
