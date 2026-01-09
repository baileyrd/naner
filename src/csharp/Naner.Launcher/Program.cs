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
    private const string Version = "1.0.0";
    private const string PhaseName = "Production Release - Pure C# Implementation";

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
            if (firstArg == "init")
            {
                return RunInit(args.Skip(1).ToArray());
            }

            // Setup vendors command
            if (firstArg == "setup-vendors")
            {
                return RunSetupVendors();
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

    static int RunInit(string[] args)
    {
        return RunInitAsync(args).GetAwaiter().GetResult();
    }

    static async System.Threading.Tasks.Task<int> RunInitAsync(string[] args)
    {
        // Show deprecation notice
        Logger.NewLine();
        Logger.Warning("NOTICE: The 'naner init' command is deprecated.");
        Logger.Info("Please use 'naner-init' for initialization and updates.");
        Logger.Info("naner-init automatically downloads the latest version from GitHub.");
        Logger.NewLine();
        Console.Write("Continue with legacy init? (y/N): ");
        var response = Console.ReadLine()?.Trim().ToLower();
        if (response != "y" && response != "yes")
        {
            Logger.Info("Cancelled. Please use 'naner-init' instead.");
            return 0;
        }
        Logger.NewLine();

        bool interactive = !args.Contains("--minimal") && !args.Contains("--quick");
        bool skipVendors = args.Contains("--skip-vendors") || args.Contains("--no-vendors");
        bool withVendors = args.Contains("--with-vendors");
        string? targetPath = null;

        // Parse arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--path" && i + 1 < args.Length)
            {
                targetPath = args[i + 1];
                i++;
            }
            else if (!args[i].StartsWith("--") && !args[i].StartsWith("-"))
            {
                targetPath = args[i];
            }
        }

        // Interactive mode with full setup
        if (interactive)
        {
            targetPath ??= SetupManager.PromptInstallLocation();

            try
            {
                targetPath = Path.GetFullPath(targetPath);

                // Run full interactive setup including vendors
                var success = await SetupManager.RunInteractiveSetupAsync(targetPath, skipVendors);
                if (!success)
                {
                    return 1;
                }

                // Ask if user wants to launch terminal
                if (SetupManager.PromptLaunchTerminal())
                {
                    Logger.NewLine();
                    Logger.Info("Launching Naner...");
                    Logger.NewLine();

                    // Launch terminal with default profile
                    try
                    {
                        Environment.SetEnvironmentVariable("NANER_ROOT", targetPath);
                        var configManager = new ConfigurationManager(targetPath);
                        var config = configManager.Load();
                        var launcher = new TerminalLauncher(targetPath, config, false);
                        return launcher.LaunchProfile(string.Empty, null);
                    }
                    catch (Exception launchEx)
                    {
                        Logger.Warning($"Could not launch terminal: {launchEx.Message}");
                        Logger.Info("You can launch manually with: naner");
                        return 0;
                    }
                }
                else
                {
                    Logger.NewLine();
                    Logger.Info("You can launch Naner anytime with: naner");
                    Logger.NewLine();
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Failure($"Setup failed: {ex.Message}");
                return 1;
            }
        }
        else
        {
            // Non-interactive quick mode
            targetPath ??= Environment.CurrentDirectory;
            Logger.Header("Naner Quick Setup");
            Console.WriteLine();

            try
            {
                targetPath = Path.GetFullPath(targetPath);
                Logger.Info($"Installation path: {targetPath}");
                Logger.NewLine();

                // Create directory structure
                if (!SetupManager.CreateDirectoryStructure(targetPath))
                {
                    return 1;
                }

                // Create default configuration
                if (!SetupManager.CreateDefaultConfiguration(targetPath))
                {
                    return 1;
                }

                // Download vendors if --with-vendors flag is set
                if (withVendors && !skipVendors)
                {
                    Logger.NewLine();
                    Logger.Status("Downloading vendor dependencies...");
                    Logger.NewLine();

                    var downloader = new DynamicVendorDownloader(targetPath);
                    await downloader.SetupRequiredVendorsAsync();
                }

                // Create initialization marker
                FirstRunDetector.CreateInitializationMarker(targetPath, Version, PhaseName);
                Logger.Success("Created initialization marker");
                Logger.NewLine();

                // Success message
                Logger.Header("Setup Complete!");
                Console.WriteLine();
                Console.WriteLine("Naner has been initialized successfully!");
                Console.WriteLine();

                if (!withVendors && !skipVendors)
                {
                    Console.WriteLine("To download vendor dependencies:");
                    Console.WriteLine("  naner setup-vendors");
                    Console.WriteLine();
                    Console.WriteLine("Or install manually with PowerShell:");
                    Console.WriteLine("  .\\src\\powershell\\Setup-NanerVendor.ps1");
                    Console.WriteLine();
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logger.Failure($"Setup failed: {ex.Message}");
                return 1;
            }
        }
    }

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

        return choice switch
        {
            "1" => 0,
            "2" => RunInit(Array.Empty<string>()),
            "3" => RunInit(new[] { "--minimal" }),
            _ => 0
        };
    }

    static int RunSetupVendors()
    {
        return RunSetupVendorsAsync().GetAwaiter().GetResult();
    }

    static async System.Threading.Tasks.Task<int> RunSetupVendorsAsync()
    {
        Logger.Header("Naner Vendor Setup");
        Logger.NewLine();

        try
        {
            // Find NANER_ROOT
            string nanerRoot;
            try
            {
                nanerRoot = PathUtilities.FindNanerRoot();
            }
            catch (DirectoryNotFoundException ex)
            {
                Logger.Failure("Could not locate Naner root directory");
                Logger.NewLine();
                Console.WriteLine(ex.Message);
                Logger.NewLine();
                Logger.Info("Please run this command from within your Naner installation,");
                Logger.Info("or run 'naner init' first to set up Naner.");
                return 1;
            }

            Logger.Info($"Naner Root: {nanerRoot}");
            Logger.NewLine();

            // Run vendor download using dynamic fetcher
            var downloader = new DynamicVendorDownloader(nanerRoot);
            await downloader.SetupRequiredVendorsAsync();

            Logger.NewLine();
            Logger.Success("Vendor setup complete!");
            Logger.Info("You can now launch Naner with: naner");

            return 0;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Vendor setup failed: {ex.Message}");
            return 1;
        }
    }
}
