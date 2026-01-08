# Phase 10.5: First-Run Experience & Initial Launch

**Date:** 2026-01-08
**Status:** Planning
**Priority:** High (enables standalone distribution)

---

## Executive Summary

Phase 10.5 adds intelligent first-run detection, setup wizard, and initialization capabilities to make naner.exe self-contained and easier to deploy.

**Goal:** Enable naner.exe to initialize its own environment, detect missing dependencies, and guide users through first-time setup.

---

## Problem Statement

### Current Limitations

1. **Requires Existing Installation**
   - naner.exe expects bin/, vendor/, config/ to already exist
   - Can't run standalone or bootstrap itself
   - No first-run detection

2. **No Setup Wizard**
   - Users must manually create directory structure
   - No guided configuration
   - Missing dependencies aren't detected

3. **No Graceful Degradation**
   - All or nothing approach
   - Can't function without full Naner installation
   - No minimal/portable mode

---

## Solution Design

### 1. First-Run Detection ⭐ PRIORITY #1

**Goal:** Detect when naner.exe is being run for the first time or outside a Naner installation.

#### 1.1 Detection Logic

```csharp
public static class FirstRunDetector
{
    public static bool IsFirstRun(string? nanerRoot = null)
    {
        // Check if NANER_ROOT can be found
        if (string.IsNullOrEmpty(nanerRoot))
        {
            try
            {
                nanerRoot = PathResolver.FindNanerRoot();
            }
            catch (DirectoryNotFoundException)
            {
                return true; // First run - no installation found
            }
        }

        // Check for first-run marker file
        var markerFile = Path.Combine(nanerRoot, ".naner-initialized");
        if (!File.Exists(markerFile))
        {
            return true; // First run - installation not initialized
        }

        // Check for essential directories
        var essentialDirs = new[] { "bin", "vendor", "config", "home" };
        foreach (var dir in essentialDirs)
        {
            var path = Path.Combine(nanerRoot, dir);
            if (!Directory.Exists(path))
            {
                return true; // First run - incomplete installation
            }
        }

        // Check for config file
        var configFile = Path.Combine(nanerRoot, "config", "naner.json");
        if (!File.Exists(configFile))
        {
            return true; // First run - missing configuration
        }

        return false; // Not first run
    }
}
```

#### 1.2 Smart Fallback

When NANER_ROOT can't be found, try to establish it:

```csharp
public static string EstablishNanerRoot()
{
    // Try 1: Current directory
    var currentDir = Environment.CurrentDirectory;
    if (IsValidNanerRoot(currentDir))
    {
        return currentDir;
    }

    // Try 2: Executable directory
    var exeDir = AppContext.BaseDirectory;
    if (IsValidNanerRoot(exeDir))
    {
        return exeDir;
    }

    // Try 3: Parent of executable directory
    var parentDir = Directory.GetParent(exeDir)?.FullName;
    if (parentDir != null && IsValidNanerRoot(parentDir))
    {
        return parentDir;
    }

    // Try 4: Environment variable
    var envRoot = Environment.GetEnvironmentVariable("NANER_ROOT");
    if (!string.IsNullOrEmpty(envRoot) && IsValidNanerRoot(envRoot))
    {
        return envRoot;
    }

    // Try 5: User profile subdirectory
    var profileNaner = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".naner");
    if (IsValidNanerRoot(profileNaner))
    {
        return profileNaner;
    }

    // No valid root found - offer to create
    return null;
}
```

---

### 2. Setup Wizard ⭐ PRIORITY #2

**Goal:** Guide users through first-time setup with interactive wizard.

#### 2.1 Welcome Screen

```csharp
static void ShowWelcome()
{
    Console.Clear();
    Logger.Header("Welcome to Naner Terminal Launcher!");
    Console.WriteLine();
    Console.WriteLine("Naner is a portable terminal environment for Windows.");
    Console.WriteLine("It provides:");
    Console.WriteLine("  • Portable PowerShell, Bash, and CMD environments");
    Console.WriteLine("  • Unified tool management (Git, Node, Python, etc.)");
    Console.WriteLine("  • Consistent configuration across machines");
    Console.WriteLine();
    Console.WriteLine("This appears to be your first time running Naner.");
    Console.WriteLine("Let's set up your environment!");
    Console.WriteLine();
}
```

#### 2.2 Installation Location Choice

```csharp
static string PromptInstallLocation()
{
    Console.WriteLine("Where would you like to install Naner?");
    Console.WriteLine();
    Console.WriteLine("  1. Current directory (recommended if you have write access)");
    Console.WriteLine($"     {Environment.CurrentDirectory}");
    Console.WriteLine();
    Console.WriteLine("  2. User profile (~/.naner)");
    Console.WriteLine($"     {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".naner")}");
    Console.WriteLine();
    Console.WriteLine("  3. Custom location (you specify)");
    Console.WriteLine();
    Console.Write("Enter choice (1-3): ");

    var choice = Console.ReadLine()?.Trim();

    return choice switch
    {
        "1" => Environment.CurrentDirectory,
        "2" => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".naner"),
        "3" => PromptCustomLocation(),
        _ => Environment.CurrentDirectory
    };
}

static string PromptCustomLocation()
{
    Console.Write("Enter installation path: ");
    var path = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(path))
    {
        return Environment.CurrentDirectory;
    }

    try
    {
        var fullPath = Path.GetFullPath(path);
        return fullPath;
    }
    catch
    {
        Logger.Warning("Invalid path, using current directory");
        return Environment.CurrentDirectory;
    }
}
```

#### 2.3 Directory Structure Creation

```csharp
static bool CreateNanerStructure(string nanerRoot)
{
    Logger.Status($"Creating Naner directory structure at: {nanerRoot}");
    Console.WriteLine();

    try
    {
        // Create main directory
        if (!Directory.Exists(nanerRoot))
        {
            Directory.CreateDirectory(nanerRoot);
            Logger.Success($"Created: {nanerRoot}");
        }

        // Create subdirectories
        var directories = new[]
        {
            "bin",
            "vendor",
            "config",
            "home",
            "home/.ssh",
            "home/.config",
            "home/.config/git",
            "home/Templates",
            "home/.vscode",
            "plugins",
            "logs"
        };

        foreach (var dir in directories)
        {
            var path = Path.Combine(nanerRoot, dir);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ Created: {dir}/");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
        Logger.Success("Directory structure created successfully");
        return true;
    }
    catch (Exception ex)
    {
        Logger.Failure($"Failed to create directory structure: {ex.Message}");
        return false;
    }
}
```

#### 2.4 Default Configuration Generation

```csharp
static bool CreateDefaultConfiguration(string nanerRoot)
{
    Logger.Status("Creating default configuration...");
    Console.WriteLine();

    var configPath = Path.Combine(nanerRoot, "config", "naner.json");

    var defaultConfig = new
    {
        VendorPaths = new Dictionary<string, string>
        {
            ["SevenZip"] = "%NANER_ROOT%\\vendor\\7-Zip\\7z.exe",
            ["PowerShell"] = "%NANER_ROOT%\\vendor\\PowerShell\\pwsh.exe",
            ["WindowsTerminal"] = "%NANER_ROOT%\\vendor\\WindowsTerminal\\wt.exe",
            ["GitBash"] = "%NANER_ROOT%\\vendor\\msys64\\usr\\bin\\bash.exe"
        },
        Environment = new
        {
            PathPrecedence = new[]
            {
                "%NANER_ROOT%\\bin",
                "%NANER_ROOT%\\vendor\\PowerShell",
                "%NANER_ROOT%\\vendor\\msys64\\usr\\bin",
                "%NANER_ROOT%\\vendor\\msys64\\mingw64\\bin"
            },
            EnvironmentVariables = new Dictionary<string, string>
            {
                ["NANER_ROOT"] = "%NANER_ROOT%",
                ["NANER_HOME"] = "%NANER_ROOT%\\home",
                ["HOME"] = "%NANER_ROOT%\\home"
            }
        },
        DefaultProfile = "Unified",
        Profiles = new Dictionary<string, object>
        {
            ["Unified"] = new
            {
                Name = "Unified Terminal",
                Description = "All tools available - PowerShell, Git, and Unix utilities",
                Shell = "PowerShell",
                StartingDirectory = "%USERPROFILE%",
                UseVendorPath = true,
                ColorScheme = "Campbell"
            },
            ["PowerShell"] = new
            {
                Name = "PowerShell",
                Description = "PowerShell with Naner tools",
                Shell = "PowerShell",
                StartingDirectory = "%USERPROFILE%",
                UseVendorPath = true,
                ColorScheme = "Campbell"
            },
            ["Bash"] = new
            {
                Name = "Git Bash",
                Description = "Unix-like Bash environment",
                Shell = "Bash",
                StartingDirectory = "%USERPROFILE%",
                UseVendorPath = true,
                ColorScheme = "One Half Dark"
            }
        },
        WindowsTerminal = new
        {
            DefaultTerminal = true,
            LaunchMode = "default",
            TabTitle = "Naner",
            SuppressApplicationTitle = true
        },
        Advanced = new
        {
            PreservePath = false,
            InheritSystemPath = true,
            VerboseLogging = false,
            DebugMode = false
        }
    };

    try
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            defaultConfig,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(configPath, json);

        Logger.Success($"Created: config/naner.json");
        Console.WriteLine();
        return true;
    }
    catch (Exception ex)
    {
        Logger.Failure($"Failed to create configuration: {ex.Message}");
        return false;
    }
}
```

#### 2.5 Initialization Marker

```csharp
static void CreateInitializationMarker(string nanerRoot)
{
    var markerFile = Path.Combine(nanerRoot, ".naner-initialized");
    var markerContent = $@"# Naner Initialization Marker
# Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
# Version: {Version}
# Phase: {PhaseName}

This file indicates that Naner has been initialized.
Do not delete this file unless you want to re-run the setup wizard.
";

    File.WriteAllText(markerFile, markerContent);
}
```

---

### 3. Bootstrap Mode ⭐ PRIORITY #3

**Goal:** Allow naner.exe to run in minimal mode without full installation.

#### 3.1 Bootstrap Command

```csharp
[Verb("init", HelpText = "Initialize Naner installation")]
class InitOptions
{
    [Option('p', "path", Required = false,
        HelpText = "Installation path (default: current directory)")]
    public string? Path { get; set; }

    [Option("minimal", Required = false,
        HelpText = "Create minimal installation (no vendors)")]
    public bool Minimal { get; set; }

    [Option("interactive", Required = false, Default = true,
        HelpText = "Interactive setup wizard")]
    public bool Interactive { get; set; }
}
```

#### 3.2 Minimal Installation Mode

```csharp
static int RunBootstrap(InitOptions opts)
{
    if (opts.Interactive)
    {
        return RunInteractiveSetup(opts);
    }
    else
    {
        return RunQuickSetup(opts);
    }
}

static int RunQuickSetup(InitOptions opts)
{
    Logger.Header("Naner Quick Setup");
    Console.WriteLine();

    var nanerRoot = opts.Path ?? Environment.CurrentDirectory;

    Logger.Status($"Installing to: {nanerRoot}");
    Console.WriteLine();

    if (!CreateNanerStructure(nanerRoot))
    {
        return 1;
    }

    if (!CreateDefaultConfiguration(nanerRoot))
    {
        return 1;
    }

    CreateInitializationMarker(nanerRoot);

    Console.WriteLine();
    Logger.Success("Naner installed successfully!");
    Console.WriteLine();

    if (opts.Minimal)
    {
        Logger.Info("Minimal installation complete.");
        Logger.Info("To add vendors, run: Setup-NanerVendor.ps1");
    }
    else
    {
        Logger.Info("Next steps:");
        Logger.Info("  1. Run Setup-NanerVendor.ps1 to install tools");
        Logger.Info("  2. Run naner.exe to launch your terminal");
    }

    Console.WriteLine();
    return 0;
}
```

---

### 4. Enhanced Program.cs Integration

**Goal:** Integrate first-run detection into main program flow.

#### 4.1 Updated Main Method

```csharp
static int Main(string[] args)
{
    // Handle special commands that don't need NANER_ROOT
    if (args.Length > 0)
    {
        var firstArg = args[0].ToLower();

        // Version command
        if (firstArg == "--version" || firstArg == "-v")
        {
            ShowVersion();
            return 0;
        }

        // Help command
        if (firstArg == "--help" || firstArg == "-h" || firstArg == "/?")
        {
            ShowHelp();
            return 0;
        }

        // Diagnostic command
        if (firstArg == "--diagnose")
        {
            return RunDiagnostics();
        }

        // Init command
        if (firstArg == "init" || firstArg == "--init")
        {
            return RunInteractiveSetup(null);
        }
    }

    // Check for first run
    if (FirstRunDetector.IsFirstRun())
    {
        return HandleFirstRun();
    }

    // Normal command parsing
    return Parser.Default.ParseArguments<Options>(args)
        .MapResult(
            opts => RunLauncher(opts),
            errs => 1);
}

static int HandleFirstRun()
{
    Console.WriteLine();
    Logger.Warning("Naner installation not found or incomplete.");
    Console.WriteLine();
    Console.WriteLine("Would you like to:");
    Console.WriteLine("  1. Run setup wizard (recommended)");
    Console.WriteLine("  2. Exit and run manually");
    Console.WriteLine();
    Console.Write("Enter choice (1-2): ");

    var choice = Console.ReadLine()?.Trim();

    if (choice == "1")
    {
        return RunInteractiveSetup(null);
    }

    Logger.Info("Setup cancelled. Run 'naner.exe init' to start setup later.");
    return 1;
}
```

---

## Implementation Plan

### Phase 10.5.1: First-Run Detection (2 hours)
- [ ] Create FirstRunDetector class
- [ ] Implement IsFirstRun() method
- [ ] Add EstablishNanerRoot() method
- [ ] Test detection logic

### Phase 10.5.2: Setup Wizard (4 hours)
- [ ] Create welcome screen
- [ ] Implement installation location prompt
- [ ] Add directory structure creation
- [ ] Generate default configuration
- [ ] Create initialization marker

### Phase 10.5.3: Bootstrap Mode (2 hours)
- [ ] Add InitOptions command
- [ ] Implement RunBootstrap()
- [ ] Create minimal installation mode
- [ ] Add quick setup option

### Phase 10.5.4: Integration (2 hours)
- [ ] Update Main() with first-run check
- [ ] Add HandleFirstRun() method
- [ ] Integrate with existing code
- [ ] Test all pathways

### Phase 10.5.5: Testing & Documentation (2 hours)
- [ ] Test first-run scenarios
- [ ] Test setup wizard
- [ ] Test bootstrap mode
- [ ] Update documentation
- [ ] Create user guide

---

## Usage Examples

### First-Time User

```
C:\> naner.exe

Welcome to Naner Terminal Launcher!

This appears to be your first time running Naner.
Let's set up your environment!

Where would you like to install Naner?
  1. Current directory
  2. User profile (~/.naner)
  3. Custom location

Enter choice (1-3): 1

Creating Naner directory structure...
  ✓ Created: bin/
  ✓ Created: vendor/
  ✓ Created: config/
  ...

Setup complete! Next steps:
  1. Run Setup-NanerVendor.ps1 to install tools
  2. Run naner.exe to launch your terminal
```

### Quick Init

```
C:\> naner.exe init --path C:\tools\naner

Naner Quick Setup
Installing to: C:\tools\naner

Creating directory structure...
Creating default configuration...

Naner installed successfully!
```

### Minimal Install

```
C:\> naner.exe init --minimal

Creating minimal Naner installation...
No vendors will be installed.
Use Setup-NanerVendor.ps1 to add tools later.
```

---

## Success Criteria

- [ ] First-run is detected automatically
- [ ] Setup wizard guides users through installation
- [ ] Directory structure is created correctly
- [ ] Default configuration is valid and functional
- [ ] Initialization marker prevents re-run
- [ ] Bootstrap mode works without existing installation
- [ ] User can complete setup without manual steps

---

## Benefits

### For Users
- **Self-contained**: naner.exe can initialize itself
- **Guided setup**: No manual directory creation
- **Flexible**: Choose installation location
- **Recoverable**: Re-run setup if needed

### For Distribution
- **Single file**: Just distribute naner.exe
- **No prerequisites**: Creates own structure
- **Portable**: Works anywhere with .NET
- **Professional**: Polished first-run experience

---

## Future Enhancements (Phase 10.6+)

### Automatic Vendor Download
- Download required tools during setup
- Progress indicators for downloads
- Retry logic for failed downloads
- Offline mode with manual vendor path

### Configuration Wizard
- Interactive profile creation
- Custom PATH configuration
- Color scheme selection
- Tool selection (minimal vs full)

### Migration Tool
- Import existing Naner PowerShell configs
- Migrate from other terminal managers
- Profile conversion utilities

---

## Conclusion

Phase 10.5 transforms naner.exe from requiring an existing installation to being fully self-bootstrapping. This enables:

1. **Single-file distribution**
2. **Professional first-run experience**
3. **Flexible deployment options**
4. **Recovery from incomplete installations**

**Estimated Effort:** 12-14 hours
**Priority:** High (enables standalone distribution)
**Dependencies:** Phase 10.4 (for error handling improvements)

---

**Document Version:** 1.0
**Created:** 2026-01-08
**Status:** Ready for Planning Review
