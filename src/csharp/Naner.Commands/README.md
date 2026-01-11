# Naner.Commands

Command Pattern infrastructure for the Naner terminal launcher.

## Purpose

This project implements the Command Pattern to provide a clean, extensible way to handle CLI commands. All command-related functionality was extracted from Naner.Launcher (Phase 7) to improve modularity and separation of concerns.

## Architecture

### Abstractions

**ICommand**
```csharp
public interface ICommand
{
    int Execute(string[] args);
}
```
Core command interface. All commands must implement this contract.

**IDiagnosticsService**
```csharp
public interface IDiagnosticsService
{
    int Run();
}
```
Interface for diagnostic health checks.

**ITerminalLauncher** & **ITerminalLauncherFactory**
```csharp
public interface ITerminalLauncher
{
    int LaunchProfile(string profileName, string? startingDirectory = null);
}

public interface ITerminalLauncherFactory
{
    ITerminalLauncher Create(string nanerRoot, bool debugMode = false);
}
```
Abstractions for terminal launching (used to break circular dependencies).

### Implementations

All command implementations follow a consistent pattern:

1. Implement `ICommand` interface
2. `Execute(string[] args)` is the entry point
3. Return 0 for success, non-zero for errors
4. Use `Logger` for output

**Available Commands:**

- **InitCommand**: Initialize new Naner installation (deprecated, directs to naner-init)
- **HelpCommand**: Display help text
- **VersionCommand**: Display version information
- **DiagnosticsCommand**: Run system diagnostics
- **SetupVendorsCommand**: Download and install vendor dependencies

### Services

**CommandRouter**
```csharp
public class CommandRouter
{
    public int Route(string[] args)
    {
        // Maps command strings to ICommand implementations
    }

    public static bool NeedsConsole(string[] args)
    {
        // Determines if command requires console output
    }
}
```
Central command dispatcher. Routes command-line arguments to appropriate command handlers.

**DiagnosticsService**
```csharp
public class DiagnosticsService : IDiagnosticsService
{
    // Orchestrates diagnostic checks
    // Delegates to specialized verifiers
}
```
Coordinates system health checks. Split into focused services (Phase 6):
- `DirectoryVerifier`: Validates directory structure
- `ConfigurationVerifier`: Validates config files and vendor paths
- `EnvironmentReporter`: Reports environment variables

**HelpTextProvider**
```csharp
public class HelpTextProvider
{
    public void ShowHelp()
    {
        // Displays formatted help text
    }
}
```
Centralizes help text formatting and display.

## Usage

### Adding a New Command

1. Create a new class in `Implementations/` that implements `ICommand`
2. Add command name constant to `CommandNames.cs`
3. Register in `CommandRouter.Route()` method
4. Write unit tests in Naner.Tests/Commands/

Example:

```csharp
namespace Naner.Commands.Implementations;

public class MyCommand : ICommand
{
    public int Execute(string[] args)
    {
        Logger.Header("My Command");
        // Command logic here
        return 0;
    }
}
```

Register in CommandRouter:
```csharp
public int Route(string[] args)
{
    // ...existing code...

    if (args.Length > 0 && args[0].ToLower() == "mycommand")
    {
        return new MyCommand().Execute(args.Skip(1).ToArray());
    }

    // ...rest of routing...
}
```

### Using CommandRouter

From Program.cs or other entry points:

```csharp
var router = new CommandRouter();
var exitCode = router.Route(args);

if (exitCode != -1)
{
    // Command was handled
    return exitCode;
}

// No command matched, proceed with default behavior
```

## Design Patterns

### Command Pattern
Each command is encapsulated as an object, allowing:
- Easy addition of new commands
- Command logic isolation
- Consistent error handling
- Testable command implementations

### Single Responsibility Principle
Each command class has one responsibility:
- `InitCommand`: Setup orchestration
- `HelpCommand`: Help text display
- `DiagnosticsCommand`: System checks
- etc.

### Dependency Inversion
Commands depend on abstractions (`IConfigurationManager`, etc.) rather than concrete implementations, enabling easier testing and flexibility.

## Dependencies

- **Naner.Core**: Path utilities, constants
- **Naner.Configuration**: Configuration management
- **Naner.Configuration.Abstractions**: Configuration interfaces
- **Naner.Setup**: Installation utilities
- **Naner.Vendors**: Vendor management
- **Naner.Infrastructure**: Logging

## Testing

All commands have comprehensive unit tests in `Naner.Tests/Commands/`. Tests verify:
- Correct interface implementation
- Return codes (success/failure)
- Output messages
- Error handling
- Namespace correctness

Run tests:
```bash
dotnet test Naner.sln --filter "FullyQualifiedName~Commands"
```

## History

**Phase 7 (2025-01-11)**: Extracted from Naner.Launcher
- Moved all command infrastructure to dedicated project
- Broke circular dependency with InitCommand
- Improved modularity score to 10/10

**Phase 6 (2025-01-11)**: Split DiagnosticsService
- Separated diagnostic concerns into focused services
- Applied Single Responsibility Principle

## Future Enhancements

1. **Plugin System**: Support dynamically loaded command plugins
2. **Async Commands**: Add `IAsyncCommand` for async operations
3. **Command Validation**: Add parameter validation framework
4. **Command Help**: Per-command help text support
5. **Command Aliases**: Support command shortcuts
