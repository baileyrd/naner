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
Install Windows Terminal from the Microsoft Store:
https://aka.ms/terminal

### Testing the script works
```powershell
# Test basic launch (should open Windows Terminal with PowerShell)
.\Launch-Naner.ps1

# Test with specific profile
.\Launch-Naner.ps1 -Profile "Command Prompt"

# Test with specific directory
.\Launch-Naner.ps1 -StartDir "C:\Projects"

# Test registration (adds context menu item)
.\Launch-Naner.ps1 -Register

# Test unregistration
.\Launch-Naner.ps1 -Unregister
```

## Customizing user-settings.json

Edit `config/user-settings.json` to customize:

```json
{
  "DefaultProfile": "PowerShell",     // Default profile to launch
  "StartupDir": "C:\\Projects"        // Default starting directory (optional)
}
```

Available profiles (depends on your Windows Terminal settings):
- "PowerShell"
- "Command Prompt"  
- "Git Bash"
- "Ubuntu" (if WSL is installed)

## Next Steps

Once the PowerShell prototype is working:
1. Test all functionality thoroughly
2. Decide if you want to port to C# for better performance
3. Add additional features (custom profiles, aliases, etc.)