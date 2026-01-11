namespace Naner.Commands.Abstractions;

/// <summary>
/// Factory for creating terminal launcher instances.
/// Breaks circular dependency between Commands and Launcher.
/// </summary>
public interface ITerminalLauncherFactory
{
    /// <summary>
    /// Creates a terminal launcher for the specified Naner root.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory path</param>
    /// <param name="debugMode">Enable debug mode</param>
    /// <returns>Terminal launcher instance</returns>
    ITerminalLauncher Create(string nanerRoot, bool debugMode = false);
}
