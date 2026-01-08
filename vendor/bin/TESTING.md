# Testing Naner.exe

This guide explains how to test the Naner C# executable.

## Quick Test

**Windows:**
```cmd
cd C:\Users\BAILEYRD\dev\naner
.\bin\test-naner.bat
```

This will run through a series of tests with pause points so you can see the output.

## Manual Testing

### Test 1: Version Check
```cmd
cd C:\Users\BAILEYRD\dev\naner\bin
.\naner.exe --version
```

**Expected Output:**
```
=====================================
Naner Terminal Launcher v0.1.0-alpha
=====================================

Phase 10.2 - Core Migration (Pure C#)
```

### Test 2: Help Output
```cmd
.\naner.exe --help
```

**Expected Output:**
Command-line parser help with all available options.

### Test 3: Debug Mode (No Launch)
```cmd
.\naner.exe --debug
```

**Expected Output:**
- Header with version
- NANER_ROOT discovery message
- Configuration loading messages
- Environment setup messages
- Terminal launch (will actually launch Windows Terminal)

### Test 4: From Wrong Location

To test error handling, try running from outside the Naner directory:

```cmd
cd C:\Temp
C:\Users\BAILEYRD\dev\naner\bin\naner.exe --version
```

**Current Behavior:**
- Exe exits immediately (error not visible in GUI mode)

**Expected After Phase 10.4:**
- Clear error message about NANER_ROOT not found
- Guidance on where to run from

## Why Does It Close Immediately?

The naner.exe is designed to run **within** the Naner directory structure. When you run it from another location:

1. It starts from the current directory
2. Searches upward for bin/, vendor/, config/ folders
3. Can't find them
4. Throws DirectoryNotFoundException
5. Exits with error code 1

**In a console window**, you'd see the error. **When double-clicked**, it closes too fast to read.

## Proper Usage

**✅ Correct - From within Naner directory:**
```cmd
cd C:\Users\BAILEYRD\dev\naner
.\bin\naner.exe
```

**✅ Correct - From bin folder:**
```cmd
cd C:\Users\BAILEYRD\dev\naner\bin
.\naner.exe
```

**❌ Incorrect - From outside Naner:**
```cmd
cd C:\Temp
C:\Users\BAILEYRD\dev\naner\bin\naner.exe
```

## Debugging Tips

### Enable Debug Output
```cmd
.\naner.exe --debug
```

This shows:
- NANER_ROOT detection
- Configuration loading
- Environment variables
- PATH composition
- Profile selection

### Test Specific Profile
```cmd
.\naner.exe --profile PowerShell --debug
```

### Test Custom Directory
```cmd
.\naner.exe --directory C:\projects --debug
```

### Verify Installation
```cmd
.\naner.exe --version
```

## Common Issues

### Issue: "Exe closes immediately"
**Cause:** Running from outside Naner directory or double-clicking
**Solution:**
1. Open Command Prompt
2. Navigate to Naner directory
3. Run `.\bin\naner.exe --version`

### Issue: "Could not locate Naner root directory"
**Cause:** Directory structure incomplete
**Solution:**
1. Verify bin/, vendor/, config/ folders exist
2. Run from correct location
3. Check NANER_ROOT environment variable

### Issue: "Configuration file not found"
**Cause:** config/naner.json missing
**Solution:** Ensure config folder has naner.json file

## Testing Checklist

Before committing changes:

- [ ] `naner.exe --version` works from bin/
- [ ] `naner.exe --help` shows help
- [ ] `naner.exe --debug` shows diagnostic info
- [ ] `naner.exe --profile PowerShell` launches PowerShell
- [ ] `naner.exe --profile Bash` launches Bash
- [ ] Error messages are clear
- [ ] Exit codes are correct (0 = success, 1 = error)

## Future Testing (Phase 10.4)

Phase 10.4 will add:
- `--diagnose` mode for installation health checks
- Better error messages with actionable solutions
- Help/version that work from any location
- Comprehensive test suite

See [PHASE-10.4-USABILITY-IMPROVEMENTS.md](../docs/PHASE-10.4-USABILITY-IMPROVEMENTS.md) for details.

---

**Last Updated:** 2026-01-08
**Executable Version:** 0.1.0-alpha (Phase 10.2)
