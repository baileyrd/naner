namespace Naner.Launcher.Abstractions;

/// <summary>
/// Interface for launching Windows Terminal with configured profiles.
/// Enables dependency injection and testability.
/// </summary>
public interface ITerminalLauncher
{
    /// <summary>
    /// Launches Windows Terminal with the specified profile.
    /// </summary>
    /// <param name="profileName">Name of the profile to launch</param>
    /// <param name="startingDirectory">Optional starting directory override</param>
    /// <returns>Exit code from the terminal launch</returns>
    int LaunchProfile(string profileName, string? startingDirectory = null);
}
