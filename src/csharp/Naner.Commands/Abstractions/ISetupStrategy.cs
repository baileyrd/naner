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
/// Specifies how vendors should be handled during setup.
/// Replaces contradictory boolean flags with clear, mutually exclusive options.
/// </summary>
public enum VendorInstallMode
{
    /// <summary>
    /// Default behavior - vendors installed in interactive mode, skipped in quick mode unless specified.
    /// </summary>
    Default,

    /// <summary>
    /// Skip vendor installation entirely.
    /// </summary>
    Skip,

    /// <summary>
    /// Force vendor installation regardless of setup mode.
    /// </summary>
    Install
}

/// <summary>
/// Options for setup strategies.
/// </summary>
public class SetupOptions
{
    /// <summary>
    /// Specifies how vendors should be handled during setup.
    /// Uses enum to avoid contradictory boolean flag states.
    /// </summary>
    public VendorInstallMode VendorMode { get; set; } = VendorInstallMode.Default;

    /// <summary>
    /// Whether debug mode is enabled.
    /// </summary>
    public bool DebugMode { get; set; }
}
