using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Naner.Vendors.Services;

namespace Naner.Init;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            // Determine if we need to show console output
            // Use centralized ConsoleManager with InitCommandNames constants
            bool needsConsole = ConsoleManager.NeedsConsole(args, InitCommandNames.ConsoleCommands);

            if (needsConsole)
            {
                ConsoleManager.Instance.EnsureConsoleAttached();
            }

            // Find or set Naner root directory
            var nanerRoot = FindNanerRoot();

            // Handle command line arguments
            if (args.Length > 0)
            {
                var command = args[0].ToLower();

                switch (command)
                {
                    case InitCommandNames.Version:
                    case InitCommandNames.VersionShort:
                        Console.WriteLine($"naner-init {NanerConstants.Version}");
                        return 0;

                    case InitCommandNames.Help:
                    case InitCommandNames.HelpShort:
                        ShowHelp();
                        return 0;

                    case InitCommandNames.Init:
                        return await InitializeNanerAsync(nanerRoot);

                    case InitCommandNames.Update:
                        return await UpdateNanerAsync(nanerRoot);

                    case InitCommandNames.CheckUpdate:
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
            Logger.Failure($"Fatal error: {ex.Message}");
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
            ConsoleManager.Instance.EnsureConsoleAttached();

            Logger.Header("Naner Initializer");
            Logger.NewLine();
            Logger.Info("Naner is not initialized yet.");
            Logger.Info("This will download the latest version of Naner from GitHub.");
            Logger.NewLine();

            Console.Write("Initialize Naner now? (Y/n): ");
            var response = Console.ReadLine()?.Trim().ToLower();

            if (response != "" && response != "y" && response != "yes")
            {
                Logger.Info("Initialization cancelled.");
                return 0;
            }

            if (!await updater.InitializeAsync())
            {
                return 1;
            }

            // Download essential vendors (7-Zip, PowerShell, Windows Terminal)
            Logger.NewLine();
            Logger.Info("Setting up essential dependencies...");
            Logger.NewLine();

            var vendorDownloader = new EssentialVendorDownloader(nanerRoot);
            await vendorDownloader.DownloadAllEssentialsAsync();

            // Note about optional additional tools
            Logger.NewLine();
            Logger.Info("Additional development tools can be installed later.");
            Logger.Info("Run 'naner setup-vendors' for more optional tools.");

            Logger.NewLine();
            Logger.Success("Naner is ready!");
            Logger.NewLine();

            // Prompt user to launch or exit
            Console.Write("Launch Naner now? (Y/n): ");
            var launchResponse = Console.ReadLine()?.Trim().ToLower();

            if (launchResponse == "" || launchResponse == "y" || launchResponse == "yes")
            {
                return updater.LaunchNaner(args);
            }

            Logger.Info("Run 'naner' or 'naner-init' to launch Naner later.");
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
        catch (Exception ex)
        {
            // Silently ignore update check failures - not critical to naner-init operation
            Logger.Debug($"Update check failed: {ex.Message}", debugMode: false);
        }

        // If update available, attach console to show notification
        if (showUpdateNotification)
        {
            ConsoleManager.Instance.EnsureConsoleAttached();
            Logger.Warning($"A new version of Naner is available: {latestVersion}");
            Logger.Info("Run 'naner-init update' to update");
            Logger.NewLine();
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
            Logger.Warning("Naner is already initialized.");
            Logger.Info($"Current version: {updater.GetInstalledVersion()}");
            Logger.Info("Use 'naner-init update' to update to the latest version.");
            return 0;
        }

        if (!await updater.InitializeAsync())
        {
            return 1;
        }

        // Download essential vendors (7-Zip, PowerShell, Windows Terminal, MSYS2)
        Logger.NewLine();
        Logger.Info("Setting up essential dependencies...");
        Logger.NewLine();

        var vendorDownloader = new EssentialVendorDownloader(nanerRoot);
        await vendorDownloader.DownloadAllEssentialsAsync();

        // Note about optional additional tools
        Logger.NewLine();
        Logger.Info("Additional development tools can be installed later.");
        Logger.Info("Run 'naner setup-vendors' for more optional tools.");

        Logger.NewLine();
        Logger.Success("Naner is ready!");
        Logger.NewLine();

        // Prompt user to launch or exit
        Console.Write("Launch Naner now? (Y/n): ");
        var launchResponse = Console.ReadLine()?.Trim().ToLower();

        if (launchResponse == "" || launchResponse == "y" || launchResponse == "yes")
        {
            return updater.LaunchNaner(Array.Empty<string>());
        }

        Logger.Info("Run 'naner' or 'naner-init' to launch Naner later.");
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
            Logger.Failure("Naner is not initialized yet.");
            Logger.Info("Run 'naner-init' to initialize first.");
            return 1;
        }

        var currentVersion = updater.GetInstalledVersion();
        Logger.Info($"Current version: {currentVersion}");

        var (updateAvailable, latestVersion) = await updater.CheckForUpdateAsync();

        if (!updateAvailable)
        {
            Logger.Success("Naner is already up to date!");
            return 0;
        }

        Logger.Info($"Latest version: {latestVersion}");
        Logger.NewLine();

        Console.Write("Update now? (Y/n): ");
        var response = Console.ReadLine()?.Trim().ToLower();

        if (response != "" && response != "y" && response != "yes")
        {
            Logger.Info("Update cancelled.");
            return 0;
        }

        return await updater.UpdateNanerExeAsync() ? 0 : 1;
    }

    /// <summary>
    /// Handles 'check-update' command.
    /// </summary>
    static async Task<int> CheckForUpdatesAsync(string nanerRoot)
    {
        var updater = new NanerUpdater(nanerRoot);

        if (!updater.IsInitialized())
        {
            Logger.Failure("Naner is not initialized yet.");
            return 1;
        }

        var currentVersion = updater.GetInstalledVersion();
        Logger.Info($"Current version: {currentVersion}");

        var (updateAvailable, latestVersion) = await updater.CheckForUpdateAsync();

        if (updateAvailable && latestVersion != null)
        {
            Logger.Warning($"Update available: {latestVersion}");
            Logger.Info("Run 'naner-init update' to update");
            return 0;
        }

        Logger.Success("Naner is up to date!");
        return 0;
    }

    /// <summary>
    /// Finds the Naner root directory.
    /// </summary>
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

    /// <summary>
    /// Shows help information.
    /// </summary>
    static void ShowHelp()
    {
        Console.WriteLine("Naner Initializer - Standalone launcher for Naner");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  naner-init                Launch Naner (auto-initialize if needed)");
        Console.WriteLine("  naner-init init           Initialize Naner (download from GitHub)");
        Console.WriteLine("  naner-init update         Update Naner to the latest version");
        Console.WriteLine("  naner-init check-update   Check if an update is available");
        Console.WriteLine("  naner-init [args]         Pass arguments to naner.exe");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  --version, -v             Show version information");
        Console.WriteLine("  --help, -h                Show this help message");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  naner-init                # Launch Naner");
        Console.WriteLine("  naner-init Unified        # Launch Naner with Unified profile");
        Console.WriteLine("  naner-init --version      # Show version");
        Console.WriteLine("  naner-init update         # Update to latest version");
        Console.WriteLine();
        Console.WriteLine("VENDOR MANAGEMENT:");
        Console.WriteLine("  Use 'naner update-vendors' to update PowerShell, Terminal, etc.");
    }
}
