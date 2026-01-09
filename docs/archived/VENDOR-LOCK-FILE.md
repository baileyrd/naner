# Vendor Lock File System

The Naner vendor lock file system provides reproducible installations by capturing exact versions of all installed dependencies.

## Overview

A vendor lock file (`vendors.lock.json`) records:
- Exact version numbers of all installed vendors
- Download URLs for each vendor
- File names and sizes
- SHA256 hashes (optional but recommended)
- Installation timestamps
- Platform information

## Why Use Lock Files?

### Problem
Without a lock file, `Setup-NanerVendor.ps1` always downloads the **latest** versions of dependencies. This can lead to:
- Version drift between team members
- Broken builds when new versions introduce breaking changes
- Difficult troubleshooting ("it works on my machine")
- Non-reproducible environments

### Solution
Lock files ensure **everyone gets the same versions**, creating:
- Reproducible builds
- Consistent development environments
- Easier troubleshooting
- Controlled dependency updates

## Creating a Lock File

### After Initial Installation

```powershell
# Install vendors normally
.\src\powershell\Setup-NanerVendor.ps1

# Export current versions to lock file
.\src\powershell\Export-VendorLockFile.ps1
```

This creates `config\vendors.lock.json` with all installed versions.

### With Hash Verification (Recommended for Production)

```powershell
# Export with SHA256 hashes
.\src\powershell\Export-VendorLockFile.ps1 -IncludeHashes
```

**Note**: Hash calculation takes longer but provides security verification.

### Custom Output Location

```powershell
# Export to specific location
.\src\powershell\Export-VendorLockFile.ps1 -OutputPath ".\my-team.lock.json"
```

## Using a Lock File

### Install from Lock File

```powershell
# Install exact versions from lock file (FUTURE FEATURE)
.\src\powershell\Setup-NanerVendor.ps1 -UseLockFile
```

**Current Status**: Lock file creation implemented. Installation from lock file is a future enhancement.

### View Lock File Contents

```powershell
# Display lock file summary
.\src\powershell\Import-VendorLockFile.ps1 -ShowSummary
```

### Validate Lock File Hashes

```powershell
# Verify downloaded files match lock file hashes
.\src\powershell\Import-VendorLockFile.ps1 -ValidateHashes
```

## Lock File Format

### Structure

```json
{
  "version": "1.0.0",
  "generated": "2026-01-07T12:00:00Z",
  "nanerVersion": "1.0.0",
  "platform": {
    "os": "Microsoft Windows 10.0.22631",
    "psVersion": "7.4.6",
    "architecture": "X64"
  },
  "vendors": {
    "PowerShell": {
      "name": "PowerShell",
      "version": "7.4.6",
      "url": "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/PowerShell-7.4.6-win-x64.zip",
      "fileName": "PowerShell-7.4.6-win-x64.zip",
      "size": "~100",
      "installed": true,
      "installedDate": "2026-01-07T10:32:00Z",
      "extractDir": "powershell",
      "sha256": "abc123...def456"
    }
  }
}
```

### Fields

| Field | Description |
|-------|-------------|
| `version` | Lock file format version |
| `generated` | ISO 8601 timestamp of lock file creation |
| `nanerVersion` | Naner version used to create lock file |
| `platform.os` | Operating system |
| `platform.psVersion` | PowerShell version |
| `platform.architecture` | CPU architecture (X64, ARM64, etc.) |
| `vendors[id].name` | Vendor display name |
| `vendors[id].version` | Exact version number |
| `vendors[id].url` | Download URL |
| `vendors[id].fileName` | Archive file name |
| `vendors[id].size` | Approximate size in MB |
| `vendors[id].installed` | Installation status |
| `vendors[id].installedDate` | Installation timestamp |
| `vendors[id].extractDir` | Extract directory name |
| `vendors[id].sha256` | SHA256 hash (optional) |

## Workflows

### Team Development

1. **Team Lead**: Install and test vendors
   ```powershell
   .\src\powershell\Setup-NanerVendor.ps1
   .\src\powershell\Export-VendorLockFile.ps1 -IncludeHashes
   git add config/vendors.lock.json
   git commit -m "Lock vendor versions"
   git push
   ```

2. **Team Members**: Clone and install from lock file
   ```powershell
   git clone <repo>
   cd naner
   .\src\powershell\Setup-NanerVendor.ps1 -UseLockFile  # FUTURE
   ```

### Version Updates

```powershell
# Update a specific vendor
.\src\powershell\Setup-NanerVendor.ps1 -VendorId PowerShell -ForceDownload

# Update lock file
.\src\powershell\Export-VendorLockFile.ps1 -IncludeHashes

# Review changes
git diff config/vendors.lock.json

# Commit if satisfied
git add config/vendors.lock.json
git commit -m "Update PowerShell to v7.5.0"
```

### CI/CD Integration

```yaml
# .github/workflows/build.yml
- name: Validate vendor lock file
  shell: pwsh
  run: |
    .\src\powershell\Import-VendorLockFile.ps1 -ValidateHashes

- name: Install vendors from lock file
  shell: pwsh
  run: |
    .\src\powershell\Setup-NanerVendor.ps1 -UseLockFile  # FUTURE
```

## Best Practices

### ✅ DO

- **Commit lock files to version control**
  ```bash
  git add config/vendors.lock.json
  ```

- **Include SHA256 hashes for production environments**
  ```powershell
  Export-VendorLockFile.ps1 -IncludeHashes
  ```

- **Update lock file after intentional vendor updates**
  ```powershell
  Setup-NanerVendor.ps1 -VendorId NodeJS -ForceDownload
  Export-VendorLockFile.ps1 -IncludeHashes
  ```

- **Review lock file changes in pull requests**
  - Check version numbers
  - Verify URLs
  - Test new versions

### ❌ DON'T

- **Don't manually edit lock files**
  - Use `Export-VendorLockFile.ps1` instead
  - Manual edits can break validation

- **Don't commit lock files without testing**
  - Install and test first
  - Verify all tools work

- **Don't mix manual and lock file installations**
  - Choose one approach per environment

## Security Considerations

### Hash Verification

SHA256 hashes protect against:
- **Man-in-the-middle attacks**: Verify downloads aren't tampered
- **Compromised mirrors**: Detect corrupted files
- **Version mismatch**: Ensure correct file downloaded

### Recommendations

1. **Always use `-IncludeHashes` for production lock files**
2. **Validate hashes before installation in CI/CD**
3. **Store lock files in secure, version-controlled repositories**
4. **Review lock file changes during code review**

## Troubleshooting

### Lock File Not Found

**Error**: `Lock file not found: config\vendors.lock.json`

**Solution**: Create lock file first
```powershell
.\src\powershell\Export-VendorLockFile.ps1
```

### Hash Validation Failed

**Error**: `Hash mismatch! Expected: abc... Actual: def...`

**Causes**:
- File corrupted during download
- File modified after download
- Wrong version downloaded

**Solutions**:
1. Delete and re-download:
   ```powershell
   Remove-Item .\downloads\<file>
   .\src\powershell\Setup-NanerVendor.ps1 -VendorId <vendor> -ForceDownload
   ```

2. Regenerate lock file if intentionally updated:
   ```powershell
   .\src\powershell\Export-VendorLockFile.ps1 -IncludeHashes
   ```

### Vendor Not in Lock File

**Error**: Vendor installed but not in lock file

**Cause**: Lock file created before vendor installation

**Solution**: Regenerate lock file
```powershell
.\src\powershell\Export-VendorLockFile.ps1
```

### Platform Mismatch

**Warning**: Lock file created on different platform

**Note**: Lock files include platform information but are generally cross-platform compatible for Naner. Review vendor versions for platform-specific differences.

## Future Enhancements

### Planned Features

- [ ] **Install from lock file** (`-UseLockFile` parameter)
  - Download exact versions specified in lock file
  - Skip version resolution (use locked versions)
  - Verify hashes during installation

- [ ] **Lock file diff tool**
  ```powershell
  Compare-VendorLockFiles -Old .\old.lock.json -New .\new.lock.json
  ```

- [ ] **Selective vendor updates**
  ```powershell
  Update-VendorLockFile -VendorId NodeJS,Rust
  ```

- [ ] **Lock file merge conflict resolution**
  - Automatic conflict detection
  - Interactive merge tool

- [ ] **Vulnerability scanning**
  - Check locked versions against CVE databases
  - Warn about outdated/vulnerable versions

## Examples

### Example 1: New Project Setup

```powershell
# Clone repository
git clone https://github.com/yourusername/naner.git
cd naner

# Install vendors
.\src\powershell\Setup-NanerVendor.ps1

# Create lock file
.\src\powershell\Export-VendorLockFile.ps1 -IncludeHashes

# Commit lock file
git add config/vendors.lock.json
git commit -m "Initial vendor lock file"
git push
```

### Example 2: Update Single Vendor

```powershell
# Update PowerShell
.\src\powershell\Setup-NanerVendor.ps1 -VendorId PowerShell -ForceDownload

# Update lock file
.\src\powershell\Export-VendorLockFile.ps1 -IncludeHashes

# Review changes
git diff config/vendors.lock.json

# Commit
git add config/vendors.lock.json
git commit -m "Update PowerShell to v7.5.0"
```

### Example 3: Verify Team Environment

```powershell
# Import and display lock file
.\src\powershell\Import-VendorLockFile.ps1 -ShowSummary

# Validate installed files
.\src\powershell\Import-VendorLockFile.ps1 -ValidateHashes
```

## See Also

- [vendors.json](../config/vendors.json) - Vendor configuration
- [vendors-schema.json](../config/vendors-schema.json) - JSON schema
- [vendors.lock.example.json](../config/vendors.lock.example.json) - Example lock file
- [Setup-NanerVendor.ps1](../src/powershell/Setup-NanerVendor.ps1) - Vendor installation
- [Export-VendorLockFile.ps1](../src/powershell/Export-VendorLockFile.ps1) - Create lock file
- [Import-VendorLockFile.ps1](../src/powershell/Import-VendorLockFile.ps1) - Read lock file
