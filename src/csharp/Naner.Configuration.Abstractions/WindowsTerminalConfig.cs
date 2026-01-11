using System.Text.Json.Serialization;

namespace Naner.Configuration.Abstractions;

/// <summary>
/// Windows Terminal specific configuration.
/// </summary>
public class WindowsTerminalConfig
{
    /// <summary>
    /// Whether to use Windows Terminal as default terminal.
    /// </summary>
    [JsonPropertyName("DefaultTerminal")]
    public bool DefaultTerminal { get; set; } = true;

    /// <summary>
    /// Launch mode: default, maximized, fullscreen, focus.
    /// </summary>
    [JsonPropertyName("LaunchMode")]
    public string LaunchMode { get; set; } = "default";

    /// <summary>
    /// Tab title for the launched terminal.
    /// </summary>
    [JsonPropertyName("TabTitle")]
    public string TabTitle { get; set; } = "Naner";

    /// <summary>
    /// Whether to suppress application title in terminal.
    /// </summary>
    [JsonPropertyName("SuppressApplicationTitle")]
    public bool SuppressApplicationTitle { get; set; } = true;
}
