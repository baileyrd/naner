using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Naner.Configuration.Abstractions;

/// <summary>
/// Environment configuration for PATH and environment variables.
/// </summary>
public class EnvironmentConfig
{
    /// <summary>
    /// Whether to use unified PATH across all profiles.
    /// </summary>
    [JsonPropertyName("UnifiedPath")]
    public bool UnifiedPath { get; set; } = true;

    /// <summary>
    /// Ordered list of paths to prepend to system PATH.
    /// Earlier entries take precedence.
    /// </summary>
    [JsonPropertyName("PathPrecedence")]
    public List<string> PathPrecedence { get; set; } = new();

    /// <summary>
    /// Environment variables to set for all profiles.
    /// Values can contain %NANER_ROOT% placeholder.
    /// </summary>
    [JsonPropertyName("EnvironmentVariables")]
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}
