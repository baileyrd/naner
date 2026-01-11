using System.Threading.Tasks;
using Naner.Vendors.Services;

namespace Naner.Init;

/// <summary>
/// Downloads essential vendors needed for Naner to function.
/// This is a minimal wrapper around UnifiedVendorInstaller for essential dependencies.
/// </summary>
public class EssentialVendorDownloader
{
    private readonly UnifiedVendorInstaller _installer;
    private readonly bool _forceUpdate;

    public EssentialVendorDownloader(string nanerRoot, bool forceUpdate = false)
    {
        _forceUpdate = forceUpdate;

        // Create installer with essential vendor definitions
        var vendors = VendorDefinitionFactory.GetEssentialVendors();
        _installer = new UnifiedVendorInstaller(nanerRoot, vendors);
    }

    /// <summary>
    /// Downloads all essential vendors for a minimal working setup.
    /// </summary>
    public async Task<bool> DownloadAllEssentialsAsync()
    {
        Logger.Info("Downloading essential vendors...");
        Logger.NewLine();

        // Download in order: 7-Zip first (needed for extraction), then others
        var success = true;

        success &= await _installer.InstallVendorAsync("7-Zip");
        Logger.NewLine();

        success &= await _installer.InstallVendorAsync("PowerShell");
        Logger.NewLine();

        success &= await _installer.InstallVendorAsync("Windows Terminal");
        Logger.NewLine();

        success &= await _installer.InstallVendorAsync("MSYS2 (Git/Bash)");
        Logger.NewLine();

        // Cleanup download directory
        _installer.CleanupDownloads();

        if (success)
        {
            Logger.Success("All essential vendors installed successfully!");
        }
        else
        {
            Logger.Warning("Some vendors failed to install, but Naner may still function");
        }

        return success;
    }
}
