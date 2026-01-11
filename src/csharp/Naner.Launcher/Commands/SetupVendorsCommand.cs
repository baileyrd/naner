using System;
using System.IO;
using System.Threading.Tasks;
using Naner.Common;
using Naner.Common.Services;

namespace Naner.Launcher.Commands;

/// <summary>
/// Command for downloading and installing vendor dependencies.
/// Extracted from Program.cs to improve SRP compliance.
/// </summary>
public class SetupVendorsCommand : ICommand
{
    /// <summary>
    /// Executes the setup-vendors command.
    /// </summary>
    /// <param name="args">Command arguments</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public int Execute(string[] args)
    {
        return ExecuteAsync(args).GetAwaiter().GetResult();
    }

    private async Task<int> ExecuteAsync(string[] args)
    {
        Logger.Header("Naner Vendor Setup");
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
                Logger.Info("or run 'naner init' first to set up Naner.");
                Logger.Debug($"Exception details: {ex}", debugMode: false);
                return 1;
            }

            Logger.Info($"Naner Root: {nanerRoot}");
            Logger.NewLine();

            // Run vendor download using unified installer
            var vendors = VendorDefinitionFactory.GetEssentialVendors();
            var installer = new UnifiedVendorInstaller(nanerRoot, vendors);
            await installer.InstallAllVendorsAsync();

            Logger.NewLine();
            Logger.Success("Vendor setup complete!");
            Logger.Info("You can now launch Naner with: naner");

            return 0;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Vendor setup failed: {ex.Message}");
            Logger.Debug($"Exception details: {ex}", debugMode: false);
            return 1;
        }
    }
}
