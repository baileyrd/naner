# user-settings.json Configuration Examples

**TIP:** Using environment variables makes your configuration portable! The same config file will work on:
- Different user accounts
- Different computers
- Team environments with shared configurations

Both Windows-style (`%VAR%`) and PowerShell-style (`$env:VAR`) variables are supported!

---

## Basic Configuration
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": null,
  "WindowsTerminalPath": null
}
```

## Using Environment Variables

### Windows-Style Variables (%VAR%)
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": "%USERPROFILE%\\Documents\\Projects",
  "WindowsTerminalPath": "%LOCALAPPDATA%\\Microsoft\\WindowsApps\\wt.exe"
}
```

### PowerShell-Style Variables ($env:VAR)
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": "$env:USERPROFILE\\Documents\\Projects",
  "WindowsTerminalPath": "$env:LOCALAPPDATA\\Microsoft\\WindowsApps\\wt.exe"
}
```

### Mixed Style (both work!)
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": "%USERPROFILE%\\Projects",
  "WindowsTerminalPath": "$env:LOCALAPPDATA\\Microsoft\\WindowsApps\\wt.exe"
}
```

## Common Environment Variables

### User-Specific Paths
- `%USERPROFILE%` or `$env:USERPROFILE` → `C:\Users\YourUsername`
- `%LOCALAPPDATA%` or `$env:LOCALAPPDATA` → `C:\Users\YourUsername\AppData\Local`
- `%APPDATA%` or `$env:APPDATA` → `C:\Users\YourUsername\AppData\Roaming`
- `%HOMEDRIVE%` or `$env:HOMEDRIVE` → `C:`
- `%HOMEPATH%` or `$env:HOMEPATH` → `\Users\YourUsername`
- `%USERNAME%` or `$env:USERNAME` → Your username

### System Paths
- `%ProgramFiles%` or `$env:ProgramFiles` → `C:\Program Files`
- `%ProgramFiles(x86)%` or `$env:ProgramFiles(x86)` → `C:\Program Files (x86)`
- `%SystemRoot%` or `$env:SystemRoot` → `C:\Windows`
- `%TEMP%` or `$env:TEMP` → Temp directory

## Practical Examples

### Example 1: Portable Configuration (Recommended)
Works on any Windows machine regardless of username:
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": "%USERPROFILE%\\Documents",
  "WindowsTerminalPath": "%LOCALAPPDATA%\\Microsoft\\WindowsApps\\wt.exe"
}
```

### Example 2: Development Projects
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": "%USERPROFILE%\\dev",
  "WindowsTerminalPath": null
}
```

### Example 3: Network Drive Starting Point
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": "\\\\server\\share\\%USERNAME%\\projects",
  "WindowsTerminalPath": null
}
```

### Example 4: Custom Installation with Variables
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": "%HOMEDRIVE%\\Projects",
  "WindowsTerminalPath": "%ProgramFiles%\\WindowsApps\\Microsoft.WindowsTerminal_1.18.2822.0_x64__8wekyb3d8bbwe\\wt.exe"
}
```

## Common Windows Terminal Locations

### Standard Installation (Microsoft Store)
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": null,
  "WindowsTerminalPath": "C:\\Users\\YourUsername\\AppData\\Local\\Microsoft\\WindowsApps\\wt.exe"
}
```

### Windows Terminal Preview
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": null,
  "WindowsTerminalPath": "C:\\Program Files\\WindowsApps\\Microsoft.WindowsTerminalPreview_1.18.2822.0_x64__8wekyb3d8bbwe\\wt.exe"
}
```

### Custom Installation
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": "C:\\Projects",
  "WindowsTerminalPath": "C:\\CustomApps\\WindowsTerminal\\wt.exe"
}
```

## How to Find Your Windows Terminal Path

### Method 1: PowerShell Command
```powershell
# Find wt.exe location
Get-Command wt.exe | Select-Object -ExpandProperty Source

# Or search for it
Get-ChildItem -Path "$env:LOCALAPPDATA\Microsoft\WindowsApps" -Filter "wt.exe" -ErrorAction SilentlyContinue
Get-ChildItem -Path "$env:ProgramFiles\WindowsApps" -Filter "wt.exe" -Recurse -ErrorAction SilentlyContinue
```

### Method 2: Windows Terminal Settings
1. Open Windows Terminal
2. Click the dropdown arrow in the title bar
3. Select "Settings"
4. Scroll to the bottom and look for installation information

### Method 3: Task Manager
1. Open Windows Terminal
2. Open Task Manager (Ctrl+Shift+Esc)
3. Find "Windows Terminal" process
4. Right-click → "Open file location"

## Configuration Options Explained

### DefaultProfile
The Windows Terminal profile to launch by default.
- Must match a profile name in your Windows Terminal settings
- Common values: "PowerShell", "Command Prompt", "Git Bash", "Ubuntu"

### StartupDir
The directory to start in when launching Windows Terminal.
- Use null to use Windows Terminal's default
- Use forward slashes or double backslashes: "C:\\Projects" or "C:/Projects"
- Example: "C:\\Users\\YourUsername\\Documents"

### WindowsTerminalPath
Full path to wt.exe executable.
- Use null to let Naner auto-detect the location
- Use double backslashes in JSON: "C:\\Path\\To\\wt.exe"
- Only needed if Windows Terminal is in a non-standard location
- Supports both regular and Preview versions

## Complete Example
```json
{
  "DefaultProfile": "PowerShell",
  "StartupDir": "C:\\Users\\BAILEYRD\\Projects",
  "WindowsTerminalPath": "C:\\Users\\BAILEYRD\\AppData\\Local\\Microsoft\\WindowsApps\\wt.exe"
}
```

## Troubleshooting

### Error: "Windows Terminal is not installed"
1. Check if Windows Terminal is actually installed
2. Try to run `wt.exe` directly from PowerShell
3. Find the path using one of the methods above
4. Set "WindowsTerminalPath" in your user-settings.json

### Path with Spaces
Always use double backslashes in JSON paths:
```json
"WindowsTerminalPath": "C:\\Program Files\\WindowsApps\\Microsoft.WindowsTerminal_1.18.2822.0_x64__8wekyb3d8bbwe\\wt.exe"
```

### Preview vs Regular Version
If you have both installed, you can choose which one to use:
- Preview: Usually has latest features but may be less stable
- Regular: More stable, updated less frequently

Just set the "WindowsTerminalPath" to point to your preferred version.