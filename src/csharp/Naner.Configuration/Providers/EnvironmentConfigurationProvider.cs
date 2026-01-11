using System;
using System.Collections.Generic;
using Naner.Configuration.Abstractions;

namespace Naner.Configuration.Providers;

/// <summary>
/// Configuration provider that reads overrides from environment variables.
/// Environment variables take precedence over file-based configuration.
///
/// Supported environment variables:
/// - NANER_DEFAULT_PROFILE: Sets the default profile
/// - NANER_INHERIT_SYSTEM_PATH: Set to "false" to disable system PATH inheritance
/// - NANER_DEBUG: Set to "true" to enable debug mode
/// - NANER_ENV_*: Custom environment variables (e.g., NANER_ENV_EDITOR=vim)
/// - NANER_PATH_*: Additional PATH entries (e.g., NANER_PATH_CUSTOM=/path/to/bin)
/// </summary>
public class EnvironmentConfigurationProvider : IConfigurationProvider
{
    private const string Prefix = "NANER_";
    private const string EnvVarPrefix = "NANER_ENV_";
    private const string PathPrefix = "NANER_PATH_";

    public int Priority => 100; // Highest priority - applied last
    public string Name => "Environment Variables";

    public bool CanLoad(string configPath)
    {
        // Environment provider doesn't load from files
        return false;
    }

    public NanerConfig Load(string configPath)
    {
        throw new NotSupportedException("Environment provider does not support loading full configuration");
    }

    public void ApplyOverrides(NanerConfig config)
    {
        // Override default profile
        var defaultProfile = Environment.GetEnvironmentVariable($"{Prefix}DEFAULT_PROFILE");
        if (!string.IsNullOrEmpty(defaultProfile))
        {
            config.DefaultProfile = defaultProfile;
            Logger.Debug($"Overriding DefaultProfile from environment: {defaultProfile}", debugMode: false);
        }

        // Override system PATH inheritance
        var inheritPath = Environment.GetEnvironmentVariable($"{Prefix}INHERIT_SYSTEM_PATH");
        if (!string.IsNullOrEmpty(inheritPath))
        {
            config.Advanced.InheritSystemPath = !inheritPath.Equals("false", StringComparison.OrdinalIgnoreCase);
        }

        // Override debug mode
        var debugMode = Environment.GetEnvironmentVariable($"{Prefix}DEBUG");
        if (!string.IsNullOrEmpty(debugMode))
        {
            config.Advanced.DebugMode = debugMode.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        // Collect custom environment variables (NANER_ENV_*)
        ApplyCustomEnvironmentVariables(config);

        // Collect additional PATH entries (NANER_PATH_*)
        ApplyAdditionalPathEntries(config);
    }

    private void ApplyCustomEnvironmentVariables(NanerConfig config)
    {
        var envVars = Environment.GetEnvironmentVariables();

        foreach (var key in envVars.Keys)
        {
            var keyStr = key?.ToString() ?? "";
            if (keyStr.StartsWith(EnvVarPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var varName = keyStr.Substring(EnvVarPrefix.Length);
                var varValue = envVars[key]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(varName))
                {
                    config.Environment.EnvironmentVariables[varName] = varValue;
                    Logger.Debug($"Added environment variable from NANER_ENV_{varName}", debugMode: false);
                }
            }
        }
    }

    private void ApplyAdditionalPathEntries(NanerConfig config)
    {
        var envVars = Environment.GetEnvironmentVariables();
        var additionalPaths = new List<string>();

        foreach (var key in envVars.Keys)
        {
            var keyStr = key?.ToString() ?? "";
            if (keyStr.StartsWith(PathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var pathValue = envVars[key]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(pathValue))
                {
                    additionalPaths.Add(pathValue);
                    Logger.Debug($"Added PATH entry from {keyStr}", debugMode: false);
                }
            }
        }

        // Prepend additional paths to the path precedence list
        if (additionalPaths.Count > 0)
        {
            additionalPaths.AddRange(config.Environment.PathPrecedence);
            config.Environment.PathPrecedence = additionalPaths;
        }
    }
}
