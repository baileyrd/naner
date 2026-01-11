using System;

namespace Naner.Launcher.Services;

/// <summary>
/// Reports environment variable information.
/// Single Responsibility: Environment variable display.
/// </summary>
public class EnvironmentReporter
{
    /// <summary>
    /// Displays environment variables relevant to Naner.
    /// </summary>
    public void Report()
    {
        Logger.Status("Environment Variables:");
        var envVars = new[] { "NANER_ROOT", "NANER_ENVIRONMENT", "HOME", "PATH" };

        foreach (var envVar in envVars)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            if (value != null)
            {
                // Truncate PATH for readability
                if (envVar == "PATH")
                {
                    value = value.Substring(0, Math.Min(100, value.Length)) + "...";
                }
                Logger.Info($"  {envVar}={value}");
            }
            else
            {
                Logger.Info($"  {envVar}=(not set)");
            }
        }
    }

    /// <summary>
    /// Displays information about the executable location.
    /// </summary>
    public void ReportExecutableInfo()
    {
        Logger.Status("Executable Information:");
        Logger.Info($"  Location: {AppContext.BaseDirectory}");
        Logger.Info($"  Command Line: {Environment.CommandLine}");
        Logger.NewLine();
    }
}
