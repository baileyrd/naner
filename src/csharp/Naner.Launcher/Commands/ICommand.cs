namespace Naner.Launcher.Commands;

/// <summary>
/// Base interface for all commands in the command pattern.
/// Enables consistent command execution and testability.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="args">Command arguments (excluding the command name itself)</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    int Execute(string[] args);
}
