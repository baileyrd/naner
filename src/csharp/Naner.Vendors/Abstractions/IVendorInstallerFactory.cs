using Naner.Infrastructure.Abstractions;

namespace Naner.Vendors.Abstractions;

/// <summary>
/// Factory interface for creating vendor installers.
/// Centralizes vendor installer instantiation to eliminate duplication (DRY principle).
/// </summary>
public interface IVendorInstallerFactory
{
    /// <summary>
    /// Creates a vendor installer for essential vendors.
    /// </summary>
    /// <returns>A configured vendor installer</returns>
    IVendorInstaller CreateEssentialVendorInstaller();

    /// <summary>
    /// Creates a vendor installer with custom vendor definitions.
    /// </summary>
    /// <param name="vendorDefinitions">Vendor definitions to use</param>
    /// <returns>A configured vendor installer</returns>
    IVendorInstaller CreateCustomVendorInstaller(IEnumerable<Models.VendorDefinition> vendorDefinitions);
}
