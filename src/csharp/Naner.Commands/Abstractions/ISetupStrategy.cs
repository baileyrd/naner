using System.Threading.Tasks;

namespace Naner.Commands.Abstractions;

/// <summary>
/// Strategy interface for different setup modes.
/// Follows the Strategy pattern to separate interactive and quick setup logic.
/// </summary>
public interface ISetupStrategy
{
    /// <summary>
    /// Gets the name of this setup strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the setup strategy.
    /// </summary>
    /// <param name="targetPath">Target installation path</param>
    /// <param name="options">Setup options</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    Task<int> ExecuteAsync(string targetPath, SetupOptions options);
}

/// <summary>
/// Options for setup strategies.
/// </summary>
public class SetupOptions
{
    /// <summary>
    /// Whether to skip vendor installation.
    /// </summary>
    public bool SkipVendors { get; set; }

    /// <summary>
    /// Whether to include vendors in quick setup.
    /// </summary>
    public bool WithVendors { get; set; }

    /// <summary>
    /// Whether debug mode is enabled.
    /// </summary>
    public bool DebugMode { get; set; }
}
