using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Naner.Init;

/// <summary>
/// Handles downloading, updating, and initializing Naner.
/// </summary>
public class NanerUpdater
{
    private const string GithubOwner = "baile320"; // TODO: Update with your actual GitHub username
    private const string GithubRepo = "naner";
    private const string NanerExeName = "naner.exe";
    private const string NanerConfigName = "naner.json";
    private const string VersionFileName = ".naner-version";

    private readonly string _nanerRoot;
    private readonly string _vendorBinDir;
    private readonly string _configDir;
    private readonly GitHubReleasesClient _githubClient;

    public NanerUpdater(string nanerRoot)
    {
        _nanerRoot = nanerRoot;
        _vendorBinDir = Path.Combine(_nanerRoot, "vendor", "bin");
        _configDir = Path.Combine(_nanerRoot, "config");
        _githubClient = new GitHubReleasesClient(GithubOwner, GithubRepo);
    }

    /// <summary>
    /// Checks if Naner is initialized (has naner.exe).
    /// </summary>
    public bool IsInitialized()
    {
        var nanerExePath = Path.Combine(_vendorBinDir, NanerExeName);
        return File.Exists(nanerExePath);
    }

    /// <summary>
    /// Gets the currently installed version of Naner.
    /// </summary>
    public string? GetInstalledVersion()
    {
        var versionFile = Path.Combine(_vendorBinDir, VersionFileName);
        if (File.Exists(versionFile))
        {
            try
            {
                return File.ReadAllText(versionFile).Trim();
            }
            catch
            {
                // Fall through to use file version
            }
        }

        // Fallback: try to get version from naner.exe itself
        var nanerExePath = Path.Combine(_vendorBinDir, NanerExeName);
        if (File.Exists(nanerExePath))
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(nanerExePath);
                return versionInfo.FileVersion ?? "0.0.0";
            }
            catch
            {
                return "0.0.0";
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if an update is available.
    /// </summary>
    public async Task<(bool UpdateAvailable, string? LatestVersion)> CheckForUpdateAsync()
    {
        var installedVersion = GetInstalledVersion();
        if (installedVersion == null)
        {
            // Not initialized, need to install
            return (true, null);
        }

        var latestRelease = await _githubClient.GetLatestReleaseAsync();
        if (latestRelease?.TagName == null)
        {
            ConsoleHelper.Warning("Could not check for updates (GitHub API unavailable)");
            return (false, null);
        }

        var latestVersion = VersionComparer.Normalize(latestRelease.TagName);
        var currentVersion = VersionComparer.Normalize(installedVersion);

        var updateAvailable = VersionComparer.IsNewer(latestVersion, currentVersion);
        return (updateAvailable, latestVersion);
    }

    /// <summary>
    /// Downloads and installs the latest version of Naner.
    /// </summary>
    public async Task<bool> InstallOrUpdateNanerAsync(bool isUpdate = false)
    {
        try
        {
            ConsoleHelper.Header(isUpdate ? "Updating Naner" : "Installing Naner");
            ConsoleHelper.NewLine();

            // Get latest release
            var latestRelease = await _githubClient.GetLatestReleaseAsync();
            if (latestRelease == null)
            {
                ConsoleHelper.Error("Failed to fetch latest release from GitHub");
                return false;
            }

            ConsoleHelper.Info($"Latest version: {latestRelease.TagName}");
            ConsoleHelper.NewLine();

            // Find naner.exe asset
            var nanerAsset = latestRelease.Assets?.FirstOrDefault(a =>
                a.Name != null && a.Name.Equals(NanerExeName, StringComparison.OrdinalIgnoreCase));

            if (nanerAsset == null)
            {
                ConsoleHelper.Error($"{NanerExeName} not found in release assets");
                return false;
            }

            // Create directories
            Directory.CreateDirectory(_vendorBinDir);
            Directory.CreateDirectory(_configDir);

            // Download naner.exe
            var tempNanerPath = Path.Combine(_vendorBinDir, $"{NanerExeName}.tmp");
            var nanerPath = Path.Combine(_vendorBinDir, NanerExeName);

            if (!await _githubClient.DownloadAssetAsync(
                nanerAsset.BrowserDownloadUrl!,
                tempNanerPath,
                NanerExeName))
            {
                return false;
            }

            // Replace existing naner.exe
            if (File.Exists(nanerPath))
            {
                try
                {
                    File.Delete(nanerPath);
                }
                catch (Exception ex)
                {
                    ConsoleHelper.Warning($"Could not delete old naner.exe: {ex.Message}");
                    ConsoleHelper.Info("Will attempt to overwrite...");
                }
            }

            File.Move(tempNanerPath, nanerPath, overwrite: true);
            ConsoleHelper.Success($"Installed {NanerExeName}");

            // Download naner.json config template (if available and not exists)
            var configPath = Path.Combine(_configDir, NanerConfigName);
            if (!File.Exists(configPath))
            {
                var configAsset = latestRelease.Assets?.FirstOrDefault(a =>
                    a.Name != null && a.Name.Equals(NanerConfigName, StringComparison.OrdinalIgnoreCase));

                if (configAsset != null)
                {
                    ConsoleHelper.NewLine();
                    await _githubClient.DownloadAssetAsync(
                        configAsset.BrowserDownloadUrl!,
                        configPath,
                        NanerConfigName);
                }
            }

            // Save version file
            var versionFile = Path.Combine(_vendorBinDir, VersionFileName);
            File.WriteAllText(versionFile, latestRelease.TagName);

            ConsoleHelper.NewLine();
            ConsoleHelper.Success(isUpdate
                ? $"Naner updated to version {latestRelease.TagName}"
                : $"Naner installed successfully (version {latestRelease.TagName})");

            return true;
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error($"Installation failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Performs first-time initialization of Naner.
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        ConsoleHelper.Header("Initializing Naner");
        ConsoleHelper.NewLine();

        // Create base directory structure
        ConsoleHelper.Status("Creating directory structure...");
        try
        {
            Directory.CreateDirectory(Path.Combine(_nanerRoot, "bin"));
            Directory.CreateDirectory(Path.Combine(_nanerRoot, "vendor"));
            Directory.CreateDirectory(Path.Combine(_nanerRoot, "vendor", "bin"));
            Directory.CreateDirectory(Path.Combine(_nanerRoot, "config"));
            Directory.CreateDirectory(Path.Combine(_nanerRoot, "home"));
            Directory.CreateDirectory(Path.Combine(_nanerRoot, "plugins"));
            Directory.CreateDirectory(Path.Combine(_nanerRoot, "logs"));

            // Create home subdirectories
            var homeDir = Path.Combine(_nanerRoot, "home");
            Directory.CreateDirectory(Path.Combine(homeDir, ".ssh"));
            Directory.CreateDirectory(Path.Combine(homeDir, ".config"));
            Directory.CreateDirectory(Path.Combine(homeDir, ".config", "git"));
            Directory.CreateDirectory(Path.Combine(homeDir, ".config", "powershell"));
            Directory.CreateDirectory(Path.Combine(homeDir, ".vscode"));

            ConsoleHelper.Success("Directory structure created");
            ConsoleHelper.NewLine();
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error($"Failed to create directories: {ex.Message}");
            return false;
        }

        // Download and install naner.exe and config
        if (!await InstallOrUpdateNanerAsync(isUpdate: false))
        {
            return false;
        }

        // Create initialization marker
        var markerFile = Path.Combine(_nanerRoot, ".naner-initialized");
        File.WriteAllText(markerFile, $"# Naner Initialization Marker\n# Created: {DateTime.Now}\n# Version: {GetInstalledVersion()}\n");

        ConsoleHelper.NewLine();
        ConsoleHelper.Success("Naner initialization completed!");
        ConsoleHelper.Info("Run 'naner-init' again to launch Naner");

        return true;
    }

    /// <summary>
    /// Launches naner.exe with the provided arguments.
    /// </summary>
    public int LaunchNaner(string[] args)
    {
        var nanerExePath = Path.Combine(_vendorBinDir, NanerExeName);

        if (!File.Exists(nanerExePath))
        {
            ConsoleHelper.Error($"Naner not found at: {nanerExePath}");
            ConsoleHelper.Info("Run 'naner-init' to install Naner first");
            return 1;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = nanerExePath,
                WorkingDirectory = _nanerRoot,
                UseShellExecute = false
            };

            // Pass through all arguments
            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = Process.Start(startInfo);
            process?.WaitForExit();

            return process?.ExitCode ?? 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error($"Failed to launch Naner: {ex.Message}");
            return 1;
        }
    }
}
