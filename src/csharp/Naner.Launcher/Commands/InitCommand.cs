using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Naner.Common;
using Naner.Vendors.Services;
using Naner.Configuration;

namespace Naner.Launcher.Commands;

/// <summary>
/// Command for initializing a new Naner installation.
/// Extracted from Program.cs to improve SRP compliance.
/// </summary>
public class InitCommand : ICommand
{
    /// <summary>
    /// Executes the init command.
    /// </summary>
    /// <param name="args">Command arguments</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public int Execute(string[] args)
    {
        return ExecuteAsync(args).GetAwaiter().GetResult();
    }

    private async Task<int> ExecuteAsync(string[] args)
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
            return await RunInteractiveSetupAsync(targetPath, skipVendors);
        }
        else
        {
            return await RunQuickSetupAsync(targetPath, skipVendors, withVendors);
        }
    }

    /// <summary>
    /// Runs interactive setup with user prompts.
    /// </summary>
    private async Task<int> RunInteractiveSetupAsync(string? targetPath, bool skipVendors)
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
                    configManager.Load();
                    var launcher = new TerminalLauncher(targetPath, configManager, false);
                    return launcher.LaunchProfile(string.Empty, null);
                }
                catch (Exception launchEx)
                {
                    Logger.Warning($"Could not launch terminal: {launchEx.Message}");
                    Logger.Debug($"Exception details: {launchEx}", debugMode: false);
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
            Logger.Debug($"Exception details: {ex}", debugMode: false);
            return 1;
        }
    }

    /// <summary>
    /// Runs quick non-interactive setup.
    /// </summary>
    private async Task<int> RunQuickSetupAsync(string? targetPath, bool skipVendors, bool withVendors)
    {
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

                var vendors = VendorDefinitionFactory.GetEssentialVendors();
                var installer = new UnifiedVendorInstaller(targetPath, vendors);
                await installer.InstallAllVendorsAsync();
            }

            // Create initialization marker
            FirstRunDetector.CreateInitializationMarker(targetPath, NanerConstants.Version, NanerConstants.PhaseName);
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
            Logger.Debug($"Exception details: {ex}", debugMode: false);
            return 1;
        }
    }
}
