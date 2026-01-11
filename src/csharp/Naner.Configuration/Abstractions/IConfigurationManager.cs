namespace Naner.Configuration.Abstractions;

/// <summary>
/// Interface for managing Naner configuration loading and processing.
/// Enables dependency injection and testability.
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Loads configuration from naner.json file.
    /// </summary>
    /// <param name="configPath">Optional custom config path. Defaults to config/naner.json</param>
    /// <returns>Loaded configuration</returns>
    /// <exception cref="FileNotFoundException">Thrown when config file doesn't exist</exception>
    /// <exception cref="JsonException">Thrown when config file is invalid JSON</exception>
    NanerConfig Load(string? configPath = null);

    /// <summary>
    /// Gets the loaded configuration. Throws if not loaded.
    /// </summary>
    NanerConfig Config { get; }

    /// <summary>
    /// Gets a profile by name from the configuration.
    /// Checks both standard profiles and custom profiles.
    /// </summary>
    /// <param name="profileName">Name of the profile to retrieve</param>
    /// <param name="useDefaultOnNotFound">If true, returns the default profile when the requested profile is not found</param>
    /// <returns>Profile configuration</returns>
    /// <exception cref="InvalidOperationException">Thrown when profile doesn't exist and no default fallback available</exception>
    ProfileConfig GetProfile(string profileName, bool useDefaultOnNotFound = false);

    /// <summary>
    /// Builds the unified PATH string from configuration.
    /// </summary>
    /// <param name="includeSystemPath">Whether to append system PATH</param>
    /// <returns>Unified PATH string with paths separated by semicolons</returns>
    string BuildUnifiedPath(bool includeSystemPath = true);

    /// <summary>
    /// Sets up environment variables from configuration.
    /// </summary>
    void SetupEnvironmentVariables();
}
