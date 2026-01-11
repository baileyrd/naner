namespace Naner.Launcher.Abstractions;

/// <summary>
/// Service for running system diagnostics and health checks.
/// </summary>
public interface IDiagnosticsService
{
    /// <summary>
    /// Runs comprehensive diagnostics on the Naner installation.
    /// </summary>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    int Run();
}
