# Naner Launcher Setup Instructions

## Current Status
✓ Script now parses correctly (no more syntax errors)
✓ ASCII-only characters (PowerShell 5.1 compatible)
✓ Enhanced directory search (finds config up to 5 levels up)

## Directory Structure

Your current structure should look like this:

```
C:\Users\BAILEYRD\dev\naner\naner_launcher\
├── config\
│   └── user-settings.json          <-- Place the provided file here
├── src\
│   └── powershell\
│       └── Launch-Naner.ps1        <-- This is your script
└── bin\                             <-- Optional: for additional tools
```

## Setup Steps

### 1. Create the config directory (if it doesn't exist)
```powershell
cd C:\Users\BAILEYRD\dev\naner\naner_launcher
mkdir config -ErrorAction SilentlyContinue
```

### 2. Place user-settings.json in the config directory
Copy the provided `user-settings.json` file to:
```
C:\Users\BAILEYRD\dev\naner\naner_launcher\config\user-settings.json
```

### 3. Test the script
```powershell
cd C:\Users\BAILEYRD\dev\naner\naner_launcher\src\powershell
.\Launch-Naner.ps1 -Verbose
```

The `-Verbose` flag will show you:
- Where it found the Naner root
- What configuration was loaded
- What Windows Terminal command is being executed

### 4. (Optional) Set NANER_ROOT environment variable
If you want to set it permanently:

```powershell
# Set for current user
[System.Environment]::SetEnvironmentVariable('NANER_ROOT', 'C:\Users\BAILEYRD\dev\naner\naner_launcher', 'User')

# Restart PowerShell for the change to take effect
```

## Troubleshooting

### Error: "Could not determine Naner root directory"
- Make sure the `config` directory exists at `C:\Users\BAILEYRD\dev\naner\naner_launcher\config`
- Or set the NANER_ROOT environment variable
- Run with `-Verbose` to see what paths are being checked

### Error: "Windows Terminal is not installed"
Windows Terminal might be installed in a non-standard location. Find and configure the path:

**Step 1: Find your Windows Terminal executable**
```powershell
# Option 1: Use Get-Command
Get-Command wt.exe | Select-Object -ExpandProperty Source

# Option 2: Search in common locations
Get-ChildItem -Path "$env:LOCALAPPDATA\Microsoft\WindowsApps" -Filter "wt.exe"
```

**Step 2: Add the path to your user-settings.json (with environment variables!)**
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": null,
  "WindowsTerminalPath": "%LOCALAPPDATA%\\Microsoft\\WindowsApps\\wt.exe"
}
```

**Common Windows Terminal Locations (with environment variables):**
- Microsoft Store: `%LOCALAPPDATA%\\Microsoft\\WindowsApps\\wt.exe`
- Preview Version: `%ProgramFiles%\\WindowsApps\\Microsoft.WindowsTerminalPreview_*\\wt.exe`
- Regular Version: `%ProgramFiles%\\WindowsApps\\Microsoft.WindowsTerminal_*\\wt.exe`

**Using environment variables makes your config portable!** Same config works for:
- BAILEYRD: `C:\Users\BAILEYRD\AppData\Local\Microsoft\WindowsApps\wt.exe`
- JohnDoe: `C:\Users\JohnDoe\AppData\Local\Microsoft\WindowsApps\wt.exe`

See `user-settings-examples.md` for more detailed configuration examples.

### Testing the script works
```powershell
# Test basic launch (should open Windows Terminal with PowerShell)
.\Launch-Naner.ps1

# Test with specific profile
.\Launch-Naner.ps1 -Profile "Command Prompt"

# Test with custom profile (if configured)
.\Launch-Naner.ps1 -Profile "PowerShell7"

# Test with specific directory
.\Launch-Naner.ps1 -StartDir "C:\Projects"

# Test registration (adds context menu item)
.\Launch-Naner.ps1 -Register

# Test unregistration
.\Launch-Naner.ps1 -Unregister
```

## Helper Scripts

### Find-WindowsTerminal.ps1
Automatically finds your Windows Terminal installation:
```powershell
.\Find-WindowsTerminal.ps1
```

### Find-PowerShellInstallations.ps1
Finds all PowerShell installations and generates config for you:
```powershell
.\Find-PowerShellInstallations.ps1
```

This will show you all PowerShell versions on your system and output the exact JSON configuration to add to your `user-settings.json`!

### Test-EnvironmentVariables.ps1
Test how environment variables expand:
```powershell
.\Test-EnvironmentVariables.ps1
```

## Customizing user-settings.json

Edit `config/user-settings.json` to customize. **Environment variables are fully supported!**

### Basic Configuration
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": "%USERPROFILE%\\Projects",
  "WindowsTerminalPath": "%LOCALAPPDATA%\\Microsoft\\WindowsApps\\wt.exe"
}
```

### With Custom PowerShell Installation
```json
{
  "DefaultProfile": "PowerShell7",
  "StartupDir": "%USERPROFILE%\\Projects",
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

**Supported Environment Variable Formats:**
- Windows style: `%USERPROFILE%`, `%LOCALAPPDATA%`, `%ProgramFiles%`
- PowerShell style: `$env:USERPROFILE`, `$env:LOCALAPPDATA`, `$env:ProgramFiles`
- Both formats work and can be mixed!

**Available default profiles** (depends on your Windows Terminal settings):
- "PowerShell"
- "Command Prompt"  
- "Git Bash"
- "Ubuntu" (if WSL is installed)

**Custom profiles** let you specify:
- Exact shell executable path (for custom PowerShell, Python, etc.)
- Command-line arguments
- Window title

**Common Environment Variables:**
- `%USERPROFILE%` → Your user folder (`C:\Users\YourName`)
- `%LOCALAPPDATA%` → Local AppData (`C:\Users\YourName\AppData\Local`)
- `%ProgramFiles%` → Program Files folder
- `%USERNAME%` → Your username

**Note:** Use double backslashes (`\\`) in JSON paths. Environment variables make your config portable across different machines!

See `user-settings-examples.md` for more detailed examples.

## Next Steps

Once the PowerShell prototype is working:
1. Test all functionality thoroughly
2. Decide if you want to port to C# for better performance
3. Add additional features (custom profiles, aliases, etc.)