using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

namespace Naner.Common;

/// <summary>
/// Manages Naner installation setup and initialization.
/// </summary>
public static class SetupManager
{
    /// <summary>
    /// Creates the Naner directory structure.
    /// </summary>
    /// <param name="nanerRoot">Root directory path.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool CreateDirectoryStructure(string nanerRoot)
    {
        Logger.Status($"Creating Naner directory structure at: {nanerRoot}");
        Logger.NewLine();

        try
        {
            // Create main directory
            if (!Directory.Exists(nanerRoot))
            {
                Directory.CreateDirectory(nanerRoot);
                Logger.Success($"Created: {nanerRoot}");
            }

            // Create subdirectories
            var directories = new[]
            {
                "bin",
                "vendor",
                "vendor/bin",
                "config",
                "home",
                "home/.ssh",
                "home/.config",
                "home/.config/git",
                "home/.config/powershell",
                "home/.config/windows-terminal",
                "home/.vscode",
                "home/Documents/PowerShell",
                "home/Documents/PowerShell/Scripts",
                "home/Templates",
                "plugins",
                "logs"
            };

            foreach (var dir in directories)
            {
                var path = Path.Combine(nanerRoot, dir);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ Created: {dir}/");
                    Console.ResetColor();
                }
            }

            Logger.NewLine();
            Logger.Success("Directory structure created successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Failed to create directory structure: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates a default naner.json configuration file.
    /// </summary>
    /// <param name="nanerRoot">Root directory path.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool CreateDefaultConfiguration(string nanerRoot)
    {
        Logger.Status("Creating default configuration...");
        Logger.NewLine();

        var configPath = Path.Combine(nanerRoot, "config", "naner.json");

        // Write JSON manually to avoid trim issues with Dictionary<string, object>
        var defaultConfigJson = @"{
  ""VendorPaths"": {
    ""SevenZip"": ""%NANER_ROOT%\\vendor\\7-Zip\\7z.exe"",
    ""PowerShell"": ""%NANER_ROOT%\\vendor\\PowerShell\\pwsh.exe"",
    ""WindowsTerminal"": ""%NANER_ROOT%\\vendor\\terminal\\wt.exe"",
    ""GitBash"": ""%NANER_ROOT%\\vendor\\msys64\\usr\\bin\\bash.exe""
  },
  ""Environment"": {
    ""PathPrecedence"": [
      ""%NANER_ROOT%\\bin"",
      ""%NANER_ROOT%\\vendor\\bin"",
      ""%NANER_ROOT%\\vendor\\PowerShell"",
      ""%NANER_ROOT%\\vendor\\msys64\\usr\\bin"",
      ""%NANER_ROOT%\\vendor\\msys64\\mingw64\\bin""
    ],
    ""EnvironmentVariables"": {
      ""NANER_ROOT"": ""%NANER_ROOT%"",
      ""NANER_HOME"": ""%NANER_ROOT%\\home"",
      ""HOME"": ""%NANER_ROOT%\\home""
    }
  },
  ""DefaultProfile"": ""Unified"",
  ""Profiles"": {
    ""Unified"": {
      ""Name"": ""Naner (Unified)"",
      ""Description"": ""All tools available - PowerShell, Git, and Unix utilities"",
      ""Shell"": ""PowerShell"",
      ""StartingDirectory"": ""%USERPROFILE%"",
      ""UseVendorPath"": true,
      ""ColorScheme"": ""Campbell""
    },
    ""PowerShell"": {
      ""Name"": ""PowerShell"",
      ""Description"": ""PowerShell with Naner tools"",
      ""Shell"": ""PowerShell"",
      ""StartingDirectory"": ""%USERPROFILE%"",
      ""UseVendorPath"": true,
      ""ColorScheme"": ""Campbell""
    },
    ""Bash"": {
      ""Name"": ""Git Bash"",
      ""Description"": ""Unix-like Bash environment"",
      ""Shell"": ""Bash"",
      ""StartingDirectory"": ""%USERPROFILE%"",
      ""UseVendorPath"": true,
      ""ColorScheme"": ""One Half Dark""
    },
    ""CMD"": {
      ""Name"": ""Command Prompt"",
      ""Description"": ""Windows Command Prompt with Naner tools"",
      ""Shell"": ""CMD"",
      ""StartingDirectory"": ""%USERPROFILE%"",
      ""UseVendorPath"": true,
      ""ColorScheme"": ""Campbell""
    }
  },
  ""WindowsTerminal"": {
    ""DefaultTerminal"": true,
    ""LaunchMode"": ""default"",
    ""TabTitle"": ""Naner"",
    ""SuppressApplicationTitle"": true
  },
  ""Advanced"": {
    ""PreservePath"": false,
    ""InheritSystemPath"": true,
    ""VerboseLogging"": false,
    ""DebugMode"": false
  },
  ""CustomProfiles"": {}
}";

        try
        {
            File.WriteAllText(configPath, defaultConfigJson);

            Logger.Success($"Created: config/naner.json");
            Logger.NewLine();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Failed to create configuration: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Prompts user for installation location choice.
    /// </summary>
    /// <returns>Selected installation path.</returns>
    public static string PromptInstallLocation()
    {
        Console.WriteLine("Where would you like to install Naner?");
        Console.WriteLine();
        Console.WriteLine("  1. Current directory (recommended if you have write access)");
        Console.WriteLine($"     {Environment.CurrentDirectory}");
        Console.WriteLine();
        Console.WriteLine("  2. User profile (~/.naner)");
        Console.WriteLine($"     {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".naner")}");
        Console.WriteLine();
        Console.WriteLine("  3. Custom location (you specify)");
        Console.WriteLine();
        Console.Write("Enter choice (1-3) [1]: ");

        var choice = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(choice))
        {
            choice = "1";
        }

        return choice switch
        {
            "1" => Environment.CurrentDirectory,
            "2" => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".naner"),
            "3" => PromptCustomLocation(),
            _ => Environment.CurrentDirectory
        };
    }

    /// <summary>
    /// Prompts user for custom installation location.
    /// </summary>
    /// <returns>Custom path entered by user.</returns>
    private static string PromptCustomLocation()
    {
        Console.Write("Enter installation path: ");
        var path = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(path))
        {
            Logger.Warning("No path entered, using current directory");
            return Environment.CurrentDirectory;
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            return fullPath;
        }
        catch
        {
            Logger.Warning("Invalid path, using current directory");
            return Environment.CurrentDirectory;
        }
    }

    /// <summary>
    /// Shows the welcome screen for first-run setup.
    /// </summary>
    public static void ShowWelcome()
    {
        Console.Clear();
        Logger.Header("Welcome to Naner Terminal Launcher!");
        Console.WriteLine();
        Console.WriteLine("Naner is a portable terminal environment for Windows.");
        Console.WriteLine("It provides:");
        Console.WriteLine("  • Portable PowerShell, Bash, and CMD environments");
        Console.WriteLine("  • Unified tool management (Git, Node, Python, etc.)");
        Console.WriteLine("  • Consistent configuration across machines");
        Console.WriteLine();
        Console.WriteLine("This appears to be your first time running Naner.");
        Console.WriteLine("Let's set up your environment!");
        Console.WriteLine();
    }

    /// <summary>
    /// Runs the complete interactive setup including vendor downloads.
    /// </summary>
    /// <param name="nanerRoot">The Naner root directory.</param>
    /// <param name="skipVendors">Whether to skip vendor downloads.</param>
    /// <returns>True if setup completed successfully.</returns>
    public static async System.Threading.Tasks.Task<bool> RunInteractiveSetupAsync(string nanerRoot, bool skipVendors = false)
    {
        ShowWelcome();

        // Create directory structure
        if (!CreateDirectoryStructure(nanerRoot))
        {
            return false;
        }

        Logger.NewLine();

        // Create configuration
        if (!CreateDefaultConfiguration(nanerRoot))
        {
            return false;
        }

        Logger.NewLine();

        // Download vendors if not skipped
        if (!skipVendors)
        {
            Logger.Status("Setting up vendor dependencies...");
            Logger.Info("This will download PowerShell, Windows Terminal, Git, and other tools.");
            Logger.NewLine();

            Console.Write("Download vendor dependencies now? [Y/n]: ");
            var response = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(response) || response == "y" || response == "yes")
            {
                var downloader = new VendorDownloader(nanerRoot);
                await downloader.SetupRequiredVendorsAsync();
            }
            else
            {
                Logger.Info("Skipping vendor downloads.");
                Logger.Info("You can download vendors later with: naner setup-vendors");
            }

            Logger.NewLine();
        }

        // Create initialization marker
        FirstRunDetector.CreateInitializationMarker(nanerRoot, "1.0.0", "Production Release");

        Logger.NewLine();
        Logger.Success("Naner setup complete!");
        Logger.Info($"Installation location: {nanerRoot}");
        Logger.NewLine();
        Logger.Info("Next steps:");
        Logger.Info("  1. Run 'naner --diagnose' to verify your setup");
        Logger.Info("  2. Run 'naner' to launch your default terminal");
        Logger.Info("  3. See 'naner --help' for more commands");
        Logger.NewLine();

        return true;
    }
}
