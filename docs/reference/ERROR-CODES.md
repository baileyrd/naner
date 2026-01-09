# Naner Error Codes Reference

Complete reference for all Naner error codes, their meanings, and resolution steps.

## Error Code Format

**Format**: `NANER-XXXX`

### Categories

| Range | Category | Description |
|-------|----------|-------------|
| 1000-1999 | General | General system errors |
| 2000-2999 | Configuration | Configuration file and settings errors |
| 3000-3999 | Network | Network and download errors |
| 4000-4999 | Installation | Vendor installation errors |
| 5000-5999 | FileSystem | File system operations errors |
| 6000-6999 | Validation | Validation and verification errors |

### Severity Levels

- **Error**: Operation cannot continue, requires user action
- **Warning**: Operation completed with issues, may require attention
- **Info**: Informational message, no action needed

---

## General Errors (1000-1999)

### NANER-1001: Failed to locate Naner root directory

**Severity**: Error
**Category**: General

**Description**: The script could not find the Naner root directory by traversing parent directories.

**Resolution**:
1. Ensure you're running the script from within the Naner directory structure
2. The script looks for three marker directories: `bin/`, `vendor/`, and `config/`
3. Verify these directories exist in your Naner installation

**Example**:
```powershell
# Correct - run from Naner directory or subdirectory
cd C:\naner\src\powershell
.\Setup-NanerVendor.ps1  # ✓ Works

# Incorrect - run from outside Naner
cd C:\
.\naner\src\powershell\Setup-NanerVendor.ps1  # ✗ May fail
```

---

### NANER-1002: Invalid parameter combination

**Severity**: Error
**Category**: General

**Description**: Conflicting or invalid parameters were provided to a script.

**Resolution**:
1. Review the script's help documentation: `Get-Help .\Script.ps1 -Detailed`
2. Check for mutually exclusive parameters
3. Verify parameter values are valid

**Example**:
```powershell
# Invalid combination
.\Setup-NanerVendor.ps1 -SkipDownload -ForceDownload  # ✗ Conflicting

# Valid usage
.\Setup-NanerVendor.ps1 -SkipDownload  # ✓ OK
.\Setup-NanerVendor.ps1 -ForceDownload  # ✓ OK
```

---

### NANER-1003: Operation cancelled by user

**Severity**: Warning
**Category**: General

**Description**: User cancelled the operation (e.g., pressed Ctrl+C or chose "No" in a prompt).

**Resolution**: No action needed. Operation was safely cancelled.

---

### NANER-1004: Insufficient permissions

**Severity**: Error
**Category**: General

**Description**: The operation requires elevated permissions or file access rights.

**Resolution**:
1. Run PowerShell as Administrator
2. Check file/directory permissions
3. Ensure files are not locked by other applications

---

## Configuration Errors (2000-2999)

### NANER-2001: Configuration file not found

**Severity**: Error
**Category**: Configuration

**Description**: Required configuration file (naner.json or vendors.json) not found.

**Resolution**:
1. Verify `config/naner.json` and `config/vendors.json` exist
2. Check file paths are correct
3. Restore missing files from repository

**Example**:
```powershell
# Check if config files exist
Test-Path .\config\naner.json
Test-Path .\config\vendors.json

# Restore from git if missing
git checkout HEAD -- config/
```

---

### NANER-2002: Invalid JSON in configuration file

**Severity**: Error
**Category**: Configuration

**Description**: Configuration file contains malformed JSON.

**Resolution**:
1. Validate JSON syntax using online validator or VS Code
2. Check for:
   - Missing commas
   - Trailing commas
   - Unescaped quotes
   - Mismatched brackets
3. Restore from git if corrupted: `git checkout HEAD -- config/naner.json`

**Common Issues**:
```json
// ✗ Trailing comma
{
  "profile": "Unified",
}

// ✓ Correct
{
  "profile": "Unified"
}
```

---

### NANER-2003: Missing required configuration field

**Severity**: Error
**Category**: Configuration

**Description**: Configuration file is missing a required field.

**Resolution**:
1. Compare configuration against `config/vendors-schema.json`
2. Check documentation for required fields
3. Restore default configuration from repository

---

### NANER-2004: Invalid vendor configuration

**Severity**: Error
**Category**: Configuration

**Description**: Vendor configuration doesn't match expected schema.

**Resolution**:
1. Validate against `config/vendors-schema.json`
2. Check vendor structure has required fields:
   - `name`
   - `extractDir`
   - `releaseSource`
3. Review example vendor configurations

---

### NANER-2005: Vendor not found in configuration

**Severity**: Error
**Category**: Configuration

**Description**: Specified vendor ID doesn't exist in vendors.json.

**Resolution**:
1. Check vendor ID spelling
2. List available vendors: `Get-VendorConfiguration -ConfigPath .\config\vendors.json | Select-Object -ExpandProperty Keys`
3. Add vendor to configuration if missing

**Example**:
```powershell
# List available vendors
$vendors = Get-Content .\config\vendors.json | ConvertFrom-Json
$vendors.vendors.PSObject.Properties.Name

# Common vendor IDs
# - SevenZip
# - PowerShell
# - WindowsTerminal
# - MSYS2
# - NodeJS
# - Miniconda
# - Go
# - Rust
# - Ruby
```

---

### NANER-2006: Lock file validation failed

**Severity**: Error
**Category**: Configuration

**Description**: Vendor lock file is invalid or corrupted.

**Resolution**:
1. Regenerate lock file: `.\Export-VendorLockFile.ps1`
2. Verify lock file JSON is valid
3. Check file wasn't manually edited

---

## Network/Download Errors (3000-3999)

### NANER-3001: Network connection failed

**Severity**: Error
**Category**: Network

**Description**: Unable to establish network connection.

**Resolution**:
1. Check internet connectivity
2. Verify proxy settings: `netsh winhttp show proxy`
3. Test URL accessibility: `Invoke-WebRequest -Uri <url>`
4. Check firewall settings

---

### NANER-3002: Download failed after maximum retries

**Severity**: Error
**Category**: Network

**Description**: File download failed after all retry attempts.

**Resolution**:
1. Check network stability
2. Verify URL is accessible
3. Try manual download and place in `downloads/` directory
4. Increase retry count if transient network issues

**Manual Download**:
```powershell
# Download manually
$url = "https://example.com/file.zip"
$output = ".\downloads\file.zip"
Invoke-WebRequest -Uri $url -OutFile $output

# Then run setup with skip download
.\Setup-NanerVendor.ps1 -SkipDownload
```

---

### NANER-3003: GitHub API rate limit exceeded

**Severity**: Warning
**Category**: Network

**Description**: GitHub API rate limit reached (60 requests/hour for unauthenticated).

**Resolution**:
1. Wait 1 hour for rate limit reset
2. Use GitHub personal access token:
   ```powershell
   $env:GITHUB_TOKEN = "your_token_here"
   ```
3. Use fallback URLs in vendor configuration

---

### NANER-3004: Invalid or unreachable URL

**Severity**: Error
**Category**: Network

**Description**: Specified URL is malformed or unreachable.

**Resolution**:
1. Verify URL syntax is correct
2. Check for typos
3. Test URL in browser
4. Update vendor configuration with correct URL

---

### NANER-3005: File hash mismatch

**Severity**: Error
**Category**: Network

**Description**: Downloaded file's SHA256 hash doesn't match expected value.

**Resolution**:
1. Delete corrupted file: `Remove-Item .\downloads\<file>`
2. Re-download: `.\Setup-NanerVendor.ps1 -VendorId <vendor> -ForceDownload`
3. If problem persists, file source may be compromised - investigate before proceeding
4. Update lock file if version changed legitimately

---

## Installation Errors (4000-4999)

### NANER-4001: Vendor installation failed

**Severity**: Error
**Category**: Installation

**Description**: Vendor installation process failed.

**Resolution**:
1. Check installation logs for specific errors
2. Verify sufficient disk space: `Get-PSDrive C | Select-Object Free`
3. Ensure no antivirus interference
4. Retry installation: `.\Setup-NanerVendor.ps1 -VendorId <vendor> -ForceDownload`

---

### NANER-4002: Archive extraction failed

**Severity**: Error
**Category**: Installation

**Description**: Failed to extract vendor archive file.

**Resolution**:
1. Verify archive file is not corrupted
2. Ensure 7-Zip is installed (install SevenZip vendor first)
3. Check extraction path has write permissions
4. Try manual extraction to `vendor/<extractDir>/`

---

### NANER-4003: PostInstall function failed

**Severity**: Warning
**Category**: Installation

**Description**: Vendor installed but post-installation configuration failed.

**Resolution**:
1. Check vendor-specific logs
2. Manually run post-install steps (see vendor documentation)
3. Vendor may still be functional despite warning

---

### NANER-4004: Vendor dependency not satisfied

**Severity**: Error
**Category**: Installation

**Description**: Required dependency vendor not installed.

**Resolution**:
1. Install dependencies first (e.g., SevenZip before others)
2. Run full setup: `.\Setup-NanerVendor.ps1` (installs in dependency order)
3. Check vendor dependencies in `config/vendors.json`

**Example**:
```powershell
# PowerShell depends on SevenZip
# Install in correct order
.\Setup-NanerVendor.ps1 -VendorId SevenZip
.\Setup-NanerVendor.ps1 -VendorId PowerShell
```

---

### NANER-4005: Vendor already installed

**Severity**: Info
**Category**: Installation

**Description**: Vendor is already installed and `-ForceDownload` not specified.

**Resolution**:
- To reinstall: `.\Setup-NanerVendor.ps1 -VendorId <vendor> -ForceDownload`
- To skip: No action needed

---

### NANER-4006: Unsupported archive format

**Severity**: Error
**Category**: Installation

**Description**: Archive file format is not supported.

**Supported Formats**:
- `.zip` - PowerShell built-in
- `.msi` - Windows Installer
- `.tar.xz` - 7-Zip
- `.7z` - 7-Zip

**Resolution**:
1. Verify file extension is correct
2. Check vendor configuration specifies correct file
3. Contact maintainers to add support for new formats

---

## File System Errors (5000-5999)

### NANER-5001: Insufficient disk space

**Severity**: Error
**Category**: FileSystem

**Description**: Not enough free disk space for installation.

**Resolution**:
1. Check free space: `Get-PSDrive C | Select-Object Free`
2. Free up space (vendor installations need ~2-5 GB)
3. Change installation location if possible

---

### NANER-5002: File or directory not found

**Severity**: Error
**Category**: FileSystem

**Description**: Expected file or directory doesn't exist.

**Resolution**:
1. Verify path is correct
2. Check for typos
3. Ensure file wasn't moved or deleted
4. Restore from git if necessary

---

### NANER-5003: File access denied

**Severity**: Error
**Category**: FileSystem

**Description**: Insufficient permissions to access file.

**Resolution**:
1. Run as Administrator
2. Check file permissions
3. Close applications using the file
4. Disable antivirus temporarily if blocking access

---

### NANER-5004: Directory creation failed

**Severity**: Error
**Category**: FileSystem

**Description**: Failed to create required directory.

**Resolution**:
1. Verify parent directory exists
2. Check write permissions
3. Ensure path length is not too long (Windows MAX_PATH limit)

---

### NANER-5005: File deletion failed

**Severity**: Warning
**Category**: FileSystem

**Description**: Failed to delete temporary or old files.

**Resolution**:
1. Check if file is in use
2. Close applications
3. Manually delete file if needed
4. Reboot if file is locked

---

## Validation Errors (6000-6999)

### NANER-6001: Vendor path validation failed

**Severity**: Warning
**Category**: Validation

**Description**: One or more vendor paths don't exist or are invalid.

**Resolution**:
1. Run: `.\Setup-NanerVendor.ps1` to install missing vendors
2. Check specific vendor paths in `vendor/` directory
3. Run: `.\Test-NanerInstallation.ps1` for full validation

---

### NANER-6002: Installation verification failed

**Severity**: Error
**Category**: Validation

**Description**: Expected files not found after installation.

**Resolution**:
1. Retry installation with `-ForceDownload`
2. Check installation logs for errors
3. Verify archive was fully extracted
4. Manually verify expected files exist

---

### NANER-6003: Version mismatch detected

**Severity**: Warning
**Category**: Validation

**Description**: Installed version differs from expected/locked version.

**Resolution**:
1. Update lock file: `.\Export-VendorLockFile.ps1`
2. Or reinstall correct version using lock file (future feature)
3. Document version change if intentional

---

### NANER-6004: SHA256 hash validation failed

**Severity**: Error
**Category**: Validation

**Description**: File hash doesn't match expected value in lock file.

**Resolution**:
1. Delete file and re-download
2. If repeatedly fails, file source may be compromised
3. Update lock file if version legitimately changed
4. Never ignore persistent hash failures

---

## Usage Examples

### Handling Errors in Scripts

```powershell
# Import error module
Import-Module .\src\powershell\ErrorCodes.psm1

# Write structured error
try {
    # ... operation ...
} catch {
    Write-NanerError -ErrorCode "NANER-2001" `
                     -AdditionalInfo "Path: $configPath" `
                     -Exception $_ `
                     -ExitScript
}

# Write warning
Write-NanerWarning -ErrorCode "NANER-4005" `
                   -AdditionalInfo "Vendor: PowerShell already installed"

# Get error information
$error = Get-NanerError -ErrorCode "NANER-3002"
Write-Host $error.Resolution
```

### Listing All Error Codes

```powershell
# Get all error codes
Import-Module .\src\powershell\ErrorCodes.psm1
Get-AllNanerErrors | Format-Table Code, Category, Severity

# Filter by category
Get-AllNanerErrors | Where-Object { $_.Category -eq "Network" }

# Export to CSV
Get-AllNanerErrors | Export-Csv -Path errors.csv -NoTypeInformation
```

---

## Troubleshooting Guide

### Quick Diagnosis

```powershell
# Run comprehensive tests
.\src\powershell\Test-NanerInstallation.ps1

# Check specific vendor
.\src\powershell\Test-NanerInstallation.ps1 -Profile Unified -VendorId PowerShell

# Validate configuration
Get-Content .\config\naner.json | ConvertFrom-Json
Get-Content .\config\vendors.json | ConvertFrom-Json

# Check lock file
.\src\powershell\Import-VendorLockFile.ps1 -ShowSummary
```

### Common Issue Resolution

| Symptom | Likely Error | Quick Fix |
|---------|--------------|-----------|
| "Config not found" | NANER-2001 | `git checkout HEAD -- config/` |
| "Download failed" | NANER-3002 | Check internet, retry with `-ForceDownload` |
| "Hash mismatch" | NANER-3005 | Delete file, re-download |
| "7-Zip not found" | NANER-4002 | Install SevenZip first |
| "Already installed" | NANER-4005 | Use `-ForceDownload` to reinstall |

---

## See Also

- [ErrorCodes.psm1](../src/powershell/ErrorCodes.psm1) - Error code module
- [Test-NanerInstallation.ps1](../src/powershell/Test-NanerInstallation.ps1) - Validation script
- [TROUBLESHOOTING.md](dev/TROUBLESHOOTING.md) - General troubleshooting
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture
