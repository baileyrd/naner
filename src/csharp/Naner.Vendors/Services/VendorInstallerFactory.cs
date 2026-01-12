using System;
using System.Collections.Generic;
using Naner.Infrastructure.Abstractions;
using Naner.Vendors.Abstractions;
using Naner.Vendors.Models;

namespace Naner.Vendors.Services;

/// <summary>
/// Factory for creating vendor installers.
/// Centralizes vendor installer instantiation to eliminate duplication (DRY principle).
/// </summary>
public class VendorInstallerFactory : IVendorInstallerFactory
{
    private readonly string _nanerRoot;
    private readonly IHttpClientWrapper? _httpClient;

    /// <summary>
    /// Creates a new vendor installer factory.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory</param>
    /// <param name="httpClient">Optional HTTP client for dependency injection</param>
    public VendorInstallerFactory(string nanerRoot, IHttpClientWrapper? httpClient = null)
    {
        _nanerRoot = nanerRoot ?? throw new ArgumentNullException(nameof(nanerRoot));
        _httpClient = httpClient;
    }

    /// <summary>
    /// Creates a vendor installer for essential vendors.
    /// </summary>
    /// <returns>A configured vendor installer</returns>
    public IVendorInstaller CreateEssentialVendorInstaller()
    {
        var vendors = VendorDefinitionFactory.GetEssentialVendors();
        return new UnifiedVendorInstaller(_nanerRoot, vendors, _httpClient);
    }

    /// <summary>
    /// Creates a vendor installer with custom vendor definitions.
    /// </summary>
    /// <param name="vendorDefinitions">Vendor definitions to use</param>
    /// <returns>A configured vendor installer</returns>
    public IVendorInstaller CreateCustomVendorInstaller(IEnumerable<VendorDefinition> vendorDefinitions)
    {
        return new UnifiedVendorInstaller(_nanerRoot, vendorDefinitions, _httpClient);
    }

    /// <summary>
    /// Static convenience method for creating an essential vendor installer.
    /// Use when DI is not available.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory</param>
    /// <returns>A configured vendor installer</returns>
    public static IVendorInstaller CreateEssential(string nanerRoot)
    {
        var factory = new VendorInstallerFactory(nanerRoot);
        return factory.CreateEssentialVendorInstaller();
    }
}
