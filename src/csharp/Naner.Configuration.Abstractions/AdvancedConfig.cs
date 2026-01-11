using System.Text.Json.Serialization;

namespace Naner.Configuration.Abstractions;

/// <summary>
/// Advanced configuration options for power users.
/// </summary>
public class AdvancedConfig
{
    /// <summary>
    /// Whether to preserve the original PATH instead of replacing it.
    /// </summary>
    [JsonPropertyName("PreservePath")]
    public bool PreservePath { get; set; } = false;

    /// <summary>
    /// Whether to inherit system PATH in addition to vendor paths.
    /// </summary>
    [JsonPropertyName("InheritSystemPath")]
    public bool InheritSystemPath { get; set; } = true;

    /// <summary>
    /// Enable verbose logging output.
    /// </summary>
    [JsonPropertyName("VerboseLogging")]
    public bool VerboseLogging { get; set; } = false;

    /// <summary>
    /// Enable debug mode with detailed diagnostics.
    /// </summary>
    [JsonPropertyName("DebugMode")]
    public bool DebugMode { get; set; } = false;
}
