# Code Quality Analysis - DRY & SOLID Principles

**Date:** 2026-01-06
**Scope:** `naner_launcher/src/powershell/` directory

## Executive Summary

Analysis of the PowerShell codebase revealed multiple violations of DRY (Don't Repeat Yourself) and SOLID principles. A common utility module (`Common.psm1`) has been created to address these issues and improve code maintainability.

---

## DRY Principle Violations

### 1. Duplicate Logging Functions ❌

**Issue:** Color-coded logging functions (`Write-Status`, `Write-Success`, `Write-Info`, `Write-Failure`) are duplicated across multiple files.

**Locations:**
- `Setup-NanerVendor.ps1` (lines 69-87)
- `Build-NanerDistribution.ps1` (lines 51-65)
- `Manage-NanerVendor.ps1` (lines 50-64)

**Impact:**
- ~60 lines of duplicate code
- Inconsistent formatting if one file is updated
- Maintenance burden

**Solution:** ✅ Consolidated into `Common.psm1`

```powershell
# Before (duplicated in 3 files)
function Write-Status {
    param([string]$Message)
    Write-Host "[*] $Message" -ForegroundColor Cyan
}

# After (single source of truth)
Import-Module (Join-Path $PSScriptRoot "Common.psm1")
Write-Status "Message"
```

---

### 2. Duplicate Naner Root Detection ❌

**Issue:** Logic to find Naner root directory (`Split-Path (Split-Path $PSScriptRoot -Parent) -Parent`) is repeated in 8 files.

**Locations:**
- `Setup-NanerVendor.ps1:91`
- `Build-NanerDistribution.ps1:80`
- `Manage-NanerVendor.ps1:46`
- `Test-NanerInstallation.ps1:103`
- `Test-WindowsTerminalLaunch.ps1:22`
- `Show-TerminalStructure.ps1:15`
- `Validate-WindowsTerminal.ps1:17`
- Only `Invoke-Naner.ps1` has proper `Find-NanerRoot` function

**Impact:**
- Fragile code (breaks if directory structure changes)
- No error handling in simple version
- Inconsistent approaches

**Solution:** ✅ Two functions in `Common.psm1`

```powershell
# Robust approach (with marker detection)
$nanerRoot = Find-NanerRoot

# Fast approach (simple parent navigation)
$nanerRoot = Get-NanerRootSimple -ScriptRoot $PSScriptRoot
```

---

### 3. Duplicate Path Expansion Logic ❌

**Issue:** Path expansion for `%NANER_ROOT%` and environment variables duplicated inline.

**Locations:**
- `Invoke-Naner.ps1` has `Expand-NanerPath` function (lines 93-124)
- `Test-NanerInstallation.ps1` duplicates inline (lines 322, 422, 436, 448):
  ```powershell
  $path -replace '%NANER_ROOT%', $nanerRoot
  [System.Environment]::ExpandEnvironmentVariables($path)
  ```

**Impact:**
- Duplicated logic across 2 files
- Inconsistent expansion behavior
- Missing PowerShell-style `$env:VAR` expansion in Test script

**Solution:** ✅ Consolidated into `Common.psm1`

```powershell
$expandedPath = Expand-NanerPath -Path "%NANER_ROOT%\vendor" -NanerRoot $nanerRoot
```

---

### 4. Duplicate GitHub API Functions ❌

**Issue:** Nearly identical GitHub API functions with different names.

**Locations:**
- `Setup-NanerVendor.ps1` - `Get-LatestGitHubRelease` (line 115)
- `Manage-NanerVendor.ps1` - `Get-GitHubLatestRelease` (line 66)

**Differences:**
- Different function names (inconsistent naming)
- Slightly different error handling
- Same core logic

**Impact:**
- ~50 lines of duplicate code
- Naming inconsistency
- Bug fixes must be applied twice

**Solution:** ✅ Single function in `Common.psm1`

```powershell
$release = Get-LatestGitHubRelease `
    -Repo "PowerShell/PowerShell" `
    -AssetPattern "*win-x64.zip" `
    -FallbackUrl "https://..."
```

---

## SOLID Principle Violations

### 1. Single Responsibility Principle (SRP) ⚠️

**Issue:** Scripts doing too many unrelated things.

#### Setup-NanerVendor.ps1 (854 lines)
Responsibilities:
1. Downloading files
2. Extracting archives (multiple formats)
3. Configuring vendors
4. Managing 7-Zip
5. Creating manifests
6. Web scraping (MSYS2)

**Recommendation:**
- Create separate modules:
  - `Vendor-Download.psm1` - Download orchestration
  - `Vendor-Archive.psm1` - Archive extraction (already well-factored)
  - `Vendor-Config.psm1` - Vendor-specific post-install

#### Invoke-Naner.ps1 (447 lines)
Responsibilities:
1. Configuration loading
2. PATH building
3. Shell command determination
4. Windows Terminal launching

**Status:** ✅ Acceptable
- Well-organized with clear region markers
- Functions have single clear purposes
- Could benefit from extracting to modules but not urgent

---

### 2. Open/Closed Principle (OCP) ⚠️

**Issue:** Vendor configuration is hardcoded in `Setup-NanerVendor.ps1`.

**Current:** Lines 229-483 contain hardcoded vendor definitions
```powershell
$vendorConfig = [ordered]@{
    SevenZip = @{ ... }
    PowerShell = @{ ... }
    WindowsTerminal = @{ ... }
    MSYS2 = @{ ... }
}
```

**Impact:**
- Adding new vendor = modifying script
- Can't reuse vendor logic elsewhere
- Testing individual vendors is difficult

**Recommendation:**
```powershell
# Future enhancement: External vendor definitions
$vendorConfig = Get-Content "config\vendor-definitions.json" | ConvertFrom-Json

# Or: Plugin-based architecture
$vendors = Get-ChildItem "vendor-plugins\*.ps1" | ForEach-Object { & $_ }
```

**Priority:** 🔴 Low (current approach works, but limits extensibility)

---

### 3. Dependency Inversion Principle (DIP) ℹ️

**Issue:** Scripts directly reference concrete file paths instead of abstractions.

**Example:**
```powershell
# Direct dependency
$wtPath = Join-Path $vendorDir "terminal\wt.exe"

# Better: Abstract interface
interface IVendorResolver {
    GetPath(string vendorName)
}
```

**Note:** PowerShell doesn't have interfaces, but we can use:
- Configuration-driven paths (already done in `naner.json`)
- Vendor registry pattern
- Service locator pattern

**Priority:** 🔴 Low (PowerShell scripting context makes this less critical)

---

## Refactoring Plan

### Phase 1: ✅ COMPLETED
- [x] Create `Common.psm1` with shared functions
- [x] Document all violations
- [x] Establish coding standards

### Phase 2: 🚀 RECOMMENDED (Next Steps)
- [ ] Update all scripts to use `Common.psm1`
- [ ] Remove duplicate function definitions
- [ ] Add unit tests for common functions
- [ ] Create `CONTRIBUTING.md` with coding guidelines

### Phase 3: 🔮 FUTURE ENHANCEMENTS
- [ ] Extract vendor management to separate module
- [ ] Create plugin architecture for extensibility
- [ ] Add Pester tests for all modules
- [ ] Set up PSScriptAnalyzer in CI/CD

---

## Migration Guide

### For Script Authors

**Before:**
```powershell
# Old way - duplicated in every file
function Write-Status {
    param([string]$Message)
    Write-Host "[*] $Message" -ForegroundColor Cyan
}

$nanerRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
```

**After:**
```powershell
# New way - import common module
Import-Module (Join-Path $PSScriptRoot "Common.psm1") -Force

$nanerRoot = Find-NanerRoot
Write-Status "Processing..."
```

### Module Import Pattern

Add this to the top of each script (after param block):
```powershell
# Import common utilities
$commonModule = Join-Path $PSScriptRoot "Common.psm1"
if (Test-Path $commonModule) {
    Import-Module $commonModule -Force
} else {
    Write-Warning "Common module not found. Some functions may be unavailable."
}
```

---

## Benefits of Refactoring

### Maintainability
- ✅ Single source of truth for common functions
- ✅ Bug fixes in one place
- ✅ Consistent behavior across all scripts

### Code Quality
- ✅ Reduced code duplication (~200+ lines eliminated potential)
- ✅ Better error handling in centralized functions
- ✅ Improved testability

### Developer Experience
- ✅ Easier to onboard new contributors
- ✅ Clear separation of concerns
- ✅ IntelliSense support for exported functions

---

## Metrics

### Code Duplication
| Function | Instances | Lines Saved |
|----------|-----------|-------------|
| Write-Status/Success/Info/Failure | 3 | ~60 |
| Naner Root Detection | 7 | ~14 |
| Path Expansion | 4+ | ~30+ |
| GitHub API | 2 | ~50 |
| **Total** | **16+** | **~154+** |

### Lines of Code
- **Before:** ~2,500 total lines (with duplicates)
- **After:** ~2,350 estimated (after refactoring)
- **Reduction:** ~150 lines (6% reduction)

### Maintainability Index
- **Before:** Medium (duplicated code, inconsistent patterns)
- **After:** High (DRY principles, centralized utilities)

---

## Testing Recommendations

### Unit Tests (Pester)
```powershell
Describe "Common Module" {
    Context "Find-NanerRoot" {
        It "Should find root from known location" {
            $root = Find-NanerRoot
            $root | Should -Not -BeNullOrEmpty
            Test-Path (Join-Path $root "bin") | Should -Be $true
        }
    }

    Context "Expand-NanerPath" {
        It "Should expand NANER_ROOT variable" {
            $expanded = Expand-NanerPath -Path "%NANER_ROOT%\test" -NanerRoot "C:\naner"
            $expanded | Should -Be "C:\naner\test"
        }
    }
}
```

---

## Conclusion

The creation of `Common.psm1` addresses the majority of DRY violations and establishes a foundation for better code organization. The recommended next step is to update all existing scripts to use the common module, which will eliminate ~150 lines of duplicate code and improve maintainability.

**Priority:** 🟢 High - Implement Phase 2 refactoring in next development cycle

**Effort:** 🔨 Medium - Estimated 2-4 hours to update all scripts

**Risk:** 🟡 Low - Changes are additive, backward-compatible approach possible
