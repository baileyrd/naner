using System;
using System.IO;
using Naner.Commands.Abstractions;

namespace Naner.Commands.Services;

/// <summary>
/// Orchestrates diagnostic checks for Naner installation.
/// Delegates to specialized services for each diagnostic area.
/// Single Responsibility: Coordinating diagnostic workflow.
/// </summary>
public class DiagnosticsService : IDiagnosticsService
{
    private readonly IDirectoryVerifier _directoryVerifier;
    private readonly IConfigurationVerifier _configurationVerifier;
    private readonly IEnvironmentReporter _environmentReporter;

    /// <summary>
    /// Creates a new DiagnosticsService with injected dependencies.
    /// Preferred constructor for DI containers and testing.
    /// </summary>
    public DiagnosticsService(
        IDirectoryVerifier directoryVerifier,
        IConfigurationVerifier configurationVerifier,
        IEnvironmentReporter environmentReporter)
    {
        _directoryVerifier = directoryVerifier ?? throw new ArgumentNullException(nameof(directoryVerifier));
        _configurationVerifier = configurationVerifier ?? throw new ArgumentNullException(nameof(configurationVerifier));
        _environmentReporter = environmentReporter ?? throw new ArgumentNullException(nameof(environmentReporter));
    }

    /// <summary>
    /// Creates a new DiagnosticsService with default implementations.
    /// Maintains backward compatibility for non-DI usage.
    /// </summary>
    public DiagnosticsService()
        : this(new DirectoryVerifier(), new ConfigurationVerifier(), new EnvironmentReporter())
    {
    }

    /// <summary>
    /// Runs comprehensive diagnostics on the Naner installation.
    /// </summary>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public int Run()
    {
        Logger.Header("Naner Diagnostics");
        Logger.NewLine();

        try
        {
            // 1. Show executable info
            _environmentReporter.ReportExecutableInfo();

            // 2. Find NANER_ROOT
            string nanerRoot;
            try
            {
                nanerRoot = PathUtilities.FindNanerRoot();
                Logger.Success($"Naner Root: {nanerRoot}");
                Logger.NewLine();
            }
            catch (DirectoryNotFoundException ex)
            {
                Logger.Failure("Could not locate Naner root directory");
                Logger.NewLine();
                Console.WriteLine(ex.Message);
                Logger.NewLine();
                Logger.Info("Please run this from within your Naner installation,");
                Logger.Info("or run 'naner init' first to set up Naner.");
                return 1;
            }

            // 3. Verify directory structure
            _directoryVerifier.Verify(nanerRoot);

            // 4. Verify configuration
            _configurationVerifier.Verify(nanerRoot);

            // 5. Show environment variables
            _environmentReporter.Report();

            Logger.NewLine();
            Logger.Success("Diagnostics complete!");
            return 0;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Diagnostics failed: {ex.Message}");
            Logger.Debug($"Exception details: {ex}", debugMode: false);
            return 1;
        }
    }
}
