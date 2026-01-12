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
    private const string GithubOwner = "baileyrd";
    private const string GithubRepo = "naner";
    private const string NanerExeName = "naner.exe";
    private const string NanerBundleName = "naner-bundle.zip";  // Bundle containing config, home, vendor folders
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
    /// Checks if Naner is initialized (has .naner-initialized marker file).
    /// </summary>
    public bool IsInitialized()
    {
        var markerFile = Path.Combine(_nanerRoot, ".naner-initialized");
        return File.Exists(markerFile);
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
            catch (Exception ex)
            {
                // Fall through to use file version as fallback
                Logger.Debug($"Could not read version file: {ex.Message}", debugMode: false);
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
            Logger.Warning("Could not check for updates (GitHub API unavailable)");
            return (false, null);
        }

        var latestVersion = VersionComparer.Normalize(latestRelease.TagName);
        var currentVersion = VersionComparer.Normalize(installedVersion);

        var updateAvailable = VersionComparer.IsNewer(latestVersion, currentVersion);
        return (updateAvailable, latestVersion);
    }

    /// <summary>
    /// Updates naner.exe to the latest version.
    /// </summary>
    public async Task<bool> UpdateNanerExeAsync()
    {
        try
        {
            Logger.Header("Updating Naner");
            Logger.NewLine();

            // Get latest release
            var latestRelease = await _githubClient.GetLatestReleaseAsync();
            if (latestRelease == null)
            {
                Logger.Failure("Failed to fetch latest release from GitHub");
                return false;
            }

            Logger.Info($"Latest version: {latestRelease.TagName}");
            Logger.NewLine();

            // Find naner.exe asset
            var nanerAsset = latestRelease.Assets?.FirstOrDefault(a =>
                a.Name != null && a.Name.Equals(NanerExeName, StringComparison.OrdinalIgnoreCase));

            if (nanerAsset == null)
            {
                Logger.Failure($"{NanerExeName} not found in release assets");
                return false;
            }

            var downloadUrl = nanerAsset.Url ?? nanerAsset.BrowserDownloadUrl;
            if (string.IsNullOrEmpty(downloadUrl))
            {
                Logger.Failure($"Download URL for {NanerExeName} is missing");
                return false;
            }

            // Download naner.exe
            var tempNanerPath = Path.Combine(_vendorBinDir, $"{NanerExeName}.tmp");
            var nanerPath = Path.Combine(_vendorBinDir, NanerExeName);

            if (!await _githubClient.DownloadAssetAsync(downloadUrl, tempNanerPath, NanerExeName))
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
                    Logger.Warning($"Could not delete old naner.exe: {ex.Message}");
                    Logger.Info("Will attempt to overwrite...");
                }
            }

            File.Move(tempNanerPath, nanerPath, overwrite: true);
            Logger.Success($"Installed {NanerExeName}");

            // Save version file
            var versionFile = Path.Combine(_vendorBinDir, VersionFileName);
            File.WriteAllText(versionFile, latestRelease.TagName);

            Logger.NewLine();
            Logger.Success($"Naner updated to version {latestRelease.TagName}");

            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Update failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Performs first-time initialization of Naner.
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        Logger.Header("Initializing Naner");
        Logger.NewLine();

        try
        {
            // Get latest release
            var latestRelease = await _githubClient.GetLatestReleaseAsync();
            if (latestRelease == null)
            {
                Logger.Failure("Failed to fetch latest release from GitHub");
                return false;
            }

            Logger.Info($"Latest version: {latestRelease.TagName}");
            Logger.NewLine();

            // Find naner-bundle.zip asset
            var bundleAsset = latestRelease.Assets?.FirstOrDefault(a =>
                a.Name != null && a.Name.Equals(NanerBundleName, StringComparison.OrdinalIgnoreCase));

            if (bundleAsset == null)
            {
                Logger.Failure($"{NanerBundleName} not found in release assets");
                return false;
            }

            var bundleDownloadUrl = bundleAsset.Url ?? bundleAsset.BrowserDownloadUrl;
            if (string.IsNullOrEmpty(bundleDownloadUrl))
            {
                Logger.Failure($"Download URL for {NanerBundleName} is missing");
                return false;
            }

            // Download bundle to temp file
            var tempBundlePath = Path.Combine(_nanerRoot, $"{NanerBundleName}.tmp");
            if (!await _githubClient.DownloadAssetAsync(bundleDownloadUrl, tempBundlePath, NanerBundleName))
            {
                return false;
            }

            // Extract bundle to naner root
            Logger.NewLine();
            Logger.Status("Extracting bundle...");
            try
            {
                ZipFile.ExtractToDirectory(tempBundlePath, _nanerRoot, overwriteFiles: true);
                Logger.Success("Bundle extracted");
            }
            catch (Exception ex)
            {
                Logger.Failure($"Failed to extract bundle: {ex.Message}");
                return false;
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempBundlePath))
                {
                    File.Delete(tempBundlePath);
                }
            }

            // Verify naner.exe was included in the bundle
            var nanerPath = Path.Combine(_vendorBinDir, NanerExeName);
            if (!File.Exists(nanerPath))
            {
                Logger.Failure($"{NanerExeName} not found in bundle (expected at vendor/bin/{NanerExeName})");
                return false;
            }
            Logger.Success($"Found {NanerExeName}");

            // Save version file
            var versionFile = Path.Combine(_vendorBinDir, VersionFileName);
            File.WriteAllText(versionFile, latestRelease.TagName);

            // Create initialization marker
            var markerFile = Path.Combine(_nanerRoot, ".naner-initialized");
            File.WriteAllText(markerFile, $"# Naner Initialization Marker\n# Created: {DateTime.Now}\n# Version: {latestRelease.TagName}\n");

            Logger.NewLine();
            Logger.Success($"Naner initialized successfully (version {latestRelease.TagName})");

            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Initialization failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Runs vendor setup by calling naner.exe setup-vendors.
    /// </summary>
    public int RunVendorSetup()
    {
        var nanerExePath = Path.Combine(_vendorBinDir, NanerExeName);

        if (!File.Exists(nanerExePath))
        {
            Logger.Failure($"Naner not found at: {nanerExePath}");
            return 1;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = nanerExePath,
                WorkingDirectory = _nanerRoot,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            startInfo.ArgumentList.Add("setup-vendors");

            using var process = Process.Start(startInfo);
            process?.WaitForExit();

            return process?.ExitCode ?? 0;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Failed to run vendor setup: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Launches naner.exe with the provided arguments.
    /// </summary>
    public int LaunchNaner(string[] args)
    {
        var nanerExePath = Path.Combine(_vendorBinDir, NanerExeName);

        if (!File.Exists(nanerExePath))
        {
            Logger.Failure($"Naner not found at: {nanerExePath}");
            Logger.Info("Run 'naner-init' to install Naner first");
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
            Logger.Failure($"Failed to launch Naner: {ex.Message}");
            return 1;
        }
    }
}
