using System;
using System.IO;
using System.Text.Json;
using Naner.Configuration.Abstractions;
using Naner.Configuration.Providers;

namespace Naner.Configuration;

/// <summary>
/// Manages loading and processing Naner configuration files.
/// Supports multiple configuration formats (JSON, YAML) and environment variable overrides.
/// </summary>
public class ConfigurationManager : IConfigurationManager
{
    private readonly string _nanerRoot;
    private readonly ConfigurationProviderService _providerService;
    private NanerConfig? _config;

    /// <summary>
    /// Creates a new configuration manager.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory path.</param>
    public ConfigurationManager(string nanerRoot)
    {
        _nanerRoot = nanerRoot ?? throw new ArgumentNullException(nameof(nanerRoot));
        _providerService = new ConfigurationProviderService(nanerRoot);
    }

    /// <summary>
    /// Creates a new configuration manager with custom providers.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory path.</param>
    /// <param name="providers">Custom configuration providers</param>
    public ConfigurationManager(string nanerRoot, IEnumerable<IConfigurationProvider> providers)
    {
        _nanerRoot = nanerRoot ?? throw new ArgumentNullException(nameof(nanerRoot));
        _providerService = new ConfigurationProviderService(nanerRoot, providers);
    }

    /// <summary>
    /// Gets the configuration provider service for advanced usage.
    /// </summary>
    public ConfigurationProviderService ProviderService => _providerService;

    /// <summary>
    /// Loads configuration from a configuration file.
    /// Supports JSON (.json), YAML (.yaml, .yml), and environment variable overrides.
    /// </summary>
    /// <param name="configPath">Optional custom config path. If null, searches default locations.</param>
    /// <returns>Loaded configuration</returns>
    /// <exception cref="FileNotFoundException">Thrown when config file doesn't exist</exception>
    /// <exception cref="InvalidOperationException">Thrown when config file is invalid</exception>
    public NanerConfig Load(string? configPath = null)
    {
        // Use provider service for multi-format support
        _config = _providerService.LoadConfiguration(configPath);

        // Expand all paths in the configuration
        ExpandConfigPaths();

        // Validate configuration
        var validator = new ConfigurationValidator(_nanerRoot);
        if (!validator.Validate(_config))
        {
            // Log warnings but don't fail
            foreach (var warning in validator.Warnings)
            {
                Logger.Warning($"Configuration validation warning: {warning}");
            }

            // Throw if there are errors
            validator.ThrowIfInvalid();
        }
        else if (validator.Warnings.Count > 0)
        {
            // Log warnings even if validation succeeded
            foreach (var warning in validator.Warnings)
            {
                Logger.Warning($"Configuration validation warning: {warning}");
            }
        }

        return _config;
    }

    /// <summary>
    /// Gets the loaded configuration. Throws if not loaded.
    /// </summary>
    public NanerConfig Config => _config ?? throw new InvalidOperationException("Configuration not loaded. Call Load() first.");

    /// <summary>
    /// Gets a profile by name from the configuration.
    /// Checks both standard profiles and custom profiles.
    /// </summary>
    /// <param name="profileName">Name of the profile to retrieve</param>
    /// <param name="useDefaultOnNotFound">If true, returns the default profile when the requested profile is not found</param>
    /// <returns>Profile configuration</returns>
    /// <exception cref="InvalidOperationException">Thrown when profile doesn't exist and no default fallback available</exception>
    public ProfileConfig GetProfile(string profileName, bool useDefaultOnNotFound = false)
    {
        if (_config == null)
        {
            throw new InvalidOperationException("Configuration not loaded");
        }

        // Check standard profiles first
        if (_config.Profiles.TryGetValue(profileName, out var profile))
        {
            return profile;
        }

        // Check custom profiles
        if (_config.CustomProfiles.TryGetValue(profileName, out var customProfile))
        {
            return customProfile;
        }

        // Try default profile if fallback is enabled
        if (useDefaultOnNotFound &&
            !string.IsNullOrEmpty(_config.DefaultProfile) &&
            _config.Profiles.TryGetValue(_config.DefaultProfile, out var defaultProfile))
        {
            Logger.Warning($"Profile '{profileName}' not found, using default: {_config.DefaultProfile}");
            return defaultProfile;
        }

        throw new InvalidOperationException($"Profile '{profileName}' not found in configuration");
    }

    /// <summary>
    /// Builds the unified PATH string from configuration.
    /// </summary>
    /// <param name="includeSystemPath">Whether to append system PATH</param>
    /// <returns>Unified PATH string with paths separated by semicolons</returns>
    public string BuildUnifiedPath(bool includeSystemPath = true)
    {
        if (_config == null)
        {
            throw new InvalidOperationException("Configuration not loaded");
        }

        // Respect the Advanced.InheritSystemPath configuration setting
        var shouldIncludeSystemPath = includeSystemPath && _config.Advanced.InheritSystemPath;

        return PathBuilder.BuildUnifiedPath(
            _config.Environment.PathPrecedence,
            _nanerRoot,
            shouldIncludeSystemPath);
    }

    /// <summary>
    /// Sets up environment variables from configuration.
    /// </summary>
    public void SetupEnvironmentVariables()
    {
        if (_config == null)
        {
            throw new InvalidOperationException("Configuration not loaded");
        }

        foreach (var (key, value) in _config.Environment.EnvironmentVariables)
        {
            var expandedValue = PathUtilities.ExpandNanerPath(value, _nanerRoot);
            Environment.SetEnvironmentVariable(key, expandedValue, EnvironmentVariableTarget.Process);
        }
    }

    /// <summary>
    /// Expands %NANER_ROOT% placeholders in all configuration paths.
    /// </summary>
    private void ExpandConfigPaths()
    {
        if (_config == null) return;

        // Expand vendor paths
        var expandedVendorPaths = new System.Collections.Generic.Dictionary<string, string>();
        foreach (var (key, path) in _config.VendorPaths)
        {
            expandedVendorPaths[key] = PathUtilities.ExpandNanerPath(path, _nanerRoot);
        }
        _config.VendorPaths = expandedVendorPaths;

        // Expand environment path precedence
        for (int i = 0; i < _config.Environment.PathPrecedence.Count; i++)
        {
            _config.Environment.PathPrecedence[i] =
                PathUtilities.ExpandNanerPath(_config.Environment.PathPrecedence[i], _nanerRoot);
        }

        // Expand environment variables
        var expandedEnvVars = new System.Collections.Generic.Dictionary<string, string>();
        foreach (var (key, value) in _config.Environment.EnvironmentVariables)
        {
            expandedEnvVars[key] = PathUtilities.ExpandNanerPath(value, _nanerRoot);
        }
        _config.Environment.EnvironmentVariables = expandedEnvVars;
    }
}
