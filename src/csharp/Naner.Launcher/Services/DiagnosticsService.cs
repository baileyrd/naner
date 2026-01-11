using System;
using Naner.Launcher.Abstractions;

namespace Naner.Launcher.Services;

/// <summary>
/// Orchestrates diagnostic services for Naner installation health checks.
/// Single Responsibility: Coordinate diagnostics workflow.
/// </summary>
public class DiagnosticsService : IDiagnosticsService
{
    private readonly DirectoryVerifier _directoryVerifier;
    private readonly ConfigurationVerifier _configurationVerifier;
    private readonly EnvironmentReporter _environmentReporter;

    /// <summary>
    /// Creates a new diagnostics service with its dependencies.
    /// </summary>
    public DiagnosticsService()
    {
        _directoryVerifier = new DirectoryVerifier();
        _configurationVerifier = new ConfigurationVerifier();
        _environmentReporter = new EnvironmentReporter();
    }

    /// <summary>
    /// Runs comprehensive diagnostics on the Naner installation.
    /// </summary>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public int Run()
    {
        Logger.Header("Naner Diagnostics");
        Console.WriteLine($"Version: {NanerConstants.Version}");
        Console.WriteLine($"Phase: {NanerConstants.PhaseName}");
        Logger.NewLine();

        // Executable location
        _environmentReporter.ReportExecutableInfo();

        // NANER_ROOT search
        Logger.Status("Searching for NANER_ROOT...");
        try
        {
            var nanerRoot = PathResolver.FindNanerRoot();
            Logger.Success($"  Found: {nanerRoot}");
            Logger.NewLine();

            // Verify structure
            _directoryVerifier.Verify(nanerRoot);

            // Config check
            _configurationVerifier.Verify(nanerRoot);

            // Environment
            _environmentReporter.Report();

            Logger.NewLine();
            Logger.Success("Diagnostics complete - Naner installation appears healthy");
            return 0;
        }
        catch (Exception ex)
        {
            DisplayNanerRootNotFoundError(ex);
            return 1;
        }
    }

    /// <summary>
    /// Displays error message when NANER_ROOT cannot be found.
    /// </summary>
    private void DisplayNanerRootNotFoundError(Exception ex)
    {
        Logger.Failure("NANER_ROOT not found");
        Logger.NewLine();
        Logger.Info("Details:");
        Logger.Info($"  {ex.Message}");
        Logger.NewLine();
        Logger.Info("This usually means:");
        Logger.Info("  1. You're running naner.exe outside the Naner directory");
        Logger.Info("  2. The Naner directory structure is incomplete");
        Logger.Info("  3. You need to set NANER_ROOT environment variable");
        Logger.NewLine();
        Logger.Info("Try:");
        Logger.Info("  1. cd <your-naner-directory>");
        Logger.Info("  2. .\\vendor\\bin\\naner.exe --diagnose");
    }
}
