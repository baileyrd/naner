# Vendor Setup Guide

This guide explains how to download and install vendor dependencies for Naner.

## What are Vendor Dependencies?

Vendor dependencies are the portable tools that Naner bundles and manages:

- **7-Zip** - File archiver (required for extracting other tools)
- **PowerShell 7** - Modern PowerShell environment
- **Windows Terminal** - Modern terminal application
- **MSYS2** - Git, Bash, and Unix utilities

## Automatic Setup (Recommended)

### During Initial Installation

When you run `naner init`, you'll be prompted to download vendors automatically:

```cmd
naner init
```

The setup wizard will ask:
```
Download vendor dependencies now? [Y/n]:
```

Press Enter or type `y` to download all required vendors automatically. This takes 5-10 minutes depending on your internet connection.

### After Installation

If you skipped vendor downloads during setup, you can install them anytime:

```cmd
naner setup-vendors
```

This command:
1. Detects your Naner installation
2. Downloads ~600 MB of vendor packages
3. Extracts and configures each tool
4. Skips tools already installed

## Skip Vendor Downloads

If you want to set up vendors manually or already have the tools installed:

```cmd
# Skip vendors during init
naner init --skip-vendors

# Or answer 'n' when prompted
naner init
```

## What Gets Downloaded

### 7-Zip (~2 MB)
- **Source:** https://www.7-zip.org/
- **Version:** 24.08 x64
- **Purpose:** Extract other archives (.zip, .tar.xz)
- **Install Location:** `vendor/7zip/`

### PowerShell 7 (~100 MB)
- **Source:** https://github.com/PowerShell/PowerShell
- **Version:** 7.4.6 win-x64
- **Purpose:** Modern PowerShell environment
- **Install Location:** `vendor/powershell/`

### Windows Terminal (~50 MB)
- **Source:** https://github.com/microsoft/terminal
- **Version:** 1.21.2361.0 x64
- **Purpose:** Launch terminals with profiles
- **Install Location:** `vendor/terminal/`

### MSYS2 (~400 MB)
- **Source:** https://repo.msys2.org/
- **Version:** 20240727 base x86_64
- **Purpose:** Git, Bash, Unix tools, GCC
- **Install Location:** `vendor/msys64/`
- **Note:** Additional packages installed on first terminal launch

## Download Progress

During download, you'll see progress indicators:

```
Downloading Vendor Dependencies
================================

This may take several minutes depending on your connection...

Downloading 7-Zip...
    Progress: 100%
  Downloaded 7z2408-x64.msi
  Installing 7-Zip...
  Installed 7-Zip

Downloading PowerShell...
    Progress: 10%
    Progress: 20%
    ...
```

## Offline Installation

If you need to install Naner without internet access:

1. **Download vendors on another machine:**
   - Download the 4 files listed above manually
   - Place them in `vendor/.downloads/` directory

2. **Extract manually:**
   ```powershell
   # Use the PowerShell vendor setup script
   .\src\powershell\Setup-NanerVendor.ps1 -SkipDownload
   ```

3. **Or extract individually** using 7-Zip to the appropriate `vendor/` subdirectories

## Troubleshooting

### Download Fails

**Problem:** "Failed to download PowerShell, skipping..."

**Solutions:**
1. Check your internet connection
2. Verify firewall/proxy settings
3. Try again - some downloads are large and may timeout
4. Download manually and extract to `vendor/<tool>/`

### Missing Tools After Download

**Problem:** "Could not locate PowerShell at %NANER_ROOT%\vendor\powershell\pwsh.exe"

**Solutions:**
1. Verify the directory exists: `dir vendor\powershell`
2. Check if pwsh.exe exists: `dir vendor\powershell\pwsh.exe`
3. Re-run vendor setup: `naner setup-vendors`
4. Manual extraction: Extract PowerShell ZIP to `vendor/powershell/`

### MSYS2 .tar.xz Extraction

**Note:** Currently, `.tar.xz` archives (MSYS2) require external tools to extract.

**Temporary Workaround:**
1. Download MSYS2 manually from https://www.msys2.org/
2. Run the installer
3. Copy `C:\msys64\` to `vendor\msys64\`

**Or use 7-Zip:**
```cmd
# Extract .tar.xz in two steps
7z x msys2-base-x86_64-20240727.tar.xz
7z x msys2-base-x86_64-20240727.tar -ovendor\
```

### Slow Downloads

Large files (especially MSYS2 at ~400 MB) may take time:
- **Good connection:** 5-10 minutes total
- **Slow connection:** 20-30 minutes
- **Very slow:** Consider offline installation

### Resume Interrupted Download

If a download is interrupted:
1. Run `naner setup-vendors` again
2. Already-installed vendors will be skipped
3. Only missing vendors will be downloaded

## Manual Vendor Management

### Check What's Installed

```cmd
# Run diagnostics to see vendor status
naner --diagnose
```

This shows which vendors are detected and their paths.

### Remove and Reinstall

```cmd
# Remove a vendor
rmdir /s vendor\powershell

# Reinstall it
naner setup-vendors
```

### Use Different Versions

To use a different version of a tool:

1. Download your preferred version
2. Extract to the vendor directory
3. Update `config/naner.json` if paths changed

Example for PowerShell 7.5.0:
```json
{
  "VendorPaths": {
    "PowerShell": "%NANER_ROOT%\\vendor\\PowerShell-7.5.0\\pwsh.exe"
  }
}
```

## See Also

- [Quick Start Guide](QUICK-START.md) - Getting started with Naner
- [Architecture](../reference/ARCHITECTURE.md) - How vendor management works
- [Troubleshooting](../archived/TROUBLESHOOTING.md) - Common issues
