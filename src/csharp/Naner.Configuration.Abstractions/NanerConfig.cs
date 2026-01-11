using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Naner.Configuration.Abstractions;

/// <summary>
/// Root configuration model for Naner terminal launcher.
/// Maps to config/naner.json schema.
/// </summary>
public class NanerConfig
{
    /// <summary>
    /// Paths to vendored tools and applications.
    /// </summary>
    [JsonPropertyName("VendorPaths")]
    public Dictionary<string, string> VendorPaths { get; set; } = new();

    /// <summary>
    /// Environment configuration including PATH and environment variables.
    /// </summary>
    [JsonPropertyName("Environment")]
    public EnvironmentConfig Environment { get; set; } = new();

    /// <summary>
    /// Default profile to use when none is specified.
    /// </summary>
    [JsonPropertyName("DefaultProfile")]
    public string DefaultProfile { get; set; } = "Unified";

    /// <summary>
    /// Terminal profiles configuration.
    /// </summary>
    [JsonPropertyName("Profiles")]
    public Dictionary<string, ProfileConfig> Profiles { get; set; } = new();

    /// <summary>
    /// Windows Terminal specific settings.
    /// </summary>
    [JsonPropertyName("WindowsTerminal")]
    public WindowsTerminalConfig WindowsTerminal { get; set; } = new();

    /// <summary>
    /// Advanced configuration options.
    /// </summary>
    [JsonPropertyName("Advanced")]
    public AdvancedConfig Advanced { get; set; } = new();

    /// <summary>
    /// Custom user-defined profiles.
    /// </summary>
    [JsonPropertyName("CustomProfiles")]
    public Dictionary<string, ProfileConfig> CustomProfiles { get; set; } = new();
}
