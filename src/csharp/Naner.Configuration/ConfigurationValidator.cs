using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Naner.Configuration;

/// <summary>
/// Validates Naner configuration for correctness and completeness.
/// Provides detailed validation errors for easier troubleshooting.
/// </summary>
public class ConfigurationValidator
{
    private readonly string _nanerRoot;
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();

    /// <summary>
    /// Creates a new configuration validator.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory for path validation</param>
    public ConfigurationValidator(string nanerRoot)
    {
        _nanerRoot = nanerRoot ?? throw new ArgumentNullException(nameof(nanerRoot));
    }

    /// <summary>
    /// Validates a configuration and returns whether it's valid.
    /// </summary>
    /// <param name="config">Configuration to validate</param>
    /// <returns>True if configuration is valid</returns>
    public bool Validate(NanerConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        _errors.Clear();
        _warnings.Clear();

        ValidateDefaultProfile(config);
        ValidateProfiles(config);
        ValidateVendorPaths(config);
        ValidateEnvironment(config);

        return _errors.Count == 0;
    }

    /// <summary>
    /// Gets validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>
    /// Gets validation warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>
    /// Validates default profile configuration.
    /// </summary>
    private void ValidateDefaultProfile(NanerConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.DefaultProfile))
        {
            _errors.Add("DefaultProfile cannot be empty");
            return;
        }

        // Check if default profile exists
        if (!config.Profiles.ContainsKey(config.DefaultProfile) &&
            !config.CustomProfiles.ContainsKey(config.DefaultProfile))
        {
            _errors.Add($"DefaultProfile '{config.DefaultProfile}' does not exist in Profiles or CustomProfiles");
        }
    }

    /// <summary>
    /// Validates all profile configurations.
    /// </summary>
    private void ValidateProfiles(NanerConfig config)
    {
        if (config.Profiles.Count == 0 && config.CustomProfiles.Count == 0)
        {
            _errors.Add("At least one profile must be defined in Profiles or CustomProfiles");
            return;
        }

        // Validate standard profiles
        foreach (var (profileName, profile) in config.Profiles)
        {
            ValidateProfile(profileName, profile, "Profiles");
        }

        // Validate custom profiles
        foreach (var (profileName, profile) in config.CustomProfiles)
        {
            ValidateProfile(profileName, profile, "CustomProfiles");
        }

        // Check for duplicate profile names
        var duplicates = config.Profiles.Keys
            .Intersect(config.CustomProfiles.Keys, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (duplicates.Any())
        {
            _warnings.Add($"Profile names appear in both Profiles and CustomProfiles: {string.Join(", ", duplicates)}");
        }
    }

    /// <summary>
    /// Validates a single profile configuration.
    /// </summary>
    private void ValidateProfile(string profileName, ProfileConfig profile, string source)
    {
        var prefix = $"{source}[{profileName}]";

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            _errors.Add($"{prefix}.Name cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(profile.Shell))
        {
            _errors.Add($"{prefix}.Shell cannot be empty");
        }
        else
        {
            var validShells = new[] { "PowerShell", "Bash", "CMD", "Custom" };
            if (!validShells.Contains(profile.Shell, StringComparer.OrdinalIgnoreCase))
            {
                _warnings.Add($"{prefix}.Shell '{profile.Shell}' is not a standard shell type (PowerShell, Bash, CMD, Custom)");
            }

            // Validate custom shell configuration
            if (profile.Shell.Equals("Custom", StringComparison.OrdinalIgnoreCase))
            {
                if (profile.CustomShell == null)
                {
                    _errors.Add($"{prefix}.CustomShell must be specified when Shell is 'Custom'");
                }
                else if (string.IsNullOrWhiteSpace(profile.CustomShell.ExecutablePath))
                {
                    _errors.Add($"{prefix}.CustomShell.ExecutablePath cannot be empty");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(profile.StartingDirectory))
        {
            _errors.Add($"{prefix}.StartingDirectory cannot be empty");
        }

        // Validate icon path if specified
        if (!string.IsNullOrWhiteSpace(profile.Icon))
        {
            var iconPath = PathUtilities.ExpandNanerPath(profile.Icon, _nanerRoot);
            if (!File.Exists(iconPath) && !iconPath.Contains("%"))
            {
                _warnings.Add($"{prefix}.Icon file does not exist: {iconPath}");
            }
        }
    }

    /// <summary>
    /// Validates vendor paths configuration.
    /// </summary>
    private void ValidateVendorPaths(NanerConfig config)
    {
        if (config.VendorPaths.Count == 0)
        {
            _warnings.Add("VendorPaths is empty - no vendor tools are configured");
            return;
        }

        foreach (var (vendor, path) in config.VendorPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _errors.Add($"VendorPaths[{vendor}] cannot be empty");
                continue;
            }

            // Check if path exists (after path expansion)
            var expandedPath = PathUtilities.ExpandNanerPath(path, _nanerRoot);
            if (!Directory.Exists(expandedPath) && !expandedPath.Contains("%"))
            {
                _warnings.Add($"VendorPaths[{vendor}] directory does not exist: {expandedPath}");
            }
        }
    }

    /// <summary>
    /// Validates environment configuration.
    /// </summary>
    private void ValidateEnvironment(NanerConfig config)
    {
        if (config.Environment == null)
        {
            _errors.Add("Environment configuration is missing");
            return;
        }

        // Validate PathPrecedence
        if (config.Environment.PathPrecedence == null || config.Environment.PathPrecedence.Count == 0)
        {
            _warnings.Add("Environment.PathPrecedence is empty - no custom paths will be added to PATH");
        }
        else
        {
            for (int i = 0; i < config.Environment.PathPrecedence.Count; i++)
            {
                var path = config.Environment.PathPrecedence[i];
                if (string.IsNullOrWhiteSpace(path))
                {
                    _errors.Add($"Environment.PathPrecedence[{i}] cannot be empty");
                    continue;
                }

                // Check if path exists (after path expansion)
                var expandedPath = PathUtilities.ExpandNanerPath(path, _nanerRoot);
                if (!Directory.Exists(expandedPath) && !expandedPath.Contains("%"))
                {
                    _warnings.Add($"Environment.PathPrecedence[{i}] directory does not exist: {expandedPath}");
                }
            }
        }

        // Validate EnvironmentVariables
        if (config.Environment.EnvironmentVariables != null)
        {
            foreach (var (key, value) in config.Environment.EnvironmentVariables)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    _errors.Add("Environment variable name cannot be empty");
                }

                if (value == null)
                {
                    _warnings.Add($"Environment variable '{key}' has null value");
                }
            }
        }
    }

    /// <summary>
    /// Throws an exception with all validation errors if validation failed.
    /// </summary>
    public void ThrowIfInvalid()
    {
        if (_errors.Count > 0)
        {
            var message = $"Configuration validation failed with {_errors.Count} error(s):\n" +
                          string.Join("\n", _errors.Select(e => $"  - {e}"));
            throw new InvalidOperationException(message);
        }
    }
}
