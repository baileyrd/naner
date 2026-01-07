# Naner Launcher

A modern, portable terminal environment launcher for Windows that unifies PowerShell, Unix tools, and development utilities.

## Directory Structure

```
naner_launcher/
├── bin/                          # Compiled executables and binaries
├── config/                       # Configuration files
│   ├── naner.json               # Main Naner configuration
│   ├── user-settings.json       # User-specific settings (gitignored)
│   └── *.json                   # Example configurations
├── docs/                         # Documentation
│   ├── ARCHITECTURE.md          # System architecture overview
│   ├── IMPLEMENTATION-GUIDE.md  # Implementation details
│   ├── QUICK-START.md          # Quick start guide
│   ├── README-VENDOR.md        # Vendor dependency info
│   └── dev/                    # Developer documentation
│       ├── 7ZIP-BUNDLING.md
│       ├── CMDER-ANALYSIS.md
│       ├── DYNAMIC-URLS.md
│       ├── LAUNCHER-COMPARISON.md
│       ├── MIGRATION-GUIDE.md
│       ├── PROJECT-SUMMARY.md
│       ├── SETUP-INSTRUCTIONS.md
│       ├── TERMINAL-LAUNCH-ISSUES.md
│       ├── TESTING-GUIDE.md
│       ├── TROUBLESHOOTING.md
│       ├── TROUBLESHOOTING-CUSTOM-PROFILES.md
│       ├── USER-SETTINGS-EXAMPLES.md
│       └── WINDOWS-TERMINAL-PORTABLE.md
├── src/                         # Source code
│   └── powershell/             # PowerShell scripts
│       ├── Build-NanerDistribution.ps1
│       ├── Invoke-Naner.ps1    # Main launcher script
│       ├── Manage-NanerVendor.ps1
│       ├── Setup-NanerVendor.ps1
│       ├── Show-TerminalStructure.ps1
│       ├── Test-NanerInstallation.ps1
│       ├── Test-WindowsTerminalLaunch.ps1
│       └── Validate-WindowsTerminal.ps1
├── naner.bat                    # Windows batch file entry point
└── .gitignore                   # Git ignore rules

## Quick Start

1. **Setup vendor dependencies:**
   ```powershell
   .\src\powershell\Setup-NanerVendor.ps1
   ```

2. **Launch Naner:**
   ```cmd
   naner.bat
   ```
   Or:
   ```powershell
   .\src\powershell\Invoke-Naner.ps1
   ```

## Documentation

- **[Quick Start Guide](docs/QUICK-START.md)** - Get started quickly
- **[Architecture](docs/ARCHITECTURE.md)** - Technical architecture overview
- **[Implementation Guide](docs/IMPLEMENTATION-GUIDE.md)** - Detailed implementation
- **[Vendor Setup](docs/README-VENDOR.md)** - Vendor dependency information

## Development

See [docs/dev/](docs/dev/) for developer documentation including:
- [Code Quality Analysis](docs/dev/CODE-QUALITY-ANALYSIS.md) - DRY & SOLID principles
- [C# Migration Roadmap](docs/dev/CSHARP-MIGRATION-ROADMAP.md) - Path to native executable
- [Migration Quick Start](docs/dev/MIGRATION-QUICK-START.md) - Fast-track guide
- Setup instructions
- Testing guides
- Troubleshooting
- Architecture decisions

## License

See LICENSE file in repository root.
