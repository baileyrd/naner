using System;
using System.IO;
using System.Text.Json;
using Naner.Common;

namespace Naner.Configuration;

/// <summary>
/// Manages loading and processing Naner configuration files.
/// </summary>
public class ConfigurationManager
{
    private readonly string _nanerRoot;
    private NanerConfig? _config;

    /// <summary>
    /// Creates a new configuration manager.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory path.</param>
    public ConfigurationManager(string nanerRoot)
    {
        _nanerRoot = nanerRoot ?? throw new ArgumentNullException(nameof(nanerRoot));
    }

    /// <summary>
    /// Loads configuration from naner.json file.
    /// </summary>
    /// <param name="configPath">Optional custom config path. Defaults to config/naner.json</param>
    /// <returns>Loaded configuration</returns>
    /// <exception cref="FileNotFoundException">Thrown when config file doesn't exist</exception>
    /// <exception cref="JsonException">Thrown when config file is invalid JSON</exception>
    public NanerConfig Load(string? configPath = null)
    {
        configPath ??= Path.Combine(_nanerRoot, "config", "naner.json");

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        var jsonContent = File.ReadAllText(configPath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        _config = JsonSerializer.Deserialize<NanerConfig>(jsonContent, options)
            ?? throw new JsonException("Failed to deserialize configuration");

        // Expand all paths in the configuration
        ExpandConfigPaths();

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
    /// <returns>Profile configuration</returns>
    /// <exception cref="InvalidOperationException">Thrown when profile doesn't exist</exception>
    public ProfileConfig GetProfile(string profileName)
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

        var pathBuilder = new System.Text.StringBuilder();

        // Add configured paths in precedence order
        foreach (var path in _config.Environment.PathPrecedence)
        {
            var expandedPath = PathUtilities.ExpandNanerPath(path, _nanerRoot);
            if (Directory.Exists(expandedPath))
            {
                if (pathBuilder.Length > 0)
                {
                    pathBuilder.Append(';');
                }
                pathBuilder.Append(expandedPath);
            }
        }

        // Optionally append system PATH
        if (includeSystemPath && _config.Advanced.InheritSystemPath)
        {
            var systemPath = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(systemPath))
            {
                if (pathBuilder.Length > 0)
                {
                    pathBuilder.Append(';');
                }
                pathBuilder.Append(systemPath);
            }
        }

        return pathBuilder.ToString();
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
