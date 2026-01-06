# Naner Vendor System

This document describes Naner's vendor dependency management system and packaging strategy.

## Overview

Naner bundles all dependencies to create a truly portable, self-contained terminal environment. This eliminates external dependencies and ensures consistent behavior across different systems.

## Directory Structure

```
naner/
├── bin/              # Naner executables and utilities
├── config/           # Configuration files
│   └── naner.json    # Main configuration
├── icons/            # Icon resources
├── opt/              # Optional user-installed tools
└── vendor/           # Bundled dependencies
    ├── .downloads/   # Download cache (can be deleted after setup)
    ├── powershell/   # PowerShell 7.x
    ├── terminal/     # Windows Terminal
    ├── msys64/       # MSYS2 (includes Git and Unix tools)
    └── vendor-manifest.json
```

## Bundled Dependencies

### 7-Zip
- **Size**: ~2MB
- **Purpose**: Archive extraction utility (required for extracting other dependencies)
- **Path**: `vendor/7zip/7z.exe`

### PowerShell 7.x
- **Size**: ~100MB
- **Purpose**: Modern PowerShell with cross-platform features
- **Path**: `vendor/powershell/pwsh.exe`

### Windows Terminal
- **Size**: ~50MB
- **Purpose**: Modern terminal emulator with tab support
- **Path**: `vendor/terminal/wt.exe`

### MSYS2
- **Size**: ~400MB (base + essential packages)
- **Purpose**: Complete Unix environment with Git and development tools
- **Paths**:
  - MSYS environment: `vendor/msys64/usr/bin/`
  - MINGW64 environment: `vendor/msys64/mingw64/bin/`
  - Git: `vendor/msys64/usr/bin/git.exe`

**Included packages**:
- git
- make
- gcc (MinGW-w64)
- diffutils, patch
- tar, zip, unzip

## Setup

### Initial Setup

1. **Download and configure vendor dependencies**:
   ```powershell
   .\Setup-NanerVendor.ps1
   ```

2. **Update configuration** (if needed):
   Edit `config/naner.json` to customize paths and profiles.

3. **Test the launcher**:
   ```powershell
   .\Invoke-Naner.ps1 -Debug
   ```

### Setup Options

```powershell
# Default setup (interactive)
.\Setup-NanerVendor.ps1

# Force re-download all dependencies
.\Setup-NanerVendor.ps1 -ForceDownload

# Skip download if already present
.\Setup-NanerVendor.ps1 -SkipDownload

# Specify custom Naner root
.\Setup-NanerVendor.ps1 -NanerRoot "D:\MyNaner"
```

## Unified Environment

Naner creates a **unified environment** where all tools are available in a single profile through smart PATH management.

### PATH Precedence

The unified PATH is built in this order:

1. `naner/bin` - Naner utilities
2. `naner/vendor/msys64/mingw64/bin` - MINGW64 tools (native Windows binaries)
3. `naner/vendor/msys64/usr/bin` - MSYS tools (POSIX-emulated)
4. `naner/vendor/powershell` - PowerShell 7
5. `naner/opt` - User-installed tools
6. System PATH (if InheritSystemPath is true)

### Available Tools in Unified Environment

When you launch Naner, you have immediate access to:

**Unix Tools**:
```bash
git, bash, make, grep, sed, awk, find, tar, gzip, ssh, curl, wget
```

**Compilers**:
```bash
gcc, g++, make, gdb
```

**PowerShell**:
```powershell
pwsh  # PowerShell 7
```

**Windows Tools**:
```cmd
cmd, where, ipconfig, netstat, etc.
```

## Configuration

### Environment Variables

Naner sets these environment variables automatically:

- `NANER_ROOT`: Root directory of Naner installation
- `MSYSTEM`: Set to `MINGW64` for native Windows binary priority
- `MSYS2_PATH_TYPE`: Set to `inherit` to include Windows PATH

### Custom Configuration

Edit `config/naner.json` to customize:

```json
{
  "Environment": {
    "UnifiedPath": true,
    "PathPrecedence": [
      "%NANER_ROOT%\\bin",
      "%NANER_ROOT%\\vendor\\msys64\\mingw64\\bin",
      "%NANER_ROOT%\\vendor\\msys64\\usr\\bin",
      "%NANER_ROOT%\\vendor\\powershell",
      "%NANER_ROOT%\\opt"
    ],
    "EnvironmentVariables": {
      "MY_CUSTOM_VAR": "value"
    }
  }
}
```

## Version Management

### Check Installed Versions

```powershell
.\Manage-NanerVendor.ps1 -ListVersions
```

### Check for Updates

```powershell
.\Manage-NanerVendor.ps1 -CheckUpdates
```

### Update Dependencies

```powershell
# Update specific dependency
.\Manage-NanerVendor.ps1 -Update PowerShell -Backup

# Update all (coming soon)
.\Manage-NanerVendor.ps1 -UpdateAll -Backup
```

## Building Portable Distribution

### Size Optimization

Total uncompressed: ~850MB
Compressed (7z): ~300MB

**Optimization strategies**:

1. **Exclude development files**:
   ```powershell
   # Remove from MSYS2
   vendor/msys64/var/cache/*
   vendor/msys64/usr/share/doc/*
   vendor/msys64/usr/share/man/*
   ```

2. **Strip debug symbols**:
   ```bash
   find vendor/msys64 -name "*.exe" -exec strip {} \;
   ```

3. **Compress with 7-Zip**:
   ```powershell
   7z a -t7z -mx=9 naner-portable.7z naner/
   ```

### Distribution Package Structure

```
naner-portable-vX.X.X/
├── naner/              # Full installation
├── README.txt          # Quick start guide
├── LICENSE.txt         # Combined licenses
└── ATTRIBUTION.txt     # Third-party attribution
```

### Licensing Compliance

All bundled dependencies are open source:

- **PowerShell**: MIT License
- **Windows Terminal**: MIT License
- **MSYS2**: Mix of licenses (GPL, BSD, MIT)
- **Git**: GPL v2

**Requirements**:
1. Include all license files in distribution
2. Provide attribution document
3. Make source code available (or link to upstream sources)

## Installation Methods

### Portable (Recommended)

1. Extract archive to any location
2. Run `Invoke-Naner.ps1`
3. No installation, no admin rights needed

### Installer (Optional)

Create an installer that:
- Installs to `C:\Program Files\Naner`
- Creates Start Menu shortcuts
- Adds context menu integration
- Optionally adds to Windows Terminal profiles

### Hybrid Approach

Offer both:
- **Portable ZIP**: For users who want flexibility
- **MSI Installer**: For enterprise deployment and Windows integration

## User-Installed Tools (opt/)

Users can add their own tools to the `opt/` directory:

```
opt/
├── mytools/
│   ├── tool1.exe
│   └── tool2.exe
└── scripts/
    ├── deploy.ps1
    └── backup.sh
```

These tools automatically become available in the unified environment.

## Advanced: Multiple Profiles

While Naner defaults to a unified environment, you can still create separate profiles:

```json
{
  "Profiles": {
    "Unified": {
      "Name": "Naner (Unified)",
      "Shell": "PowerShell",
      "UseVendorPath": true
    },
    
    "BashOnly": {
      "Name": "Pure Bash",
      "Shell": "Bash",
      "UseVendorPath": false,
      "CustomShell": {
        "ExecutablePath": "%NANER_ROOT%\\vendor\\msys64\\usr\\bin\\bash.exe",
        "Arguments": "--login -i"
      }
    }
  }
}
```

## Troubleshooting

### Vendor Setup Issues

**Problem**: Download fails
```powershell
# Use cached downloads
.\Setup-NanerVendor.ps1 -SkipDownload

# Or download manually and place in vendor/.downloads/
```

**Problem**: MSYS2 initialization fails
```powershell
# Manually initialize
cd vendor\msys64
.\msys2_shell.cmd -defterm -no-start -c "pacman -Sy"
```

### PATH Conflicts

**Problem**: Wrong version of tool is used
```powershell
# Check PATH order
.\Invoke-Naner.ps1 -Debug

# Adjust PathPrecedence in naner.json
```

### Windows Terminal Not Found

**Problem**: `wt.exe` not found after setup
```powershell
# Verify extraction
ls vendor\terminal\wt.exe

# Re-run setup with force
.\Setup-NanerVendor.ps1 -ForceDownload
```

## Development

### Testing Vendor Setup

```powershell
# Test with debug output
.\Invoke-Naner.ps1 -Debug

# Test specific profile
.\Invoke-Naner.ps1 -Profile Bash -Debug
```

### Adding New Vendor Dependencies

1. Update `Setup-NanerVendor.ps1`:
   ```powershell
   $vendorConfig = @{
       MyTool = @{
           Name = "My Tool"
           Url = "https://example.com/mytool.zip"
           FileName = "mytool.zip"
           ExtractDir = "mytool"
           PostInstall = { param($path) # Custom setup }
       }
   }
   ```

2. Update `naner.json`:
   ```json
   {
       "VendorPaths": {
           "MyTool": "%NANER_ROOT%\\vendor\\mytool\\mytool.exe"
       }
   }
   ```

3. Update PATH precedence if needed

## Future Enhancements

- [ ] Automatic updates via `Manage-NanerVendor.ps1`
- [ ] Backup/restore functionality
- [ ] Multiple MSYS2 package presets (minimal, development, full)
- [ ] GUI configuration tool
- [ ] Integration with Windows Package Manager (winget)
- [ ] Chocolatey package
- [ ] MSI installer with context menu integration

## Contributing

When adding new vendor dependencies:

1. Ensure they're open source and redistributable
2. Document licensing requirements
3. Optimize for size where possible
4. Add version checking to `Manage-NanerVendor.ps1`
5. Update this README

## Support

For issues related to:
- **Naner**: Open issue on Naner repository
- **PowerShell**: https://github.com/PowerShell/PowerShell
- **Windows Terminal**: https://github.com/microsoft/terminal
- **MSYS2**: https://github.com/msys2/msys2

## License

Naner vendor system: [Your License]

Bundled dependencies maintain their original licenses (see ATTRIBUTION.txt).
