# Naner Vendor Setup - Troubleshooting Quick Reference

## Quick Fixes for Common Issues

### 🔴 "Cannot extract .tar.xz files without 7-Zip"

**What happened**: MSYS2 extraction failed - this shouldn't happen anymore as 7-Zip is bundled.

**Quick Fix**:
```powershell
# Check if 7-Zip was extracted
Test-Path vendor\7zip\7z.exe

# If missing, re-run setup from scratch
Remove-Item vendor -Recurse -Force
.\Setup-NanerVendor.ps1
```

**Why**: 7-Zip is now bundled and extracted first. If you see this error, the 7-Zip extraction may have failed.

---

### 🔴 "tar: Cannot connect to C: resolve failed"

**What happened**: This error shouldn't occur anymore as setup uses bundled 7-Zip.

**Quick Fix**: Re-run setup to ensure 7-Zip is properly extracted:
```powershell
.\Setup-NanerVendor.ps1 -ForceDownload
```

---

### 🔴 "MSYS2 shell script not found"

**What happened**: MSYS2 extraction failed or was incomplete.

**Check**:
```powershell
# See if files were extracted
Get-ChildItem vendor\msys64
```

**Quick Fix**:
```powershell
# Delete incomplete extraction
Remove-Item vendor\msys64 -Recurse -Force

# Re-run setup
.\Setup-NanerVendor.ps1 -ForceDownload
```

**Note**: 7-Zip is bundled, so this should work without any external dependencies.

---

### 🟡 GitHub API Rate Limit

**What happened**: Too many API requests in one hour.

**Message**: `API rate limit exceeded`

**Quick Fix**: 
- Wait an hour, OR
- Script automatically uses fallback URLs - just continue

**Check your rate limit**:
```powershell
Invoke-RestMethod https://api.github.com/rate_limit | 
    Select-Object -ExpandProperty rate
```

---

### 🔴 Download Failed

**What happened**: Network issue or URL changed.

**Quick Fix**:
```powershell
# Delete partial download
Remove-Item vendor\.downloads\* -Force

# Retry
.\Setup-NanerVendor.ps1
```

**Still failing?**:
- Check internet connection
- Try with `-ForceDownload` flag
- Check firewall/proxy settings

---

### 🔴 PowerShell Execution Policy

**What happened**: Can't run scripts.

**Message**: `cannot be loaded because running scripts is disabled`

**Quick Fix**:
```powershell
# Allow scripts for current session (safest)
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# Then run setup
.\Setup-NanerVendor.ps1
```

**Permanent fix** (admin required):
```powershell
# As Administrator
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

---

### 🟡 "Already installed. Re-download?"

**What happened**: Setup detected existing vendor files.

**Options**:
- Press `n` to keep existing installation
- Press `y` to re-download (useful if installation was corrupted)

**Force re-download without prompts**:
```powershell
.\Setup-NanerVendor.ps1 -ForceDownload
```

---

### 🔴 Windows Terminal Extraction Issues

**What happened**: MSIX bundle extraction failed.

**Quick Fix**:
```powershell
# Clear and retry
Remove-Item vendor\terminal -Recurse -Force
Remove-Item vendor\.downloads\*.msixbundle -Force
.\Setup-NanerVendor.ps1 -ForceDownload
```

---

### 🔴 MSYS2 Package Installation Failed

**What happened**: `pacman` couldn't install packages.

**Quick Fix**:
```powershell
# Manual package installation
cd vendor\msys64
.\msys2_shell.cmd -defterm -no-start -c "pacman -Sy"
.\msys2_shell.cmd -defterm -no-start -c "pacman -S --noconfirm git make mingw-w64-x86_64-gcc"
```

---

### 🔴 Disk Space Issues

**What happened**: Not enough space for downloads.

**Space Required**:
- Downloads: ~550MB
- Extracted: ~600MB
- Total: ~1.2GB (temp)
- Final: ~600MB (after cleanup)

**Quick Fix**:
```powershell
# Check available space
Get-PSDrive C | Select-Object Used,Free

# Clean up download cache after successful setup
Remove-Item vendor\.downloads -Recurse -Force
```

---

## Verification Commands

### Check if Setup Succeeded
```powershell
# Run validation tests
.\Test-NanerInstallation.ps1

# Should show:
# [✓] All tests passed!
```

### Check Installed Versions
```powershell
# View manifest
Get-Content vendor\vendor-manifest.json | ConvertFrom-Json

# Or use management tool
.\Manage-NanerVendor.ps1 -ListVersions
```

### Check Individual Components
```powershell
# PowerShell
vendor\powershell\pwsh.exe --version

# Windows Terminal
Test-Path vendor\terminal\wt.exe

# MSYS2
Test-Path vendor\msys64\msys2_shell.cmd

# Git (via MSYS2)
vendor\msys64\usr\bin\git.exe --version
```

---

## Emergency Recovery

### Complete Reset
```powershell
# Delete everything and start over
Remove-Item vendor -Recurse -Force
New-Item -Path vendor -ItemType Directory

# Re-run setup
.\Setup-NanerVendor.ps1
```

### Minimal Installation (Skip MSYS2)
If MSYS2 keeps failing, you can manually install just PowerShell and Terminal:

```powershell
# Download manually:
# PowerShell: https://github.com/PowerShell/PowerShell/releases
# Terminal: https://github.com/microsoft/terminal/releases

# Extract to:
vendor\powershell\
vendor\terminal\

# Then continue with your system Git/tools
```

---

## Getting Help

### Collect Diagnostic Info
```powershell
# Run full diagnostics
.\Test-NanerInstallation.ps1 -Full > diagnostic.txt

# Check system info
$PSVersionTable
Get-ComputerInfo | Select-Object WindowsVersion, OsArchitecture
```

### Check Logs
Setup logs appear in console. To save:
```powershell
.\Setup-NanerVendor.ps1 *> setup-log.txt
```

### Before Asking for Help, Include:
1. Error message (exact text)
2. PowerShell version: `$PSVersionTable.PSVersion`
3. Windows version: `[System.Environment]::OSVersion`
4. Output of: `.\Test-NanerInstallation.ps1`
5. Have you installed 7-Zip?

---

## Prevention Tips

✅ **Do this**:
- Run from PowerShell (not Command Prompt)
- Ensure stable internet connection
- Have at least 2GB free disk space
- Close any running terminal instances

❌ **Don't do this**:
- Don't run from OneDrive/DropBox synced folders
- Don't run multiple setups simultaneously
- Don't interrupt downloads (let retries work)
- Don't modify vendor files during setup

---

## Known Issues

### 1. Antivirus False Positives
Some antivirus software flags MSYS2 tools as suspicious.

**Solution**: Add vendor directory to exclusions temporarily during setup.

### 2. Corporate Proxy
Downloads may fail behind corporate proxies.

**Solution**: Configure proxy for PowerShell:
```powershell
$proxy = "http://proxy.company.com:8080"
[System.Net.WebRequest]::DefaultWebProxy = New-Object System.Net.WebProxy($proxy)
```

### 3. Network Drives
Setup may fail on network drives due to permission issues.

**Solution**: Run setup from local drive (C:, D:, etc.)

---

## Success Checklist

After setup completes, verify:

- [ ] `vendor\powershell\pwsh.exe` exists and runs
- [ ] `vendor\terminal\wt.exe` exists
- [ ] `vendor\msys64\msys2_shell.cmd` exists
- [ ] `vendor\msys64\usr\bin\git.exe` exists and runs
- [ ] `vendor\vendor-manifest.json` exists
- [ ] `.\Test-NanerInstallation.ps1` passes
- [ ] `.\Invoke-Naner.ps1 -DebugMode` launches terminal

If all ✓, you're good to go! 🎉

---

## Still Stuck?

1. Try the complete reset procedure above
2. Check all verification commands
3. Ensure 7-Zip is installed
4. Review IMPLEMENTATION-GUIDE.md for detailed steps
5. Check DYNAMIC-URLS.md for download issues

Most issues are resolved by re-running setup with `-ForceDownload`. All dependencies including 7-Zip are bundled.
