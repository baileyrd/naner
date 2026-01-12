# Comprehensive Refactoring Design Plan
## Combining DRY, SOLID, and Modularity Improvements

---

## Executive Summary

This design plan addresses the three major architectural concerns identified in the Naner codebase:

1. **DRY Violations**: 8 major areas of code duplication
2. **SOLID Violations**: Lack of interfaces, god classes, tight coupling
3. **Modularity Issues**: Score of 4.5/10, with critical isolation problems

**Target Outcome**: Increase code quality from current **D+ (4.5/10)** to **B+ (8.5/10)** through systematic refactoring.

**Estimated Effort**: 40-60 hours of development work

---

## Phase 1: Foundation - Fix Critical Modularity Issues
**Priority**: 🔴 CRITICAL | **Effort**: 8 hours | **Impact**: High

### Goal
Establish proper project dependencies and eliminate the most egregious code duplication.

### 1.1 Fix Naner.Init Project Dependencies

**Current Problem**:
- `Naner.Init` has ZERO project references
- Duplicates `ConsoleHelper`, `FindNanerRoot`, download logic

**Solution**:

#### Step 1.1.1: Add Project Reference
**File**: `src/csharp/Naner.Init/Naner.Init.csproj`

```xml
<ItemGroup>
  <ProjectReference Include="..\Naner.Common\Naner.Common.csproj" />
</ItemGroup>
```

#### Step 1.1.2: Delete ConsoleHelper.cs
**File to Delete**: `src/csharp/Naner.Init/ConsoleHelper.cs`

**Replace all usages**:
```csharp
// Before:
ConsoleHelper.Status("...");
ConsoleHelper.Success("...");
ConsoleHelper.Error("...");

// After:
Logger.Status("...");
Logger.Success("...");
Logger.Failure("..."); // Note: Error becomes Failure to match Logger
```

**Files to update**:
- `src/csharp/Naner.Init/Program.cs`
- `src/csharp/Naner.Init/NanerUpdater.cs`
- `src/csharp/Naner.Init/GitHubReleasesClient.cs`
- `src/csharp/Naner.Init/EssentialVendorDownloader.cs`

#### Step 1.1.3: Replace FindNanerRoot Implementation
**File**: `src/csharp/Naner.Init/Program.cs:275-309`

```csharp
// DELETE: static string FindNanerRoot() { ... }

// REPLACE with:
static string FindNanerRoot()
{
    try
    {
        return PathUtilities.FindNanerRoot();
    }
    catch (DirectoryNotFoundException)
    {
        // Fallback: current directory
        return Directory.GetCurrentDirectory();
    }
}
```

**Impact**:
- ✅ Eliminates 150+ lines of duplicate code
- ✅ Establishes proper dependency chain
- ✅ Consistent logging across all projects

---

### 1.2 Consolidate Vendor Downloader Classes

**Current Problem**:
- `VendorDownloader` and `DynamicVendorDownloader` are 95% identical
- Both in `Naner.Common`, causing confusion

**Solution**:

#### Step 1.2.1: Create Vendor Definition Model
**New File**: `src/csharp/Naner.Common/Models/VendorDefinition.cs`

```csharp
namespace Naner.Common.Models;

/// <summary>
/// Defines how to fetch and install a vendor package.
/// </summary>
public class VendorDefinition
{
    public string Name { get; set; } = "";
    public string ExtractDir { get; set; } = "";
    public VendorSourceType SourceType { get; set; }

    // For static URLs
    public string? StaticUrl { get; set; }
    public string? FileName { get; set; }

    // For GitHub releases
    public string? GitHubOwner { get; set; }
    public string? GitHubRepo { get; set; }
    public string? AssetPattern { get; set; }
    public string? AssetPatternEnd { get; set; }

    // For web scraping
    public WebScrapeConfig? WebScrapeConfig { get; set; }

    // Fallback
    public string? FallbackUrl { get; set; }
}

public enum VendorSourceType
{
    StaticUrl,
    GitHub,
    WebScrape
}

public class WebScrapeConfig
{
    public string Url { get; set; } = "";
    public string Pattern { get; set; } = "";
    public string BaseUrl { get; set; } = "";
}
```

#### Step 1.2.2: Merge into Single Unified VendorDownloader
**File**: `src/csharp/Naner.Common/VendorDownloader.cs`

**Changes**:
1. Rename `VendorDownloader` → `UnifiedVendorDownloader`
2. Accept `List<VendorDefinition>` instead of hardcoded array
3. Include dynamic fetching logic from `DynamicVendorDownloader`
4. Delete `DynamicVendorDownloader.cs`

**New constructor**:
```csharp
public class UnifiedVendorDownloader
{
    private readonly string _nanerRoot;
    private readonly string _vendorDir;
    private readonly string _downloadDir;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public UnifiedVendorDownloader(
        string nanerRoot,
        HttpClient httpClient,
        ILogger logger)
    {
        _nanerRoot = nanerRoot;
        _vendorDir = Path.Combine(nanerRoot, "vendor");
        _downloadDir = Path.Combine(_vendorDir, ".downloads");
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SetupVendorsAsync(List<VendorDefinition> vendors)
    {
        // Unified logic from both classes
    }
}
```

#### Step 1.2.3: Create Vendor Configuration
**New File**: `config/vendors.json`

```json
{
  "vendors": [
    {
      "name": "7-Zip",
      "extractDir": "7zip",
      "sourceType": "WebScrape",
      "webScrapeConfig": {
        "url": "https://www.7-zip.org/download.html",
        "pattern": "href=\"(/a/7z\\d+-x64\\.msi)\"",
        "baseUrl": "https://www.7-zip.org"
      },
      "fallbackUrl": "https://www.7-zip.org/a/7z2408-x64.msi"
    },
    {
      "name": "PowerShell",
      "extractDir": "powershell",
      "sourceType": "GitHub",
      "gitHubOwner": "PowerShell",
      "gitHubRepo": "PowerShell",
      "assetPattern": "win-x64.zip",
      "fallbackUrl": "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/PowerShell-7.4.6-win-x64.zip"
    }
  ]
}
```

**Impact**:
- ✅ Eliminates 500+ lines of duplicate code
- ✅ Vendor definitions now configurable (not hardcoded)
- ✅ Single source of truth for vendor management

---

## Phase 2: Extract Shared Services (DRY)
**Priority**: 🔴 HIGH | **Effort**: 12 hours | **Impact**: Very High

### Goal
Extract all duplicated functionality into reusable, single-responsibility services.

### 2.1 Create HttpDownloadService

**Current Problem**:
- 4 different implementations of file download with progress
- Located in: `VendorDownloader`, `DynamicVendorDownloader`, `GitHubReleasesClient`, `EssentialVendorDownloader`

**Solution**:

#### New File: `src/csharp/Naner.Common/Services/HttpDownloadService.cs`

```csharp
namespace Naner.Common.Services;

public interface IHttpDownloadService
{
    Task<bool> DownloadFileAsync(
        string url,
        string outputPath,
        string displayName,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}

public class HttpDownloadService : IHttpDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public HttpDownloadService(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> DownloadFileAsync(
        string url,
        string outputPath,
        string displayName,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Status($"Downloading {displayName}...");

            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Handle GitHub API downloads
            if (url.StartsWith("https://api.github.com/"))
            {
                request.Headers.Remove("Accept");
                request.Headers.Add("Accept", "application/octet-stream");
            }

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(
                outputPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                8192,
                true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            var lastPercent = -1;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    var percent = (int)((totalRead * 100) / totalBytes);
                    if (percent != lastPercent && percent % 10 == 0)
                    {
                        progress?.Report(percent);
                        Console.Write($"\r  Progress: {percent}%");
                        lastPercent = percent;
                    }
                }
            }

            if (totalBytes > 0)
            {
                progress?.Report(100);
                Console.Write("\r  Progress: 100%");
                Console.WriteLine();
            }

            _logger.Success($"Downloaded {displayName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Failure($"Download failed: {ex.Message}");
            return false;
        }
    }
}
```

**Update all usages** in:
- `UnifiedVendorDownloader`
- `GitHubReleasesClient`
- `EssentialVendorDownloader`

**Impact**:
- ✅ Eliminates 200+ lines of duplicate code
- ✅ Centralized download logic
- ✅ Testable via interface
- ✅ Supports cancellation and progress reporting

---

### 2.2 Create ArchiveExtractorService

**Current Problem**:
- Identical extraction logic in `VendorDownloader` and `DynamicVendorDownloader`
- 300+ lines duplicated

**Solution**:

#### New File: `src/csharp/Naner.Common/Services/ArchiveExtractorService.cs`

```csharp
namespace Naner.Common.Services;

public interface IArchiveExtractor
{
    bool CanExtract(string filePath);
    bool Extract(string archivePath, string targetDir, string vendorName);
}

public class ArchiveExtractorService
{
    private readonly ILogger _logger;
    private readonly List<IArchiveExtractor> _extractors;

    public ArchiveExtractorService(ILogger logger, string sevenZipPath)
    {
        _logger = logger;
        _extractors = new List<IArchiveExtractor>
        {
            new ZipExtractor(logger),
            new MsiExtractor(logger),
            new TarXzExtractor(logger, sevenZipPath)
        };
    }

    public bool Extract(string archivePath, string targetDir, string vendorName)
    {
        var extractor = _extractors.FirstOrDefault(e => e.CanExtract(archivePath));

        if (extractor == null)
        {
            var extension = Path.GetExtension(archivePath);
            _logger.Warning($"Unsupported archive format: {extension}");
            return false;
        }

        return extractor.Extract(archivePath, targetDir, vendorName);
    }
}

// Individual extractors
internal class ZipExtractor : IArchiveExtractor
{
    private readonly ILogger _logger;

    public ZipExtractor(ILogger logger) => _logger = logger;

    public bool CanExtract(string filePath)
        => Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase);

    public bool Extract(string archivePath, string targetDir, string vendorName)
    {
        try
        {
            Directory.CreateDirectory(targetDir);
            ZipFile.ExtractToDirectory(archivePath, targetDir, overwriteFiles: true);

            // Flatten single-directory structures
            var entries = Directory.GetFileSystemEntries(targetDir);
            if (entries.Length == 1 && Directory.Exists(entries[0]))
            {
                FlattenDirectory(entries[0], targetDir);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Failure($"ZIP extraction failed: {ex.Message}");
            return false;
        }
    }

    private void FlattenDirectory(string sourceDir, string targetDir)
    {
        var tempDir = targetDir + "_temp";
        Directory.Move(sourceDir, tempDir);

        foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(tempDir, file);
            var destPath = Path.Combine(targetDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Move(file, destPath, overwrite: true);
        }

        Directory.Delete(tempDir, recursive: true);
    }
}

internal class MsiExtractor : IArchiveExtractor
{
    private readonly ILogger _logger;

    public MsiExtractor(ILogger logger) => _logger = logger;

    public bool CanExtract(string filePath)
        => Path.GetExtension(filePath).Equals(".msi", StringComparison.OrdinalIgnoreCase);

    public bool Extract(string archivePath, string targetDir, string vendorName)
    {
        try
        {
            Directory.CreateDirectory(targetDir);

            var startInfo = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = $"/a \"{archivePath}\" /qn TARGETDIR=\"{targetDir}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();

            // MSI-specific cleanup: move from Files/7-Zip subdirectory
            var filesDir = Path.Combine(targetDir, "Files", "7-Zip");
            if (Directory.Exists(filesDir))
            {
                foreach (var file in Directory.GetFiles(filesDir))
                {
                    var destFile = Path.Combine(targetDir, Path.GetFileName(file));
                    File.Move(file, destFile, overwrite: true);
                }

                var files7ZipDir = Path.Combine(targetDir, "Files");
                if (Directory.Exists(files7ZipDir))
                {
                    Directory.Delete(files7ZipDir, recursive: true);
                }
            }

            return process?.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Failure($"MSI extraction failed: {ex.Message}");
            return false;
        }
    }
}

internal class TarXzExtractor : IArchiveExtractor
{
    private readonly ILogger _logger;
    private readonly string _sevenZipPath;

    public TarXzExtractor(ILogger logger, string sevenZipPath)
    {
        _logger = logger;
        _sevenZipPath = sevenZipPath;
    }

    public bool CanExtract(string filePath)
        => filePath.EndsWith(".tar.xz", StringComparison.OrdinalIgnoreCase);

    public bool Extract(string archivePath, string targetDir, string vendorName)
    {
        if (!File.Exists(_sevenZipPath))
        {
            _logger.Warning($"7-Zip not found at {_sevenZipPath}");
            return false;
        }

        try
        {
            Directory.CreateDirectory(targetDir);

            // Step 1: Extract .xz to .tar
            _logger.Info("Extracting .xz archive...");
            var tarPath = archivePath.Replace(".tar.xz", ".tar");

            if (!Run7Zip($"e \"{archivePath}\" -o\"{Path.GetDirectoryName(archivePath)}\" -y"))
            {
                return false;
            }

            // Step 2: Extract .tar to directory
            _logger.Info("Extracting .tar archive...");

            if (!Run7Zip($"x \"{tarPath}\" -o\"{targetDir}\" -y"))
            {
                return false;
            }

            // Cleanup intermediate .tar file
            try
            {
                if (File.Exists(tarPath))
                {
                    File.Delete(tarPath);
                }
            }
            catch { /* Ignore cleanup errors */ }

            // Flatten single subdirectory
            var entries = Directory.GetFileSystemEntries(targetDir);
            if (entries.Length == 1 && Directory.Exists(entries[0]))
            {
                FlattenDirectory(entries[0], targetDir);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Failure($".tar.xz extraction failed: {ex.Message}");
            return false;
        }
    }

    private bool Run7Zip(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _sevenZipPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo);
        process?.WaitForExit();

        return process?.ExitCode == 0;
    }

    private void FlattenDirectory(string sourceDir, string targetDir)
    {
        var tempDir = targetDir + "_temp";
        Directory.Move(sourceDir, tempDir);

        foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(tempDir, file);
            var destPath = Path.Combine(targetDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Move(file, destPath, overwrite: true);
        }

        Directory.Delete(tempDir, recursive: true);
    }
}
```

**Impact**:
- ✅ Eliminates 300+ lines of duplicate code
- ✅ Strategy pattern - easy to add new archive formats
- ✅ Follows Open/Closed Principle
- ✅ Each extractor has single responsibility

---

### 2.3 Create Console Management Service

**Current Problem**:
- Duplicate Windows API imports in `Naner.Launcher.Program` and `Naner.Init.Program`
- Console attachment logic duplicated

**Solution**:

#### New File: `src/csharp/Naner.Common/Services/ConsoleManager.cs`

```csharp
using System.Runtime.InteropServices;

namespace Naner.Common.Services;

/// <summary>
/// Manages console attachment for GUI applications.
/// </summary>
public static class ConsoleManager
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    private const int ATTACH_PARENT_PROCESS = -1;

    /// <summary>
    /// Attaches to parent console or allocates new one if needed.
    /// </summary>
    /// <returns>True if console was attached or allocated.</returns>
    public static bool EnsureConsole()
    {
        if (!AttachConsole(ATTACH_PARENT_PROCESS))
        {
            return AllocConsole();
        }
        return true;
    }

    /// <summary>
    /// Conditionally ensures console based on a predicate.
    /// </summary>
    public static bool EnsureConsoleIf(bool condition)
    {
        return condition && EnsureConsole();
    }
}
```

**Update in**:
- `src/csharp/Naner.Launcher/Program.cs:44-65`
- `src/csharp/Naner.Init/Program.cs:14-37`

```csharp
// Before:
if (needsConsole)
{
    if (!AttachConsole(ATTACH_PARENT_PROCESS))
    {
        AllocConsole();
    }
}

// After:
ConsoleManager.EnsureConsoleIf(needsConsole);
```

**Impact**:
- ✅ Eliminates P/Invoke duplication
- ✅ Centralized console management
- ✅ Simpler call sites

---

## Phase 3: Introduce Interfaces (SOLID - Interface Segregation & Dependency Inversion)
**Priority**: 🟡 HIGH | **Effort**: 10 hours | **Impact**: Very High

### Goal
Make all services testable and substitutable through interfaces.

### 3.1 Create Core Service Interfaces

#### New File: `src/csharp/Naner.Common/Abstractions/ILogger.cs`

```csharp
namespace Naner.Common.Abstractions;

/// <summary>
/// Logging abstraction for Naner applications.
/// </summary>
public interface ILogger
{
    void Status(string message);
    void Success(string message);
    void Failure(string message);
    void Info(string message);
    void Debug(string message, bool debugMode = false);
    void Warning(string message);
    void NewLine();
    void Header(string header);
}

/// <summary>
/// Console-based implementation of ILogger.
/// </summary>
public class ConsoleLogger : ILogger
{
    public void Status(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[*] {message}");
        Console.ResetColor();
    }

    public void Success(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK] {message}");
        Console.ResetColor();
    }

    public void Failure(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[✗] {message}");
        Console.ResetColor();
    }

    public void Info(string message)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"    {message}");
        Console.ResetColor();
    }

    public void Debug(string message, bool debugMode = false)
    {
        if (!debugMode) return;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[DEBUG] {message}");
        Console.ResetColor();
    }

    public void Warning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[!] {message}");
        Console.ResetColor();
    }

    public void NewLine()
    {
        Console.WriteLine();
    }

    public void Header(string header)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(header);
        Console.WriteLine(new string('=', header.Length));
        Console.ResetColor();
        Console.WriteLine();
    }
}
```

#### New File: `src/csharp/Naner.Configuration/Abstractions/IConfigurationManager.cs`

```csharp
namespace Naner.Configuration.Abstractions;

public interface IConfigurationManager
{
    NanerConfig Load(string? configPath = null);
    NanerConfig Config { get; }
    ProfileConfig GetProfile(string profileName);
    string BuildUnifiedPath(bool includeSystemPath = true);
    void SetupEnvironmentVariables();
}
```

#### Update: `src/csharp/Naner.Configuration/ConfigurationManager.cs`

```csharp
public class ConfigurationManager : IConfigurationManager
{
    private readonly string _nanerRoot;
    private readonly ILogger _logger;
    private NanerConfig? _config;

    public ConfigurationManager(string nanerRoot, ILogger logger)
    {
        _nanerRoot = nanerRoot ?? throw new ArgumentNullException(nameof(nanerRoot));
        _logger = logger;
    }

    // ... existing implementation
}
```

#### New File: `src/csharp/Naner.Launcher/Abstractions/ITerminalLauncher.cs`

```csharp
namespace Naner.Launcher.Abstractions;

public interface ITerminalLauncher
{
    int LaunchProfile(string profileName, string? startingDirectory = null);
}
```

### 3.2 Update Logger Class to Adapter Pattern

#### File: `src/csharp/Naner.Common/Logger.cs`

```csharp
namespace Naner.Common;

/// <summary>
/// Static facade for console logging (maintains backward compatibility).
/// Delegates to ILogger implementation.
/// </summary>
public static class Logger
{
    private static ILogger _instance = new ConsoleLogger();

    /// <summary>
    /// Sets custom logger implementation (for testing).
    /// </summary>
    public static void SetLogger(ILogger logger)
    {
        _instance = logger;
    }

    public static void Status(string message) => _instance.Status(message);
    public static void Success(string message) => _instance.Success(message);
    public static void Failure(string message) => _instance.Failure(message);
    public static void Info(string message) => _instance.Info(message);
    public static void Debug(string message, bool debugMode = false)
        => _instance.Debug(message, debugMode);
    public static void Warning(string message) => _instance.Warning(message);
    public static void NewLine() => _instance.NewLine();
    public static void Header(string header) => _instance.Header(header);
}
```

**Impact**:
- ✅ Maintains backward compatibility (existing code doesn't break)
- ✅ Allows testing via `Logger.SetLogger(mockLogger)`
- ✅ Enables new code to use `ILogger` directly

---

## Phase 4: Break Up God Classes (SOLID - Single Responsibility)
**Priority**: 🟡 MEDIUM | **Effort**: 16 hours | **Impact**: High

### Goal
Split large, multi-responsibility classes into focused, cohesive services.

### 4.1 Refactor Naner.Launcher.Program

**Current Problem**:
- 695 lines, handles: CLI parsing, diagnostics, setup, first-run, launching
- Violates Single Responsibility Principle

**Solution**:

#### New Structure:
```
Naner.Launcher/
├── Program.cs                    # Entry point only (50 lines)
├── Commands/
│   ├── DiagnosticsCommand.cs    # --diagnose
│   ├── InitCommand.cs            # init
│   ├── LaunchCommand.cs          # default launch
│   └── SetupVendorsCommand.cs    # setup-vendors
├── Services/
│   ├── CommandRouter.cs          # Routes args to commands
│   └── FirstRunHandler.cs        # First-run logic
└── ... existing files
```

#### New File: `src/csharp/Naner.Launcher/Services/CommandRouter.cs`

```csharp
namespace Naner.Launcher.Services;

public interface ICommandRouter
{
    Task<int> RouteAsync(string[] args);
}

public class CommandRouter : ICommandRouter
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, Func<string[], Task<int>>> _commands;

    public CommandRouter(ILogger logger)
    {
        _logger = logger;
        _commands = new Dictionary<string, Func<string[], Task<int>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["--version"] = HandleVersionAsync,
            ["-v"] = HandleVersionAsync,
            ["--help"] = HandleHelpAsync,
            ["-h"] = HandleHelpAsync,
            ["/?"] = HandleHelpAsync,
            ["--diagnose"] = HandleDiagnosticsAsync,
            ["init"] = HandleInitAsync,
            ["setup-vendors"] = HandleSetupVendorsAsync
        };
    }

    public async Task<int> RouteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            return await HandleLaunchAsync(args);
        }

        var command = args[0].ToLower();

        if (_commands.TryGetValue(command, out var handler))
        {
            return await handler(args);
        }

        // Default: parse as launch options
        return await HandleLaunchAsync(args);
    }

    private Task<int> HandleVersionAsync(string[] args)
    {
        new VersionCommand().Execute();
        return Task.FromResult(0);
    }

    private Task<int> HandleHelpAsync(string[] args)
    {
        new HelpCommand(_logger).Execute();
        return Task.FromResult(0);
    }

    private async Task<int> HandleDiagnosticsAsync(string[] args)
    {
        var command = new DiagnosticsCommand(_logger);
        return await command.ExecuteAsync();
    }

    private async Task<int> HandleInitAsync(string[] args)
    {
        var command = new InitCommand(_logger);
        return await command.ExecuteAsync(args.Skip(1).ToArray());
    }

    private async Task<int> HandleSetupVendorsAsync(string[] args)
    {
        var command = new SetupVendorsCommand(_logger);
        return await command.ExecuteAsync();
    }

    private async Task<int> HandleLaunchAsync(string[] args)
    {
        var command = new LaunchCommand(_logger);
        return await command.ExecuteAsync(args);
    }
}
```

#### New File: `src/csharp/Naner.Launcher/Commands/DiagnosticsCommand.cs`

```csharp
namespace Naner.Launcher.Commands;

public class DiagnosticsCommand
{
    private readonly ILogger _logger;

    public DiagnosticsCommand(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<int> ExecuteAsync()
    {
        _logger.Header("Naner Diagnostics");
        Console.WriteLine($"Version: {AssemblyInfo.Version}");
        Console.WriteLine($"Phase: {AssemblyInfo.Phase}");
        _logger.NewLine();

        // Executable location
        _logger.Status("Executable Information:");
        _logger.Info($"  Location: {AppContext.BaseDirectory}");
        _logger.Info($"  Command Line: {Environment.CommandLine}");
        _logger.NewLine();

        // NANER_ROOT search
        _logger.Status("Searching for NANER_ROOT...");
        try
        {
            var nanerRoot = PathUtilities.FindNanerRoot();
            _logger.Success($"  Found: {nanerRoot}");
            _logger.NewLine();

            await VerifyStructureAsync(nanerRoot);
            await VerifyConfigurationAsync(nanerRoot);
            VerifyEnvironment();

            _logger.NewLine();
            _logger.Success("Diagnostics complete - Naner installation appears healthy");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.Failure("NANER_ROOT not found");
            _logger.NewLine();
            _logger.Info("Details:");
            _logger.Info($"  {ex.Message}");
            _logger.NewLine();
            ShowTroubleshootingSteps();
            return 1;
        }
    }

    private async Task VerifyStructureAsync(string nanerRoot)
    {
        _logger.Status("Verifying directory structure:");
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

        _logger.NewLine();
    }

    private async Task VerifyConfigurationAsync(string nanerRoot)
    {
        var configPath = Path.Combine(nanerRoot, "config", "naner.json");

        if (!File.Exists(configPath))
        {
            _logger.Failure($"Configuration file missing: {configPath}");
            return;
        }

        _logger.Success("Configuration file found");
        _logger.Info($"  Path: {configPath}");

        try
        {
            var logger = new ConsoleLogger();
            var configManager = new ConfigurationManager(nanerRoot, logger);
            var config = configManager.Load(configPath);

            _logger.Info($"  Default Profile: {config.DefaultProfile}");
            _logger.Info($"  Vendor Paths: {config.VendorPaths.Count}");
            _logger.Info($"  Profiles: {config.Profiles.Count}");
            _logger.NewLine();

            await VerifyVendorPathsAsync(config);
        }
        catch (Exception ex)
        {
            _logger.Failure($"Configuration error: {ex.Message}");
        }
    }

    private async Task VerifyVendorPathsAsync(NanerConfig config)
    {
        _logger.Status("Vendor Paths:");

        var vendorsToCheck = new[]
        {
            ("WindowsTerminal", "wt.exe"),
            ("PowerShell", "pwsh.exe"),
            ("GitBash", "bash.exe")
        };

        foreach (var (key, exeName) in vendorsToCheck)
        {
            if (config.VendorPaths.TryGetValue(key, out var path))
            {
                var exists = File.Exists(path);
                var symbol = exists ? "✓" : "✗";
                var color = exists ? ConsoleColor.Green : ConsoleColor.Red;

                Console.ForegroundColor = color;
                Console.WriteLine($"  {symbol} {key}: {path}");
                Console.ResetColor();
            }
        }
    }

    private void VerifyEnvironment()
    {
        _logger.Status("Environment Variables:");
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
                _logger.Info($"  {envVar}={value}");
            }
            else
            {
                _logger.Info($"  {envVar}=(not set)");
            }
        }
    }

    private void ShowTroubleshootingSteps()
    {
        _logger.Info("This usually means:");
        _logger.Info("  1. You're running naner.exe outside the Naner directory");
        _logger.Info("  2. The Naner directory structure is incomplete");
        _logger.Info("  3. You need to set NANER_ROOT environment variable");
        _logger.NewLine();
        _logger.Info("Try:");
        _logger.Info("  1. cd <your-naner-directory>");
        _logger.Info("  2. .\\vendor\\bin\\naner.exe --diagnose");
    }
}
```

#### Simplified: `src/csharp/Naner.Launcher/Program.cs`

```csharp
using Naner.Common;
using Naner.Common.Services;
using Naner.Launcher.Services;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Determine if we need console output
        bool needsConsole = NeedsConsole(args);
        ConsoleManager.EnsureConsoleIf(needsConsole);

        // Check for first run
        if (FirstRunDetector.IsFirstRun())
        {
            var firstRunHandler = new FirstRunHandler(new ConsoleLogger());
            return await firstRunHandler.HandleAsync();
        }

        // Route to appropriate command
        var logger = new ConsoleLogger();
        var router = new CommandRouter(logger);
        return await router.RouteAsync(args);
    }

    static bool NeedsConsole(string[] args)
    {
        if (args.Length == 0) return false;

        var firstArg = args[0].ToLower();
        return firstArg switch
        {
            "--version" or "-v" or "--help" or "-h" or "/?" => true,
            "--diagnose" or "init" or "setup-vendors" => true,
            "--debug" => true,
            _ => false
        };
    }
}
```

**Impact**:
- ✅ Reduces Program.cs from 695 lines to ~50 lines
- ✅ Each command is isolated and testable
- ✅ Easy to add new commands
- ✅ Follows Single Responsibility Principle

---

### 4.2 Refactor VendorDownloader/Installer

**Current Problem**:
- Single class handles HTTP, extraction, GitHub API, web scraping, post-install config
- 520+ lines

**Solution**:

#### New Structure:
```
Naner.Common/
├── Services/
│   ├── HttpDownloadService.cs         # Already created in Phase 2.1
│   ├── ArchiveExtractorService.cs     # Already created in Phase 2.2
│   ├── GitHubApiClient.cs             # NEW
│   ├── WebScraperService.cs           # NEW
│   ├── VendorSourceResolver.cs        # NEW
│   └── VendorInstaller.cs             # NEW (orchestrator)
```

#### New File: `src/csharp/Naner.Common/Services/GitHubApiClient.cs`

```csharp
namespace Naner.Common.Services;

public interface IGitHubApiClient
{
    Task<GitHubRelease?> GetLatestReleaseAsync(string owner, string repo);
}

public class GitHubApiClient : IGitHubApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public GitHubApiClient(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configure headers
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Naner/1.0.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

        // Add token if available
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }
    }

    public async Task<GitHubRelease?> GetLatestReleaseAsync(string owner, string repo)
    {
        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning($"GitHub API returned {response.StatusCode}");
                return await GetLatestReleaseFromAllReleasesAsync(owner, repo);
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to fetch GitHub release: {ex.Message}");
            return null;
        }
    }

    private async Task<GitHubRelease?> GetLatestReleaseFromAllReleasesAsync(string owner, string repo)
    {
        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/releases";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return releases?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}
```

#### New File: `src/csharp/Naner.Common/Services/VendorSourceResolver.cs`

```csharp
namespace Naner.Common.Services;

public interface IVendorSourceResolver
{
    Task<VendorDownloadInfo?> ResolveAsync(VendorDefinition vendor);
}

public class VendorSourceResolver : IVendorSourceResolver
{
    private readonly IGitHubApiClient _githubClient;
    private readonly IWebScraper _webScraper;
    private readonly ILogger _logger;

    public VendorSourceResolver(
        IGitHubApiClient githubClient,
        IWebScraper webScraper,
        ILogger logger)
    {
        _githubClient = githubClient;
        _webScraper = webScraper;
        _logger = logger;
    }

    public async Task<VendorDownloadInfo?> ResolveAsync(VendorDefinition vendor)
    {
        try
        {
            return vendor.SourceType switch
            {
                VendorSourceType.StaticUrl => ResolveStaticUrl(vendor),
                VendorSourceType.GitHub => await ResolveGitHubAsync(vendor),
                VendorSourceType.WebScrape => await ResolveWebScrapeAsync(vendor),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to resolve {vendor.Name}: {ex.Message}");

            // Fallback to static URL
            if (!string.IsNullOrEmpty(vendor.FallbackUrl))
            {
                _logger.Info("Using fallback URL");
                return new VendorDownloadInfo
                {
                    Url = vendor.FallbackUrl,
                    FileName = vendor.FileName ?? Path.GetFileName(vendor.FallbackUrl),
                    Version = "fallback"
                };
            }

            return null;
        }
    }

    private VendorDownloadInfo? ResolveStaticUrl(VendorDefinition vendor)
    {
        if (string.IsNullOrEmpty(vendor.StaticUrl))
        {
            return null;
        }

        return new VendorDownloadInfo
        {
            Url = vendor.StaticUrl,
            FileName = vendor.FileName ?? Path.GetFileName(vendor.StaticUrl),
            Version = "static"
        };
    }

    private async Task<VendorDownloadInfo?> ResolveGitHubAsync(VendorDefinition vendor)
    {
        if (string.IsNullOrEmpty(vendor.GitHubOwner) || string.IsNullOrEmpty(vendor.GitHubRepo))
        {
            return null;
        }

        var release = await _githubClient.GetLatestReleaseAsync(vendor.GitHubOwner, vendor.GitHubRepo);
        if (release?.Assets == null)
        {
            return null;
        }

        var asset = release.Assets.FirstOrDefault(a =>
        {
            if (a.Name == null) return false;

            var matchesStart = string.IsNullOrEmpty(vendor.AssetPattern) ||
                               a.Name.Contains(vendor.AssetPattern, StringComparison.OrdinalIgnoreCase);

            var matchesEnd = string.IsNullOrEmpty(vendor.AssetPatternEnd) ||
                            a.Name.Contains(vendor.AssetPatternEnd, StringComparison.OrdinalIgnoreCase);

            return matchesStart && matchesEnd;
        });

        if (asset?.BrowserDownloadUrl == null)
        {
            return null;
        }

        return new VendorDownloadInfo
        {
            Url = asset.BrowserDownloadUrl,
            FileName = asset.Name ?? Path.GetFileName(asset.BrowserDownloadUrl),
            Version = release.TagName
        };
    }

    private async Task<VendorDownloadInfo?> ResolveWebScrapeAsync(VendorDefinition vendor)
    {
        if (vendor.WebScrapeConfig == null)
        {
            return null;
        }

        var url = await _webScraper.ScrapeUrlAsync(
            vendor.WebScrapeConfig.Url,
            vendor.WebScrapeConfig.Pattern,
            vendor.WebScrapeConfig.BaseUrl);

        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        var fileName = Path.GetFileName(url);
        var versionMatch = Regex.Match(fileName, @"(\d+\.?\d*\.?\d*\.?\d*)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : "latest";

        return new VendorDownloadInfo
        {
            Url = url,
            FileName = fileName,
            Version = version
        };
    }
}

public class VendorDownloadInfo
{
    public string Url { get; set; } = "";
    public string FileName { get; set; } = "";
    public string? Version { get; set; }
}
```

#### New File: `src/csharp/Naner.Common/Services/VendorInstaller.cs`

```csharp
namespace Naner.Common.Services;

/// <summary>
/// Orchestrates vendor download and installation.
/// </summary>
public class VendorInstaller
{
    private readonly string _nanerRoot;
    private readonly string _vendorDir;
    private readonly string _downloadDir;
    private readonly IVendorSourceResolver _sourceResolver;
    private readonly IHttpDownloadService _downloadService;
    private readonly ArchiveExtractorService _extractorService;
    private readonly ILogger _logger;

    public VendorInstaller(
        string nanerRoot,
        IVendorSourceResolver sourceResolver,
        IHttpDownloadService downloadService,
        ArchiveExtractorService extractorService,
        ILogger logger)
    {
        _nanerRoot = nanerRoot;
        _vendorDir = Path.Combine(nanerRoot, "vendor");
        _downloadDir = Path.Combine(_vendorDir, ".downloads");
        _sourceResolver = sourceResolver;
        _downloadService = downloadService;
        _extractorService = extractorService;
        _logger = logger;
    }

    public async Task<bool> InstallVendorsAsync(List<VendorDefinition> vendors)
    {
        _logger.Header("Installing Vendor Dependencies");
        _logger.NewLine();
        _logger.Status("This may take several minutes...");
        _logger.NewLine();

        // Create download directory
        Directory.CreateDirectory(_downloadDir);

        var allSucceeded = true;

        foreach (var vendor in vendors)
        {
            if (!await InstallVendorAsync(vendor))
            {
                allSucceeded = false;
            }
        }

        // Cleanup
        CleanupDownloads();

        _logger.NewLine();
        if (allSucceeded)
        {
            _logger.Success("All vendors installed successfully!");
        }
        else
        {
            _logger.Warning("Some vendors failed to install. See logs above.");
        }

        return allSucceeded;
    }

    private async Task<bool> InstallVendorAsync(VendorDefinition vendor)
    {
        var targetDir = Path.Combine(_vendorDir, vendor.ExtractDir);

        // Skip if already installed
        if (Directory.Exists(targetDir) && Directory.GetFileSystemEntries(targetDir).Length > 0)
        {
            _logger.Info($"Skipping {vendor.Name} (already installed)");
            return true;
        }

        _logger.Status($"Installing {vendor.Name}...");

        try
        {
            // 1. Resolve download URL
            var downloadInfo = await _sourceResolver.ResolveAsync(vendor);
            if (downloadInfo == null)
            {
                _logger.Warning($"Failed to resolve download for {vendor.Name}");
                return false;
            }

            if (!string.IsNullOrEmpty(downloadInfo.Version))
            {
                _logger.Info($"  Version: {downloadInfo.Version}");
            }

            // 2. Download
            var downloadPath = Path.Combine(_downloadDir, downloadInfo.FileName);
            if (!await _downloadService.DownloadFileAsync(
                downloadInfo.Url,
                downloadPath,
                vendor.Name))
            {
                return false;
            }

            // 3. Extract
            _logger.Status($"  Extracting {vendor.Name}...");
            if (!_extractorService.Extract(downloadPath, targetDir, vendor.Name))
            {
                _logger.Warning($"Failed to extract {vendor.Name}");
                return false;
            }

            // 4. Post-install configuration
            PerformPostInstall(vendor.Name, targetDir);

            _logger.Success($"  Installed {vendor.Name}");
            _logger.NewLine();
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Error installing {vendor.Name}: {ex.Message}");
            _logger.NewLine();
            return false;
        }
    }

    private void PerformPostInstall(string vendorName, string targetDir)
    {
        if (vendorName.Contains("Windows Terminal", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureWindowsTerminal(targetDir);
        }
    }

    private void ConfigureWindowsTerminal(string targetDir)
    {
        try
        {
            // Create .portable file
            var portableFile = Path.Combine(targetDir, ".portable");
            File.WriteAllText(portableFile, "");
            _logger.Info("    Created .portable file for portable mode");

            // Create settings directory
            var settingsDir = Path.Combine(targetDir, "settings");
            Directory.CreateDirectory(settingsDir);

            // Note: Settings file can be created separately or via template
            _logger.Info("    Created settings directory");
        }
        catch (Exception ex)
        {
            _logger.Warning($"    Post-install configuration warning: {ex.Message}");
        }
    }

    private void CleanupDownloads()
    {
        try
        {
            if (Directory.Exists(_downloadDir))
            {
                Directory.Delete(_downloadDir, true);
            }
        }
        catch (Exception ex)
        {
            _logger.Debug($"Cleanup error: {ex.Message}", true);
        }
    }
}
```

**Impact**:
- ✅ Single Responsibility: Each service has one job
- ✅ Easy to test each component in isolation
- ✅ Easy to swap implementations (GitHub API, web scraper)
- ✅ Reduced from 1 god class to 6 focused services

---

## Phase 5: Configuration-Driven Design (Open/Closed Principle)
**Priority**: 🟢 MEDIUM | **Effort**: 8 hours | **Impact**: Medium

### Goal
Move hardcoded values to configuration, making system extensible without code changes.

### 5.1 Create Centralized Constants

#### New File: `src/csharp/Naner.Common/NanerConstants.cs`

```csharp
namespace Naner.Common;

/// <summary>
/// Centralized constants for Naner application.
/// </summary>
public static class NanerConstants
{
    public const string Version = "1.0.0";
    public const string ProductName = "Naner Terminal Launcher";
    public const string InitializationMarkerFile = ".naner-initialized";
    public const string VersionFile = ".naner-version";
    public const string ConfigFileName = "naner.json";
    public const string VendorsConfigFileName = "vendors.json";

    public static class GitHub
    {
        public const string Owner = "baileyrd";
        public const string Repo = "naner";
        public const string UserAgent = "Naner/1.0.0";
    }

    public static class DirectoryNames
    {
        public const string Bin = "bin";
        public const string Vendor = "vendor";
        public const string VendorBin = "vendor/bin";
        public const string Config = "config";
        public const string Home = "home";
        public const string Plugins = "plugins";
        public const string Logs = "logs";
        public const string Downloads = ".downloads";
    }

    public static class Executables
    {
        public const string Naner = "naner.exe";
        public const string NanerInit = "naner-init.exe";
        public const string WindowsTerminal = "wt.exe";
        public const string PowerShell = "pwsh.exe";
        public const string Bash = "bash.exe";
        public const string SevenZip = "7z.exe";
    }
}
```

### 5.2 Load Vendors from Configuration

#### File: `config/vendors.json` (created in Phase 1.2.3)

Make vendors configurable rather than hardcoded in source code.

#### New File: `src/csharp/Naner.Common/Services/VendorConfigurationLoader.cs`

```csharp
namespace Naner.Common.Services;

public class VendorConfigurationLoader
{
    private readonly string _configPath;
    private readonly ILogger _logger;

    public VendorConfigurationLoader(string nanerRoot, ILogger logger)
    {
        _configPath = Path.Combine(nanerRoot, NanerConstants.DirectoryNames.Config, NanerConstants.VendorsConfigFileName);
        _logger = logger;
    }

    public List<VendorDefinition> LoadVendors()
    {
        if (!File.Exists(_configPath))
        {
            _logger.Warning($"Vendor configuration not found: {_configPath}");
            return GetDefaultVendors();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<VendorConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            return config?.Vendors ?? GetDefaultVendors();
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to load vendor configuration: {ex.Message}");
            _logger.Info("Using default vendor definitions");
            return GetDefaultVendors();
        }
    }

    private List<VendorDefinition> GetDefaultVendors()
    {
        // Fallback to built-in definitions
        return new List<VendorDefinition>
        {
            new VendorDefinition
            {
                Name = "7-Zip",
                ExtractDir = "7zip",
                SourceType = VendorSourceType.StaticUrl,
                StaticUrl = "https://www.7-zip.org/a/7z2408-x64.msi",
                FileName = "7z2408-x64.msi"
            },
            new VendorDefinition
            {
                Name = "PowerShell",
                ExtractDir = "powershell",
                SourceType = VendorSourceType.GitHub,
                GitHubOwner = "PowerShell",
                GitHubRepo = "PowerShell",
                AssetPattern = "win-x64.zip",
                FallbackUrl = "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/PowerShell-7.4.6-win-x64.zip"
            },
            new VendorDefinition
            {
                Name = "Windows Terminal",
                ExtractDir = "terminal",
                SourceType = VendorSourceType.GitHub,
                GitHubOwner = "microsoft",
                GitHubRepo = "terminal",
                AssetPattern = "Microsoft.WindowsTerminal_",
                AssetPatternEnd = "_x64.zip",
                FallbackUrl = "https://github.com/microsoft/terminal/releases/download/v1.21.2361.0/Microsoft.WindowsTerminal_1.21.2361.0_x64.zip"
            },
            new VendorDefinition
            {
                Name = "MSYS2 (Git/Bash)",
                ExtractDir = "msys64",
                SourceType = VendorSourceType.StaticUrl,
                StaticUrl = "https://repo.msys2.org/distrib/x86_64/msys2-base-x86_64-20240727.tar.xz",
                FileName = "msys2-base-x86_64-20240727.tar.xz"
            }
        };
    }
}

public class VendorConfiguration
{
    public List<VendorDefinition> Vendors { get; set; } = new();
}
```

**Impact**:
- ✅ Vendors can be added/modified without recompiling
- ✅ Easy to customize for different environments
- ✅ Fallback to defaults if config missing

---

## Phase 6: Testing Infrastructure
**Priority**: 🟢 LOW | **Effort**: 12 hours | **Impact**: High (Long-term)

### Goal
Enable unit testing through proper abstractions and dependency injection.

### 6.1 Add Test Project

#### New File: `src/csharp/Naner.Tests/Naner.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageReference Include="xunit" Version="2.8.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Naner.Common\Naner.Common.csproj" />
    <ProjectReference Include="..\Naner.Configuration\Naner.Configuration.csproj" />
    <ProjectReference Include="..\Naner.Launcher\Naner.Launcher.csproj" />
  </ItemGroup>

</Project>
```

### 6.2 Example Unit Tests

#### New File: `src/csharp/Naner.Tests/Services/HttpDownloadServiceTests.cs`

```csharp
using Moq;
using Xunit;
using FluentAssertions;
using Naner.Common.Services;
using Naner.Common.Abstractions;

namespace Naner.Tests.Services;

public class HttpDownloadServiceTests
{
    [Fact]
    public async Task DownloadFileAsync_WithValidUrl_DownloadsSuccessfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var httpClient = new HttpClient();
        var service = new HttpDownloadService(httpClient, mockLogger.Object);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            var result = await service.DownloadFileAsync(
                "https://www.example.com/",
                tempFile,
                "test-file");

            // Assert
            result.Should().BeTrue();
            File.Exists(tempFile).Should().BeTrue();
            mockLogger.Verify(
                x => x.Status(It.Is<string>(s => s.Contains("Downloading"))),
                Times.Once);
            mockLogger.Verify(
                x => x.Success(It.Is<string>(s => s.Contains("Downloaded"))),
                Times.Once);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task DownloadFileAsync_WithInvalidUrl_ReturnsFalse()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var httpClient = new HttpClient();
        var service = new HttpDownloadService(httpClient, mockLogger.Object);
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            var result = await service.DownloadFileAsync(
                "https://invalid-url-that-does-not-exist-12345.com/",
                tempFile,
                "test-file");

            // Assert
            result.Should().BeFalse();
            mockLogger.Verify(
                x => x.Failure(It.Is<string>(s => s.Contains("failed"))),
                Times.Once);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
```

#### New File: `src/csharp/Naner.Tests/Services/VersionComparerTests.cs`

```csharp
using Xunit;
using FluentAssertions;
using Naner.Init;

namespace Naner.Tests.Services;

public class VersionComparerTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("1.0.1", "1.0.0", 1)]
    [InlineData("1.0.0", "1.0.1", -1)]
    [InlineData("2.0.0", "1.9.9", 1)]
    [InlineData("v1.0.0", "1.0.0", 0)]
    [InlineData("1.0.0-beta", "1.0.0", 0)]
    public void Compare_ReturnsCorrectComparison(string version1, string version2, int expected)
    {
        // Act
        var result = VersionComparer.Compare(version1, version2);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("1.0.1", "1.0.0", true)]
    [InlineData("1.0.0", "1.0.1", false)]
    [InlineData("2.0.0", "1.9.9", true)]
    [InlineData("v1.0.1", "1.0.0", true)]
    public void IsNewer_ReturnsCorrectResult(string version1, string version2, bool expected)
    {
        // Act
        var result = VersionComparer.IsNewer(version1, version2);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("v1.0.0", "1.0.0")]
    [InlineData("1.0.0-beta", "1.0.0")]
    [InlineData("V2.3.4-rc1", "2.3.4")]
    public void Normalize_RemovesPrefixAndSuffix(string input, string expected)
    {
        // Act
        var result = VersionComparer.Normalize(input);

        // Assert
        result.Should().Be(expected);
    }
}
```

**Impact**:
- ✅ Enables confident refactoring
- ✅ Prevents regressions
- ✅ Documents expected behavior
- ✅ Supports TDD for new features

---

## Phase 7: Documentation & Final Cleanup
**Priority**: 🟢 LOW | **Effort**: 4 hours | **Impact**: Medium

### 7.1 Update README with New Architecture

Document the refactored architecture, service responsibilities, and configuration options.

### 7.2 Add Architecture Diagram

Create visual documentation showing:
- Project dependencies
- Service interactions
- Data flow

### 7.3 Code Cleanup Checklist

- [ ] Remove all empty catch blocks or add logging
- [ ] Consistent error handling patterns
- [ ] Remove dead code
- [ ] Consistent naming conventions
- [ ] XML documentation on all public APIs
- [ ] Remove magic numbers/strings

---

## Implementation Strategy

### Recommended Order

1. **Week 1**: Phase 1 (Foundation)
   - Fix Naner.Init dependencies
   - Consolidate VendorDownloader classes
   - **Milestone**: Zero duplicate code between projects

2. **Week 2**: Phase 2 (Extract Services)
   - HttpDownloadService
   - ArchiveExtractorService
   - ConsoleManager
   - **Milestone**: All shared functionality in services

3. **Week 3**: Phase 3 (Interfaces)
   - Add ILogger, IConfigurationManager, etc.
   - Update Logger to adapter pattern
   - **Milestone**: All services have interfaces

4. **Week 4**: Phase 4 (Break Up God Classes)
   - Refactor Naner.Launcher.Program
   - Create command pattern
   - Refactor vendor installer
   - **Milestone**: No class over 200 lines

5. **Week 5**: Phase 5 (Configuration)
   - Centralize constants
   - Load vendors from config
   - **Milestone**: Minimal hardcoded values

6. **Week 6**: Phase 6 (Testing)
   - Add test project
   - Write unit tests for services
   - **Milestone**: 50%+ code coverage

7. **Week 7**: Phase 7 (Documentation & Cleanup)
   - Update documentation
   - Final cleanup
   - **Milestone**: Production-ready

---

## Success Metrics

### Before Refactoring
- **Code Duplication**: ~800 lines duplicated
- **Modularity Score**: 4.5/10
- **Testability**: 3/10 (no tests possible)
- **Largest Class**: 695 lines (Program.cs)
- **Service Interfaces**: 0
- **Unit Tests**: 0

### After Refactoring (Target)
- **Code Duplication**: <50 lines
- **Modularity Score**: 8.5/10
- **Testability**: 9/10 (all services mockable)
- **Largest Class**: <200 lines
- **Service Interfaces**: 12+
- **Unit Tests**: 50+ tests, 60%+ coverage

---

## Risk Mitigation

### Risks

1. **Breaking existing functionality**
   - Mitigation: Write tests before refactoring
   - Mitigation: Refactor incrementally, test after each phase

2. **Increased complexity initially**
   - Mitigation: Follow phases in order
   - Mitigation: Document architectural decisions

3. **Time investment**
   - Mitigation: Each phase delivers standalone value
   - Mitigation: Can stop after any phase with improvements

### Rollback Strategy

- Git branch per phase
- Keep backward compatibility via adapter pattern (Logger)
- Can deploy partial refactoring

---

## Conclusion

This refactoring plan addresses all identified DRY, SOLID, and modularity issues through systematic, phased improvements. The result will be a codebase that is:

- ✅ **Maintainable**: No duplication, clear responsibilities
- ✅ **Testable**: Interfaces and dependency injection throughout
- ✅ **Extensible**: Open/Closed principle, configuration-driven
- ✅ **Modular**: Proper project boundaries and service isolation
- ✅ **Professional**: Industry best practices, ready for team collaboration

**Estimated total effort**: 60-70 hours over 6-7 weeks

**Priority**: Start with Phase 1 (Foundation) immediately - it provides the highest impact for lowest effort and eliminates the most critical issues.
