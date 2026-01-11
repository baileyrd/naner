using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CommandLine;
using Naner.Common;
using Naner.Configuration;
using Naner.Launcher;
using Naner.Launcher.Commands;
using Naner.Launcher.Models;
using Naner.Launcher.Services;
using Naner.Common.Services;

class Program
{

    static int Main(string[] args)
    {
        // Determine if we need console output
        var consoleManager = new ConsoleManager();
        bool needsConsole = CommandRouter.NeedsConsole(args) || FirstRunDetector.IsFirstRun() ||
                           args.Any(a => a.ToLower() == "--debug");

        if (needsConsole)
        {
            consoleManager.EnsureConsoleAttached();
        }

        // Route commands using command pattern
        var router = new CommandRouter();
        var result = router.Route(args);

        // If router returns -1, it means no command was matched, proceed with normal launcher
        if (result != -1)
        {
            return result;
        }

        // All commands are now handled by CommandRouter
        // Legacy command handling removed - commands extracted to ICommand implementations

        // Check for first run
        if (FirstRunDetector.IsFirstRun())
        {
            return HandleFirstRun();
        }

        // Normal command parsing
        return Parser.Default.ParseArguments<LaunchOptions>(args)
            .MapResult(
                opts => RunLauncher(opts),
                errs => 1);
    }

    static void ShowVersion()
    {
        Console.WriteLine($"naner {NanerConstants.Version}");
        Console.WriteLine($"{NanerConstants.PhaseName}");
    }

    static void ShowHelp()
    {
        Logger.Header("Naner Terminal Launcher");
        Console.WriteLine($"Version {NanerConstants.Version} - {NanerConstants.PhaseName}");
        Console.WriteLine();

        Console.WriteLine("USAGE:");
        Console.WriteLine("  naner.exe [OPTIONS]");
        Console.WriteLine();

        Console.WriteLine("COMMANDS:");
        Console.WriteLine("  init [PATH]                Initialize Naner in specified directory");
        Console.WriteLine("                             Options: --minimal, --quick, --skip-vendors, --with-vendors");
        Console.WriteLine("  setup-vendors              Download and install vendor dependencies");
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
        Console.WriteLine("  naner.exe init                     # Interactive setup wizard");
        Console.WriteLine("  naner.exe init --minimal           # Quick setup in current dir");
        Console.WriteLine("  naner.exe init --minimal --with-vendors  # Setup with auto vendor download");
        Console.WriteLine("  naner.exe init C:\\MyNaner          # Setup in specific directory");
        Console.WriteLine("  naner.exe                          # Launch default profile");
        Console.WriteLine("  naner.exe --profile PowerShell     # Launch PowerShell profile");
        Console.WriteLine("  naner.exe -p Bash -d C:\\projects   # Launch Bash in specific dir");
        Console.WriteLine("  naner.exe --debug                  # Show detailed diagnostics");
        Console.WriteLine("  naner.exe --diagnose               # Check installation health");
        Console.WriteLine();

        Console.WriteLine("REQUIREMENTS:");
        Console.WriteLine("  naner.exe must be run from within a Naner installation that");
        Console.WriteLine("  contains bin/, vendor/, and config/ subdirectories.");
        Console.WriteLine();

        Console.WriteLine("DOCUMENTATION:");
        Console.WriteLine("  https://github.com/yourusername/naner");
        Console.WriteLine();
    }

    static int RunDiagnostics()
    {
        Logger.Header("Naner Diagnostics");
        Console.WriteLine($"Version: {NanerConstants.Version}");
        Console.WriteLine($"Phase: {NanerConstants.PhaseName}");
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
            Logger.NewLine();

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
                Logger.Success($"Configuration file found");
                Logger.Info($"  Path: {configPath}");
                try
                {
                    var configManager = new ConfigurationManager(nanerRoot);
                    var config = configManager.Load(configPath);
                    Logger.Info($"  Default Profile: {config.DefaultProfile}");
                    Logger.Info($"  Vendor Paths: {config.VendorPaths.Count}");
                    Logger.Info($"  Profiles: {config.Profiles.Count}");
                    Logger.NewLine();
                    Logger.Status("Vendor Paths:");
                    if (config.VendorPaths.TryGetValue("WindowsTerminal", out var wtPath))
                    {
                        var exists = File.Exists(wtPath);
                        var symbol = exists ? "✓" : "✗";
                        var color = exists ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.ForegroundColor = color;
                        Console.WriteLine($"  {symbol} WindowsTerminal: {wtPath}");
                        Console.ResetColor();
                    }
                    if (config.VendorPaths.TryGetValue("PowerShell", out var pwshPath))
                    {
                        var exists = File.Exists(pwshPath);
                        var symbol = exists ? "✓" : "✗";
                        var color = exists ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.ForegroundColor = color;
                        Console.WriteLine($"  {symbol} PowerShell: {pwshPath}");
                        Console.ResetColor();
                    }
                    if (config.VendorPaths.TryGetValue("GitBash", out var bashPath))
                    {
                        var exists = File.Exists(bashPath);
                        var symbol = exists ? "✓" : "✗";
                        var color = exists ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.ForegroundColor = color;
                        Console.WriteLine($"  {symbol} GitBash: {bashPath}");
                        Console.ResetColor();
                    }
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
                else
                {
                    Logger.Info($"  {envVar}=(not set)");
                }
            }

            Logger.NewLine();
            Logger.Success("Diagnostics complete - Naner installation appears healthy");
            return 0;
        }
        catch (Exception ex)
        {
            Logger.Failure("NANER_ROOT not found");
            Logger.NewLine();
            Logger.Info("Details:");
            Logger.Info($"  {ex.Message}");
            Logger.NewLine();
            Logger.Info("This usually means:");
            Logger.Info("  1. You're running naner.exe outside the Naner directory");
            Logger.Info("  2. The Naner directory structure is incomplete");
            Logger.Info("  3. You need to set NANER_ROOT environment variable");
            Logger.NewLine();
            Logger.Info("Try:");
            Logger.Info("  1. cd <your-naner-directory>");
            Logger.Info("  2. .\\vendor\\bin\\naner.exe --diagnose");
            return 1;
        }
    }

    static int RunLauncher(LaunchOptions opts)
    {
        try
        {
            // Handle version flag
            if (opts.Version)
            {
                ShowVersion();
                return 0;
            }

            // Quiet mode: suppress output unless debug is enabled
            bool quietMode = !opts.Debug;

            // Display header only in debug mode
            if (!quietMode)
            {
                Logger.Header("Naner Terminal Launcher");
                Logger.Debug($"Version: {NanerConstants.Version}", opts.Debug);
                Logger.Debug($"Phase: {NanerConstants.PhaseName}", opts.Debug);
            }

            // 1. Find NANER_ROOT
            Logger.Debug("Finding Naner root directory...", opts.Debug);
            var nanerRoot = PathResolver.FindNanerRoot();
            if (!quietMode)
            {
                Logger.Success($"Naner Root: {nanerRoot}");
            }

            // 2. Load configuration
            var configPath = opts.ConfigPath ?? Path.Combine(nanerRoot, "config", "naner.json");
            Logger.Debug($"Loading configuration from: {configPath}", opts.Debug);

            var configManager = new ConfigurationManager(nanerRoot);
            var config = configManager.Load(configPath);

            if (!quietMode)
            {
                Logger.Success($"Configuration loaded");
            }
            Logger.Debug($"Default profile: {config.DefaultProfile}", opts.Debug);

            // 3. Setup environment variables
            if (!quietMode)
            {
                Logger.Status("Setting up environment...");
            }
            PathResolver.SetupEnvironment(nanerRoot, opts.Environment);
            configManager.SetupEnvironmentVariables();

            var unifiedPath = configManager.BuildUnifiedPath(config.Advanced.InheritSystemPath);
            Environment.SetEnvironmentVariable("PATH", unifiedPath, EnvironmentVariableTarget.Process);

            if (!quietMode)
            {
                Logger.Success("Environment configured");
            }

            if (opts.Debug)
            {
                Logger.Debug($"NANER_ROOT={Environment.GetEnvironmentVariable("NANER_ROOT")}", true);
                Logger.Debug($"NANER_ENVIRONMENT={Environment.GetEnvironmentVariable("NANER_ENVIRONMENT")}", true);
                Logger.Debug($"PATH (first 150 chars)={unifiedPath.Substring(0, Math.Min(150, unifiedPath.Length))}...", true);
            }

            // 4. Determine profile to launch
            var profileName = opts.Profile ?? config.DefaultProfile;
            Logger.Debug($"Selected profile: {profileName}", opts.Debug);

            // 5. Launch terminal with profile
            if (!quietMode)
            {
                Logger.NewLine();
            }
            var launcher = new TerminalLauncher(nanerRoot, configManager, opts.Debug);
            var exitCode = launcher.LaunchProfile(profileName, opts.Directory);

            return exitCode;
        }
        catch (DirectoryNotFoundException ex)
        {
            Logger.Failure("Could not locate Naner root directory");
            Logger.NewLine();
            Console.WriteLine(ex.Message);
            Logger.Debug(ex.ToString(), opts.Debug);
            return 1;
        }
        catch (FileNotFoundException ex)
        {
            Logger.Failure($"File not found: {ex.Message}");
            Logger.Debug(ex.ToString(), opts.Debug);
            return 1;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Fatal error: {ex.Message}");
            Logger.Debug(ex.ToString(), opts.Debug);
            return 1;
        }
    }

    // RunInit and RunInitAsync methods moved to InitCommand.cs
    // This improves SRP by extracting command logic from Program.cs

    static int HandleFirstRun()
    {
        Logger.Header("First Run Detected");
        Console.WriteLine();
        Console.WriteLine("It looks like this is your first time running Naner,");
        Console.WriteLine("or the installation is incomplete.");
        Console.WriteLine();
        Logger.Warning("IMPORTANT: Please use 'naner-init' for initialization!");
        Console.WriteLine();
        Console.WriteLine("naner-init provides:");
        Console.WriteLine("  • Automatic download of latest Naner from GitHub");
        Console.WriteLine("  • Automatic updates when new versions are available");
        Console.WriteLine("  • Simpler setup process");
        Console.WriteLine();
        Console.WriteLine("Would you like to:");
        Console.WriteLine("  1. Exit and run 'naner-init' (recommended)");
        Console.WriteLine("  2. Run legacy setup wizard");
        Console.WriteLine("  3. Quick setup in current directory (legacy)");
        Console.WriteLine();
        Console.Write("Enter choice (1-3) [1]: ");

        var choice = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(choice))
        {
            choice = "1";
        }

        // Use InitCommand for option 2 and 3
        return choice switch
        {
            "1" => 0,
            "2" => new InitCommand().Execute(Array.Empty<string>()),
            "3" => new InitCommand().Execute(new[] { "--minimal" }),
            _ => 0
        };
    }

    // RunSetupVendors and RunSetupVendorsAsync moved to SetupVendorsCommand.cs
    // This improves SRP by extracting command logic from Program.cs

    // NeedsConsole method removed - use CommandRouter.NeedsConsole() instead
    // This eliminates duplication between Program.cs and CommandRouter.cs
}
