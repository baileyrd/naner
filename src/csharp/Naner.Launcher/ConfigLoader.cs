using System.Text.Json;
using System.Text.Json.Serialization;

namespace Naner.Launcher;

/// <summary>
/// Loads and parses Naner configuration
/// </summary>
public static class ConfigLoader
{
    /// <summary>
    /// Loads naner.json configuration
    /// </summary>
    /// <param name="configPath">Path to naner.json</param>
    /// <returns>Parsed configuration</returns>
    public static NanerConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        var json = File.ReadAllText(configPath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        var config = JsonSerializer.Deserialize<NanerConfig>(json, options);

        if (config == null)
        {
            throw new InvalidOperationException($"Failed to parse configuration file: {configPath}");
        }

        return config;
    }

    /// <summary>
    /// Builds the unified PATH environment variable from configuration
    /// </summary>
    /// <param name="config">Naner configuration</param>
    /// <param name="nanerRoot">Naner root directory</param>
    /// <returns>PATH string with semicolon-separated directories</returns>
    public static string BuildUnifiedPath(NanerConfig config, string nanerRoot)
    {
        var pathComponents = new List<string>();

        if (config.Environment?.PathPrecedence != null)
        {
            foreach (var pathEntry in config.Environment.PathPrecedence)
            {
                var expandedPath = PathResolver.ExpandNanerPath(pathEntry, nanerRoot);

                // Only add paths that exist
                if (Directory.Exists(expandedPath))
                {
                    pathComponents.Add(expandedPath);
                }
            }
        }

        // Append system PATH if configured
        if (config.Environment?.InheritSystemPath ?? true)
        {
            var systemPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

            if (!string.IsNullOrEmpty(systemPath))
                pathComponents.Add(systemPath);
            if (!string.IsNullOrEmpty(userPath))
                pathComponents.Add(userPath);
        }

        return string.Join(";", pathComponents);
    }
}

/// <summary>
/// Naner configuration model (minimal subset for Phase 1)
/// </summary>
public class NanerConfig
{
    [JsonPropertyName("VendorPaths")]
    public Dictionary<string, string>? VendorPaths { get; set; }

    [JsonPropertyName("Environment")]
    public EnvironmentConfig? Environment { get; set; }

    [JsonPropertyName("DefaultProfile")]
    public string? DefaultProfile { get; set; }
}

/// <summary>
/// Environment configuration
/// </summary>
public class EnvironmentConfig
{
    [JsonPropertyName("PathPrecedence")]
    public List<string>? PathPrecedence { get; set; }

    [JsonPropertyName("InheritSystemPath")]
    public bool InheritSystemPath { get; set; } = true;

    [JsonPropertyName("EnvironmentVariables")]
    public Dictionary<string, string>? EnvironmentVariables { get; set; }
}
