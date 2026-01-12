namespace Naner.Commands.Abstractions;

/// <summary>
/// Service for reporting environment variable information.
/// </summary>
public interface IEnvironmentReporter
{
    /// <summary>
    /// Displays environment variables relevant to Naner.
    /// </summary>
    void Report();

    /// <summary>
    /// Displays information about the executable location.
    /// </summary>
    void ReportExecutableInfo();
}
