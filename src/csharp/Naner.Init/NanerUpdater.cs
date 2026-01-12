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
    private readonly string _initVersion;

    public NanerUpdater(string nanerRoot)
    {
        _nanerRoot = nanerRoot;
        _vendorBinDir = Path.Combine(_nanerRoot, "vendor", "bin");
        _configDir = Path.Combine(_nanerRoot, "config");
        _githubClient = new GitHubReleasesClient(GithubOwner, GithubRepo);
        _initVersion = GetNanerInitVersion();
    }

    /// <summary>
    /// Gets the version of naner-init.exe from its assembly information.
    /// </summary>
    private static string GetNanerInitVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Try InformationalVersion first (includes full semver)
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(infoVersion))
        {
            // Remove any +metadata suffix (e.g., "0.4.0+abc123" -> "0.4.0")
            var plusIndex = infoVersion.IndexOf('+');
            return plusIndex >= 0 ? infoVersion.Substring(0, plusIndex) : infoVersion;
        }

        // Fall back to FileVersion
        var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        if (!string.IsNullOrEmpty(fileVersion))
        {
            // FileVersion is typically "0.4.0.0", trim the last .0 if present
            var parts = fileVersion.Split('.');
            if (parts.Length == 4 && parts[3] == "0")
            {
                return string.Join(".", parts.Take(3));
            }
            return fileVersion;
        }

        // Last resort: assembly version
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
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
    /// Gets the version that naner-init will download (its own version).
    /// </summary>
    public string GetTargetVersion() => _initVersion;

    /// <summary>
    /// Checks if the installed naner.exe version differs from naner-init's version.
    /// Returns true if an update/reinstall is needed to match naner-init's version.
    /// </summary>
    public async Task<(bool UpdateAvailable, string? LatestVersion)> CheckForUpdateAsync()
    {
        var installedVersion = GetInstalledVersion();
        if (installedVersion == null)
        {
            // Not initialized, need to install
            return (true, _initVersion);
        }

        // Check if installed version matches naner-init's version
        var normalizedInstalled = VersionComparer.Normalize(installedVersion);
        var normalizedTarget = VersionComparer.Normalize(_initVersion);

        // Update available if versions don't match
        var updateNeeded = normalizedInstalled != normalizedTarget;
        return (updateNeeded, updateNeeded ? _initVersion : null);
    }

    /// <summary>
    /// Updates naner.exe to match naner-init's version.
    /// </summary>
    public async Task<bool> UpdateNanerExeAsync()
    {
        try
        {
            Logger.Header("Updating Naner");
            Logger.NewLine();

            // Get release matching naner-init's version
            Logger.Info($"Fetching release v{_initVersion}...");
            var targetRelease = await _githubClient.GetReleaseByTagAsync(_initVersion);
            if (targetRelease == null)
            {
                Logger.Failure($"Failed to fetch release v{_initVersion} from GitHub");
                return false;
            }

            Logger.Info($"Target version: {targetRelease.TagName}");
            Logger.NewLine();

            // Find naner.exe asset
            var nanerAsset = targetRelease.Assets?.FirstOrDefault(a =>
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
            File.WriteAllText(versionFile, targetRelease.TagName);

            Logger.NewLine();
            Logger.Success($"Naner updated to version {targetRelease.TagName}");

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
    /// Downloads the release matching naner-init's version.
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        Logger.Header("Initializing Naner");
        Logger.NewLine();

        try
        {
            // Get release matching naner-init's version
            Logger.Info($"Fetching release v{_initVersion}...");
            var targetRelease = await _githubClient.GetReleaseByTagAsync(_initVersion);
            if (targetRelease == null)
            {
                Logger.Failure($"Failed to fetch release v{_initVersion} from GitHub");
                return false;
            }

            Logger.Info($"Target version: {targetRelease.TagName}");
            Logger.NewLine();

            // Find naner-bundle.zip asset
            var bundleAsset = targetRelease.Assets?.FirstOrDefault(a =>
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
            File.WriteAllText(versionFile, targetRelease.TagName);

            // Create initialization marker
            var markerFile = Path.Combine(_nanerRoot, ".naner-initialized");
            File.WriteAllText(markerFile, $"# Naner Initialization Marker\n# Created: {DateTime.Now}\n# Version: {targetRelease.TagName}\n");

            Logger.NewLine();
            Logger.Success($"Naner initialized successfully (version {targetRelease.TagName})");

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
