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
│   ├── vendors.json             # Vendor definitions
│   ├── plugin-schema.json       # Plugin schema
│   └── user-settings.json       # User overrides (gitignored)
├── home/                         # Portable home directory (created by init)
│   ├── .config/                 # Application configurations
│   ├── .ssh/                    # SSH keys (gitignored)
│   └── Documents/PowerShell/    # PowerShell modules
├── src/csharp/                  # C# source code
│   ├── Naner.Archives/          # Archive extraction services
│   ├── Naner.Commands/          # Command pattern implementations
│   ├── Naner.Configuration/     # Configuration management
│   ├── Naner.Configuration.Abstractions/  # Config interfaces
│   ├── Naner.Core/              # Core utilities and constants
│   ├── Naner.DependencyInjection/  # Service registration
│   ├── Naner.Infrastructure/    # HTTP, logging services
│   ├── Naner.Init/              # Standalone initialization tool
│   ├── Naner.Launcher/          # Main launcher application
│   ├── Naner.Setup/             # Setup and first-run logic
│   ├── Naner.Tests/             # Unit tests
│   └── Naner.Vendors/           # Vendor management
├── tests/                        # Test suites
├── docs/                         # Documentation
│   ├── guides/                  # User guides
│   ├── development/             # Developer docs
│   ├── reference/               # Technical references
│   └── archived/                # Historical docs
├── naner.bat                     # Windows entry point
└── README.md                     # This file
```

## Architecture

Naner follows **Clean Architecture** principles with a layered design emphasizing:
- **SOLID Principles** - Single Responsibility, Open/Closed, Interface Segregation, Dependency Inversion
- **Design Patterns** - Command, Strategy, Adapter, Dependency Injection, Configuration-Driven Design
- **Testability** - Interface-based abstractions for unit testing
- **Modularity** - Clear separation of concerns across projects

See [ARCHITECTURE.md](docs/reference/ARCHITECTURE.md) for complete architectural documentation including service catalog, design patterns, and extension points.

### Recent Refactoring (2026-01-10)

The codebase underwent a comprehensive 7-phase refactoring to transform it from a monolithic structure into a well-architected, modular system:

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| **Modularity Score** | 4.5/10 | 8.5/10 | **+89%** |
| **Code Duplication** | ~800 lines | <50 lines | **-94%** |
| **Service Interfaces** | 0 | 7 interfaces | New |
| **Unit Tests** | 0 | 19 tests (100% pass) | New |
| **SOLID Compliance** | Partial | Full | Achieved |

See [REFACTORING_COMPLETE.md](docs/development/REFACTORING_COMPLETE.md) for the complete refactoring summary including phase-by-phase breakdown, design patterns, and success metrics.

## Documentation

**Getting Started:**
- [Quick Start Guide](docs/guides/QUICK-START.md) - Get up and running
- [Documentation Index](docs/README.md) - Complete documentation guide

**Architecture & Design:**
- [Architecture Guide](docs/reference/ARCHITECTURE.md) - System design (C# implementation)
- [Refactoring Summary](docs/development/REFACTORING_COMPLETE.md) - Complete refactoring documentation

**User Guides:**
- [Portable Tool Guides](docs/guides/) - Setting up portable development tools
- [Multi-Environment Setup](docs/guides/MULTI-ENVIRONMENT.md) - Managing multiple environments
- [Plugin Development](docs/guides/PLUGIN-DEVELOPMENT.md) - Creating custom plugins

**Technical Reference:**
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
# Run C# unit tests
cd src/csharp
dotnet test

# Run PowerShell test suite (legacy)
cd tests
.\Run-AllTests.ps1
```

The C# test suite includes 19 tests covering services, commands, and configuration with 100% pass rate.

### Project Structure

The codebase is organized into multiple projects following Clean Architecture:

- **`Naner.Core`** - Core abstractions and constants
  - `NanerConstants.cs` - Centralized configuration constants
  - `PathUtilities.cs` - Path resolution and building utilities
  - `ILogger.cs` - Logging abstraction

- **`Naner.Configuration`** - Configuration management
  - `ConfigurationManager.cs` - Configuration loading with provider pattern
  - JSON and YAML configuration providers

- **`Naner.Configuration.Abstractions`** - Configuration interfaces and models
  - `IConfigurationManager.cs`, `IConfigurationProvider.cs`
  - `NanerConfig.cs`, `ProfileConfig.cs` - Data models

- **`Naner.Commands`** - Command pattern implementations
  - `CommandRouter.cs` - Central command dispatcher
  - `InitCommand`, `HelpCommand`, `VersionCommand`, `DiagnosticsCommand`

- **`Naner.Launcher`** - Main launcher application
  - `Program.cs` - Entry point
  - `TerminalLauncher.cs` - Core terminal launch logic

- **`Naner.Init`** - Standalone initialization tool
  - `GitHubReleasesClient.cs` - GitHub API client
  - `EssentialVendorDownloader.cs` - Vendor download logic

- **`Naner.Infrastructure`** - Infrastructure services
  - `HttpClientWrapper.cs`, `HttpDownloadService.cs` - HTTP operations

- **`Naner.Archives`** - Archive extraction
  - `ArchiveExtractorService.cs` - ZIP, TAR.XZ, MSI extraction

- **`Naner.Setup`** - Setup and installation
  - `SetupManager.cs`, `FirstRunDetector.cs`

- **`Naner.Vendors`** - Vendor management
  - `VendorDefinition.cs`, `VendorConfigurationLoader.cs`

- **`Naner.DependencyInjection`** - Service registration
  - `NanerServiceHost.cs` - DI container configuration

- **`Naner.Tests`** - Unit tests (xUnit, FluentAssertions, Moq)

## Version History

**v1.0.0** (2026-01-08) - Production Release
- Pure C# implementation with zero dependencies
- Self-contained 11 MB executable
- Self-bootstrapping with `naner init` command
- Smart NANER_ROOT detection
- Enhanced error messages and diagnostics

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
