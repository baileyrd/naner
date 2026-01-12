# Naner Implementation Guide

Technical implementation reference for Naner v1.0.0 (Pure C# Implementation).

## Overview

Naner is built as a pure C# application targeting .NET 8.0, compiled to a self-contained single-file executable (~11 MB) that requires no external dependencies.

## Project Architecture

### Solution Structure

```
src/csharp/
├── Naner.Core/                    # Core abstractions and constants
├── Naner.Configuration/           # Configuration loading and management
├── Naner.Configuration.Abstractions/  # Config interfaces and models
├── Naner.Commands/                # Command pattern implementations
├── Naner.Launcher/                # Main entry point
├── Naner.Init/                    # Standalone initialization tool
├── Naner.Infrastructure/          # HTTP and I/O services
├── Naner.Archives/                # Archive extraction services
├── Naner.Setup/                   # First-run and setup logic
├── Naner.Vendors/                 # Vendor management
├── Naner.DependencyInjection/     # Service container
└── Naner.Tests/                   # Unit tests
```

### Dependency Graph

```
                    Naner.Launcher (Entry Point)
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            ▼            ▼
       Naner.Commands  Naner.Setup  Naner.Configuration
              │            │            │
              └────────────┼────────────┘
                           │
                           ▼
                     Naner.Core
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            ▼            ▼
    Naner.Infrastructure  Naner.Vendors  Naner.Archives
```

## Key Components

### 1. Entry Point (Naner.Launcher)

**Program.cs** - Main entry point
- Console attachment for CLI commands
- Command routing via `CommandRouter`
- First-run detection
- Launch option parsing with CommandLineParser

```csharp
static int Main(string[] args)
{
    // Console attachment if needed
    if (CommandRouter.NeedsConsole(args) || FirstRunDetector.IsFirstRun())
    {
        ConsoleManager.Instance.EnsureConsoleAttached();
    }

    // Route commands
    var router = new CommandRouter();
    var result = router.Route(args);

    if (result != -1) return result;

    // Default: parse launch options and run
    return Parser.Default.ParseArguments<LaunchOptions>(args)
        .MapResult(RunLauncher, errs => 1);
}
```

### 2. Command System (Naner.Commands)

**ICommand Interface**
```csharp
public interface ICommand
{
    int Execute(string[] args);
}
```

**Available Commands:**
- `InitCommand` - Initialize installation (deprecated, directs to naner-init)
- `HelpCommand` - Display usage help
- `VersionCommand` - Display version info
- `DiagnosticsCommand` - System health checks
- `SetupVendorsCommand` - Download vendor dependencies

**CommandRouter** - Central dispatcher
```csharp
public int Route(string[] args)
{
    // Maps CLI arguments to ICommand implementations
    // Returns -1 if no command matched (fall through to launcher)
}
```

### 3. Configuration System (Naner.Configuration)

**Multi-Provider Architecture**
- `JsonConfigurationProvider` (Priority 10) - JSON files
- `YamlConfigurationProvider` (Priority 20) - YAML files
- `EnvironmentConfigurationProvider` (Priority 30) - Environment variables

**Auto-Discovery**
Configuration files are searched in order:
1. `naner.json`
2. `naner.yaml`
3. `naner.yml`

**Configuration Model**
```csharp
public class NanerConfig
{
    public Dictionary<string, string> VendorPaths { get; set; }
    public EnvironmentConfig Environment { get; set; }
    public string DefaultProfile { get; set; }
    public Dictionary<string, ProfileConfig> Profiles { get; set; }
    public WindowsTerminalConfig WindowsTerminal { get; set; }
    public AdvancedConfig Advanced { get; set; }
}
```

### 4. Terminal Launching (Naner.Launcher)

**TerminalLauncher** - Core launch logic
1. Resolve NANER_ROOT directory
2. Load configuration
3. Build unified PATH
4. Set environment variables
5. Launch Windows Terminal with profile

**PATH Management**
```csharp
var pathBuilder = new PathBuilder(nanerRoot);
pathBuilder
    .Add("bin")
    .Add("vendor/msys64/mingw64/bin")
    .Add("vendor/msys64/usr/bin")
    .Add("vendor/powershell")
    .Add("opt");

if (config.Environment.InheritSystemPath)
    pathBuilder.AddSystemPath();

Environment.SetEnvironmentVariable("PATH", pathBuilder.Build());
```

### 5. Vendor Management (Naner.Vendors)

**VendorDefinition Model**
```csharp
public class VendorDefinition
{
    public string Name { get; set; }
    public string ExtractDir { get; set; }
    public string SourceType { get; set; }  // GitHub, StaticUrl, etc.
    public string GitHubOwner { get; set; }
    public string GitHubRepo { get; set; }
    public string AssetPattern { get; set; }
    public List<string> Dependencies { get; set; }
}
```

**VendorConfigurationLoader**
- Loads from `config/vendors.json`
- Provides default vendors if file missing
- Supports vendor dependencies

### 6. Infrastructure Services

**HttpDownloadService**
- File downloads with progress tracking
- Custom headers for GitHub API
- Cancellation support

**ArchiveExtractorService**
- Strategy pattern for different formats
- Supports: .zip, .tar.xz, .msi
- Uses 7-Zip for .tar.xz extraction

**ConsoleManager**
- Windows console API abstraction
- Attach/allocate/detach console
- Singleton pattern for consistent state

### 7. Setup System (Naner.Setup)

**FirstRunDetector**
- Checks for `.naner-initialized` marker
- Determines if setup needed

**SetupManager**
- Interactive setup wizard
- Minimal setup mode
- Directory structure creation
- Default configuration generation

## Build System

### Build Script (build.ps1)

```powershell
# Build self-contained executable
dotnet publish Naner.Launcher/Naner.Launcher.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=true `
    -o ../../vendor/bin
```

### Build Configuration

**Directory.Build.props**
```xml
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <PublishTrimmed>true</PublishTrimmed>
</PropertyGroup>
```

## Testing

### Test Project (Naner.Tests)

- Framework: xUnit 2.8.0
- Assertions: FluentAssertions 6.12.0
- Mocking: Moq 4.20.70

### Test Categories

```
Naner.Tests/
├── Commands/           # Command tests
├── Services/           # Service tests
├── Configuration/      # Config tests
└── Helpers/           # Test utilities (TestLogger)
```

### Running Tests

```powershell
cd src/csharp
dotnet test

# With verbosity
dotnet test --logger "console;verbosity=detailed"

# Specific category
dotnet test --filter "FullyQualifiedName~Commands"
```

## Design Patterns

1. **Command Pattern** - CLI command encapsulation
2. **Strategy Pattern** - Archive extraction, configuration providers
3. **Adapter Pattern** - Logger static facade
4. **Dependency Injection** - Constructor injection throughout
5. **Configuration-Driven Design** - Behavior via config files
6. **Singleton Pattern** - ConsoleManager

## Extension Points

### Adding New Commands

1. Implement `ICommand` in Naner.Commands
2. Register in `CommandRouter.Route()`
3. Add tests in Naner.Tests/Commands/

### Adding Configuration Providers

1. Implement `IConfigurationProvider`
2. Register in `ConfigurationProviderService`
3. Set appropriate priority

### Adding Vendor Sources

1. Add source type in `VendorDefinition.SourceType`
2. Implement download logic in vendor installer
3. Update `vendors.json` schema

## Performance Considerations

- **Startup**: 100-200ms cold start
- **Memory**: ~15 MB before terminal launch
- **Executable Size**: ~11 MB (trimmed, single-file)
- **Build Time**: ~1.4s for full solution
- **Test Execution**: 30ms for 19 tests

## Security

- HTTPS enforced for downloads
- Path validation to prevent traversal
- Process isolation for external tools
- Guard clauses on public methods

## References

- [Architecture Overview](ARCHITECTURE.md) - Design patterns and service catalog
- [Refactoring Documentation](../development/REFACTORING_COMPLETE.md) - Phase-by-phase breakdown
