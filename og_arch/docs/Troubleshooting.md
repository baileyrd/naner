# Troubleshooting Custom Profiles

If your custom profiles aren't working, follow these diagnostic steps:

## Step 1: Run the Diagnostic Script

```powershell
.\Test-NanerConfig.ps1 -Profile "YourProfileName"
```

This will show you:
- If the config file is being found and loaded
- If your custom profiles are being parsed correctly
- If the shell paths exist
- What the exact configuration looks like

## Step 2: Run with Verbose Output

```powershell
.\Launch-Naner.ps1 -Profile "YourProfileName" -Verbose
```

This will show you:
- What profile is being selected
- Whether it's recognized as a custom profile
- The exact command being executed
- Any errors that occur

## Common Issues and Solutions

### Issue 1: "Profile not found in custom profiles"

**Symptom:** Verbose output shows "Profile 'X' not found in custom profiles"

**Cause:** The profile name doesn't match exactly (case-sensitive!)

**Solution:**
```powershell
# Run diagnostic to see exact profile names
.\Test-NanerConfig.ps1

# Make sure profile name matches exactly, including case
.\Launch-Naner.ps1 -Profile "PowerShell7"  # ✓ Correct
.\Launch-Naner.ps1 -Profile "powershell7"  # ✗ Wrong case
```

### Issue 2: "Shell NOT FOUND"

**Symptom:** Diagnostic shows "Shell NOT FOUND ✗"

**Cause:** The ShellPath doesn't exist or environment variable didn't expand

**Solutions:**

**A. Check the path exists:**
```powershell
Test-Path "C:\Program Files\PowerShell\7\pwsh.exe"
```

**B. Try with full path first (no env vars):**
```json
{
  "CustomProfiles": {
    "PowerShell7": {
      "ShellPath": "C:\\Program Files\\PowerShell\\7\\pwsh.exe",
      "Arguments": "-NoLogo",
      "Title": "PowerShell 7"
    }
  }
}
```

**C. Check environment variable expansion:**
```powershell
.\Test-EnvironmentVariables.ps1
```

### Issue 3: Configuration file not loading

**Symptom:** Diagnostic shows "Configuration file not found!"

**Cause:** The config/user-settings.json file is missing or in wrong location

**Solution:**
```powershell
# Check if config directory exists
Test-Path "C:\Users\BAILEYRD\dev\naner\naner_launcher\config"

# Check if user-settings.json exists
Test-Path "C:\Users\BAILEYRD\dev\naner\naner_launcher\config\user-settings.json"

# Create directory if needed
mkdir "C:\Users\BAILEYRD\dev\naner\naner_launcher\config" -Force

# Copy example file
cp user-settings-with-custom-profiles.json "C:\Users\BAILEYRD\dev\naner\naner_launcher\config\user-settings.json"
```

### Issue 4: JSON syntax error

**Symptom:** Script shows "Failed to load user settings"

**Cause:** Invalid JSON syntax in user-settings.json

**Common JSON mistakes:**
1. Missing comma between items
2. Extra comma after last item
3. Single backslash instead of double (`\` vs `\\`)
4. Missing quotes around strings
5. Comments (JSON doesn't support comments!)

**Solution:**
```powershell
# Validate your JSON
Get-Content config\user-settings.json -Raw | ConvertFrom-Json

# If error, check:
# - All strings in double quotes
# - Double backslashes in paths: "C:\\Path\\To\\File"
# - Commas between items (but not after last item)
# - No comments (remove any // or /* */ comments)
```

**Valid JSON example:**
```json
{
  "DefaultProfile": "PowerShell7",
  "StartupDir": null,
  "WindowsTerminalPath": "%LOCALAPPDATA%\\Microsoft\\WindowsApps\\wt.exe",
  "CustomProfiles": {
    "PowerShell7": {
      "ShellPath": "%ProgramFiles%\\PowerShell\\7\\pwsh.exe",
      "Arguments": "-NoLogo",
      "Title": "PowerShell 7"
    }
  }
}
```

### Issue 5: Windows Terminal doesn't launch custom shell

**Symptom:** Windows Terminal opens but uses wrong shell

**Cause:** Windows Terminal command syntax issue (fixed in latest version)

**Solution:** Make sure you're using the latest Launch-Naner.ps1 which includes the `--` separator

The command should look like:
```
wt.exe --title "PowerShell 7" -d "C:\Projects" -- "C:\Program Files\PowerShell\7\pwsh.exe" -NoLogo
```

### Issue 6: Profile names with spaces

**Symptom:** Profile with spaces in name doesn't work

**Solution:** Use quotes when calling:
```powershell
.\Launch-Naner.ps1 -Profile "My Custom PowerShell"
```

In JSON, no special handling needed:
```json
{
  "CustomProfiles": {
    "My Custom PowerShell": {
      "ShellPath": "C:\\Path\\To\\pwsh.exe",
      "Arguments": "-NoLogo",
      "Title": "My Custom PowerShell"
    }
  }
}
```

## Complete Diagnostic Workflow

1. **Verify config file exists and is valid JSON:**
   ```powershell
   Get-Content config\user-settings.json -Raw | ConvertFrom-Json
   ```

2. **Run diagnostic script:**
   ```powershell
   .\Test-NanerConfig.ps1 -Profile "PowerShell7"
   ```

3. **Check if shell path exists:**
   ```powershell
   Test-Path "C:\Program Files\PowerShell\7\pwsh.exe"
   ```

4. **Test launch with verbose:**
   ```powershell
   .\Launch-Naner.ps1 -Profile "PowerShell7" -Verbose
   ```

5. **If still not working, try minimal config:**
   ```json
   {
     "DefaultProfile": "PowerShell",
     "CustomProfiles": {
       "Test": {
         "ShellPath": "C:\\Windows\\System32\\cmd.exe",
         "Arguments": "",
         "Title": "Test"
       }
     }
   }
   ```
   
   Then test:
   ```powershell
   .\Launch-Naner.ps1 -Profile "Test" -Verbose
   ```

## Getting Help

If you're still having issues, run these commands and share the output:

```powershell
# Diagnostic output
.\Test-NanerConfig.ps1 -Profile "YourProfileName"

# Config file contents
Get-Content config\user-settings.json

# Verbose launch attempt
.\Launch-Naner.ps1 -Profile "YourProfileName" -Verbose
```

## Quick Test

To quickly test if custom profiles work at all:

1. Create this minimal config:
   ```json
   {
     "DefaultProfile": "PowerShell",
     "CustomProfiles": {
       "CMD": {
         "ShellPath": "C:\\Windows\\System32\\cmd.exe",
         "Arguments": "",
         "Title": "Command Prompt"
       }
     }
   }
   ```

2. Test it:
   ```powershell
   .\Launch-Naner.ps1 -Profile "CMD" -Verbose
   ```

If this works, the issue is with your specific profile configuration, not the feature itself.