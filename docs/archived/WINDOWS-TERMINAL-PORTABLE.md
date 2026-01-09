# Windows Terminal Portable Mode

## Overview

Windows Terminal supports **portable mode** through a simple `.portable` file marker. When this file exists in the same directory as `wt.exe`, Windows Terminal stores all settings, profiles, and state locally instead of in the user's AppData folder.

## Why Portable Mode Matters

### Without `.portable` File
```
Settings Location:
%LOCALAPPDATA%\Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\

Problems:
❌ Settings don't travel with Naner
❌ Conflicts with system Windows Terminal
❌ Not truly portable
❌ Different settings on each machine
❌ Lost when moving Naner folder
```

### With `.portable` File
```
Settings Location:
vendor\terminal\LocalState\

Benefits:
✓ Settings travel with Naner
✓ Isolated from system Windows Terminal
✓ Truly portable
✓ Same settings everywhere
✓ Backup by copying folder
```

## Implementation

### Creating the `.portable` File

The setup script automatically creates this file:

```powershell
# In Setup-NanerVendor.ps1 PostInstall for Windows Terminal
$portableFile = Join-Path $extractPath ".portable"
New-Item -Path $portableFile -ItemType File -Force | Out-Null
Write-Info "Created .portable file for portable mode"
```

**Location**: `vendor/terminal/.portable`

**Content**: Empty file (presence is what matters, not content)

### How Windows Terminal Detects It

When Windows Terminal starts:
```
1. Check for .portable file in executable directory
2. If found:
   - Use {exe_directory}/LocalState/ for settings
   - Create LocalState folder if needed
3. If not found:
   - Use %LOCALAPPDATA%/Packages/.../LocalState/
```

Source: [Windows Terminal Portable Mode Documentation](https://learn.microsoft.com/en-us/windows/terminal/install#portable-mode)

## Directory Structure

With portable mode enabled:

```
vendor/terminal/
├── .portable                    ← Marker file
├── wt.exe                       ← Executable
├── WindowsTerminal.exe
├── OpenConsole.exe
├── *.dll                        ← Dependencies
└── LocalState/                  ← Created automatically
    ├── settings.json            ← User settings
    ├── state.json               ← Window state, positions
    └── profiles.json            ← (if customized)
```

## Settings Stored Locally

When using portable mode, these files are stored in `vendor/terminal/LocalState/`:

### `settings.json`
- Color schemes
- Profiles (PowerShell, CMD, bash, etc.)
- Keybindings
- Default profile
- Startup behavior
- Font settings
- Background images

### `state.json`
- Window size and position
- Tab state
- Recently opened directories
- Pane configurations

## Benefits for Naner

### 1. True Portability
Move Naner folder to USB drive, network share, or another machine - settings come with it.

### 2. Isolation
System Windows Terminal and Naner Windows Terminal don't interfere with each other.

### 3. Version Control Friendly
Settings can be committed to git:
```bash
git add vendor/terminal/LocalState/settings.json
git commit -m "Update terminal color scheme"
```

### 4. Easy Backup
Copy entire Naner folder = backup includes terminal settings.

### 5. Consistent Experience
Every machine where Naner is used has identical terminal configuration.

## Configuration Example

Users can customize their portable Windows Terminal:

```json
// vendor/terminal/LocalState/settings.json
{
    "defaultProfile": "{...}",
    "profiles": {
        "defaults": {
            "font": {
                "face": "Cascadia Code",
                "size": 11
            },
            "colorScheme": "One Half Dark"
        },
        "list": [
            {
                "name": "Naner (Unified)",
                "commandline": "...",
                "icon": "..."
            }
        ]
    },
    "schemes": [
        {
            "name": "Custom Naner Theme",
            "background": "#1e1e1e",
            "foreground": "#d4d4d4"
        }
    ]
}
```

Changes are stored locally and persist across restarts.

## Verification

### Check if Portable Mode is Active

```powershell
# Check for .portable file
Test-Path vendor\terminal\.portable

# Check if LocalState is being used
Test-Path vendor\terminal\LocalState\settings.json

# Run Windows Terminal and check path in settings
vendor\terminal\wt.exe
# Click Settings > Open JSON file
# File path should be: vendor\terminal\LocalState\settings.json
```

### Manual Verification

1. Launch Naner's Windows Terminal
2. Open Settings (Ctrl+,)
3. Click "Open JSON file"
4. Path should show: `...\vendor\terminal\LocalState\settings.json`
5. If it shows `%LOCALAPPDATA%\...` then portable mode is NOT active

## Troubleshooting

### `.portable` File Not Working

**Symptoms**: Settings stored in `%LOCALAPPDATA%` instead of `LocalState/`

**Causes**:
1. `.portable` file doesn't exist
2. Windows Terminal was installed system-wide
3. Path to wt.exe is wrong

**Solutions**:
```powershell
# Recreate .portable file
New-Item vendor\terminal\.portable -ItemType File -Force

# Verify location
Get-Item vendor\terminal\.portable

# Ensure using vendored terminal
$env:PATH = "$(Get-Location)\vendor\terminal;$env:PATH"
wt.exe  # Should use vendored version
```

### Settings Not Persisting

**Symptoms**: Changes to settings don't save between sessions

**Causes**:
1. LocalState directory not writable
2. Settings file locked
3. File permissions issue

**Solutions**:
```powershell
# Check permissions
Get-Acl vendor\terminal\LocalState

# Fix permissions
$acl = Get-Acl vendor\terminal\LocalState
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $env:USERNAME, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl vendor\terminal\LocalState $acl
```

### LocalState Directory Not Created

**Symptoms**: `.portable` exists but no `LocalState/` folder

**Cause**: Windows Terminal hasn't been launched yet

**Solution**:
```powershell
# Launch Windows Terminal once
vendor\terminal\wt.exe -d .

# LocalState will be created automatically
```

## Comparison: System vs Portable

| Feature | System Install | Portable Mode |
|---------|----------------|---------------|
| Settings location | `%LOCALAPPDATA%` | `{exe_dir}\LocalState\` |
| Travels with folder | ❌ | ✓ |
| Isolated from system | ❌ | ✓ |
| Backup friendly | ❌ | ✓ |
| Version control | ❌ | ✓ |
| Multi-machine | ❌ | ✓ |
| Setup required | Install MSI | Copy folder |

## Advanced: Pre-configuring Settings

You can ship Naner with pre-configured Windows Terminal settings:

```powershell
# Create default settings
New-Item vendor\terminal\LocalState -ItemType Directory -Force

# Copy pre-configured settings
Copy-Item config\default-wt-settings.json vendor\terminal\LocalState\settings.json

# Now first launch will use these settings
```

This allows you to:
- Set default color scheme
- Pre-configure Naner profiles
- Set custom keybindings
- Configure default shell

## Best Practices

### 1. Always Create `.portable`
Include it in setup script (already done in Setup-NanerVendor.ps1)

### 2. Document Settings Location
Tell users where to customize: `vendor/terminal/LocalState/settings.json`

### 3. Provide Default Settings (Optional)
```
config\
└── windows-terminal-defaults\
    └── settings.json
```

### 4. Backup Considerations
```powershell
# Backup includes terminal settings
Copy-Item vendor\terminal -Destination backup\ -Recurse
```

### 5. Git Considerations
```gitignore
# In .gitignore
vendor/terminal/LocalState/state.json  # Window positions (per-machine)

# Keep settings.json if you want consistent config
!vendor/terminal/LocalState/settings.json
```

## Security Considerations

### Settings File Permissions
LocalState directory has same permissions as parent folder. Ensure:
- Not world-writable
- Protected from modification
- Appropriate for portable context

### Sensitive Information
Don't store sensitive information in settings:
- ❌ API keys in environment variables
- ❌ Passwords in profile commands
- ✓ Use secrets management instead

## Future Enhancements

1. **Default Naner Theme**
   - Ship with custom color scheme
   - Pre-configured profiles
   - Naner branding

2. **Settings Sync**
   - Optional cloud sync
   - Git-based sync
   - Multi-device coordination

3. **Profile Generator**
   - Auto-generate profiles for detected tools
   - Dynamic profile creation
   - Profile templates

## References

- [Windows Terminal Portable Mode Official Docs](https://learn.microsoft.com/en-us/windows/terminal/install#portable-mode)
- [Windows Terminal Settings Schema](https://learn.microsoft.com/en-us/windows/terminal/customize-settings/profile-general)
- [Windows Terminal on GitHub](https://github.com/microsoft/terminal)

## Summary

The `.portable` file is a small but crucial component that makes Windows Terminal truly portable:

✓ **One file** enables complete portability
✓ **Zero configuration** by users
✓ **Automatic** - works transparently
✓ **Isolated** - no conflicts
✓ **Reliable** - official Windows Terminal feature

This ensures Naner delivers on its promise of being a **truly portable** terminal environment.
