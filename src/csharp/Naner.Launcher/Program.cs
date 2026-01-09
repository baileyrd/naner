using System;
using System.IO;
using System.Linq;
using CommandLine;
using Naner.Common;
using Naner.Configuration;
using Naner.Launcher;

// Command-line options
class Options
{
    [Option('p', "profile", Required = false,
        HelpText = "Terminal profile to launch (Unified, PowerShell, Bash, CMD)")]
    public string? Profile { get; set; }

    [Option('e', "environment", Required = false,
        HelpText = "Environment name (default, work, personal, etc.)")]
    public string Environment { get; set; } = "default";

    [Option('d', "directory", Required = false,
        HelpText = "Starting directory for terminal session")]
    public string? Directory { get; set; }

    [Option('c', "config", Required = false,
        HelpText = "Path to naner.json config file")]
    public string? ConfigPath { get; set; }

    [Option("debug", Required = false,
        HelpText = "Enable debug/verbose output")]
    public bool Debug { get; set; }

    [Option('v', "version", Required = false,
        HelpText = "Display version information")]
    public bool Version { get; set; }
}

class Program
{
    private const string Version = "0.1.0-alpha";
    private const string PhaseName = "Phase 10.4 - Usability & Testing";

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

    static void ShowVersion()
    {
        Console.WriteLine($"naner {Version}");
        Console.WriteLine($"{PhaseName}");
    }

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
        Console.WriteLine($"Version: {Version}");
        Console.WriteLine($"Phase: {PhaseName}");
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

    static int RunLauncher(Options opts)
    {
        try
        {
            // Handle version flag
            if (opts.Version)
            {
                ShowVersion();
                return 0;
            }

            // Display header (unless in quiet mode)
            Logger.Header("Naner Terminal Launcher");
            Logger.Debug($"Version: {Version}", opts.Debug);
            Logger.Debug($"Phase: {PhaseName}", opts.Debug);

            // 1. Find NANER_ROOT
            Logger.Debug("Finding Naner root directory...", opts.Debug);
            var nanerRoot = PathResolver.FindNanerRoot();
            Logger.Success($"Naner Root: {nanerRoot}");

            // 2. Load configuration
            var configPath = opts.ConfigPath ?? Path.Combine(nanerRoot, "config", "naner.json");
            Logger.Debug($"Loading configuration from: {configPath}", opts.Debug);

            var configManager = new ConfigurationManager(nanerRoot);
            var config = configManager.Load(configPath);

            Logger.Success($"Configuration loaded");
            Logger.Debug($"Default profile: {config.DefaultProfile}", opts.Debug);

            // 3. Setup environment variables
            Logger.Status("Setting up environment...");
            PathResolver.SetupEnvironment(nanerRoot, opts.Environment);
            configManager.SetupEnvironmentVariables();

            var unifiedPath = configManager.BuildUnifiedPath(config.Advanced.InheritSystemPath);
            Environment.SetEnvironmentVariable("PATH", unifiedPath, EnvironmentVariableTarget.Process);

            Logger.Success("Environment configured");

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
            Logger.NewLine();
            var launcher = new TerminalLauncher(nanerRoot, config, opts.Debug);
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
}
