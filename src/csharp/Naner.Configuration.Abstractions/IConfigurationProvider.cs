namespace Naner.Configuration.Abstractions;

/// <summary>
/// Interface for configuration providers that can load configuration from various sources.
/// Implements the Strategy pattern for configuration loading.
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Gets the priority of this provider. Lower values are checked first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets the name of this configuration provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines if this provider can load configuration from the specified path.
    /// </summary>
    /// <param name="configPath">Path to the configuration file or source identifier</param>
    /// <returns>True if this provider can handle the configuration source</returns>
    bool CanLoad(string configPath);

    /// <summary>
    /// Loads configuration from the specified source.
    /// </summary>
    /// <param name="configPath">Path to the configuration file or source identifier</param>
    /// <returns>The loaded configuration</returns>
    /// <exception cref="FileNotFoundException">Thrown when the configuration file doesn't exist</exception>
    /// <exception cref="InvalidOperationException">Thrown when configuration cannot be parsed</exception>
    NanerConfig Load(string configPath);

    /// <summary>
    /// Applies configuration overrides from this provider to an existing configuration.
    /// Used for layered configuration (e.g., environment variables override file config).
    /// </summary>
    /// <param name="config">The configuration to modify</param>
    void ApplyOverrides(NanerConfig config);
}
