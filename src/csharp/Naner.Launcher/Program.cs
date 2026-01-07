using CommandLine;
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
    private const string PhaseName = "Phase 10.1 - C# Wrapper";

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
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Naner Terminal Launcher v{Version}");
                Console.WriteLine(PhaseName);
                Console.ResetColor();
                return 0;
            }

            // Display header
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Naner Terminal Launcher (C# Wrapper)");
            Console.WriteLine("=====================================");
            Console.ResetColor();
            Console.WriteLine();

            // 1. Find NANER_ROOT
            if (opts.Debug)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[DEBUG] Finding Naner root directory...");
                Console.ResetColor();
            }

            var nanerRoot = PathResolver.FindNanerRoot();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[✓] Naner Root: {nanerRoot}");
            Console.ResetColor();

            // 2. Load configuration
            var configPath = opts.ConfigPath ??
                Path.Combine(nanerRoot, "config", "naner.json");

            if (opts.Debug)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DEBUG] Loading configuration from: {configPath}");
                Console.ResetColor();
            }

            var config = ConfigLoader.Load(configPath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[✓] Configuration: {configPath}");
            Console.ResetColor();
            Console.WriteLine();

            // 3. Setup environment variables
            PathResolver.SetupEnvironment(nanerRoot, opts.Environment);

            var unifiedPath = ConfigLoader.BuildUnifiedPath(config, nanerRoot);
            Environment.SetEnvironmentVariable("PATH", unifiedPath, EnvironmentVariableTarget.Process);

            if (opts.Debug)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[DEBUG] Environment variables set:");
                Console.WriteLine($"  NANER_ROOT={Environment.GetEnvironmentVariable("NANER_ROOT")}");
                Console.WriteLine($"  NANER_ENVIRONMENT={Environment.GetEnvironmentVariable("NANER_ENVIRONMENT")}");
                Console.WriteLine($"  PATH={unifiedPath.Substring(0, Math.Min(200, unifiedPath.Length))}...");
                Console.ResetColor();
            }

            // 4. Extract embedded PowerShell scripts
            if (opts.Debug)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[DEBUG] Extracting embedded PowerShell scripts...");
                Console.ResetColor();
            }

            var scriptDir = PowerShellHost.ExtractEmbeddedScripts();

            if (opts.Debug)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DEBUG] Scripts extracted to: {scriptDir}");
                Console.ResetColor();
            }

            // 5. Build PowerShell arguments
            var psArgs = new List<string>();
            if (opts.Profile != null)
                psArgs.Add($"-Profile {opts.Profile}");
            if (opts.Environment != "default")
                psArgs.Add($"-Environment {opts.Environment}");
            if (opts.Directory != null)
                psArgs.Add($"-StartingDirectory '{opts.Directory}'");
            if (opts.Debug)
                psArgs.Add("-DebugMode");

            // 6. Execute PowerShell launcher
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[*] Launching Naner via PowerShell...");
            Console.ResetColor();
            Console.WriteLine();

            var exitCode = PowerShellHost.ExecuteScript(
                scriptPath: Path.Combine(scriptDir, "Invoke-Naner.ps1"),
                arguments: psArgs,
                debugMode: opts.Debug
            );

            // 7. Cleanup temp files
            PowerShellHost.Cleanup(scriptDir);

            return exitCode;
        }
        catch (DirectoryNotFoundException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[✗] {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Make sure you're running naner.exe from within the Naner directory structure.");
            if (opts.Debug)
            {
                Console.WriteLine();
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
            }
            return 1;
        }
        catch (FileNotFoundException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[✗] {ex.Message}");
            Console.ResetColor();
            if (opts.Debug)
            {
                Console.WriteLine();
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
            }
            return 1;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[✗] Fatal error: {ex.Message}");
            Console.ResetColor();
            if (opts.Debug)
            {
                Console.WriteLine();
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }
}
