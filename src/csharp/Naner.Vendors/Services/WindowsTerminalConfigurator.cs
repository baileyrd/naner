using System;
using System.IO;

namespace Naner.Vendors.Services;

/// <summary>
/// Handles Windows Terminal-specific configuration during vendor installation.
/// Extracted from VendorInstallerBase to follow Single Responsibility Principle.
/// </summary>
public class WindowsTerminalConfigurator
{
    private readonly string _nanerRoot;

    /// <summary>
    /// Creates a new Windows Terminal configurator.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory</param>
    public WindowsTerminalConfigurator(string nanerRoot)
    {
        _nanerRoot = nanerRoot ?? throw new ArgumentNullException(nameof(nanerRoot));
    }

    /// <summary>
    /// Configures Windows Terminal for portable mode after installation.
    /// Creates .portable marker file and settings.json with Naner profiles.
    /// </summary>
    /// <param name="targetDir">Windows Terminal installation directory</param>
    public void ConfigurePortableMode(string targetDir)
    {
        if (string.IsNullOrEmpty(targetDir))
            throw new ArgumentException("Target directory cannot be null or empty", nameof(targetDir));

        // Create .portable marker file
        var portableFile = Path.Combine(targetDir, ".portable");
        File.WriteAllText(portableFile, "");
        Logger.Info($"    Created .portable file for portable mode");

        // Create LocalState directory and settings.json with Naner profiles
        var localStateDir = Path.Combine(targetDir, "LocalState");
        Directory.CreateDirectory(localStateDir);

        var settingsFile = Path.Combine(localStateDir, "settings.json");
        CreateSettings(settingsFile);
        Logger.Info($"    Created LocalState/settings.json with Naner profiles");
    }

    /// <summary>
    /// Creates Windows Terminal settings.json with Naner profiles.
    /// Copies from template in home/.config/windows-terminal/settings.json and expands paths.
    /// </summary>
    /// <param name="settingsPath">Path to write settings.json</param>
    public void CreateSettings(string settingsPath)
    {
        if (string.IsNullOrEmpty(settingsPath))
            throw new ArgumentException("Settings path cannot be null or empty", nameof(settingsPath));

        // Try to copy from template
        var templatePath = Path.Combine(_nanerRoot, "home", ".config", "windows-terminal", "settings.json");

        if (File.Exists(templatePath))
        {
            // Read template and expand %NANER_ROOT% to actual path
            // Use PathUtilities for consistent path expansion
            var templateContent = File.ReadAllText(templatePath);
            var expandedContent = templateContent.Replace("%NANER_ROOT%", _nanerRoot.Replace("\\", "\\\\"));
            File.WriteAllText(settingsPath, expandedContent);
        }
        else
        {
            // Fallback: create basic settings inline
            File.WriteAllText(settingsPath, DefaultSettings);
        }
    }

    /// <summary>
    /// Checks if the vendor name indicates Windows Terminal.
    /// </summary>
    /// <param name="vendorName">Name of the vendor</param>
    /// <returns>True if vendor is Windows Terminal</returns>
    public static bool IsWindowsTerminal(string vendorName)
    {
        return !string.IsNullOrEmpty(vendorName) &&
               vendorName.Contains("Windows Terminal", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Default Windows Terminal settings when no template is available.
    /// </summary>
    private const string DefaultSettings = @"{
    ""$schema"": ""https://aka.ms/terminal-profiles-schema"",
    ""defaultProfile"": ""{naner-unified}"",
    ""copyOnSelect"": false,
    ""copyFormatting"": ""none"",
    ""profiles"": {
        ""defaults"": {},
        ""list"": [
            {
                ""guid"": ""{naner-unified}"",
                ""name"": ""Naner (Unified)"",
                ""commandline"": ""pwsh.exe"",
                ""startingDirectory"": ""%USERPROFILE%"",
                ""colorScheme"": ""Campbell""
            }
        ]
    },
    ""schemes"": [],
    ""actions"": []
}";
}
