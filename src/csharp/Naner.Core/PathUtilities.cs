using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Naner.Core;

/// <summary>
/// Utilities for path resolution and expansion in Naner environment.
/// Migrated from Common.psm1 Find-NanerRoot and Expand-NanerPath functions.
/// </summary>
public static class PathUtilities
{
    /// <summary>
    /// Finds the Naner root directory by traversing up from a starting path
    /// looking for marker directories (bin/, vendor/, config/).
    /// </summary>
    /// <param name="startPath">The path to start searching from. If null, uses current directory.</param>
    /// <param name="maxDepth">Maximum number of parent directories to traverse (default: 10).</param>
    /// <returns>The absolute path to Naner root directory.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when Naner root cannot be found.</exception>
    public static string FindNanerRoot(string? startPath = null, int maxDepth = NanerConstants.MaxNanerRootSearchDepth)
    {
        // Default to current directory if not specified
        startPath ??= AppContext.BaseDirectory;

        var currentPath = Path.GetFullPath(startPath);
        var searchedPaths = new System.Collections.Generic.List<string>();
        var depth = 0;

        while (depth < maxDepth)
        {
            searchedPaths.Add(currentPath);

            // Check for marker directories
            var binPath = Path.Combine(currentPath, "bin");
            var vendorPath = Path.Combine(currentPath, "vendor");
            var configPath = Path.Combine(currentPath, "config");

            if (Directory.Exists(binPath) &&
                Directory.Exists(vendorPath) &&
                Directory.Exists(configPath))
            {
                return currentPath;
            }

            // Move to parent directory
            var parentInfo = Directory.GetParent(currentPath);
            if (parentInfo == null || parentInfo.FullName == currentPath)
            {
                break;
            }

            currentPath = parentInfo.FullName;
            depth++;
        }

        // Enhanced error message with search details
        var pathsList = string.Join("\n", searchedPaths.Select(p => $"    - {p}"));
        throw new DirectoryNotFoundException(
            $"Could not locate Naner root directory.\n\n" +
            $"Search Details:\n" +
            $"  Starting path: {startPath}\n" +
            $"  Executable location: {AppContext.BaseDirectory}\n" +
            $"  Paths searched ({searchedPaths.Count}):\n{pathsList}\n\n" +
            $"Requirements:\n" +
            $"  Naner root must contain:\n" +
            $"    - bin/      (binaries directory)\n" +
            $"    - vendor/   (vendor dependencies)\n" +
            $"    - config/   (configuration files)\n\n" +
            $"Solutions:\n" +
            $"  1. Copy naner.exe to vendor/bin/ in your Naner installation\n" +
            $"  2. Run from within the Naner directory structure\n" +
            $"  3. Set NANER_ROOT environment variable to your Naner directory");
    }

    /// <summary>
    /// Expands a path containing %NANER_ROOT% and environment variables.
    /// Handles both Windows-style (%VAR%) and PowerShell-style ($env:VAR) variables.
    /// </summary>
    /// <param name="path">The path string to expand.</param>
    /// <param name="nanerRoot">The Naner root directory path.</param>
    /// <returns>The expanded path with all variables resolved.</returns>
    public static string ExpandNanerPath(string path, string nanerRoot)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        // Replace %NANER_ROOT% first (case-insensitive)
        var expanded = path.Replace("%NANER_ROOT%", nanerRoot, StringComparison.OrdinalIgnoreCase);

        // Expand Windows-style environment variables (%VAR%)
        expanded = Environment.ExpandEnvironmentVariables(expanded);

        // Handle PowerShell-style environment variables ($env:VAR)
        // Pattern: $env:VARIABLE_NAME
        var envVarPattern = new Regex(@"\$env:(\w+)", RegexOptions.IgnoreCase);
        expanded = envVarPattern.Replace(expanded, match =>
        {
            var varName = match.Groups[1].Value;
            var varValue = Environment.GetEnvironmentVariable(varName);
            return varValue ?? match.Value; // Return original if variable doesn't exist
        });

        return expanded;
    }

    /// <summary>
    /// Gets the Naner root directory using simple parent directory navigation.
    /// Assumes the script is in src/powershell or src/csharp directory.
    /// Faster but less robust than FindNanerRoot.
    /// </summary>
    /// <param name="scriptRoot">The directory of the current executable/script.</param>
    /// <returns>The Naner root directory path.</returns>
    public static string GetNanerRootSimple(string scriptRoot)
    {
        // Go up two levels: src/csharp -> src -> naner_root
        var parent = Directory.GetParent(scriptRoot);
        if (parent == null)
        {
            throw new DirectoryNotFoundException($"Cannot find parent of {scriptRoot}");
        }

        var grandparent = Directory.GetParent(parent.FullName);
        if (grandparent == null)
        {
            throw new DirectoryNotFoundException($"Cannot find grandparent of {scriptRoot}");
        }

        return grandparent.FullName;
    }

    /// <summary>
    /// Validates that a path exists (file or directory).
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path exists, false otherwise.</returns>
    public static bool PathExists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="directoryPath">The directory path to ensure exists.</param>
    public static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Normalizes a path by resolving relative paths and standardizing separators.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized absolute path.</returns>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        return Path.GetFullPath(path);
    }
}
