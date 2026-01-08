using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Naner.Common;
using Naner.Configuration;

namespace Naner.Launcher;

/// <summary>
/// Handles launching Windows Terminal with configured profiles.
/// </summary>
public class TerminalLauncher
{
    private readonly string _nanerRoot;
    private readonly NanerConfig _config;
    private readonly bool _debugMode;

    /// <summary>
    /// Creates a new terminal launcher.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory</param>
    /// <param name="config">Loaded Naner configuration</param>
    /// <param name="debugMode">Enable debug output</param>
    public TerminalLauncher(string nanerRoot, NanerConfig config, bool debugMode = false)
    {
        _nanerRoot = nanerRoot ?? throw new ArgumentNullException(nameof(nanerRoot));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _debugMode = debugMode;
    }

    /// <summary>
    /// Launches Windows Terminal with the specified profile.
    /// </summary>
    /// <param name="profileName">Name of the profile to launch</param>
    /// <param name="startingDirectory">Optional starting directory override</param>
    /// <returns>Exit code from the terminal launch</returns>
    public int LaunchProfile(string profileName, string? startingDirectory = null)
    {
        // Get the profile configuration
        ProfileConfig profile;
        try
        {
            profile = GetProfile(profileName);
        }
        catch (InvalidOperationException ex)
        {
            Logger.Failure($"Profile not found: {profileName}");
            Logger.Info($"Available profiles: {string.Join(", ", _config.Profiles.Keys)}");
            if (_debugMode)
            {
                Logger.Debug(ex.ToString(), true);
            }
            return 1;
        }

        // Get Windows Terminal path
        var wtPath = GetWindowsTerminalPath();
        if (string.IsNullOrEmpty(wtPath))
        {
            Logger.Failure("Windows Terminal not found");
            Logger.Info("Please install Windows Terminal or configure vendor path");
            return 1;
        }

        // Build command-line arguments
        var arguments = BuildTerminalArguments(profile, startingDirectory);

        if (_debugMode)
        {
            Logger.Debug($"Terminal Path: {wtPath}", true);
            Logger.Debug($"Arguments: {arguments}", true);
            Logger.Debug($"Profile: {profile.Name}", true);
            Logger.Debug($"Shell: {profile.Shell}", true);
        }

        // Setup PATH environment variable
        SetupPathEnvironment();

        // Launch Windows Terminal
        try
        {
            Logger.Status($"Launching {profile.Name}...");

            var startInfo = new ProcessStartInfo
            {
                FileName = wtPath,
                Arguments = arguments,
                UseShellExecute = false,
                WorkingDirectory = _nanerRoot
            };

            var process = Process.Start(startInfo);

            if (process == null)
            {
                Logger.Failure("Failed to start Windows Terminal");
                return 1;
            }

            Logger.Success($"Launched: {profile.Name}");
            return 0;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Failed to launch terminal: {ex.Message}");
            if (_debugMode)
            {
                Logger.Debug(ex.ToString(), true);
            }
            return 1;
        }
    }

    /// <summary>
    /// Gets a profile by name from configuration.
    /// </summary>
    private ProfileConfig GetProfile(string profileName)
    {
        // Check standard profiles
        if (_config.Profiles.TryGetValue(profileName, out var profile))
        {
            return profile;
        }

        // Check custom profiles
        if (_config.CustomProfiles.TryGetValue(profileName, out var customProfile))
        {
            return customProfile;
        }

        // Try default profile if specified profile not found
        if (!string.IsNullOrEmpty(_config.DefaultProfile) &&
            _config.Profiles.TryGetValue(_config.DefaultProfile, out var defaultProfile))
        {
            Logger.Warning($"Profile '{profileName}' not found, using default: {_config.DefaultProfile}");
            return defaultProfile;
        }

        throw new InvalidOperationException($"Profile '{profileName}' not found");
    }

    /// <summary>
    /// Gets the path to Windows Terminal executable.
    /// </summary>
    private string? GetWindowsTerminalPath()
    {
        // Check vendor path first
        if (_config.VendorPaths.TryGetValue("WindowsTerminal", out var vendorPath) &&
            File.Exists(vendorPath))
        {
            return vendorPath;
        }

        // Check if wt.exe is in PATH
        var wtInPath = FindExecutableInPath("wt.exe");
        if (!string.IsNullOrEmpty(wtInPath))
        {
            return wtInPath;
        }

        // Try default Windows installation location
        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Microsoft\WindowsApps\wt.exe");

        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        return null;
    }

    /// <summary>
    /// Builds Windows Terminal command-line arguments.
    /// </summary>
    private string BuildTerminalArguments(ProfileConfig profile, string? startingDirectoryOverride)
    {
        var args = new StringBuilder();

        // Add launch mode if specified
        if (!string.IsNullOrEmpty(_config.WindowsTerminal.LaunchMode) &&
            _config.WindowsTerminal.LaunchMode != "default")
        {
            args.Append($"--{_config.WindowsTerminal.LaunchMode} ");
        }

        // Start new tab with profile
        args.Append("new-tab ");

        // Add profile name
        if (!string.IsNullOrEmpty(profile.Name))
        {
            args.Append($"--title \"{profile.Name}\" ");
        }

        // Add starting directory
        var startDir = startingDirectoryOverride ?? profile.StartingDirectory;
        if (!string.IsNullOrEmpty(startDir))
        {
            var expandedDir = PathUtilities.ExpandNanerPath(startDir, _nanerRoot);
            expandedDir = Environment.ExpandEnvironmentVariables(expandedDir);
            args.Append($"--startingDirectory \"{expandedDir}\" ");
        }

        // Add custom shell if specified
        if (profile.CustomShell != null && !string.IsNullOrEmpty(profile.CustomShell.ExecutablePath))
        {
            var shellPath = PathUtilities.ExpandNanerPath(profile.CustomShell.ExecutablePath, _nanerRoot);

            if (!string.IsNullOrEmpty(profile.CustomShell.Arguments))
            {
                args.Append($"-- \"{shellPath}\" {profile.CustomShell.Arguments}");
            }
            else
            {
                args.Append($"-- \"{shellPath}\"");
            }
        }
        else
        {
            // Use default shell based on profile type
            var shellPath = GetDefaultShellPath(profile.Shell);
            if (!string.IsNullOrEmpty(shellPath))
            {
                args.Append($"-- \"{shellPath}\"");
            }
        }

        return args.ToString().Trim();
    }

    /// <summary>
    /// Gets the default shell path for a shell type.
    /// </summary>
    private string? GetDefaultShellPath(string shellType)
    {
        return shellType.ToLowerInvariant() switch
        {
            "powershell" => _config.VendorPaths.GetValueOrDefault("PowerShell") ?? "pwsh.exe",
            "bash" => _config.VendorPaths.GetValueOrDefault("GitBash") ?? "bash.exe",
            "cmd" => "cmd.exe",
            _ => null
        };
    }

    /// <summary>
    /// Sets up the PATH environment variable from configuration.
    /// </summary>
    private void SetupPathEnvironment()
    {
        var configManager = new ConfigurationManager(_nanerRoot);
        var unifiedPath = configManager.BuildUnifiedPath(_config.Advanced.InheritSystemPath);

        Environment.SetEnvironmentVariable("PATH", unifiedPath, EnvironmentVariableTarget.Process);

        if (_debugMode)
        {
            Logger.Debug($"PATH set to: {unifiedPath.Substring(0, Math.Min(200, unifiedPath.Length))}...", true);
        }
    }

    /// <summary>
    /// Finds an executable in the PATH environment variable.
    /// </summary>
    private static string? FindExecutableInPath(string executableName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var paths = path.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var dir in paths)
        {
            try
            {
                var fullPath = Path.Combine(dir.Trim(), executableName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            catch
            {
                // Ignore invalid paths
            }
        }

        return null;
    }
}
