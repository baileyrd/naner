namespace Naner.Launcher;

/// <summary>
/// Utilities for finding Naner root and expanding path placeholders
/// </summary>
public static class PathResolver
{
    /// <summary>
    /// Finds the Naner root directory by looking for marker directories
    /// </summary>
    /// <param name="startPath">Starting path (defaults to executable location)</param>
    /// <param name="maxDepth">Maximum number of parent directories to search</param>
    /// <returns>Absolute path to Naner root</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when root cannot be found</exception>
    public static string FindNanerRoot(string? startPath = null, int maxDepth = 10)
    {
        var currentPath = startPath ?? AppContext.BaseDirectory;
        var depth = 0;

        while (depth < maxDepth)
        {
            // Check for marker directories that identify Naner root
            var binPath = Path.Combine(currentPath, "bin");
            var vendorPath = Path.Combine(currentPath, "vendor");
            var configPath = Path.Combine(currentPath, "config");

            if (Directory.Exists(binPath) &&
                Directory.Exists(vendorPath) &&
                Directory.Exists(configPath))
            {
                return Path.GetFullPath(currentPath);
            }

            // Move to parent directory
            var parentPath = Directory.GetParent(currentPath)?.FullName;
            if (string.IsNullOrEmpty(parentPath) || parentPath == currentPath)
            {
                break; // Reached root of filesystem
            }

            currentPath = parentPath;
            depth++;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate Naner root directory from '{startPath ?? AppContext.BaseDirectory}'. " +
            "Ensure bin/, vendor/, and config/ directories exist in the Naner installation.");
    }

    /// <summary>
    /// Expands path placeholders and environment variables
    /// </summary>
    /// <param name="path">Path containing placeholders like %NANER_ROOT%</param>
    /// <param name="nanerRoot">Absolute path to Naner root</param>
    /// <returns>Expanded absolute path</returns>
    public static string ExpandNanerPath(string path, string nanerRoot)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        // Replace %NANER_ROOT% placeholder
        var expanded = path.Replace("%NANER_ROOT%", nanerRoot, StringComparison.OrdinalIgnoreCase);

        // Expand Windows environment variables
        expanded = Environment.ExpandEnvironmentVariables(expanded);

        return expanded;
    }

    /// <summary>
    /// Sets up process environment variables for Naner
    /// </summary>
    /// <param name="nanerRoot">Naner root directory</param>
    /// <param name="environment">Environment name (e.g., "default")</param>
    public static void SetupEnvironment(string nanerRoot, string environment)
    {
        Environment.SetEnvironmentVariable("NANER_ROOT", nanerRoot, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("NANER_ENVIRONMENT", environment, EnvironmentVariableTarget.Process);
    }
}
