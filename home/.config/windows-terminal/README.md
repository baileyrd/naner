# Naner Windows Terminal Configuration

Enhanced Windows Terminal configuration with custom color schemes, optimized profiles, and productivity keybindings.

## Overview

This configuration provides:
- **5 custom color schemes** (Naner Dark, Light, Ocean, Forest, Mocha)
- **Naner-specific profiles** for PowerShell, Bash, and CMD
- **Enhanced keybindings** for productivity
- **Optimized font settings** with Cascadia Code
- **Consistent theming** across all shells

## Installation

### Automatic Installation (Recommended)

The settings are automatically applied when you:
1. Launch Windows Terminal from within Naner environment
2. Have `NANER_ROOT` environment variable set

### Manual Installation

Copy the settings to Windows Terminal's config directory:

```powershell
# Backup existing settings
$wtSettings = "$env:LOCALAPPDATA\Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState"
Copy-Item "$wtSettings\settings.json" "$wtSettings\settings.backup.json"

# Copy Naner settings
Copy-Item "$env:NANER_ROOT\home\.config\windows-terminal\settings.json" "$wtSettings\settings.json"
```

## Color Schemes

### Naner Dark (Default)
**Best for:** Extended coding sessions, low-light environments
- Background: `#1e1e2e` (Dark blue-grey)
- Foreground: `#cdd6f4` (Light grey-blue)
- Accent colors: Catppuccin Mocha inspired
- **Use case:** Default scheme, easy on the eyes

### Naner Light
**Best for:** Daytime work, bright environments
- Background: `#eff1f5` (Light grey)
- Foreground: `#4c4f69` (Dark grey)
- Accent colors: Catppuccin Latte inspired
- **Use case:** High ambient light conditions

### Naner Ocean
**Best for:** Frontend development, creative work
- Background: `#0f1419` (Very dark blue)
- Foreground: `#b3b1ad` (Warm grey)
- Accent colors: Ayu Dark inspired
- **Use case:** Modern, vibrant aesthetic

### Naner Forest
**Best for:** Terminal multiplexing, tmux/screen users
- Background: `#232634` (Dark blue-purple)
- Foreground: `#c6d0f5` (Light lavender)
- Accent colors: Catppuccin Frappe inspired
- **Use case:** Multiple panes, easy color differentiation

### Naner Mocha
**Best for:** Night coding, dark mode preference
- Background: `#1e1e2e` (Dark blue-grey)
- Foreground: `#cdd6f4` (Light grey-blue)
- Accent colors: Catppuccin Mocha with pink purple
- **Use case:** Late-night sessions, reduced blue light

## Switching Color Schemes

### Via Settings UI

1. Open Windows Terminal settings (`Ctrl+,`)
2. Navigate to profile (e.g., "PowerShell (Naner)")
3. Under "Appearance" → "Color scheme"
4. Select desired Naner scheme

### Via Settings JSON

Edit `settings.json` and change the profile's `colorScheme`:

```json
{
    "name": "PowerShell (Naner)",
    "colorScheme": "Naner Ocean"  // Change this
}
```

### Set as Default for All Profiles

Edit the `defaults` section:

```json
"profiles": {
    "defaults": {
        "colorScheme": "Naner Light"  // Applies to all new profiles
    }
}
```

## Profiles

### PowerShell (Naner)
- **GUID:** `{574e775e-4f2a-5b96-ac1e-a2962a402336}`
- **Command:** `%NANER_ROOT%\vendor\powershell\pwsh.exe -NoLogo`
- **Icon:** PowerShell icon from Naner
- **Shortcut:** `Ctrl+Shift+1` (new tab)
- **Default profile**

### Bash (Naner)
- **GUID:** `{b453ae62-4e3d-5e58-b989-0a998ec441b8}`
- **Command:** `%NANER_ROOT%\vendor\msys64\usr\bin\bash.exe --login -i`
- **Icon:** Bash icon from Naner
- **Shortcut:** `Ctrl+Shift+2` (new tab)

### CMD (Naner)
- **GUID:** `{0caa0dad-35be-5f56-a8ff-afceeeaa6101}`
- **Command:** `cmd.exe /K %NANER_ROOT%\bin\naner-launcher.bat cmd`
- **Icon:** CMD icon from Naner
- **Shortcut:** `Ctrl+Shift+3` (new tab)

## Keybindings

### Tab Management
| Shortcut | Action |
|----------|--------|
| `Ctrl+Shift+1` | New PowerShell tab |
| `Ctrl+Shift+2` | New Bash tab |
| `Ctrl+Shift+3` | New CMD tab |
| `Ctrl+Shift+D` | Duplicate current tab |
| `Ctrl+Shift+W` | Close current pane/tab |
| `Ctrl+Tab` | Next tab (Windows Terminal default) |
| `Ctrl+Shift+Tab` | Previous tab (Windows Terminal default) |

### Pane Management
| Shortcut | Action |
|----------|--------|
| `Alt+Shift+D` | Split pane (auto) with duplicate |
| `Alt+↑` | Move focus up |
| `Alt+↓` | Move focus down |
| `Alt+←` | Move focus left |
| `Alt+→` | Move focus right |

### Copy/Paste
| Shortcut | Action |
|----------|--------|
| `Ctrl+C` | Copy selection |
| `Ctrl+V` | Paste |
| `Ctrl+Shift+F` | Find/search |

### View Management
| Shortcut | Action |
|----------|--------|
| `Ctrl+Shift+F11` | Toggle focus mode |
| `Alt+Enter` | Toggle fullscreen |
| `Ctrl++` | Increase font size (default) |
| `Ctrl+-` | Decrease font size (default) |
| `Ctrl+0` | Reset font size (default) |

## Font Configuration

### Default Font: Cascadia Code

```json
"font": {
    "face": "Cascadia Code",
    "size": 11,
    "weight": "normal"
}
```

**Features:**
- **Programming ligatures** - Enhanced code readability
- **Powerline glyphs** - For fancy prompts
- **Monospace** - Perfect alignment

### Alternative Fonts

Edit profile font settings:

```json
// Fira Code (popular with ligatures)
"font": {
    "face": "Fira Code",
    "size": 10
}

// JetBrains Mono (clean, modern)
"font": {
    "face": "JetBrains Mono",
    "size": 10
}

// Consolas (Windows classic)
"font": {
    "face": "Consolas",
    "size": 11
}
```

Download fonts:
- [Cascadia Code](https://github.com/microsoft/cascadia-code/releases)
- [Fira Code](https://github.com/tonsky/FiraCode/releases)
- [JetBrains Mono](https://www.jetbrains.com/lp/mono/)

## Customization

### Change Opacity

```json
"profiles": {
    "defaults": {
        "opacity": 90,           // 0-100
        "useAcrylic": true      // Enable blur effect
    }
}
```

### Change Cursor

```json
"profiles": {
    "defaults": {
        "cursorShape": "vintage",  // bar, vintage, underscore, filledBox, emptyBox
        "cursorColor": "#FF0000"
    }
}
```

### Change Starting Directory

Per profile:
```json
{
    "name": "PowerShell (Naner)",
    "startingDirectory": "%USERPROFILE%\\Projects"
}
```

### Background Images

```json
{
    "backgroundImage": "%NANER_ROOT%\\home\\.config\\windows-terminal\\backgrounds\\image.jpg",
    "backgroundImageOpacity": 0.3,
    "backgroundImageStretchMode": "uniformToFill"
}
```

### Padding

```json
"profiles": {
    "defaults": {
        "padding": "12, 12, 12, 12"  // top, right, bottom, left
    }
}
```

## Creating Custom Color Schemes

### From Existing Scheme

1. Copy an existing scheme block in `settings.json`
2. Change the `name` field
3. Modify colors as desired
4. Apply to profile

### Color Scheme Template

```json
{
    "name": "My Custom Scheme",
    "background": "#RRGGBB",
    "foreground": "#RRGGBB",
    "cursorColor": "#RRGGBB",
    "selectionBackground": "#RRGGBB",

    "black": "#RRGGBB",
    "red": "#RRGGBB",
    "green": "#RRGGBB",
    "yellow": "#RRGGBB",
    "blue": "#RRGGBB",
    "purple": "#RRGGBB",
    "cyan": "#RRGGBB",
    "white": "#RRGGBB",

    "brightBlack": "#RRGGBB",
    "brightRed": "#RRGGBB",
    "brightGreen": "#RRGGBB",
    "brightYellow": "#RRGGBB",
    "brightBlue": "#RRGGBB",
    "brightPurple": "#RRGGBB",
    "brightCyan": "#RRGGBB",
    "brightWhite": "#RRGGBB"
}
```

### Color Scheme Tools

- [Windows Terminal Themes](https://windowsterminalthemes.dev/) - Browse and download schemes
- [Terminal.sexy](https://terminal.sexy/) - Create schemes visually
- [Gogh](https://github.com/Gogh-Co/Gogh) - Collection of color schemes

## Tips & Tricks

### Quick Profile Switching

Use `Ctrl+Shift+Space` to open command palette, then type profile name.

### Create Profile Variants

Duplicate a profile and modify:
- Color scheme
- Starting directory
- Font size

Example: "PowerShell (Naner) - Large Font"

### Tab Titles

Customize tab titles:
```json
{
    "tabTitle": "Dev Server",
    "suppressApplicationTitle": true
}
```

### Quake Mode (Dropdown Terminal)

Enable in settings:
```json
{
    "actions": [
        {
            "command": "quakeMode",
            "keys": "win+`"
        }
    ]
}
```

### Multiple Windows

Launch Windows Terminal with specific profile:
```powershell
wt -p "PowerShell (Naner)"
```

Launch with multiple tabs/panes:
```powershell
wt -p "PowerShell (Naner)" ; split-pane -p "Bash (Naner)"
```

## Troubleshooting

### Settings Not Applied

**Issue:** Changes don't appear after edit

**Solutions:**
- Reload Windows Terminal (close all windows and reopen)
- Check JSON syntax (use online validator)
- Check Windows Terminal version (Settings → About)

### Icons Not Showing

**Issue:** Profile icons not displaying

**Solutions:**
- Verify icon paths exist: `Test-Path $env:NANER_ROOT\icons\powershell.png`
- Use absolute paths instead of `%NANER_ROOT%`
- Use built-in icon names: `"icon": "ms-appx:///ProfileIcons/{0caa0dad-35be-5f56-a8ff-afceeeaa6101}.png"`

### Color Scheme Not Applied

**Issue:** Profile using wrong colors

**Solutions:**
- Check color scheme name matches exactly (case-sensitive)
- Verify scheme exists in `schemes` array
- Set in profile, not just defaults

### Font Not Loading

**Issue:** Custom font not working

**Solutions:**
- Install font system-wide (right-click font file → Install)
- Restart Windows Terminal after installing font
- Check font name exactly matches installed name

### Performance Issues

**Issue:** Terminal feels slow

**Solutions:**
```json
{
    "useAcrylic": false,      // Disable transparency effects
    "opacity": 100,           // Full opacity
    "antialiasingMode": "cleartype"  // Or "aliased" for performance
}
```

## Best Practices

### ✅ DO

- **Backup settings** before major changes
- **Test color schemes** in different lighting conditions
- **Use consistent** font sizes across profiles
- **Set appropriate opacity** for your workflow
- **Customize keybindings** to avoid conflicts

### ❌ DON'T

- **Don't edit settings while Terminal is open** (may be overwritten)
- **Don't set very low opacity** (reduces readability)
- **Don't use tiny font sizes** (< 9pt typically hard to read)
- **Don't disable important keybindings** (copy/paste)

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-07 | Initial configuration with 5 color schemes |

## Related Documentation

- [Windows Terminal Documentation](https://docs.microsoft.com/en-us/windows/terminal/)
- [Color Scheme Documentation](https://docs.microsoft.com/en-us/windows/terminal/customize-settings/color-schemes)
- [Naner VS Code Settings](../../.vscode/settings.json)

---

**Maintained by:** Naner Project
**Last Updated:** 2026-01-07
