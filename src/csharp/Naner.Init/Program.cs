using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Naner.Init;

class Program
{
    private const string Version = "1.0.0";

    // Import Windows API for console attachment
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    private const int ATTACH_PARENT_PROCESS = -1;

    static async Task<int> Main(string[] args)
    {
        try
        {
            // Determine if we need to show console output
            bool needsConsole = NeedsConsole(args);

            if (needsConsole)
            {
                // Attach to parent console if launched from command line
                // or allocate a new console if double-clicked
                if (!AttachConsole(ATTACH_PARENT_PROCESS))
                {
                    AllocConsole();
                }
            }

            // Find or set Naner root directory
            var nanerRoot = FindNanerRoot();

            // Handle command line arguments
            if (args.Length > 0)
            {
                var command = args[0].ToLower();

                switch (command)
                {
                    case "--version":
                    case "-v":
                        Console.WriteLine($"naner-init {Version}");
                        return 0;

                    case "--help":
                    case "-h":
                        ShowHelp();
                        return 0;

                    case "init":
                        return await InitializeNanerAsync(nanerRoot);

                    case "update":
                        return await UpdateNanerAsync(nanerRoot);

                    case "check-update":
                        return await CheckForUpdatesAsync(nanerRoot);

                    default:
                        // Pass through to naner.exe
                        break;
                }
            }

            // Default behavior: check for updates and launch naner
            return await RunDefaultFlowAsync(nanerRoot, args);
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error($"Fatal error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Default flow: check initialization, check updates, launch naner.
    /// </summary>
    static async Task<int> RunDefaultFlowAsync(string nanerRoot, string[] args)
    {
        var updater = new NanerUpdater(nanerRoot);

        // Check if initialized
        if (!updater.IsInitialized())
        {
            // Need console for initialization prompts
            EnsureConsoleAttached();

            ConsoleHelper.Header("Naner Initializer");
            ConsoleHelper.NewLine();
            ConsoleHelper.Info("Naner is not initialized yet.");
            ConsoleHelper.Info("This will download the latest version of Naner from GitHub.");
            ConsoleHelper.NewLine();

            Console.Write("Initialize Naner now? (Y/n): ");
            var response = Console.ReadLine()?.Trim().ToLower();

            if (response != "" && response != "y" && response != "yes")
            {
                ConsoleHelper.Info("Initialization cancelled.");
                return 0;
            }

            if (!await updater.InitializeAsync())
            {
                return 1;
            }

            // Download essential vendors (7-Zip, PowerShell, Windows Terminal)
            ConsoleHelper.NewLine();
            ConsoleHelper.Info("Setting up essential dependencies...");
            ConsoleHelper.NewLine();

            var vendorDownloader = new EssentialVendorDownloader(nanerRoot);
            await vendorDownloader.DownloadAllEssentialsAsync();

            // Note about optional additional tools
            ConsoleHelper.NewLine();
            ConsoleHelper.Info("Additional development tools can be installed later.");
            ConsoleHelper.Info("Run 'naner setup-vendors' for more optional tools.");

            ConsoleHelper.NewLine();
            ConsoleHelper.Success("Naner is ready! Run 'naner' to launch your terminal environment.");
            return 0;
        }

        // Check for updates (silently, only show if update available)
        bool showUpdateNotification = false;
        string? latestVersion = null;
        try
        {
            var (updateAvailable, version) = await updater.CheckForUpdateAsync();
            if (updateAvailable && version != null)
            {
                showUpdateNotification = true;
                latestVersion = version;
            }
        }
        catch
        {
            // Silently ignore update check failures
        }

        // If update available, attach console to show notification
        if (showUpdateNotification)
        {
            EnsureConsoleAttached();
            ConsoleHelper.Warning($"A new version of Naner is available: {latestVersion}");
            ConsoleHelper.Info("Run 'naner-init update' to update");
            ConsoleHelper.NewLine();
        }

        // Launch naner.exe (will use GUI mode, no window flash)
        return updater.LaunchNaner(args);
    }

    /// <summary>
    /// Handles 'init' command.
    /// </summary>
    static async Task<int> InitializeNanerAsync(string nanerRoot)
    {
        var updater = new NanerUpdater(nanerRoot);

        if (updater.IsInitialized())
        {
            ConsoleHelper.Warning("Naner is already initialized.");
            ConsoleHelper.Info($"Current version: {updater.GetInstalledVersion()}");
            ConsoleHelper.Info("Use 'naner-init update' to update to the latest version.");
            return 0;
        }

        if (!await updater.InitializeAsync())
        {
            return 1;
        }

        // Download essential vendors (7-Zip, PowerShell, Windows Terminal, MSYS2)
        ConsoleHelper.NewLine();
        ConsoleHelper.Info("Setting up essential dependencies...");
        ConsoleHelper.NewLine();

        var vendorDownloader = new EssentialVendorDownloader(nanerRoot);
        await vendorDownloader.DownloadAllEssentialsAsync();

        // Note about optional additional tools
        ConsoleHelper.NewLine();
        ConsoleHelper.Info("Additional development tools can be installed later.");
        ConsoleHelper.Info("Run 'naner setup-vendors' for more optional tools.");

        ConsoleHelper.NewLine();
        ConsoleHelper.Success("Naner is ready! Run 'naner' to launch your terminal environment.");
        return 0;
    }

    /// <summary>
    /// Handles 'update' command.
    /// </summary>
    static async Task<int> UpdateNanerAsync(string nanerRoot)
    {
        var updater = new NanerUpdater(nanerRoot);

        if (!updater.IsInitialized())
        {
            ConsoleHelper.Error("Naner is not initialized yet.");
            ConsoleHelper.Info("Run 'naner-init' to initialize first.");
            return 1;
        }

        var currentVersion = updater.GetInstalledVersion();
        ConsoleHelper.Info($"Current version: {currentVersion}");

        var (updateAvailable, latestVersion) = await updater.CheckForUpdateAsync();

        if (!updateAvailable)
        {
            ConsoleHelper.Success("Naner is already up to date!");
            return 0;
        }

        ConsoleHelper.Info($"Latest version: {latestVersion}");
        ConsoleHelper.NewLine();

        Console.Write("Update now? (Y/n): ");
        var response = Console.ReadLine()?.Trim().ToLower();

        if (response != "" && response != "y" && response != "yes")
        {
            ConsoleHelper.Info("Update cancelled.");
            return 0;
        }

        return await updater.InstallOrUpdateNanerAsync(isUpdate: true) ? 0 : 1;
    }

    /// <summary>
    /// Handles 'check-update' command.
    /// </summary>
    static async Task<int> CheckForUpdatesAsync(string nanerRoot)
    {
        var updater = new NanerUpdater(nanerRoot);

        if (!updater.IsInitialized())
        {
            ConsoleHelper.Error("Naner is not initialized yet.");
            return 1;
        }

        var currentVersion = updater.GetInstalledVersion();
        ConsoleHelper.Info($"Current version: {currentVersion}");

        var (updateAvailable, latestVersion) = await updater.CheckForUpdateAsync();

        if (updateAvailable && latestVersion != null)
        {
            ConsoleHelper.Warning($"Update available: {latestVersion}");
            ConsoleHelper.Info("Run 'naner-init update' to update");
            return 0;
        }

        ConsoleHelper.Success("Naner is up to date!");
        return 0;
    }

    /// <summary>
    /// Finds the Naner root directory.
    /// </summary>
    static string FindNanerRoot()
    {
        // Check NANER_ROOT environment variable
        var nanerRoot = Environment.GetEnvironmentVariable("NANER_ROOT");
        if (!string.IsNullOrEmpty(nanerRoot) && Directory.Exists(nanerRoot))
        {
            return nanerRoot;
        }

        // Check current directory and parent directories
        var currentDir = Directory.GetCurrentDirectory();
        var searchDir = currentDir;

        for (int i = 0; i < 10; i++)
        {
            // Look for markers: vendor/, config/, bin/ directories
            if (Directory.Exists(Path.Combine(searchDir, "vendor")) ||
                Directory.Exists(Path.Combine(searchDir, "config")) ||
                File.Exists(Path.Combine(searchDir, ".naner-initialized")))
            {
                return searchDir;
            }

            var parent = Directory.GetParent(searchDir);
            if (parent == null)
            {
                break;
            }

            searchDir = parent.FullName;
        }

        // Default to current directory
        return currentDir;
    }

    /// <summary>
    /// Shows help information.
    /// </summary>
    static void ShowHelp()
    {
        Console.WriteLine("Naner Initializer - Standalone launcher for Naner");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  naner-init              Launch Naner (auto-initialize if needed)");
        Console.WriteLine("  naner-init init         Initialize Naner (download from GitHub)");
        Console.WriteLine("  naner-init update       Update Naner to the latest version");
        Console.WriteLine("  naner-init check-update Check if an update is available");
        Console.WriteLine("  naner-init [args]       Pass arguments to naner.exe");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  --version, -v           Show version information");
        Console.WriteLine("  --help, -h              Show this help message");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  naner-init              # Launch Naner");
        Console.WriteLine("  naner-init Unified      # Launch Naner with Unified profile");
        Console.WriteLine("  naner-init --version    # Show version");
        Console.WriteLine("  naner-init update       # Update to latest version");
    }

    /// <summary>
    /// Determines if the command needs console output.
    /// </summary>
    static bool NeedsConsole(string[] args)
    {
        // No arguments = just launching naner, no console needed
        if (args.Length == 0)
        {
            return false;
        }

        var firstArg = args[0].ToLower();

        // Commands that need console output
        return firstArg switch
        {
            "--version" or "-v" => true,
            "--help" or "-h" => true,
            "init" => true,
            "update" => true,
            "check-update" => true,
            _ => false // Passing args to naner.exe, no console needed
        };
    }

    /// <summary>
    /// Ensures a console is attached for output (used when we need to show messages dynamically).
    /// </summary>
    static void EnsureConsoleAttached()
    {
        // Try to attach to parent console, or allocate new one
        if (!AttachConsole(ATTACH_PARENT_PROCESS))
        {
            AllocConsole();
        }
    }
}
