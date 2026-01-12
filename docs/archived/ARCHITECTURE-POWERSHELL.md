# Naner Architecture Overview

**Version:** 1.0.0
**Last Updated:** 2026-01-10
**Status:** Production Ready

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Architecture Principles](#architecture-principles)
3. [Project Structure](#project-structure)
4. [Layer Architecture](#layer-architecture)
5. [Service Catalog](#service-catalog)
6. [Design Patterns](#design-patterns)
7. [Data Flow](#data-flow)
8. [Extension Points](#extension-points)

---

## System Overview

Naner is a unified terminal environment manager for Windows that provides:
- Terminal profile management
- Vendor package installation (PowerShell, Git Bash, Windows Terminal, etc.)
- Environment configuration and path management
- Automated vendor downloads and extraction

### Core Capabilities

```
┌─────────────────────────────────────────────┐
│              Naner System                    │
├─────────────────────────────────────────────┤
│                                              │
│  ┌──────────────┐  ┌──────────────┐        │
│  │   Terminal   │  │    Vendor    │        │
│  │   Launcher   │  │   Installer  │        │
│  └──────┬───────┘  └──────┬───────┘        │
│         │                  │                 │
│         ├──────────────────┤                │
│         │                  │                 │
│  ┌──────▼──────┐  ┌───────▼───────┐        │
│  │Configuration│  │   Downloads   │        │
│  │  Management │  │  & Extraction │        │
│  └─────────────┘  └───────────────┘        │
│                                              │
└─────────────────────────────────────────────┘
```

---

## Architecture Principles

### SOLID Principles

**1. Single Responsibility Principle (SRP)**
- Each service has ONE clear responsibility
- Commands handle ONE specific command
- Example: `HttpDownloadService` only downloads, `ArchiveExtractorService` only extracts

**2. Open/Closed Principle (OCP)**
- Open for extension: Add new commands by implementing `ICommand`
- Closed for modification: Add new vendors via `vendors.json` (no code changes)
- Example: Strategy pattern in `ArchiveExtractorService` allows new formats without modification

**3. Liskov Substitution Principle (LSP)**
- Any `ILogger` implementation can substitute `ConsoleLogger`
- All interface implementations are substitutable
- Example: `TestLogger` substitutes `ConsoleLogger` in tests

**4. Interface Segregation Principle (ISP)**
- Interfaces are minimal and focused
- Example: `ICommand` has single `Execute()` method
- Example: `ILogger` has only logging methods (no unrelated functionality)

**5. Dependency Inversion Principle (DIP)**
- High-level modules depend on abstractions, not implementations
- Example: Services accept `ILogger`, not `ConsoleLogger`
- Example: `CommandRouter` depends on `ICommand` abstraction

### Clean Architecture

```
┌───────────────────────────────────────┐
│         Presentation Layer            │
│  (Commands, CLI Interface)            │
└─────────────┬─────────────────────────┘
              │
┌─────────────▼─────────────────────────┐
│         Application Layer             │
│  (CommandRouter, Services)            │
└─────────────┬─────────────────────────┘
              │
┌─────────────▼─────────────────────────┐
│         Domain Layer                  │
│  (Models, Abstractions)               │
└─────────────┬─────────────────────────┘
              │
┌─────────────▼─────────────────────────┐
│       Infrastructure Layer            │
│  (File System, HTTP, Windows APIs)    │
└───────────────────────────────────────┘
```

---

## Project Structure

### Physical Organization

```
Naner/
├── src/csharp/
│   ├── Naner.Common/              # Shared utilities (Core layer)
│   ├── Naner.Configuration/       # Config management (Domain layer)
│   ├── Naner.Launcher/            # Terminal launcher (Application layer)
│   ├── Naner.Init/                # Bootstrap tool (Application layer)
│   └── Naner.Tests/               # Unit tests
│
├── config/                         # Configuration files
│   ├── naner.json                 # Main configuration
│   └── vendors.json               # Vendor definitions
│
└── docs/                          # Documentation
    ├── ARCHITECTURE.md            # This file
    ├── REFACTORING_COMPLETE.md   # Refactoring summary
    └── PHASE*_SUMMARY.md         # Phase summaries
```

### Logical Organization

```
┌─────────────────────────────────────────────┐
│            Naner.Launcher                    │
│  (Entry point, command routing)             │
└──────────────────┬──────────────────────────┘
                   │
        ┌──────────┴──────────┐
        │                     │
┌───────▼─────────┐  ┌───────▼──────────┐
│ Naner.Config    │  │   Naner.Init     │
│ (Config mgmt)   │  │  (Bootstrap)     │
└───────┬─────────┘  └───────┬──────────┘
        │                     │
        └──────────┬──────────┘
                   │
        ┌──────────▼──────────┐
        │   Naner.Common      │
        │  (Services, Models) │
        └─────────────────────┘

        ┌─────────────────────┐
        │   Naner.Tests       │
        │  (Unit Tests)       │
        └─────────────────────┘
              │  │  │
              ▼  ▼  ▼
          All Projects
```

---

## Layer Architecture

### Layer 1: Abstractions (Interfaces)

**Location:** `Naner.Common/Abstractions/`, `Naner.Configuration/Abstractions/`, `Naner.Launcher/Abstractions/`

**Purpose:** Define contracts for services

**Interfaces:**
- `ILogger` - Logging abstraction
- `IConfigurationManager` - Configuration management
- `ITerminalLauncher` - Terminal launching
- `IVendorInstaller` - Vendor installation
- `IGitHubClient` - GitHub API access

**Benefits:**
- Enables dependency injection
- Allows mocking in tests
- Supports multiple implementations

### Layer 2: Models (Domain)

**Location:** `Naner.Common/Models/`, `Naner.Configuration/Models/`

**Purpose:** Define data structures

**Models:**
- `VendorDefinition` - Vendor package metadata
- `VendorConfiguration` - Vendor config root
- `NanerConfig` - Main configuration
- `ProfileConfig` - Terminal profile
- `GitHubRelease` - GitHub release data
- `GitHubAsset` - GitHub asset data

### Layer 3: Services (Application Logic)

**Location:** `Naner.Common/Services/`, `Naner.Configuration/`, `Naner.Launcher/Services/`

**Purpose:** Implement business logic

**Services:**
- `ConsoleLogger` - Console output implementation
- `HttpDownloadService` - File downloads
- `ArchiveExtractorService` - Archive extraction
- `ConsoleManager` - Windows console API
- `VendorConfigurationLoader` - Config loading
- `ConfigurationManager` - Main config management
- `CommandRouter` - Command dispatch

### Layer 4: Commands (Presentation)

**Location:** `Naner.Launcher/Commands/`

**Purpose:** CLI command handling

**Commands:**
- `VersionCommand` - Display version
- `HelpCommand` - Display help
- `DiagnosticsCommand` - System diagnostics

### Layer 5: Entry Points

**Location:** `Naner.Launcher/Program.cs`, `Naner.Init/Program.cs`

**Purpose:** Application bootstrapping

---

## Service Catalog

### Core Services

#### ILogger / ConsoleLogger
**Responsibility:** Logging output to console
**Pattern:** Adapter (Logger static facade)
**Location:** `Naner.Common/Abstractions/ILogger.cs`, `Naner.Common/Logger.cs`

**Methods:**
```csharp
void Status(string message);      // Cyan [*]
void Success(string message);     // Green [OK]
void Failure(string message);     // Red [✗]
void Info(string message);        // Gray (indented)
void Warning(string message);     // Yellow [!]
void Debug(string, bool);         // Yellow [DEBUG] (conditional)
void NewLine();                   // Blank line
void Header(string header);       // Cyan header with separator
```

**Usage:**
```csharp
// Static facade (backward compatible)
Logger.Status("Working...");

// Or via interface (testable)
ILogger logger = new ConsoleLogger();
logger.Success("Done!");
```

---

#### HttpDownloadService
**Responsibility:** Download files over HTTP
**Pattern:** Service with dependency injection
**Location:** `Naner.Common/Services/HttpDownloadService.cs`

**Methods:**
```csharp
Task<bool> DownloadFileAsync(string url, string outputPath, string? displayName = null, ...);
Task<bool> DownloadFileWithCustomHeadersAsync(string url, string outputPath, ...);
void AddHeader(string name, string value);
```

**Features:**
- Progress tracking
- Custom HTTP headers (for GitHub API)
- Cancellation token support
- Automatic directory creation

**Usage:**
```csharp
var downloader = new HttpDownloadService(logger);
await downloader.DownloadFileAsync(
    "https://example.com/file.zip",
    "c:\\downloads\\file.zip",
    "My File");
```

---

#### ArchiveExtractorService
**Responsibility:** Extract various archive formats
**Pattern:** Strategy pattern
**Location:** `Naner.Common/Services/ArchiveExtractorService.cs`

**Methods:**
```csharp
Task<bool> ExtractArchiveAsync(string archivePath, string destinationPath, ...);
```

**Supported Formats:**
- `.zip` (using .NET ZipFile)
- `.tar.xz` (using 7-Zip, two-pass)
- `.msi` (using msiexec)

**Strategy Selection:**
```csharp
var success = extension switch
{
    ".zip" => await ExtractZipAsync(...),
    ".xz" => await ExtractTarXzAsync(...),
    ".msi" => await ExtractMsiAsync(...),
    _ => throw new NotSupportedException()
};
```

---

#### ConsoleManager
**Responsibility:** Windows console API abstraction
**Pattern:** Service wrapper
**Location:** `Naner.Common/Services/ConsoleManager.cs`

**Methods:**
```csharp
bool EnsureConsoleAttached();
bool AttachToParentConsole();
bool AllocateNewConsole();
bool DetachConsole();
bool HasConsole { get; }
static bool NeedsConsole(string[] args, string[] consoleCommands);
```

**Usage:**
```csharp
var consoleManager = new ConsoleManager();
if (CommandRouter.NeedsConsole(args))
{
    consoleManager.EnsureConsoleAttached();
}
```

---

#### VendorConfigurationLoader
**Responsibility:** Load vendor definitions from config
**Pattern:** Configuration-driven design
**Location:** `Naner.Common/Services/VendorConfigurationLoader.cs`

**Methods:**
```csharp
List<VendorDefinition> LoadVendors();
```

**Features:**
- Loads from `config/vendors.json`
- Graceful fallback to default vendors
- JSON parsing with error handling
- Support for comments and trailing commas

**Usage:**
```csharp
var loader = new VendorConfigurationLoader(nanerRoot, logger);
var vendors = loader.LoadVendors(); // 4 default vendors if file missing
```

---

#### ConfigurationManager
**Responsibility:** Main configuration management
**Interface:** `IConfigurationManager`
**Location:** `Naner.Configuration/ConfigurationManager.cs`

**Methods:**
```csharp
NanerConfig Load(string? configPath = null);
NanerConfig Config { get; }
ProfileConfig GetProfile(string profileName);
string BuildUnifiedPath(bool includeSystemPath = true);
void SetupEnvironmentVariables();
```

---

#### CommandRouter
**Responsibility:** Route CLI arguments to commands
**Pattern:** Command pattern + Router pattern
**Location:** `Naner.Launcher/Services/CommandRouter.cs`

**Methods:**
```csharp
int Route(string[] args);
static bool NeedsConsole(string[] args);
```

**Command Registration:**
```csharp
_commands = new Dictionary<string, ICommand>
{
    ["--version"] = new VersionCommand(),
    ["--help"] = new HelpCommand(),
    ["--diagnose"] = new DiagnosticsCommand()
};
```

---

## Design Patterns

### 1. Adapter Pattern

**Implementation:** `Logger` static facade
**Purpose:** Maintain backward compatibility while enabling DI

```csharp
public static class Logger
{
    private static ILogger _instance = new ConsoleLogger();

    public static void SetLogger(ILogger logger)
        => _instance = logger;

    public static void Status(string message)
        => _instance.Status(message);
}
```

**Benefits:**
- Existing code using `Logger.Status()` still works
- New code can use `ILogger` for DI
- Tests can use `Logger.SetLogger(mockLogger)`

---

### 2. Strategy Pattern

**Implementation:** `ArchiveExtractorService`
**Purpose:** Different extraction strategies per format

```csharp
private Task<bool> ExtractZipAsync(...) { }
private Task<bool> ExtractTarXzAsync(...) { }
private Task<bool> ExtractMsiAsync(...) { }
```

**Benefits:**
- Easy to add new formats
- Each strategy is isolated
- Open/Closed Principle compliance

---

### 3. Command Pattern

**Implementation:** `ICommand` interface and commands
**Purpose:** Encapsulate commands as objects

```csharp
public interface ICommand
{
    int Execute(string[] args);
}

public class VersionCommand : ICommand { }
public class HelpCommand : ICommand { }
public class DiagnosticsCommand : ICommand { }
```

**Benefits:**
- Commands are testable
- Easy to add new commands
- Single Responsibility Principle

---

### 4. Dependency Injection

**Implementation:** Constructor injection throughout
**Purpose:** Loose coupling, testability

```csharp
public VendorConfigurationLoader(string nanerRoot, ILogger logger)
{
    _logger = logger;
}
```

**Benefits:**
- Services are mockable
- Easy to swap implementations
- Dependency Inversion Principle

---

### 5. Configuration-Driven Design

**Implementation:** `VendorConfigurationLoader`, `vendors.json`
**Purpose:** Behavior controlled by config, not code

```json
{
  "vendors": [
    {
      "name": "PowerShell",
      "sourceType": "GitHub",
      "gitHubOwner": "PowerShell",
      "gitHubRepo": "PowerShell"
    }
  ]
}
```

**Benefits:**
- Add vendors without code changes
- Open/Closed Principle
- User customization

---

### 6. Repository Pattern (Partial)

**Implementation:** `IGitHubClient`, `IConfigurationManager`
**Purpose:** Abstract external data sources

**Benefits:**
- Isolate data access
- Easy to mock in tests
- Swap implementations

---

## Data Flow

### Terminal Launch Flow

```
User runs naner.exe
        │
        ▼
┌───────────────┐
│  Program.cs   │ Entry point
└───────┬───────┘
        │
        ▼
┌───────────────┐
│ConsoleManager │ Attach console if needed
└───────┬───────┘
        │
        ▼
┌───────────────┐
│CommandRouter  │ Route to command
└───────┬───────┘
        │
        ├──────────────┬──────────────┐
        │              │              │
        ▼              ▼              ▼
  ┌─────────┐  ┌──────────┐  ┌──────────┐
  │ Version │  │   Help   │  │Diagnostic│
  │ Command │  │ Command  │  │ Command  │
  └─────────┘  └──────────┘  └──────────┘
        │              │              │
        └──────────────┴──────────────┘
                       │
                       ▼
                 Exit with code
```

### Vendor Installation Flow

```
User requests vendor install
        │
        ▼
┌──────────────────────┐
│VendorConfiguration  │ Load vendors.json
│Loader                │ (or use defaults)
└──────────┬───────────┘
           │
           ▼
     ┌────────────┐
     │  Vendor    │ For each vendor...
     │Definition  │
     └─────┬──────┘
           │
           ▼
┌──────────────────────┐
│HttpDownloadService   │ Download package
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│ArchiveExtractor     │ Extract archive
│Service               │
└──────────┬───────────┘
           │
           ▼
     Vendor installed
```

---

## Extension Points

### Adding New Commands

1. Create command class implementing `ICommand`:
```csharp
public class MyCommand : ICommand
{
    public int Execute(string[] args)
    {
        // Implementation
        return 0;
    }
}
```

2. Register in `CommandRouter`:
```csharp
_commands["mycommand"] = new MyCommand();
```

### Adding New Archive Formats

1. Add new strategy method in `ArchiveExtractorService`:
```csharp
private Task<bool> ExtractRarAsync(string archivePath, ...)
{
    // Implementation
}
```

2. Add to switch statement:
```csharp
".rar" => await ExtractRarAsync(...),
```

### Adding New Vendors

1. Edit `config/vendors.json`:
```json
{
  "vendors": [
    {
      "name": "My Tool",
      "extractDir": "mytool",
      "sourceType": "GitHub",
      "gitHubOwner": "owner",
      "gitHubRepo": "repo"
    }
  ]
}
```

**No code changes required!**

---

## Testing Architecture

### Test Project Structure

```
Naner.Tests/
├── Helpers/
│   └── TestLogger.cs          # ILogger test implementation
├── Services/
│   ├── ConsoleManagerTests.cs
│   └── VendorConfigurationLoaderTests.cs
├── Commands/
│   └── VersionCommandTests.cs
└── NanerConstantsTests.cs
```

### Test Patterns

**Arrange-Act-Assert (AAA):**
```csharp
[Fact]
public void Test_Method_Behavior()
{
    // Arrange
    var logger = new TestLogger();
    var service = new MyService(logger);

    // Act
    var result = service.DoWork();

    // Assert
    result.Should().BeTrue();
}
```

**Theory-Based Testing:**
```csharp
[Theory]
[InlineData("--version", true)]
[InlineData("launch", false)]
public void NeedsConsole_Returns_Expected(string cmd, bool expected)
{
    var result = CommandRouter.NeedsConsole(new[] { cmd });
    result.Should().Be(expected);
}
```

---

## Performance Considerations

### Build Time
- **Current:** ~1.4s for full solution
- **Acceptable** for improved architecture

### Test Execution
- **Current:** 30ms for 19 tests
- **Excellent** for fast feedback

### Runtime
- Service abstractions add **negligible overhead**
- Strategy pattern has **zero runtime cost** (compile-time dispatch)

---

## Security Considerations

### Input Validation
- All public methods validate parameters
- Guard clauses with `ArgumentNullException`
- Path validation to prevent directory traversal

### Download Security
- HTTPS URLs enforced where possible
- Custom User-Agent for transparency
- Timeout limits prevent hanging

### Archive Extraction
- Destination path validation
- Cleanup of temporary files
- Process isolation for external tools (7-Zip, msiexec)

---

## Conclusion

Naner's architecture demonstrates:

✅ **SOLID Principles** throughout
✅ **Clean Architecture** with clear layers
✅ **Design Patterns** for common problems
✅ **Testability** through dependency injection
✅ **Extensibility** through configuration
✅ **Maintainability** through documentation

This architecture supports:
- Easy testing
- Future growth
- Team collaboration
- Code quality
- Production deployment

---

**Architecture Status:** ✅ **Production Ready**
**Last Review:** 2026-01-10
**Next Review:** Quarterly or when major features added

