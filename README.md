# Naner

**A modern, portable terminal environment launcher for Windows**

Naner is a self-contained, zero-dependency terminal launcher that provides a unified development environment with PowerShell, Unix tools, and portable development utilities.

## Features

- **Zero Dependencies** - Single 11 MB self-contained executable
- **Self-Bootstrapping** - Automatic setup with `naner init`
- **Smart Configuration** - Automatic NANER_ROOT detection with 6 fallback strategies
- **Portable Tools** - Support for portable Git, Python, Node.js, Ruby, Rust, Go, and more
- **Profile Management** - Launch different terminal profiles with custom environments
- **Professional CLI** - Proper exit codes, helpful error messages, and diagnostics

## Quick Start

### Installation

1. **Download** the latest [release](https://github.com/baileyrd/naner/releases) or clone this repository
2. **Run first-time setup:**
   ```cmd
   naner init
   ```
3. **Launch Naner:**
   ```cmd
   naner
   ```

### Commands

```cmd
naner --version           # Show version information
naner --help             # Display help
naner --diagnose         # Run diagnostics
naner init               # Interactive setup wizard
naner init --minimal     # Quick minimal setup
naner <profile>          # Launch specific profile
```

## Directory Structure

```
naner/
├── vendor/
│   └── bin/                      # Naner executable (naner.exe)
├── bin/                          # User executables (high PATH priority)
├── config/
│   ├── naner.json               # Main configuration
│   └── user-settings.json       # User overrides (gitignored)
├── home/                         # Portable home directory
│   ├── .config/                 # Application configurations
│   ├── .ssh/                    # SSH keys (gitignored)
│   ├── Documents/PowerShell/    # PowerShell modules
│   └── Templates/               # Project templates
├── plugins/                      # Optional vendor plugins
├── src/csharp/                  # C# source code
│   ├── Naner.Common/            # Common utilities
│   ├── Naner.Configuration/     # Configuration management
│   └── Naner.Launcher/          # Main launcher
├── tests/                        # Test suites
├── docs/                         # Documentation
│   ├── guides/                  # User guides
│   ├── reference/               # Technical reference
│   ├── development/             # Developer docs
│   └── archived/                # Historical docs
├── naner.bat                     # Windows entry point
└── RELEASE-NOTES-v1.0.0.md      # Release notes
```

## Documentation

**Getting Started:**
- [Quick Start Guide](docs/guides/QUICK-START.md) - Get up and running
- [Release Notes](RELEASE-NOTES-v1.0.0.md) - v1.0.0 release information
- [Documentation Index](docs/README.md) - Complete documentation guide

**User Guides:**
- [Portable Tool Guides](docs/guides/) - Setting up portable development tools
- [Multi-Environment Setup](docs/guides/MULTI-ENVIRONMENT.md) - Managing multiple environments
- [Plugin Development](docs/guides/PLUGIN-DEVELOPMENT.md) - Creating custom plugins

**Technical Reference:**
- [Architecture](docs/reference/ARCHITECTURE.md) - System design and architecture
- [Implementation Guide](docs/reference/IMPLEMENTATION-GUIDE.md) - Technical implementation
- [Error Codes](docs/reference/ERROR-CODES.md) - Error code reference

**For Developers:**
- [Capability Roadmap](docs/development/CAPABILITY-ROADMAP.md) - Feature roadmap
- [C# Migration Documentation](docs/development/) - Phase 10 migration docs
- [Testing Guide](docs/development/TESTING_GUIDE.md) - Testing procedures

## Building from Source

### Prerequisites
- .NET 8.0 SDK or later
- Windows 10/11

### Build Steps

```powershell
# Navigate to the C# source directory
cd src/csharp

# Build the release executable
.\build.ps1

# Output: vendor/bin/naner.exe
```

The build produces a self-contained executable at `vendor\bin\naner.exe` (approximately 11 MB).

## Development

### Running Tests

```powershell
# Run PowerShell test suite
cd tests
.\Run-AllTests.ps1

# Run C# unit tests (if available)
cd src/csharp
dotnet test
```

### Project Structure

- `src/csharp/Naner.Common/` - Shared utilities (PathUtilities, FirstRunDetector, SetupManager)
- `src/csharp/Naner.Configuration/` - Configuration management with JSON source generation
- `src/csharp/Naner.Launcher/` - Main launcher application (Program.cs, TerminalLauncher.cs)

## Version History

**v1.0.0** (2026-01-08) - Production Release
- Pure C# implementation with zero dependencies
- Self-contained 11 MB executable
- Self-bootstrapping with `naner init` command
- Smart NANER_ROOT detection
- Enhanced error messages and diagnostics
- See [RELEASE-NOTES-v1.0.0.md](RELEASE-NOTES-v1.0.0.md) for details

For earlier versions and PowerShell implementation history, see [archived documentation](docs/archived/).

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

See [docs/development/](docs/development/) for development guidelines.

## License

See LICENSE file in repository root.

## Support

- **Issues:** https://github.com/baileyrd/naner/issues
- **Releases:** https://github.com/baileyrd/naner/releases
- **Diagnostics:** Run `naner --diagnose` for troubleshooting
