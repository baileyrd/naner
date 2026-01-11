using Microsoft.Extensions.DependencyInjection;
using Naner.Archives.Abstractions;
using Naner.Archives.Extractors;
using Naner.Archives.Services;
using Naner.Commands.Abstractions;
using Naner.Commands.Implementations;
using Naner.Commands.Services;
using Naner.Configuration;
using Naner.Configuration.Abstractions;
using Naner.Core.Abstractions;
using Naner.Core.Events;
using Naner.Infrastructure.Abstractions;
using Naner.Infrastructure.Console;
using Naner.Infrastructure.Http;
using Naner.Vendors;
using Naner.Vendors.Abstractions;
using Naner.Vendors.Services;

namespace Naner.DependencyInjection;

/// <summary>
/// Extension methods for configuring Naner services in a DI container.
/// Provides a clean, modular approach to dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Naner core services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="nanerRoot">The Naner root directory path</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNanerCore(this IServiceCollection services, string nanerRoot)
    {
        // Register the Naner root as a singleton configuration value
        services.AddSingleton(new NanerRootPath(nanerRoot));

        // Core services
        services.AddSingleton<ILogger, ConsoleLogger>();

        // Event system
        services.AddSingleton<IEventAggregator>(EventAggregator.Instance);

        return services;
    }

    /// <summary>
    /// Adds infrastructure services (HTTP, console management).
    /// </summary>
    public static IServiceCollection AddNanerInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IHttpClientWrapper, HttpClientWrapper>();
        services.AddSingleton<ConsoleManager>();

        return services;
    }

    /// <summary>
    /// Adds configuration services with support for multiple formats.
    /// </summary>
    public static IServiceCollection AddNanerConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<IConfigurationManager>(sp =>
        {
            var nanerRoot = sp.GetRequiredService<NanerRootPath>();
            return new ConfigurationManager(nanerRoot.Path);
        });

        services.AddSingleton<ConfigurationValidator>(sp =>
        {
            var nanerRoot = sp.GetRequiredService<NanerRootPath>();
            return new ConfigurationValidator(nanerRoot.Path);
        });

        return services;
    }

    /// <summary>
    /// Adds archive extraction services with all supported formats.
    /// </summary>
    public static IServiceCollection AddNanerArchives(this IServiceCollection services)
    {
        // Register individual extractors
        services.AddSingleton<IArchiveExtractor, ZipExtractor>();

        // Register the extractor service that coordinates multiple extractors
        services.AddSingleton<ArchiveExtractorService>(sp =>
        {
            var nanerRoot = sp.GetRequiredService<NanerRootPath>();
            var sevenZipPath = Path.Combine(nanerRoot.Path, "vendor", "7zip", "7z.exe");
            return new ArchiveExtractorService(sevenZipPath);
        });

        return services;
    }

    /// <summary>
    /// Adds vendor installation services.
    /// </summary>
    public static IServiceCollection AddNanerVendors(this IServiceCollection services)
    {
        services.AddSingleton<IVendorInstaller>(sp =>
        {
            var nanerRoot = sp.GetRequiredService<NanerRootPath>();
            var httpClient = sp.GetRequiredService<IHttpClientWrapper>();
            var definitions = VendorDefinitionFactory.GetEssentialVendors();
            return new UnifiedVendorInstaller(nanerRoot.Path, definitions, httpClient);
        });

        services.AddSingleton<VendorConfigurationLoader>(sp =>
        {
            var nanerRoot = sp.GetRequiredService<NanerRootPath>();
            var logger = sp.GetRequiredService<ILogger>();
            return new VendorConfigurationLoader(nanerRoot.Path, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds command pattern services with support for plugin loading.
    /// </summary>
    public static IServiceCollection AddNanerCommands(this IServiceCollection services)
    {
        // Register built-in commands
        services.AddTransient<ICommand, VersionCommand>();
        services.AddTransient<ICommand, HelpCommand>();
        services.AddTransient<ICommand, InitCommand>();
        services.AddTransient<ICommand, DiagnosticsCommand>();
        services.AddTransient<ICommand, SetupVendorsCommand>();

        // Register the command router
        services.AddSingleton<CommandRouter>();

        // Register diagnostics sub-services
        services.AddTransient<IDiagnosticsService, DiagnosticsService>();
        services.AddTransient<DirectoryVerifier>();
        services.AddTransient<ConfigurationVerifier>();
        services.AddTransient<EnvironmentReporter>();

        return services;
    }

    /// <summary>
    /// Adds all Naner services to the service collection.
    /// This is the recommended method for typical applications.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="nanerRoot">The Naner root directory path</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNaner(this IServiceCollection services, string nanerRoot)
    {
        return services
            .AddNanerCore(nanerRoot)
            .AddNanerInfrastructure()
            .AddNanerConfiguration()
            .AddNanerArchives()
            .AddNanerVendors()
            .AddNanerCommands();
    }
}

/// <summary>
/// Wrapper for the Naner root path to enable DI registration.
/// </summary>
public class NanerRootPath
{
    public string Path { get; }

    public NanerRootPath(string path)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public static implicit operator string(NanerRootPath nanerRoot) => nanerRoot.Path;
}
