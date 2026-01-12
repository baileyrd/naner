using System;
using System.IO;
using System.Threading.Tasks;
using Naner.Vendors.Services;

namespace Naner.Commands.Implementations;

/// <summary>
/// Command for updating vendor dependencies to their latest versions.
/// Unlike setup-vendors, this will re-download even if already installed.
/// </summary>
public class UpdateVendorsCommand : ICommand
{
    /// <summary>
    /// Executes the update-vendors command.
    /// </summary>
    /// <param name="args">Command arguments</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public int Execute(string[] args)
    {
        return ExecuteAsync(args).GetAwaiter().GetResult();
    }

    private async Task<int> ExecuteAsync(string[] args)
    {
        Logger.Header("Updating Essential Vendors");
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
                Logger.Info("or run 'naner-init' first to set up Naner.");
                Logger.Debug($"Exception details: {ex}", debugMode: false);
                return 1;
            }

            Logger.Info($"Naner Root: {nanerRoot}");
            Logger.NewLine();

            // Run vendor update using unified installer with force update
            var vendors = VendorDefinitionFactory.GetEssentialVendors();
            var installer = new UnifiedVendorInstaller(nanerRoot, vendors);
            await installer.UpdateAllVendorsAsync();

            Logger.NewLine();
            Logger.Success("Vendor updates completed!");

            return 0;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Vendor update failed: {ex.Message}");
            Logger.Debug($"Exception details: {ex}", debugMode: false);
            return 1;
        }
    }
}
