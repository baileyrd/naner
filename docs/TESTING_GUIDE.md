# Quick Start Testing Guide

Get the PowerShell prototype running in minutes!

## Prerequisites Check

### 1. Check Windows Terminal Installation

Open PowerShell and run:
```powershell
Get-Command wt.exe
```

**If not found**: Install Windows Terminal from [Microsoft Store](https://aka.ms/terminal)

### 2. Check PowerShell Version

```powershell
$PSVersionTable.PSVersion
```

Should be 5.1 or higher (built into Windows 10/11)

## Installation

### Option 1: Quick Test (No Installation)

1. Download/extract the project
2. Open PowerShell in the project directory
3. Run:

```powershell
.\src\powershell\Launch-Cmder.ps1
```

### Option 2: Use Batch Wrapper

```batch
Cmder.bat
```

### Option 3: Bypass Execution Policy (If Needed)

If you get execution policy errors:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\src\powershell\Launch-Cmder.ps1
```

## First Test

### Test 1: Basic Launch

```powershell
.\Cmder.bat
```

**Expected**: Windows Terminal opens in current directory

### Test 2: Custom Directory

```powershell
.\Cmder.bat -StartDir "C:\Windows"
```

**Expected**: Windows Terminal opens in C:\Windows

### Test 3: Verbose Mode

```powershell
.\Cmder.bat -Verbose
```

**Expected**: See detailed logging output showing:
- Environment variables being set
- Windows Terminal detection
- Launch arguments

### Test 4: Profile Creation

If this is your first run:
- Script will ask to create Cmder profile
- Answer 'Y' to create it
- Check Windows Terminal settings (`Ctrl+,`) to see the new profile

## Verification Steps

### Check Environment Variables

After launching, in the new Windows Terminal window:

```batch
echo %CMDER_ROOT%
echo %CMDER_USER_CONFIG%
echo %CMDER_USER_BIN%
```

**Expected**: Should show your project paths

### Check Directory Structure

```powershell
Get-ChildItem $env:CMDER_ROOT
```

**Expected**: Should see config and bin directories created

### Check Windows Terminal Settings

1. Open Windows Terminal
2. Press `Ctrl+,` (Settings)
3. Look for "Cmder" in profiles list

## Testing Shell Integration

### Register Context Menu (Current User)

```batch
.\Cmder.bat -Register USER
```

**Verify**:
1. Open Windows Explorer
2. Right-click in a folder background
3. Look for "Cmder Here" option

### Test Context Menu

1. Navigate to a test folder
2. Right-click in empty space
3. Click "Cmder Here"
4. **Expected**: Windows Terminal opens in that folder

### Unregister (Cleanup)

```batch
.\Cmder.bat -Unregister USER
```

## Advanced Testing

### Test Different Profiles

```batch
.\Cmder.bat -Profile "Windows PowerShell"
.\Cmder.bat -Profile "Command Prompt"
.\Cmder.bat -Profile "Ubuntu"  # If you have WSL
```

### Test Window Modes

```batch
# Open in new window
.\Cmder.bat -New

# Open in existing window (if one is open)
.\Cmder.bat -Single
```

### Test Compatibility Mode

```batch
# Old Cmder syntax
.\Cmder.bat /start "C:\Projects"
.\Cmder.bat /task "PowerShell"
```

## Troubleshooting Tests

### Problem: "Execution Policy" Error

**Fix**:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**Or bypass temporarily**:
```powershell
powershell.exe -ExecutionPolicy Bypass -File .\src\powershell\Launch-Cmder.ps1
```

### Problem: "Windows Terminal not found"

**Check installation**:
```powershell
Get-AppxPackage -Name Microsoft.WindowsTerminal
```

**Install if missing**:
- Microsoft Store: https://aka.ms/terminal
- Or use `winget install Microsoft.WindowsTerminal`

### Problem: Context Menu Doesn't Appear

**Refresh Explorer**:
- Press F5 in Explorer
- Or restart Explorer process:
  ```powershell
  Stop-Process -Name explorer -Force
  ```

### Problem: Profile Not Found

**Manually create profile**:
1. Open Windows Terminal Settings (`Ctrl+,`)
2. Click "Add a new profile" → "New empty profile"
3. Name it "Cmder"
4. Set command line to: `cmd.exe /k %CMDER_ROOT%\vendor\init.bat`
5. Save

## Performance Testing

### Measure Startup Time

```powershell
Measure-Command { .\src\powershell\Launch-Cmder.ps1 }
```

**Expected**: 200-500ms

### Compare with C# Version (After Port)

Once C# version is ready:

```powershell
# PowerShell
Measure-Command { .\src\powershell\Launch-Cmder.ps1 }

# C#
Measure-Command { .\src\csharp\bin\Release\net6.0-windows\win-x64\Cmder.exe }
```

**Expected**: C# should be 3-5x faster

## Feature Validation Checklist

Use this to validate all features before porting to C#:

- [ ] Basic launch works
- [ ] Custom start directory works
- [ ] Profile selection works
- [ ] Single window mode works
- [ ] New window mode works
- [ ] Environment variables set correctly
- [ ] Config directory created
- [ ] Bin directory created
- [ ] Windows Terminal detection works
- [ ] Profile auto-creation works
- [ ] Shell integration (register) works
- [ ] Shell integration (unregister) works
- [ ] Context menu appears
- [ ] Context menu launches correctly
- [ ] Verbose logging works
- [ ] Error messages are clear
- [ ] Handles missing Windows Terminal gracefully
- [ ] Handles missing profile gracefully
- [ ] Compatible with Cmder syntax

## Collecting Feedback

While testing, note:

1. **Performance Issues**
   - Slow startup?
   - Lag anywhere?

2. **Usability Issues**
   - Confusing messages?
   - Missing features?

3. **Bugs**
   - Crashes?
   - Wrong behavior?

4. **Feature Requests**
   - What would make it better?
   - What's missing?

## Ready for C# Port?

Port to C# when:
- ✅ All checklist items pass
- ✅ No critical bugs
- ✅ Performance is acceptable
- ✅ User feedback incorporated
- ✅ Features are stable

## Next Steps

1. **Complete testing** using this guide
2. **Document any issues** in a file
3. **Fix bugs** in PowerShell version
4. **When stable**, begin C# port using the Migration Guide
5. **Keep PowerShell version** for comparison and legacy support

---

**Happy Testing!** 🚀

The PowerShell prototype is your proving ground. Test thoroughly before committing to C#!
