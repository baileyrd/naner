# Phase 10.4: Usability & Testing Improvements

**Date:** 2026-01-08
**Status:** Planning
**Priority:** High (enables proper testing and user experience)

---

## Executive Summary

Phase 10.4 focuses on improving the usability and testability of the C# executable, addressing issues discovered during initial testing where the executable closes immediately when run outside the Naner directory structure.

**Goal:** Make naner.exe easier to test, debug, and use with better error messages and diagnostic tools.

---

## Problem Statement

### Current Issues

1. **Immediate Exit on Error**
   - When run outside Naner directory, exe closes immediately
   - No visible error message for GUI users
   - Difficult to diagnose issues

2. **Poor Error Context**
   - Generic DirectoryNotFoundException doesn't explain the requirement
   - No guidance on where the exe should be run from
   - Missing information about what directories it's searching

3. **Limited Testing Support**
   - No easy way to verify the exe is working
   - --version and --help require NANER_ROOT (shouldn't)
   - No diagnostic mode for troubleshooting

---

## Solution Design

### 1. Enhanced Error Messages ⭐ PRIORITY #1

**Goal:** Provide clear, actionable error messages when things go wrong.

#### 1.1 Improved PathResolver Error Messages

**Current:**
```csharp
throw new DirectoryNotFoundException(
    "Could not locate Naner root directory. " +
    "Ensure bin/, vendor/, and config/ folders exist.");
```

**Improved:**
```csharp
throw new DirectoryNotFoundException(
    $"Could not locate Naner root directory.\n" +
    $"  Searched from: {startPath}\n" +
    $"  Looking for: bin/, vendor/, and config/ subdirectories\n" +
    $"  Search depth: {depth} levels\n\n" +
    $"Solutions:\n" +
    $"  1. Run naner.exe from within the Naner directory structure\n" +
    $"  2. Place naner.exe in the 'bin' folder of your Naner installation\n" +
    $"  3. Set NANER_ROOT environment variable to your Naner directory");
```

#### 1.2 Path Search Diagnostics

Add detailed logging of the search process:

```csharp
public static string FindNanerRoot(string? startPath = null, int maxDepth = 10)
{
    startPath ??= AppContext.BaseDirectory;
    var currentPath = Path.GetFullPath(startPath);
    var searchedPaths = new List<string>();
    var depth = 0;

    while (depth < maxDepth)
    {
        searchedPaths.Add(currentPath);

        var binPath = Path.Combine(currentPath, "bin");
        var vendorPath = Path.Combine(currentPath, "vendor");
        var configPath = Path.Combine(currentPath, "config");

        if (Directory.Exists(binPath) &&
            Directory.Exists(vendorPath) &&
            Directory.Exists(configPath))
        {
            return currentPath;
        }

        var parentInfo = Directory.GetParent(currentPath);
        if (parentInfo == null || parentInfo.FullName == currentPath)
        {
            break;
        }

        currentPath = parentInfo.FullName;
        depth++;
    }

    // Enhanced error with search details
    throw new DirectoryNotFoundException(
        $"Could not locate Naner root directory.\n\n" +
        $"Search Details:\n" +
        $"  Starting path: {startPath}\n" +
        $"  Executable location: {AppContext.BaseDirectory}\n" +
        $"  Paths searched:\n{string.Join("\n", searchedPaths.Select(p => $"    - {p}"))}\n\n" +
        $"Requirements:\n" +
        $"  Naner root must contain:\n" +
        $"    - bin/      (binaries directory)\n" +
        $"    - vendor/   (vendor dependencies)\n" +
        $"    - config/   (configuration files)\n\n" +
        $"Solutions:\n" +
        $"  1. Copy naner.exe to your Naner installation's bin/ folder\n" +
        $"  2. Run from within the Naner directory structure\n" +
        $"  3. Set NANER_ROOT environment variable");
}
```

#### 1.3 Configuration Loading Errors

Improve config error messages:

```csharp
catch (FileNotFoundException ex)
{
    Logger.Failure($"Configuration file not found: {ex.FileName}");
    Logger.Info($"Expected location: {configPath}");
    Logger.Info("Ensure your Naner installation has a config/naner.json file");
    Logger.Debug(ex.ToString(), opts.Debug);
    return 1;
}
catch (JsonException ex)
{
    Logger.Failure($"Configuration file is invalid JSON");
    Logger.Info($"File: {configPath}");
    Logger.Info($"Error: {ex.Message}");
    Logger.Info("Validate your naner.json syntax at jsonlint.com");
    Logger.Debug(ex.ToString(), opts.Debug);
    return 1;
}
```

---

### 2. Test Batch File ⭐ PRIORITY #2

**Goal:** Make it easy to test and diagnose naner.exe behavior.

#### 2.1 Test Script (test-naner.bat)

Create `bin/test-naner.bat`:

```batch
@echo off
setlocal enabledelayedexpansion

echo =====================================
echo Naner Executable Test Suite
echo =====================================
echo.

:: Save original directory
set "ORIGINAL_DIR=%CD%"

:: Determine if running from bin folder
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

echo [TEST 1] Version Check
echo Command: naner.exe --version
echo ---
naner.exe --version
set "EXIT_CODE=%ERRORLEVEL%"
echo.
echo Exit Code: %EXIT_CODE%
echo.

echo [TEST 2] Help Output
echo Command: naner.exe --help
echo ---
naner.exe --help
set "EXIT_CODE=%ERRORLEVEL%"
echo.
echo Exit Code: %EXIT_CODE%
echo.

echo [TEST 3] Debug Mode (No Launch)
echo Command: naner.exe --debug --help
echo ---
naner.exe --debug --help
set "EXIT_CODE=%ERRORLEVEL%"
echo.
echo Exit Code: %EXIT_CODE%
echo.

echo [TEST 4] NANER_ROOT Detection
echo Command: naner.exe --diagnose
echo ---
naner.exe --diagnose
set "EXIT_CODE=%ERRORLEVEL%"
echo.
echo Exit Code: %EXIT_CODE%
echo.

echo =====================================
echo Test Suite Complete
echo =====================================
echo.
echo Press any key to exit...
pause >nul

:: Restore directory
cd /d "%ORIGINAL_DIR%"
```

#### 2.2 PowerShell Test Script

Create `bin/Test-Naner.ps1`:

```powershell
#!/usr/bin/env pwsh

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Naner Executable Test Suite" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $scriptDir

function Test-NanerCommand {
    param(
        [string]$Name,
        [string]$Arguments
    )

    Write-Host "[$Name]" -ForegroundColor Yellow
    Write-Host "Command: naner.exe $Arguments" -ForegroundColor Gray
    Write-Host "---" -ForegroundColor Gray

    $output = & ./naner.exe $Arguments.Split() 2>&1
    $exitCode = $LASTEXITCODE

    Write-Host $output
    Write-Host
    Write-Host "Exit Code: $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })
    Write-Host

    return $exitCode
}

# Run tests
$results = @{}
$results['Version'] = Test-NanerCommand "TEST 1: Version Check" "--version"
$results['Help'] = Test-NanerCommand "TEST 2: Help Output" "--help"
$results['Debug'] = Test-NanerCommand "TEST 3: Debug Mode" "--debug --help"
$results['Diagnose'] = Test-NanerCommand "TEST 4: Diagnostics" "--diagnose"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Test Results Summary" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
foreach ($test in $results.GetEnumerator()) {
    $status = if ($test.Value -eq 0) { "PASS" } else { "FAIL" }
    $color = if ($test.Value -eq 0) { "Green" } else { "Red" }
    Write-Host "$($test.Key): " -NoNewline
    Write-Host $status -ForegroundColor $color
}
Write-Host

Pop-Location
```

---

### 3. Help/Diagnose Mode ⭐ PRIORITY #3

**Goal:** Allow --version, --help, and --diagnose to work without NANER_ROOT.

#### 3.1 Early Command Detection

Modify Program.cs to handle special commands before NANER_ROOT search:

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
    }

    // Normal command parsing
    return Parser.Default.ParseArguments<Options>(args)
        .MapResult(
            opts => RunLauncher(opts),
            errs => 1);
}
```

#### 3.2 Diagnostic Mode Implementation

```csharp
static int RunDiagnostics()
{
    Logger.Header("Naner Diagnostics");
    Logger.Info($"Version: {Version}");
    Logger.Info($"Phase: {PhaseName}");
    Logger.NewLine();

    // Executable location
    Logger.Status("Executable Information:");
    Logger.Info($"  Location: {AppContext.BaseDirectory}");
    Logger.Info($"  Command Line: {Environment.CommandLine}");
    Logger.NewLine();

    // NANER_ROOT search
    Logger.Status("Searching for NANER_ROOT...");
    try
    {
        var nanerRoot = PathResolver.FindNanerRoot();
        Logger.Success($"  Found: {nanerRoot}");

        // Verify structure
        Logger.Status("Verifying directory structure:");
        var dirs = new[] { "bin", "vendor", "config", "home" };
        foreach (var dir in dirs)
        {
            var path = Path.Combine(nanerRoot, dir);
            var exists = Directory.Exists(path);
            var symbol = exists ? "✓" : "✗";
            var color = exists ? ConsoleColor.Green : ConsoleColor.Red;

            Console.ForegroundColor = color;
            Console.WriteLine($"  {symbol} {dir}/");
            Console.ResetColor();
        }
        Logger.NewLine();

        // Config check
        var configPath = Path.Combine(nanerRoot, "config", "naner.json");
        if (File.Exists(configPath))
        {
            Logger.Success($"Configuration file found: {configPath}");
            try
            {
                var configManager = new ConfigurationManager(nanerRoot);
                var config = configManager.Load(configPath);
                Logger.Info($"  Default Profile: {config.DefaultProfile}");
                Logger.Info($"  Vendor Paths: {config.VendorPaths.Count}");
                Logger.Info($"  Profiles: {config.Profiles.Count}");
            }
            catch (Exception ex)
            {
                Logger.Failure($"Configuration error: {ex.Message}");
            }
        }
        else
        {
            Logger.Failure($"Configuration file missing: {configPath}");
        }
        Logger.NewLine();

        // Environment
        Logger.Status("Environment Variables:");
        var envVars = new[] { "NANER_ROOT", "NANER_ENVIRONMENT", "HOME", "PATH" };
        foreach (var envVar in envVars)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            if (value != null)
            {
                if (envVar == "PATH")
                {
                    value = value.Substring(0, Math.Min(100, value.Length)) + "...";
                }
                Logger.Info($"  {envVar}={value}");
            }
        }

        Logger.NewLine();
        Logger.Success("Diagnostics complete - Naner installation appears healthy");
        return 0;
    }
    catch (Exception ex)
    {
        Logger.Failure($"NANER_ROOT not found: {ex.Message}");
        Logger.NewLine();
        Logger.Info("This usually means:");
        Logger.Info("  1. You're running naner.exe outside the Naner directory");
        Logger.Info("  2. The Naner directory structure is incomplete");
        Logger.Info("  3. You need to set NANER_ROOT environment variable");
        Logger.NewLine();
        Logger.Info("Try:");
        Logger.Info("  1. cd <your-naner-directory>");
        Logger.Info("  2. .\\bin\\naner.exe --diagnose");
        return 1;
    }
}
```

#### 3.3 Enhanced Help Output

```csharp
static void ShowHelp()
{
    Logger.Header("Naner Terminal Launcher");
    Console.WriteLine($"Version {Version} - {PhaseName}");
    Console.WriteLine();

    Console.WriteLine("USAGE:");
    Console.WriteLine("  naner.exe [OPTIONS]");
    Console.WriteLine();

    Console.WriteLine("OPTIONS:");
    Console.WriteLine("  -p, --profile <NAME>       Terminal profile to launch");
    Console.WriteLine("                             (Unified, PowerShell, Bash, CMD)");
    Console.WriteLine("  -e, --environment <NAME>   Environment name (default, work, etc.)");
    Console.WriteLine("  -d, --directory <PATH>     Starting directory for terminal");
    Console.WriteLine("  -c, --config <PATH>        Path to naner.json config file");
    Console.WriteLine("  --debug                    Enable debug/verbose output");
    Console.WriteLine("  -v, --version              Display version information");
    Console.WriteLine("  -h, --help                 Display this help message");
    Console.WriteLine("  --diagnose                 Run diagnostic checks");
    Console.WriteLine();

    Console.WriteLine("EXAMPLES:");
    Console.WriteLine("  naner.exe                          # Launch default profile");
    Console.WriteLine("  naner.exe --profile PowerShell     # Launch PowerShell profile");
    Console.WriteLine("  naner.exe -p Bash -d C:\\projects   # Launch Bash in specific dir");
    Console.WriteLine("  naner.exe --debug                  # Show detailed diagnostics");
    Console.WriteLine("  naner.exe --diagnose               # Check installation health");
    Console.WriteLine();

    Console.WriteLine("REQUIREMENTS:");
    Console.WriteLine("  naner.exe must be run from within a Naner installation directory");
    Console.WriteLine("  that contains bin/, vendor/, and config/ subdirectories.");
    Console.WriteLine();

    Console.WriteLine("DOCUMENTATION:");
    Console.WriteLine("  https://github.com/yourusername/naner");
    Console.WriteLine();
}
```

---

## Implementation Plan

### Step 1: Enhanced Error Messages (1-2 hours)
- [ ] Update PathUtilities.FindNanerRoot with detailed error message
- [ ] Add path search diagnostics
- [ ] Improve Program.cs exception handling
- [ ] Add specific error handlers for common issues

### Step 2: Diagnostic Mode (2-3 hours)
- [ ] Implement --diagnose command
- [ ] Add RunDiagnostics() method
- [ ] Create diagnostic output formatting
- [ ] Test with various error scenarios

### Step 3: Help Mode (1 hour)
- [ ] Implement --help command
- [ ] Create ShowHelp() method
- [ ] Update --version to work without NANER_ROOT
- [ ] Add ShowVersion() method

### Step 4: Test Scripts (1 hour)
- [ ] Create test-naner.bat
- [ ] Create Test-Naner.ps1
- [ ] Add test scripts to bin/ directory
- [ ] Document test procedures

### Step 5: Documentation (1 hour)
- [ ] Update PHASE-10.1-CSHARP-WRAPPER.md with testing info
- [ ] Create troubleshooting guide
- [ ] Update README with diagnostic commands
- [ ] Add example error scenarios

### Step 6: Testing & Validation (2 hours)
- [ ] Test from correct location (within Naner dir)
- [ ] Test from incorrect location (outside Naner dir)
- [ ] Test with missing directories
- [ ] Test with corrupted config
- [ ] Test all command-line options
- [ ] Verify error messages are helpful

---

## Expected Outcomes

### User Experience Improvements

**Before:**
- Exe closes immediately with no visible error
- User has no idea what went wrong
- No way to verify installation

**After:**
- Clear error messages explaining the problem
- Guidance on how to fix issues
- Diagnostic mode to check installation health
- Help available without NANER_ROOT

### Testing Improvements

**Before:**
- Manual testing required
- No standardized test procedure
- Difficult to verify functionality

**After:**
- Automated test scripts
- Standard test suite in bin/
- Easy verification of installation
- Clear pass/fail indicators

---

## Success Criteria

- [ ] --version works from any location
- [ ] --help works from any location
- [ ] --diagnose provides useful installation health check
- [ ] Error messages include actionable solutions
- [ ] Test scripts execute successfully
- [ ] User can troubleshoot issues independently
- [ ] All common error scenarios have clear messages

---

## Future Enhancements (Phase 10.5+)

### Advanced Diagnostics
- Network connectivity checks
- Vendor availability verification
- Configuration validation
- Permission checks

### Interactive Setup
- First-run wizard
- Automatic NANER_ROOT detection
- Configuration file generation
- Vendor installation prompts

### Logging System
- Structured logging to file
- Log rotation
- Verbosity levels
- Diagnostic log export

---

## Conclusion

Phase 10.4 addresses critical usability gaps discovered during initial testing. These improvements will:

1. Make the executable easier to test and debug
2. Provide better user experience with clear error messages
3. Enable self-service troubleshooting
4. Establish foundation for future testing

**Estimated Effort:** 8-10 hours
**Priority:** High (blocks proper testing)
**Risk:** Low (mostly additive changes)

---

**Document Version:** 1.0
**Created:** 2026-01-08
**Status:** Ready for Implementation
