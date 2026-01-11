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
using Naner.Vendors.Services;

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

    // ShowHelp method removed - logic moved to HelpTextProvider
    // This improves modularity by separating presentation from entry point logic

    // RunDiagnostics method removed - logic moved to DiagnosticsService
    // This improves modularity by extracting diagnostic logic from Program.cs

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
