using System;
using System.IO;
using System.Threading.Tasks;
using Naner.Commands.Abstractions;
using Naner.Vendors.Services;

namespace Naner.Commands.Implementations.Setup;

/// <summary>
/// Quick non-interactive setup strategy.
/// Follows the Strategy pattern for InitCommand setup modes.
/// </summary>
public class QuickSetupStrategy : ISetupStrategy
{
    public string Name => "Quick Setup";

    public async Task<int> ExecuteAsync(string targetPath, SetupOptions options)
    {
        targetPath = string.IsNullOrEmpty(targetPath)
            ? Environment.CurrentDirectory
            : targetPath;

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
            if (options.WithVendors && !options.SkipVendors)
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

            if (!options.WithVendors && !options.SkipVendors)
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
            Logger.Debug($"Exception details: {ex}", debugMode: options.DebugMode);
            return 1;
        }
    }
}
