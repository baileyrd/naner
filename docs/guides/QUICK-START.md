# Quick Start Guide

Get up and running with Naner in minutes.

## What is Naner?

Naner is a **modern, portable terminal environment launcher for Windows** that provides:
- Zero-dependency, self-contained executable (~11 MB)
- Unified PowerShell + Unix tools environment
- Portable development tools (Git, Python, Node.js, etc.)
- Smart configuration with automatic root detection

## Installation

### Option 1: Download Release (Recommended)

1. **Download** the latest release from [GitHub Releases](https://github.com/baileyrd/naner/releases)
2. **Extract** to your desired location (e.g., `C:\naner` or portable drive)
3. **Run first-time setup:**
   ```cmd
   naner init
   ```
4. **Launch Naner:**
   ```cmd
   naner
   ```

### Option 2: Clone Repository

```cmd
git clone https://github.com/baileyrd/naner.git
cd naner
naner init
```

### Option 3: Build from Source

```powershell
# Requires .NET 8.0 SDK
cd src/csharp
.\build.ps1
```

## First Run

When you run `naner` for the first time, it will:
1. Detect first-run state automatically
2. Prompt you to initialize the installation
3. Create the directory structure
4. Generate default configuration

### Interactive Setup
```cmd
naner init
```
Follow the prompts to configure your installation.

### Quick Setup (Non-Interactive)
```cmd
naner init --minimal
```
Creates a minimal installation with default settings.

## Essential Commands

| Command | Description |
|---------|-------------|
| `naner` | Launch default terminal profile |
| `naner --help` | Display help and usage information |
| `naner --version` | Show version information |
| `naner --diagnose` | Run diagnostic health checks |
| `naner init` | Initialize/reinitialize installation |
| `naner setup-vendors` | Download vendor dependencies |

## Terminal Profiles

Naner supports multiple terminal profiles:

| Profile | Description |
|---------|-------------|
| **Unified** | PowerShell with Unix tools (default) |
| **PowerShell** | Pure PowerShell 7 environment |
| **Bash** | MSYS2 Bash environment |
| **CMD** | Windows Command Prompt |

Launch a specific profile:
```cmd
naner PowerShell
naner Bash
```

## Directory Structure

After initialization, your Naner directory will contain:

```
naner/
├── vendor/
│   └── bin/
│       └── naner.exe          # Main executable
├── bin/                       # User executables (high PATH priority)
├── config/
│   ├── naner.json            # Main configuration
│   └── vendors.json          # Vendor definitions
├── home/                      # Portable home directory
│   ├── .ssh/                 # SSH keys
│   ├── .config/              # App configurations
│   └── Documents/PowerShell/ # PowerShell modules
├── plugins/                   # Custom plugins
└── logs/                      # Log files
```

## Installing Vendor Dependencies

Naner can automatically download portable tools:

```cmd
naner setup-vendors
```

This downloads:
- **7-Zip** (~2 MB) - Archive extraction
- **PowerShell 7** (~100 MB) - Modern shell
- **Windows Terminal** (~50 MB) - Terminal UI
- **MSYS2** (~400 MB) - Git, Bash, Unix tools

See [Vendor Setup Guide](VENDOR-SETUP.md) for details.

## Configuration

Main configuration is in `config/naner.json`:

```json
{
  "DefaultProfile": "Unified",
  "VendorPaths": {
    "PowerShell": "%NANER_ROOT%\\vendor\\powershell\\pwsh.exe",
    "WindowsTerminal": "%NANER_ROOT%\\vendor\\terminal\\wt.exe"
  },
  "Environment": {
    "UnifiedPath": true
  }
}
```

### Configuration Formats

Naner supports multiple configuration formats:
- **JSON**: `naner.json` (default)
- **YAML**: `naner.yaml` or `naner.yml`

YAML configuration is auto-discovered if JSON is not present.

## Troubleshooting

### Run Diagnostics
```cmd
naner --diagnose
```

This checks:
- Installation structure
- Configuration validity
- Vendor paths
- Environment variables

### Common Issues

**"Could not locate Naner root directory"**
- Run `naner init` to initialize
- Ensure you're in or below the Naner directory

**"Configuration file not found"**
- Run `naner init --minimal` to create default config
- Check `config/naner.json` exists

**"Windows Terminal not found"**
- Run `naner setup-vendors` to download
- Or install Windows Terminal from Microsoft Store

## Next Steps

- [Vendor Setup Guide](VENDOR-SETUP.md) - Install portable tools
- [Multi-Environment Setup](MULTI-ENVIRONMENT.md) - Manage multiple environments
- [Plugin Development](PLUGIN-DEVELOPMENT.md) - Create custom plugins
- [Architecture Overview](../reference/ARCHITECTURE.md) - Technical deep-dive

## Getting Help

- **Diagnostics:** `naner --diagnose`
- **Help:** `naner --help`
- **Issues:** https://github.com/baileyrd/naner/issues
- **Documentation:** https://github.com/baileyrd/naner/docs
