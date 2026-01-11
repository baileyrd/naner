using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Naner.Core;

/// <summary>
/// Utility for building unified PATH environment variables.
/// Consolidates duplicate PATH building logic from ConfigurationManager and TerminalLauncher.
/// </summary>
public static class PathBuilder
{
    /// <summary>
    /// Builds a unified PATH string from a list of paths with optional system PATH inheritance.
    /// </summary>
    /// <param name="pathPrecedence">List of paths in priority order</param>
    /// <param name="nanerRoot">Naner root directory for path expansion</param>
    /// <param name="includeSystemPath">Whether to append the current system PATH</param>
    /// <returns>Unified PATH string with paths separated by semicolons</returns>
    public static string BuildUnifiedPath(
        IEnumerable<string> pathPrecedence,
        string nanerRoot,
        bool includeSystemPath = true)
    {
        if (pathPrecedence == null)
            throw new ArgumentNullException(nameof(pathPrecedence));
        if (string.IsNullOrEmpty(nanerRoot))
            throw new ArgumentException("Naner root cannot be null or empty", nameof(nanerRoot));

        var pathBuilder = new StringBuilder();

        // Add configured paths in precedence order
        foreach (var path in pathPrecedence)
        {
            var expandedPath = PathUtilities.ExpandNanerPath(path, nanerRoot);

            // Only add paths that exist
            if (Directory.Exists(expandedPath))
            {
                if (pathBuilder.Length > 0)
                {
                    pathBuilder.Append(';');
                }
                pathBuilder.Append(expandedPath);
            }
        }

        // Optionally append system PATH
        if (includeSystemPath)
        {
            var systemPath = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(systemPath))
            {
                if (pathBuilder.Length > 0)
                {
                    pathBuilder.Append(';');
                }
                pathBuilder.Append(systemPath);
            }
        }

        return pathBuilder.ToString();
    }

    /// <summary>
    /// Sets the PATH environment variable for the current process.
    /// </summary>
    /// <param name="pathValue">The PATH value to set</param>
    public static void SetProcessPath(string pathValue)
    {
        Environment.SetEnvironmentVariable("PATH", pathValue, EnvironmentVariableTarget.Process);
    }

    /// <summary>
    /// Builds and sets the unified PATH for the current process.
    /// </summary>
    /// <param name="pathPrecedence">List of paths in priority order</param>
    /// <param name="nanerRoot">Naner root directory for path expansion</param>
    /// <param name="includeSystemPath">Whether to append the current system PATH</param>
    /// <returns>The unified PATH that was set</returns>
    public static string BuildAndSetPath(
        IEnumerable<string> pathPrecedence,
        string nanerRoot,
        bool includeSystemPath = true)
    {
        var unifiedPath = BuildUnifiedPath(pathPrecedence, nanerRoot, includeSystemPath);
        SetProcessPath(unifiedPath);
        return unifiedPath;
    }
}
