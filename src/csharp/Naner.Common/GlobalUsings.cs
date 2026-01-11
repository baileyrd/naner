// Global type forwarding aliases for backward compatibility
// These classes have been moved to domain-specific projects

// Core utilities
global using Logger = Naner.Core.Logger;
global using NanerConstants = Naner.Core.NanerConstants;
global using PathUtilities = Naner.Core.PathUtilities;
global using PathBuilder = Naner.Core.PathBuilder;
global using ILogger = Naner.Core.Abstractions.ILogger;
global using ConsoleLogger = Naner.Core.Abstractions.ConsoleLogger;

// Archive services (moved to Naner.Archives)
global using IArchiveExtractor = Naner.Archives.Abstractions.IArchiveExtractor;
global using ArchiveExtractorService = Naner.Archives.Services.ArchiveExtractorService;

// Infrastructure services (moved to Naner.Infrastructure)
global using IHttpClientWrapper = Naner.Infrastructure.Abstractions.IHttpClientWrapper;
global using IGitHubClient = Naner.Infrastructure.Abstractions.IGitHubClient;
global using HttpClientWrapper = Naner.Infrastructure.Http.HttpClientWrapper;
global using HttpDownloadService = Naner.Infrastructure.Http.HttpDownloadService;
global using ConsoleManager = Naner.Infrastructure.Console.ConsoleManager;

// Vendor services (moved to Naner.Vendors)
global using IVendorInstaller = Naner.Vendors.Abstractions.IVendorInstaller;
global using VendorInstallerBase = Naner.Vendors.Services.VendorInstallerBase;
global using UnifiedVendorInstaller = Naner.Vendors.Services.UnifiedVendorInstaller;
global using VendorConfigurationLoader = Naner.Vendors.Services.VendorConfigurationLoader;
global using VendorDefinition = Naner.Vendors.Models.VendorDefinition;
global using VendorConfiguration = Naner.Vendors.Models.VendorConfiguration;
global using VendorDefinitionFactory = Naner.Vendors.VendorDefinitionFactory;
