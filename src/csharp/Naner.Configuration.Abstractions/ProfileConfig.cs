using System.Text.Json.Serialization;

namespace Naner.Configuration.Abstractions;

/// <summary>
/// Terminal profile configuration.
/// </summary>
public class ProfileConfig
{
    /// <summary>
    /// Display name for the profile.
    /// </summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this profile provides.
    /// </summary>
    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    /// <summary>
    /// Shell type: PowerShell, Bash, CMD, or custom.
    /// </summary>
    [JsonPropertyName("Shell")]
    public string Shell { get; set; } = "PowerShell";

    /// <summary>
    /// Starting directory for the shell.
    /// Can contain environment variables.
    /// </summary>
    [JsonPropertyName("StartingDirectory")]
    public string StartingDirectory { get; set; } = "%USERPROFILE%";

    /// <summary>
    /// Path to icon file for the profile.
    /// </summary>
    [JsonPropertyName("Icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Color scheme name for Windows Terminal.
    /// </summary>
    [JsonPropertyName("ColorScheme")]
    public string ColorScheme { get; set; } = "Campbell";

    /// <summary>
    /// Whether to include vendor paths in PATH.
    /// </summary>
    [JsonPropertyName("UseVendorPath")]
    public bool UseVendorPath { get; set; } = true;

    /// <summary>
    /// Custom shell configuration (optional).
    /// </summary>
    [JsonPropertyName("CustomShell")]
    public CustomShellConfig? CustomShell { get; set; }
}

/// <summary>
/// Custom shell executable configuration.
/// </summary>
public class CustomShellConfig
{
    /// <summary>
    /// Path to shell executable.
    /// </summary>
    [JsonPropertyName("ExecutablePath")]
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// Command-line arguments for the shell.
    /// </summary>
    [JsonPropertyName("Arguments")]
    public string? Arguments { get; set; }
}
