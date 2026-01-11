using System.Threading.Tasks;

namespace Naner.Vendors.Abstractions;

/// <summary>
/// Interface for installing vendor packages.
/// Enables dependency injection and testability for vendor management.
/// </summary>
public interface IVendorInstaller
{
    /// <summary>
    /// Checks if a vendor is already installed.
    /// </summary>
    /// <param name="vendorName">Name of the vendor package</param>
    /// <returns>True if installed, false otherwise</returns>
    bool IsInstalled(string vendorName);

    /// <summary>
    /// Installs a vendor package.
    /// </summary>
    /// <param name="vendorName">Name of the vendor package to install</param>
    /// <returns>True if installation succeeded, false otherwise</returns>
    Task<bool> InstallVendorAsync(string vendorName);

    /// <summary>
    /// Gets the installation path for a vendor.
    /// </summary>
    /// <param name="vendorName">Name of the vendor package</param>
    /// <returns>Full path to vendor directory, or null if not installed</returns>
    string? GetVendorPath(string vendorName);
}
