using Naner.Common;

namespace Naner.Launcher;

/// <summary>
/// Launcher-specific path resolver that delegates to Naner.Common.PathUtilities.
/// Provides environment setup specific to the launcher.
/// </summary>
public static class PathResolver
{
    /// <summary>
    /// Finds the Naner root directory by looking for marker directories.
    /// Delegates to Naner.Common.PathUtilities.FindNanerRoot.
    /// </summary>
    /// <param name="startPath">Starting path (defaults to executable location)</param>
    /// <param name="maxDepth">Maximum number of parent directories to search</param>
    /// <returns>Absolute path to Naner root</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when root cannot be found</exception>
    public static string FindNanerRoot(string? startPath = null, int maxDepth = 10)
    {
        return PathUtilities.FindNanerRoot(startPath, maxDepth);
    }

    /// <summary>
    /// Expands path placeholders and environment variables.
    /// Delegates to Naner.Common.PathUtilities.ExpandNanerPath.
    /// </summary>
    /// <param name="path">Path containing placeholders like %NANER_ROOT%</param>
    /// <param name="nanerRoot">Absolute path to Naner root</param>
    /// <returns>Expanded absolute path</returns>
    public static string ExpandNanerPath(string path, string nanerRoot)
    {
        return PathUtilities.ExpandNanerPath(path, nanerRoot);
    }

    /// <summary>
    /// Sets up process environment variables for Naner launcher.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory</param>
    /// <param name="environment">Environment name (e.g., "default")</param>
    public static void SetupEnvironment(string nanerRoot, string environment)
    {
        Environment.SetEnvironmentVariable("NANER_ROOT", nanerRoot, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("NANER_ENVIRONMENT", environment, EnvironmentVariableTarget.Process);

        // Also setup HOME environment variables for shells
        var homePath = Path.Combine(nanerRoot, "home");
        if (Directory.Exists(homePath))
        {
            Environment.SetEnvironmentVariable("HOME", homePath, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("NANER_HOME", homePath, EnvironmentVariableTarget.Process);
        }
    }
}
