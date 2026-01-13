using System;
using System.IO;
using System.Linq;
using Naner.Core;

namespace Naner.Setup;

/// <summary>
/// Detects first-run scenarios and incomplete Naner installations.
/// </summary>
public static class FirstRunDetector
{
    // Use centralized constant for initialization marker file
    private const string InitMarkerFile = NanerConstants.InitializationMarkerFile;

    /// <summary>
    /// Checks if this is the first run of Naner or if the installation is incomplete.
    /// </summary>
    /// <param name="nanerRoot">Optional Naner root path. If null, attempts to find it.</param>
    /// <returns>True if first run or incomplete installation, false otherwise.</returns>
    public static bool IsFirstRun(string? nanerRoot = null)
    {
        // Try to find Naner root if not provided
        if (string.IsNullOrEmpty(nanerRoot))
        {
            try
            {
                nanerRoot = PathUtilities.FindNanerRoot();
            }
            catch (DirectoryNotFoundException)
            {
                return true; // First run - no installation found
            }
        }

        // Check for initialization marker file
        var markerFile = Path.Combine(nanerRoot, InitMarkerFile);
        if (!File.Exists(markerFile))
        {
            return true; // First run - installation not initialized
        }

        // Check for essential directories (using centralized constant)
        foreach (var dir in NanerConstants.DirectoryNames.Essential)
        {
            var path = Path.Combine(nanerRoot, dir);
            if (!Directory.Exists(path))
            {
                return true; // First run - incomplete installation
            }
        }

        // Check for config file
        var configFile = Path.Combine(nanerRoot, NanerConstants.DirectoryNames.Config, NanerConstants.ConfigFileName);
        if (!File.Exists(configFile))
        {
            return true; // First run - missing configuration
        }

        return false; // Not first run
    }

    /// <summary>
    /// Creates the initialization marker file to indicate Naner has been initialized.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory path.</param>
    /// <param name="version">Version string to include in marker.</param>
    /// <param name="phase">Phase name to include in marker.</param>
    public static void CreateInitializationMarker(string nanerRoot, string version, string phase)
    {
        var markerFile = Path.Combine(nanerRoot, InitMarkerFile);
        var markerContent = $@"# Naner Initialization Marker
# Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
# Version: {version}
# Phase: {phase}

This file indicates that Naner has been initialized.
Do not delete this file unless you want to re-run the setup wizard.
";

        File.WriteAllText(markerFile, markerContent);
    }

    /// <summary>
    /// Attempts to establish a Naner root directory using multiple strategies.
    /// </summary>
    /// <returns>Suggested Naner root path, or null if none found.</returns>
    public static string? EstablishNanerRoot()
    {
        // Try 1: Current directory
        var currentDir = Environment.CurrentDirectory;
        if (IsValidNanerRoot(currentDir))
        {
            return currentDir;
        }

        // Try 2: Executable directory (trim trailing separator)
        var exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (IsValidNanerRoot(exeDir))
        {
            return exeDir;
        }

        // Try 3: Parent of executable directory
        var parentDir = Directory.GetParent(exeDir)?.FullName;
        if (parentDir != null && IsValidNanerRoot(parentDir))
        {
            return parentDir;
        }

        // Try 4: Grandparent of executable directory (if exe is in vendor/bin)
        if (parentDir != null)
        {
            var grandparentDir = Directory.GetParent(parentDir)?.FullName;
            if (grandparentDir != null && IsValidNanerRoot(grandparentDir))
            {
                return grandparentDir;
            }
        }

        // Try 5: Environment variable
        var envRoot = Environment.GetEnvironmentVariable("NANER_ROOT");
        if (!string.IsNullOrEmpty(envRoot) && IsValidNanerRoot(envRoot))
        {
            return envRoot;
        }

        // Try 6: User profile subdirectory
        var profileNaner = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".naner");
        if (IsValidNanerRoot(profileNaner))
        {
            return profileNaner;
        }

        // No valid root found
        return null;
    }

    /// <summary>
    /// Checks if a directory is a valid Naner root (contains required subdirectories).
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <returns>True if valid Naner root, false otherwise.</returns>
    private static bool IsValidNanerRoot(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return false;
        }

        // Check for core marker directories using centralized constants
        var coreDirectories = new[]
        {
            NanerConstants.DirectoryNames.Bin,
            NanerConstants.DirectoryNames.Vendor,
            NanerConstants.DirectoryNames.Config
        };

        return coreDirectories.All(dir =>
            Directory.Exists(Path.Combine(path, dir)));
    }
}
