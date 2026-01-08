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
    private const string PhaseName = "Phase 10.2 - Core Migration (Pure C#)";

    static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<Options>(args)
            .MapResult(
                opts => RunLauncher(opts),
                errs => 1);
    }

    static int RunLauncher(Options opts)
    {
        try
        {
            // Handle version flag
            if (opts.Version)
            {
                Logger.Header($"Naner Terminal Launcher v{Version}");
                Console.WriteLine(PhaseName);
                Console.WriteLine();
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
            Logger.Failure(ex.Message);
            Logger.Info("Make sure you're running naner.exe from within the Naner directory structure.");
            Logger.Debug(ex.ToString(), opts.Debug);
            return 1;
        }
        catch (FileNotFoundException ex)
        {
            Logger.Failure(ex.Message);
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
