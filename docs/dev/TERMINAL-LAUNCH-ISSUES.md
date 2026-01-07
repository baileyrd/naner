# Windows Terminal Not Appearing - Quick Fix Guide

## Symptom
`Invoke-Naner.ps1` shows "✓ Launched successfully!" but Windows Terminal window doesn't appear.

## Quick Diagnostic

### Step 1: Run Diagnostic Tool
```powershell
.\Test-WindowsTerminalLaunch.ps1
```

This will test various launch methods and identify the issue.

### Step 2: Try Manual Launch
```powershell
# Navigate to terminal directory
cd vendor\terminal

# Try launching directly
.\wt.exe
```

**Did a window appear?**
- ✓ **YES** → Windows Terminal works, issue is with Invoke-Naner.ps1
- ✗ **NO** → Windows Terminal installation issue

## Common Issues & Fixes

### Issue 0: Incomplete Windows Terminal Extraction (MOST COMMON)

**Symptom**: Process starts but exits immediately, or window doesn't appear

**Cause**: Not all Windows Terminal files were copied during extraction

**Diagnostic**:
```powershell
.\Validate-WindowsTerminal.ps1

# Should show all critical files present
```

**Fix**:
```powershell
# Delete incomplete installation
Remove-Item vendor\terminal -Recurse -Force

# Re-run setup (now uses improved extraction)
.\Setup-NanerVendor.ps1 -ForceDownload

# Verify it worked
.\Validate-WindowsTerminal.ps1
```

**What Changed**: The new Setup-NanerVendor.ps1 now copies ALL files from the MSIX package instead of just specific patterns. This ensures nothing is missed.

### Issue 1: Missing DLLs

**Symptom**: Process starts but exits immediately

**Fix**:
```powershell
# Re-run vendor setup
.\Setup-NanerVendor.ps1 -ForceDownload

# Focus on Windows Terminal
# Make sure all DLLs are extracted
```

### Issue 2: Process Running But Hidden

**Check Task Manager**:
1. Press `Ctrl+Shift+Esc`
2. Look for `WindowsTerminal.exe` or `OpenConsole.exe`
3. If found → Process is running but window is hidden/minimized

**Fix**:
```powershell
# Kill any hidden instances
Get-Process WindowsTerminal,OpenConsole -ErrorAction SilentlyContinue | Stop-Process

# Try again
.\Invoke-Naner.ps1
```

### Issue 3: Windows Terminal Needs Initialization

**Symptom**: First launch always fails, subsequent launches work

**Fix**:
```powershell
# Initialize Windows Terminal manually
vendor\terminal\wt.exe

# Wait for it to fully load, then close it

# Now Invoke-Naner should work
.\Invoke-Naner.ps1
```

### Issue 4: Corrupted Windows Terminal Installation

**Fix**:
```powershell
# Remove and reinstall
Remove-Item vendor\terminal -Recurse -Force

# Re-run setup
.\Setup-NanerVendor.ps1 -ForceDownload
```

### Issue 5: Antivirus Blocking

**Symptom**: Process starts but is terminated immediately

**Fix**:
1. Check antivirus logs
2. Add exception for `vendor\terminal\`
3. Try again

### Issue 6: Missing .portable File

**Symptom**: Settings errors or silent failures

**Fix**:
```powershell
# Create .portable file
New-Item vendor\terminal\.portable -ItemType File -Force

# Try again
.\Invoke-Naner.ps1
```

## Alternative Launch Methods

### Method 1: Direct Launch (Bypass Invoke-Naner)
```powershell
# Set PATH first
$env:PATH = "$(pwd)\vendor\msys64\mingw64\bin;$(pwd)\vendor\msys64\usr\bin;$(pwd)\vendor\powershell;$env:PATH"

# Launch Windows Terminal with PowerShell
vendor\terminal\wt.exe -- vendor\powershell\pwsh.exe -NoExit
```

### Method 2: Use System Windows Terminal
If you have Windows Terminal installed system-wide:
```powershell
# Launch with vendored PowerShell
wt.exe -- C:\tools\cmd_line\naner\vendor\powershell\pwsh.exe -NoExit
```

### Method 3: Use Windows Terminal Preview
Download Windows Terminal Preview from Microsoft Store as alternative.

## Detailed Diagnostics

### Check Windows Terminal Version Compatibility
```powershell
# Windows Terminal requires Windows 10 1903 or later
[System.Environment]::OSVersion

# Should show at least Windows 10.0.18362
```

### Check for Missing Dependencies
```powershell
# List all files in terminal directory
Get-ChildItem vendor\terminal

# Should see:
# - wt.exe
# - WindowsTerminal.exe  
# - OpenConsole.exe
# - Many .dll files
# - .portable file
```

### Check Event Viewer
```powershell
# Open Event Viewer
eventvwr.msc

# Navigate to: Windows Logs > Application
# Look for Windows Terminal errors
```

### Enable Debug Mode
```powershell
.\Invoke-Naner.ps1 -DebugMode

# This shows exactly what command is being run
# And what environment variables are set
```

## Manual Testing Commands

### Test 1: Simplest Launch
```powershell
cd vendor\terminal
.\wt.exe
```

### Test 2: Launch with CMD
```powershell
.\wt.exe -- cmd.exe
```

### Test 3: Launch with PowerShell
```powershell
.\wt.exe -- ..\powershell\pwsh.exe -NoExit
```

### Test 4: Launch with Full Path
```powershell
.\wt.exe -d C:\Users\$env:USERNAME -- ..\powershell\pwsh.exe -NoExit
```

## Still Not Working?

### Collect Information
```powershell
# Run full diagnostics
.\Test-WindowsTerminalLaunch.ps1 > wt-diagnostic.txt

# Check installation
.\Test-NanerInstallation.ps1 > install-test.txt

# Include these files when asking for help
```

### Try System-Wide Installation
As a workaround, install Windows Terminal from Microsoft Store:
```powershell
# Via winget
winget install Microsoft.WindowsTerminal

# Then use system WT with Naner
wt.exe -- C:\path\to\naner\vendor\powershell\pwsh.exe -NoExit
```

### Report Issue
If still not working, report with:
1. Output of `Test-WindowsTerminalLaunch.ps1`
2. Output of `Test-NanerInstallation.ps1`
3. Windows version: `[System.Environment]::OSVersion`
4. PowerShell version: `$PSVersionTable`

## Success Indicators

When working properly, you should see:
1. "✓ Launched successfully!" message
2. Windows Terminal window appears within 1-2 seconds
3. PowerShell prompt visible
4. Can run: `git --version`, `ls --version`, `pwsh --version`

## Quick Reference

```powershell
# Diagnostic tool
.\Test-WindowsTerminalLaunch.ps1

# Installation test
.\Test-NanerInstallation.ps1

# Launch with debug
.\Invoke-Naner.ps1 -DebugMode

# Manual test
vendor\terminal\wt.exe -- vendor\powershell\pwsh.exe -NoExit

# Kill stuck processes
Get-Process WindowsTerminal,OpenConsole -EA SilentlyContinue | Stop-Process

# Reinstall Windows Terminal
Remove-Item vendor\terminal -Recurse -Force
.\Setup-NanerVendor.ps1 -ForceDownload
```
