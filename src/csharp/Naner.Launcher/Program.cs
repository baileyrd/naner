using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CommandLine;
using Naner.Configuration;
using Naner.Configuration.Abstractions;
using Naner.Core;
using Naner.Launcher;
using Naner.Launcher.Models;
using Naner.Vendors.Services;

class Program
{

    static int Main(string[] args)
    {
        // Determine if we need console output
        // Use singleton ConsoleManager for consistent state tracking
        bool needsConsole = CommandRouter.NeedsConsole(args) || FirstRunDetector.IsFirstRun() ||
                           args.Any(a => a.ToLower() == "--debug") ||
                           args.Any(a => a.ToLower() == "--export-env");

        if (needsConsole)
        {
            ConsoleManager.Instance.EnsureConsoleAttached();
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

            // 2. Load configuration (auto-discovers naner.json, naner.yaml, or naner.yml if not specified)
            var configPath = opts.ConfigPath;
            Logger.Debug($"Loading configuration from: {configPath ?? "auto-discover"}", opts.Debug);

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

            // Handle --export-env: output environment setup commands and exit
            if (opts.ExportEnv)
            {
                return HandleExportEnv(config, unifiedPath, opts.Format, opts.NoComments);
            }

            // Handle --setup-only: environment is already configured, just exit
            if (opts.SetupOnly)
            {
                Logger.Debug("Setup-only mode: environment configured, exiting without launching terminal", opts.Debug);
                return 0;
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

    static int HandleExportEnv(NanerConfig config, string unifiedPath, string format, bool noComments)
    {
        try
        {
            var shellFormat = EnvironmentExporter.ParseFormat(format);

            // Collect all environment variables that were set
            var envVars = new Dictionary<string, string>();

            // Add core Naner variables
            var nanerRoot = Environment.GetEnvironmentVariable("NANER_ROOT");
            if (!string.IsNullOrEmpty(nanerRoot))
                envVars["NANER_ROOT"] = nanerRoot;

            var nanerEnv = Environment.GetEnvironmentVariable("NANER_ENVIRONMENT");
            if (!string.IsNullOrEmpty(nanerEnv))
                envVars["NANER_ENVIRONMENT"] = nanerEnv;

            var nanerHome = Environment.GetEnvironmentVariable("NANER_HOME");
            if (!string.IsNullOrEmpty(nanerHome))
                envVars["NANER_HOME"] = nanerHome;

            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                envVars["HOME"] = home;

            // Add configured environment variables
            foreach (var (key, value) in config.Environment.EnvironmentVariables)
            {
                var expandedValue = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrEmpty(expandedValue))
                    envVars[key] = expandedValue;
            }

            // Generate and output the export commands
            // TrimEnd removes trailing newline to avoid empty line errors when piped to Invoke-Expression
            var output = EnvironmentExporter.Export(envVars, unifiedPath, shellFormat, noComments);
            Console.Write(output.TrimEnd());

            return 0;
        }
        catch (ArgumentException ex)
        {
            Logger.Failure(ex.Message);
            return 1;
        }
    }

    static int HandleFirstRun()
    {
        // Get detailed first-run information
        var firstRunInfo = FirstRunDetector.GetFirstRunInfo();

        Logger.Header("First Run Detected");
        Console.WriteLine();

        // Display what triggered the first-run detection
        Console.WriteLine("The following issues were detected:");
        Console.WriteLine();
        foreach (var message in firstRunInfo.Messages)
        {
            Console.WriteLine($"  • {message}");
        }
        if (firstRunInfo.NanerRoot != null)
        {
            Console.WriteLine();
            Console.WriteLine($"  Checked location: {firstRunInfo.NanerRoot}");
        }
        Console.WriteLine();

        Console.WriteLine("Please run 'naner-init' to initialize Naner.");
        Console.WriteLine();
        Console.WriteLine("naner-init provides:");
        Console.WriteLine("  • Automatic download of latest Naner from GitHub");
        Console.WriteLine("  • Automatic updates when new versions are available");
        Console.WriteLine("  • Simpler setup process");
        Console.WriteLine();

        return 0;
    }
}
