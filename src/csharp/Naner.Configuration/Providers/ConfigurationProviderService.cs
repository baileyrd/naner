using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Naner.Configuration.Abstractions;

namespace Naner.Configuration.Providers;

/// <summary>
/// Service that coordinates multiple configuration providers to load configuration
/// from various sources with proper layering and override support.
/// </summary>
public class ConfigurationProviderService
{
    /// <summary>
    /// Default configuration file names in priority order.
    /// Extracted to eliminate duplication (DRY principle).
    /// </summary>
    private static readonly string[] DefaultConfigFileNames = new[]
    {
        "naner.json",
        "naner.yaml",
        "naner.yml"
    };

    private readonly List<IConfigurationProvider> _providers;
    private readonly string _nanerRoot;

    public ConfigurationProviderService(string nanerRoot)
    {
        _nanerRoot = nanerRoot ?? throw new ArgumentNullException(nameof(nanerRoot));
        _providers = new List<IConfigurationProvider>();

        // Register default providers in priority order
        RegisterDefaultProviders();
    }

    public ConfigurationProviderService(string nanerRoot, IEnumerable<IConfigurationProvider> providers)
    {
        _nanerRoot = nanerRoot ?? throw new ArgumentNullException(nameof(nanerRoot));
        _providers = providers.OrderBy(p => p.Priority).ToList();
    }

    private void RegisterDefaultProviders()
    {
        _providers.Add(new JsonConfigurationProvider());
        _providers.Add(new YamlConfigurationProvider());
        _providers.Add(new EnvironmentConfigurationProvider());

        // Sort by priority
        _providers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    /// <summary>
    /// Gets all registered configuration providers.
    /// </summary>
    public IReadOnlyList<IConfigurationProvider> Providers => _providers.AsReadOnly();

    /// <summary>
    /// Registers a custom configuration provider.
    /// </summary>
    /// <param name="provider">The provider to register</param>
    public void RegisterProvider(IConfigurationProvider provider)
    {
        _providers.Add(provider);
        _providers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    /// <summary>
    /// Loads configuration from the first matching provider and applies all overrides.
    /// </summary>
    /// <param name="configPath">Optional specific config path. If null, searches default locations.</param>
    /// <returns>The loaded and merged configuration</returns>
    public NanerConfig LoadConfiguration(string? configPath = null)
    {
        NanerConfig? config = null;

        // If specific path provided, use it
        if (!string.IsNullOrEmpty(configPath))
        {
            config = LoadFromPath(configPath);
        }
        else
        {
            // Search default config locations in priority order
            config = SearchDefaultLocations();
        }

        if (config == null)
        {
            throw new FileNotFoundException(
                "No configuration file found. Please create naner.json or naner.yaml in the config directory.");
        }

        // Apply overrides from all providers (e.g., environment variables)
        ApplyAllOverrides(config);

        return config;
    }

    private NanerConfig? LoadFromPath(string configPath)
    {
        var provider = _providers.FirstOrDefault(p => p.CanLoad(configPath));

        if (provider == null)
        {
            throw new NotSupportedException(
                $"No configuration provider found for: {configPath}. " +
                $"Supported formats: {string.Join(", ", _providers.Select(p => p.Name))}");
        }

        Logger.Debug($"Loading configuration using {provider.Name} provider", debugMode: false);
        return provider.Load(configPath);
    }

    private NanerConfig? SearchDefaultLocations()
    {
        var configDir = Path.Combine(_nanerRoot, "config");

        foreach (var fileName in DefaultConfigFileNames)
        {
            var fullPath = Path.Combine(configDir, fileName);
            if (File.Exists(fullPath))
            {
                Logger.Debug($"Found configuration file: {fullPath}", debugMode: false);
                return LoadFromPath(fullPath);
            }
        }

        return null;
    }

    private void ApplyAllOverrides(NanerConfig config)
    {
        foreach (var provider in _providers)
        {
            provider.ApplyOverrides(config);
        }
    }

    /// <summary>
    /// Gets the path to the first available configuration file.
    /// </summary>
    /// <returns>The path if found, null otherwise</returns>
    public string? FindConfigurationFile()
    {
        var configDir = Path.Combine(_nanerRoot, "config");

        foreach (var fileName in DefaultConfigFileNames)
        {
            var fullPath = Path.Combine(configDir, fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}
