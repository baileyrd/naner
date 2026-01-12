using System;
using System.IO;
using System.Threading.Tasks;
using Naner.Commands.Abstractions;
using Naner.Vendors.Services;

namespace Naner.Commands.Implementations.Setup;

/// <summary>
/// Interactive setup strategy with user prompts.
/// Follows the Strategy pattern for InitCommand setup modes.
/// </summary>
public class InteractiveSetupStrategy : ISetupStrategy
{
    public string Name => "Interactive Setup";

    public async Task<int> ExecuteAsync(string targetPath, SetupOptions options)
    {
        targetPath = string.IsNullOrEmpty(targetPath)
            ? SetupManager.PromptInstallLocation()
            : targetPath;

        try
        {
            targetPath = Path.GetFullPath(targetPath);

            // Run full interactive setup including vendors (unless explicitly skipped)
            var skipVendors = options.VendorMode == VendorInstallMode.Skip;
            var success = await SetupManager.RunInteractiveSetupAsync(targetPath, skipVendors);
            if (!success)
            {
                return 1;
            }

            // Setup complete
            Logger.NewLine();
            Logger.Success("Setup complete!");
            Logger.Info("You can launch Naner anytime with: naner");
            Logger.NewLine();
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
