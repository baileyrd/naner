using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Naner.Vendors.Models;
using Naner.Vendors.Services;

namespace Naner.Commands.Implementations;

/// <summary>
/// Command for installing optional vendor packages interactively.
/// Allows users to install Ruby, Miniconda, Node.js, etc. without editing config files.
/// </summary>
public class InstallVendorsCommand : ICommand
{
    private const string ListFlag = "--list";
    private const string AllFlag = "--all";

    /// <summary>
    /// Executes the install command.
    /// </summary>
    /// <param name="args">Command arguments</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public int Execute(string[] args)
    {
        return ExecuteAsync(args).GetAwaiter().GetResult();
    }

    private async Task<int> ExecuteAsync(string[] args)
    {
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
                Logger.Info("or run 'naner-init' first to set up Naner.");
                return 1;
            }

            // Load vendor configuration
            var configLoader = new VendorConfigurationLoader(nanerRoot, new StaticLoggerAdapter());
            var allVendors = configLoader.LoadVendors();

            // Handle --list flag
            if (args.Length > 0 && args[0].Equals(ListFlag, StringComparison.OrdinalIgnoreCase))
            {
                return ShowVendorList(configLoader, allVendors);
            }

            // Handle --all flag
            if (args.Length > 0 && args[0].Equals(AllFlag, StringComparison.OrdinalIgnoreCase))
            {
                return await InstallAllOptionalVendors(nanerRoot, configLoader, allVendors);
            }

            // Handle no arguments - show help
            if (args.Length == 0)
            {
                ShowInstallHelp();
                return 0;
            }

            // Install specific vendor(s)
            return await InstallSpecificVendors(nanerRoot, configLoader, allVendors, args);
        }
        catch (Exception ex)
        {
            Logger.Failure($"Install failed: {ex.Message}");
            Logger.Debug($"Exception details: {ex}", debugMode: false);
            return 1;
        }
    }

    /// <summary>
    /// Shows the list of available vendors and their installation status.
    /// </summary>
    private int ShowVendorList(VendorConfigurationLoader configLoader, List<VendorDefinition> allVendors)
    {
        Logger.Header("Available Vendors");
        Logger.NewLine();

        // Essential vendors
        var essentialVendors = allVendors.Where(v => v.Required).ToList();
        if (essentialVendors.Any())
        {
            Console.WriteLine("Essential (always installed):");
            foreach (var vendor in essentialVendors)
            {
                var installed = configLoader.IsVendorInstalled(vendor);
                var status = installed ? "[OK]" : "[ ]";
                var statusColor = installed ? ConsoleColor.Green : ConsoleColor.Gray;

                Console.ForegroundColor = statusColor;
                Console.Write($"  {status} ");
                Console.ResetColor();
                Console.WriteLine($"{vendor.Name,-20} {vendor.Description}");
            }
            Logger.NewLine();
        }

        // Optional vendors
        var optionalVendors = allVendors.Where(v => !v.Required).ToList();
        if (optionalVendors.Any())
        {
            Console.WriteLine("Optional:");
            foreach (var vendor in optionalVendors)
            {
                var installed = configLoader.IsVendorInstalled(vendor);
                var status = installed ? "[OK]" : "[ ]";
                var statusColor = installed ? ConsoleColor.Green : ConsoleColor.Gray;

                Console.ForegroundColor = statusColor;
                Console.Write($"  {status} ");
                Console.ResetColor();
                Console.WriteLine($"{vendor.Name,-20} {vendor.Description}");
            }
            Logger.NewLine();
        }

        Console.WriteLine("Use 'naner install <name>' to install a vendor.");
        Console.WriteLine("Use 'naner install --all' to install all optional vendors.");
        Logger.NewLine();

        return 0;
    }

    /// <summary>
    /// Installs all optional vendors.
    /// </summary>
    private async Task<int> InstallAllOptionalVendors(
        string nanerRoot,
        VendorConfigurationLoader configLoader,
        List<VendorDefinition> allVendors)
    {
        Logger.Header("Installing All Optional Vendors");
        Logger.NewLine();

        var optionalVendors = allVendors.Where(v => !v.Required).ToList();
        var vendorsToInstall = optionalVendors.Where(v => !configLoader.IsVendorInstalled(v)).ToList();

        if (!vendorsToInstall.Any())
        {
            Logger.Success("All optional vendors are already installed!");
            return 0;
        }

        Logger.Status($"Installing {vendorsToInstall.Count} vendor(s)...");
        Logger.NewLine();

        var installer = new UnifiedVendorInstaller(nanerRoot, allVendors);
        var failedCount = 0;

        foreach (var vendor in vendorsToInstall)
        {
            var success = await InstallVendorWithDependencies(installer, configLoader, allVendors, vendor);
            if (!success)
            {
                failedCount++;
            }
            Logger.NewLine();
        }

        installer.CleanupDownloads();

        Logger.NewLine();
        if (failedCount == 0)
        {
            Logger.Success("All optional vendors installed successfully!");
        }
        else
        {
            Logger.Warning($"Completed with {failedCount} failure(s).");
        }

        Logger.Info("Restart your terminal to use the newly installed tools.");
        return failedCount > 0 ? 1 : 0;
    }

    /// <summary>
    /// Installs specific vendors by name.
    /// </summary>
    private async Task<int> InstallSpecificVendors(
        string nanerRoot,
        VendorConfigurationLoader configLoader,
        List<VendorDefinition> allVendors,
        string[] vendorNames)
    {
        // Resolve vendor names to definitions
        var vendorsToInstall = new List<VendorDefinition>();
        var notFound = new List<string>();

        foreach (var name in vendorNames)
        {
            var vendor = configLoader.GetVendorByKey(name);
            if (vendor != null)
            {
                vendorsToInstall.Add(vendor);
            }
            else
            {
                notFound.Add(name);
            }
        }

        // Report any not found
        if (notFound.Any())
        {
            foreach (var name in notFound)
            {
                Logger.Failure($"Unknown vendor: {name}");
            }
            Logger.NewLine();
            Logger.Info("Use 'naner install --list' to see available vendors.");

            if (!vendorsToInstall.Any())
            {
                return 1;
            }
            Logger.NewLine();
        }

        // Check which vendors need installation
        var alreadyInstalled = vendorsToInstall.Where(v => configLoader.IsVendorInstalled(v)).ToList();
        var needsInstall = vendorsToInstall.Where(v => !configLoader.IsVendorInstalled(v)).ToList();

        // Report already installed
        foreach (var vendor in alreadyInstalled)
        {
            Logger.Info($"{vendor.Name} is already installed");
        }

        if (!needsInstall.Any())
        {
            if (alreadyInstalled.Any())
            {
                Logger.NewLine();
                Logger.Success("Nothing to install.");
            }
            return 0;
        }

        if (alreadyInstalled.Any())
        {
            Logger.NewLine();
        }

        // Install vendors
        Logger.Header($"Installing {needsInstall.Count} Vendor(s)");
        Logger.NewLine();

        var installer = new UnifiedVendorInstaller(nanerRoot, allVendors);
        var failedCount = 0;

        foreach (var vendor in needsInstall)
        {
            var success = await InstallVendorWithDependencies(installer, configLoader, allVendors, vendor);
            if (!success)
            {
                failedCount++;
            }
            Logger.NewLine();
        }

        installer.CleanupDownloads();

        Logger.NewLine();
        if (failedCount == 0)
        {
            Logger.Success("Installation completed successfully!");
        }
        else
        {
            Logger.Warning($"Completed with {failedCount} failure(s).");
        }

        Logger.Info("Restart your terminal to use the newly installed tools.");
        return failedCount > 0 ? 1 : 0;
    }

    /// <summary>
    /// Installs a vendor and its dependencies.
    /// </summary>
    private async Task<bool> InstallVendorWithDependencies(
        UnifiedVendorInstaller installer,
        VendorConfigurationLoader configLoader,
        List<VendorDefinition> allVendors,
        VendorDefinition vendor)
    {
        // Check and install dependencies first
        if (vendor.Dependencies.Any())
        {
            foreach (var depKey in vendor.Dependencies)
            {
                var dep = configLoader.GetVendorByKey(depKey);
                if (dep != null && !configLoader.IsVendorInstalled(dep))
                {
                    Logger.Info($"Installing dependency: {dep.Name}");
                    var depResult = await installer.InstallVendorAsync(dep.Name);
                    if (!depResult)
                    {
                        Logger.Failure($"Failed to install dependency: {dep.Name}");
                        return false;
                    }
                }
            }
        }

        // Install the vendor itself
        return await installer.InstallVendorAsync(vendor.Name);
    }

    /// <summary>
    /// Shows help for the install command.
    /// </summary>
    private void ShowInstallHelp()
    {
        Logger.Header("Install Vendor Packages");
        Logger.NewLine();

        Console.WriteLine("USAGE:");
        Console.WriteLine("  naner install [OPTIONS] [VENDOR...]");
        Logger.NewLine();

        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  --list                     List available vendors and status");
        Console.WriteLine("  --all                      Install all optional vendors");
        Logger.NewLine();

        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  naner install --list       # Show available vendors");
        Console.WriteLine("  naner install ruby         # Install Ruby");
        Console.WriteLine("  naner install nodejs go    # Install Node.js and Go");
        Console.WriteLine("  naner install --all        # Install all optional vendors");
        Logger.NewLine();

        Console.WriteLine("AVAILABLE VENDORS:");
        Console.WriteLine("  nodejs, miniconda, go, rust, ruby, dotnetsdk");
        Logger.NewLine();
    }

    /// <summary>
    /// Adapter to use static Logger with ILogger interface.
    /// </summary>
    private class StaticLoggerAdapter : Naner.Core.Abstractions.ILogger
    {
        public void Status(string message) => Logger.Status(message);
        public void Success(string message) => Logger.Success(message);
        public void Failure(string message) => Logger.Failure(message);
        public void Info(string message) => Logger.Info(message);
        public void Debug(string message, bool debugMode) => Logger.Debug(message, debugMode);
        public void Warning(string message) => Logger.Warning(message);
        public void NewLine() => Logger.NewLine();
        public void Header(string header) => Logger.Header(header);
    }
}
