# Cmder Windows Terminal Launcher

A modern launcher that brings Cmder-like functionality to Windows Terminal, using a hybrid development approach.

## 🎯 Project Status

- **Phase 1: PowerShell Prototype** ✅ **CURRENT**
- **Phase 2: C# Production Build** 📋 Planned
- **Phase 3: Optimization & Polish** 📋 Planned

## 📋 Features

### Current (PowerShell v1.0.0)

✅ Launch Windows Terminal with Cmder profile  
✅ Set CMDER_ROOT and related environment variables  
✅ Auto-detect Windows Terminal installation  
✅ Support for custom starting directories  
✅ Profile/task selection  
✅ Single window vs new window modes  
✅ Shell integration (context menu registration)  
✅ Auto-create Cmder profile if missing  
✅ Verbose logging mode  
✅ Compatible with Cmder command-line syntax  

### Planned (C# Production)

📋 Faster startup time (<100ms)  
📋 Smaller executable size  
📋 Code signing support  
📋 Advanced configuration management  
📋 GUI configuration tool  
📋 Automatic Windows Terminal settings backup  
📋 Multiple profile templates  

## 🚀 Quick Start

### Prerequisites

- Windows 10/11
- Windows Terminal (install from [Microsoft Store](https://aka.ms/terminal))
- PowerShell 5.1+ (included with Windows)

### Installation

1. Download or clone this repository
2. Run the launcher:

```batch
Cmder.bat
```

Or directly with PowerShell:

```powershell
.\src\powershell\Launch-Cmder.ps1
```

### First Run

On first run, the launcher will:
1. Detect Windows Terminal location
2. Ask if you want to create a Cmder profile
3. Set up environment variables
4. Launch Windows Terminal

## 📖 Usage

### Basic Usage

Launch in current directory:
```batch
Cmder.bat
```

Launch in specific directory:
```batch
Cmder.bat -StartDir "C:\Projects"
```

Or:
```batch
Cmder.bat "C:\Projects"
```

### Advanced Usage

Use specific profile:
```batch
Cmder.bat -Profile "PowerShell"
```

Open in existing window (if available):
```batch
Cmder.bat -Single
```

Force new window:
```batch
Cmder.bat -New
```

Verbose logging:
```batch
Cmder.bat -Verbose
```

### Shell Integration (Context Menu)

Register "Cmder Here" in Windows Explorer (current user):
```batch
Cmder.bat -Register USER
```

Register for all users (requires admin):
```batch
Cmder.bat -Register ALL
```

Unregister:
```batch
Cmder.bat -Unregister USER
```

### Cmder Compatibility Mode

The launcher supports original Cmder syntax:

```batch
Cmder.bat /start "C:\Projects"
Cmder.bat /task "PowerShell"
Cmder.bat /c "C:\custom\config"
```

## 🔧 Configuration

### Environment Variables

The launcher sets the following variables:

- `CMDER_ROOT` - Root directory of Cmder installation
- `CMDER_USER_CONFIG` - Path to user configuration
- `CMDER_USER_BIN` - Path to user binaries

### Directory Structure

```
cmder-wt-launcher/
├── Cmder.bat              # Batch wrapper for easy execution
├── src/
│   ├── powershell/        # Current PowerShell implementation
│   │   └── Launch-Cmder.ps1
│   └── csharp/            # Future C# port (empty for now)
├── config/                # User configuration files
├── bin/                   # User scripts and utilities
├── icons/                 # Icons for the launcher
└── docs/                  # Documentation
```

## 🎨 Customizing Windows Terminal Profile

The launcher creates a basic Cmder profile in Windows Terminal. You can customize it:

1. Open Windows Terminal Settings (`Ctrl+,`)
2. Find the "Cmder" profile
3. Customize:
   - Color scheme
   - Font
   - Background image
   - Startup command
   - And more!

### Recommended Cmder Profile Settings

```json
{
    "name": "Cmder",
    "commandline": "cmd.exe /k %CMDER_ROOT%\\vendor\\init.bat",
    "startingDirectory": "%USERPROFILE%",
    "icon": "%CMDER_ROOT%\\icons\\cmder.ico",
    "colorScheme": "One Half Dark",
    "font": {
        "face": "Cascadia Code",
        "size": 10
    },
    "backgroundImage": "%CMDER_ROOT%\\icons\\background.png",
    "backgroundImageOpacity": 0.1,
    "useAcrylic": true,
    "acrylicOpacity": 0.8
}
```

## 🐛 Troubleshooting

### "Windows Terminal not found"

**Solution**: Install Windows Terminal from:
- Microsoft Store: https://aka.ms/terminal
- GitHub: https://github.com/microsoft/terminal/releases

### "Execution Policy" error

**Solution**: Run PowerShell as admin and execute:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Context menu not appearing

**Solution**: 
1. Try registering again with admin rights:
   ```batch
   Cmder.bat -Register ALL
   ```
2. Refresh Explorer (F5) or restart Explorer process

### Profile not found in Windows Terminal

**Solution**: Run the launcher once and allow it to create the profile, or manually add it to Windows Terminal settings.

## 🔄 Hybrid Development Roadmap

### Phase 1: PowerShell Prototype (CURRENT)
- ✅ Implement all core features
- ✅ Test and validate functionality
- ✅ Gather user feedback
- ✅ Document requirements

### Phase 2: C# Port (NEXT)
- [ ] Set up .NET 6+ project
- [ ] Port PowerShell logic to C#
- [ ] Add unit tests
- [ ] Optimize performance
- [ ] Create installer
- [ ] Add code signing

### Phase 3: Production Ready
- [ ] Benchmark and optimize
- [ ] Add GUI configuration tool
- [ ] Comprehensive documentation
- [ ] Release v1.0.0
- [ ] Maintain PowerShell version as legacy/simple option

## 🤝 Contributing

This is a hybrid development project:

- **PowerShell branch**: Quick features, experiments, testing
- **C# branch**: Production code, optimization, distribution

Feel free to contribute to either!

## 📊 Performance Comparison

| Metric | PowerShell (Current) | C# (Target) |
|--------|---------------------|-------------|
| Startup Time | ~300-500ms | <100ms |
| File Size | 1-5MB (ps2exe) | ~500KB |
| Dependencies | PowerShell 5.1+ | .NET 6+ Runtime |
| Ease of Modify | ✅ Very Easy | Moderate |
| User Trust | ⚠️ Scripts | ✅ Executable |

## 📝 License

MIT License - feel free to use and modify!

## 🔗 Resources

- [Windows Terminal Documentation](https://docs.microsoft.com/en-us/windows/terminal/)
- [Original Cmder Project](https://cmder.app/)
- [PowerShell Documentation](https://docs.microsoft.com/en-us/powershell/)

## ⚡ Quick Command Reference

```batch
# Basic launch
Cmder.bat

# Custom directory
Cmder.bat "C:\Projects"
Cmder.bat -StartDir "C:\Projects"

# Different profile
Cmder.bat -Profile "PowerShell"
Cmder.bat -Task "Ubuntu"

# Window management
Cmder.bat -Single          # Open in existing window
Cmder.bat -New             # Force new window

# Shell integration
Cmder.bat -Register USER   # Add to context menu
Cmder.bat -Unregister USER # Remove from context menu

# Debugging
Cmder.bat -Verbose         # Show detailed logs
```

## 🎯 Why Hybrid Approach?

1. **Fast Iteration**: PowerShell allows rapid development and testing
2. **User Testing**: Get real feedback before committing to C#
3. **Feature Validation**: Ensure all features work as expected
4. **Clear Specification**: Working prototype serves as spec for C# port
5. **Fallback Option**: Keep PowerShell version for users who prefer it

---

**Current Version**: 1.0.0-prototype (PowerShell)  
**Next Milestone**: C# port with <100ms startup time
