using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Naner.Vendors.Abstractions;
using Naner.Archives.Abstractions;
using Naner.Infrastructure.Abstractions;
using Naner.Vendors.Models;

namespace Naner.Vendors.Services;

/// <summary>
/// Unified vendor installer that handles both static URLs and dynamic fetching (GitHub/WebScrape).
/// Consolidates VendorDownloader and DynamicVendorDownloader into a single implementation.
/// </summary>
public class UnifiedVendorInstaller : VendorInstallerBase
{
    private readonly Dictionary<string, VendorDefinition> _vendorDefinitions;

    public UnifiedVendorInstaller(
        string nanerRoot,
        IEnumerable<VendorDefinition> vendorDefinitions,
        IHttpClientWrapper? httpClient = null)
        : base(nanerRoot, httpClient)
    {
        _vendorDefinitions = vendorDefinitions.ToDictionary(v => v.Name, v => v, StringComparer.OrdinalIgnoreCase);
    }

    public override async Task<bool> InstallVendorAsync(string vendorName)
    {
        return await InstallVendorAsync(vendorName, skipIfExists: true);
    }

    /// <summary>
    /// Installs a vendor with optional skip behavior.
    /// </summary>
    /// <param name="vendorName">Name of vendor to install</param>
    /// <param name="skipIfExists">If true, skip installation when vendor already exists</param>
    private async Task<bool> InstallVendorAsync(string vendorName, bool skipIfExists)
    {
        if (!_vendorDefinitions.TryGetValue(vendorName, out var vendor))
        {
            Logger.Failure($"Unknown vendor: {vendorName}");
            return false;
        }

        var targetDir = Path.Combine(VendorDir, vendor.ExtractDir);

        // Skip if already installed (when skipIfExists is true)
        if (skipIfExists && Directory.Exists(targetDir) && Directory.GetFileSystemEntries(targetDir).Length > 0)
        {
            Logger.Info($"Skipping {vendor.Name} (already installed)");
            return true;
        }

        Logger.Status($"Fetching latest {vendor.Name}...");

        try
        {
            // Fetch download URL based on source type
            var downloadInfo = await FetchVendorDownloadInfoAsync(vendor);
            if (downloadInfo == null)
            {
                Logger.Warning($"Failed to fetch {vendor.Name}, skipping...");
                return false;
            }

            Logger.Info($"  Latest version: {downloadInfo.Version ?? "Unknown"}");
            Logger.Status($"  Downloading {downloadInfo.FileName}...");

            EnsureDownloadDirectoryExists();
            var downloadPath = Path.Combine(DownloadDir, downloadInfo.FileName);

            // Download file
            if (!await DownloadFileAsync(downloadInfo.Url, downloadPath))
            {
                Logger.Warning($"Failed to download {vendor.Name}, skipping...");
                return false;
            }

            Logger.Success($"  Downloaded {downloadInfo.FileName}");

            // Verify checksum if provided
            if (!VerifyChecksum(downloadPath, vendor.Checksum))
            {
                Logger.Failure($"  Checksum verification failed for {vendor.Name}");
                return false;
            }

            // Extract file or run installer (overwrites existing files)
            Logger.Status($"  Installing {vendor.Name}...");

            if (!ExtractArchive(downloadPath, targetDir, vendor.Name, vendor.InstallerArgs))
            {
                Logger.Warning($"Failed to install {vendor.Name}, skipping...");
                return false;
            }

            // Post-install configuration
            PostInstallConfiguration(vendor.Name, targetDir);

            // Save vendor version file
            SaveVendorVersion(targetDir, downloadInfo.Version);

            Logger.Success($"  Installed {vendor.Name}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning($"Error setting up {vendor.Name}: {ex.Message}");
            return false;
        }
    }

    public override string? GetVendorPath(string vendorName)
    {
        if (!_vendorDefinitions.TryGetValue(vendorName, out var vendor))
        {
            return null;
        }

        var path = Path.Combine(VendorDir, vendor.ExtractDir);
        return Directory.Exists(path) ? path : null;
    }

    /// <summary>
    /// Fetches download information for a vendor based on its source type.
    /// </summary>
    private async Task<VendorDownloadInfo?> FetchVendorDownloadInfoAsync(VendorDefinition vendor)
    {
        try
        {
            var result = vendor.SourceType switch
            {
                VendorSourceType.StaticUrl => FetchFromStaticUrl(vendor),
                VendorSourceType.GitHub => await FetchFromGitHubAsync(vendor),
                VendorSourceType.WebScrape => await FetchFromWebScrapeAsync(vendor),
                VendorSourceType.NodeJsApi => await FetchFromNodeJsApiAsync(),
                _ => null
            };

            // If dynamic fetch returned null, try fallback
            if (result == null && !string.IsNullOrEmpty(vendor.FallbackUrl))
            {
                Logger.Info($"  No matching release found, using fallback URL");
                return new VendorDownloadInfo
                {
                    Url = vendor.FallbackUrl,
                    FileName = vendor.FallbackFileName ?? Path.GetFileName(vendor.FallbackUrl),
                    Version = vendor.FallbackVersion ?? "fallback"
                };
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.Warning($"  Failed to fetch dynamically: {ex.Message}");
            Logger.Info($"  Using fallback URL");

            if (!string.IsNullOrEmpty(vendor.FallbackUrl))
            {
                return new VendorDownloadInfo
                {
                    Url = vendor.FallbackUrl,
                    FileName = vendor.FallbackFileName ?? Path.GetFileName(vendor.FallbackUrl),
                    Version = vendor.FallbackVersion ?? "fallback"
                };
            }

            return null;
        }
    }

    /// <summary>
    /// Fetches download info from a static URL.
    /// </summary>
    private VendorDownloadInfo? FetchFromStaticUrl(VendorDefinition vendor)
    {
        if (string.IsNullOrEmpty(vendor.StaticUrl) || string.IsNullOrEmpty(vendor.FileName))
        {
            return null;
        }

        return new VendorDownloadInfo
        {
            Url = vendor.StaticUrl,
            FileName = vendor.FileName,
            Version = ExtractVersionFromFileName(vendor.FileName)
        };
    }

    /// <summary>
    /// Fetches download info from GitHub releases API.
    /// </summary>
    private async Task<VendorDownloadInfo?> FetchFromGitHubAsync(VendorDefinition vendor)
    {
        if (string.IsNullOrEmpty(vendor.GitHubOwner) || string.IsNullOrEmpty(vendor.GitHubRepo))
        {
            return null;
        }

        var url = $"https://api.github.com/repos/{vendor.GitHubOwner}/{vendor.GitHubRepo}/releases/latest";
        var response = await HttpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (release?.Assets == null)
        {
            return null;
        }

        // Find asset matching pattern
        var asset = release.Assets.FirstOrDefault(a =>
        {
            if (a.Name == null) return false;

            var matchesStart = string.IsNullOrEmpty(vendor.AssetPattern) ||
                               a.Name.Contains(vendor.AssetPattern, StringComparison.OrdinalIgnoreCase);

            var matchesEnd = string.IsNullOrEmpty(vendor.AssetPatternEnd) ||
                            a.Name.Contains(vendor.AssetPatternEnd, StringComparison.OrdinalIgnoreCase);

            return matchesStart && matchesEnd;
        });

        if (asset?.BrowserDownloadUrl == null)
        {
            return null;
        }

        return new VendorDownloadInfo
        {
            Url = asset.BrowserDownloadUrl,
            FileName = asset.Name ?? Path.GetFileName(asset.BrowserDownloadUrl),
            Version = release.TagName
        };
    }

    /// <summary>
    /// Fetches download info by scraping a web page.
    /// </summary>
    private async Task<VendorDownloadInfo?> FetchFromWebScrapeAsync(VendorDefinition vendor)
    {
        if (vendor.WebScrapeConfig == null)
        {
            return null;
        }

        var response = await HttpClient.GetAsync(vendor.WebScrapeConfig.Url);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var html = await response.Content.ReadAsStringAsync();
        var regex = new Regex(vendor.WebScrapeConfig.Pattern, RegexOptions.IgnoreCase);
        var match = regex.Match(html);

        if (!match.Success)
        {
            return null;
        }

        var relativeUrl = match.Groups[1].Value;
        var fullUrl = vendor.WebScrapeConfig.BaseUrl.TrimEnd('/') + "/" + relativeUrl.TrimStart('/');
        var fileName = Path.GetFileName(relativeUrl);

        return new VendorDownloadInfo
        {
            Url = fullUrl,
            FileName = fileName,
            Version = ExtractVersionFromFileName(fileName)
        };
    }

    /// <summary>
    /// Fetches download info from Node.js distribution API.
    /// </summary>
    private async Task<VendorDownloadInfo?> FetchFromNodeJsApiAsync()
    {
        var response = await HttpClient.GetAsync("https://nodejs.org/dist/index.json");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var releases = JsonSerializer.Deserialize<List<NodeJsRelease>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Get latest release that has win-x64-zip available
        var latest = releases?.FirstOrDefault(r =>
            r.Files != null && r.Files.Contains("win-x64-zip"));

        if (latest?.Version == null)
        {
            return null;
        }

        // Node.js download URL pattern: https://nodejs.org/dist/v20.11.0/node-v20.11.0-win-x64.zip
        var fileName = $"node-{latest.Version}-win-x64.zip";
        var url = $"https://nodejs.org/dist/{latest.Version}/{fileName}";

        return new VendorDownloadInfo
        {
            Url = url,
            FileName = fileName,
            Version = latest.Version
        };
    }

    /// <summary>
    /// Extracts version number from filename.
    /// </summary>
    private string ExtractVersionFromFileName(string fileName)
    {
        var versionMatch = Regex.Match(fileName, @"(\d+\.?\d*\.?\d*\.?\d*)");
        return versionMatch.Success ? versionMatch.Groups[1].Value : "latest";
    }

    /// <summary>
    /// Cleans up the download directory after installation.
    /// </summary>
    public void CleanupDownloads()
    {
        CleanupDownloadDirectory();
    }

    /// <summary>
    /// Installs all vendors defined in the vendor definitions.
    /// Skips vendors that are already installed.
    /// </summary>
    public async Task<bool> InstallAllVendorsAsync()
    {
        Logger.Header("Downloading Vendor Dependencies");
        Logger.NewLine();
        Logger.Status("This may take several minutes depending on your connection...");
        Logger.NewLine();

        EnsureDownloadDirectoryExists();

        foreach (var vendor in _vendorDefinitions.Values)
        {
            await InstallVendorAsync(vendor.Name);
            Logger.NewLine();
        }

        CleanupDownloadDirectory();

        Logger.NewLine();
        Logger.Success("Vendor setup completed!");
        Logger.Info("Note: MSYS2 packages (git, make, gcc) will be installed on first terminal launch");

        return true;
    }

    /// <summary>
    /// Updates all vendors to their latest versions.
    /// Re-downloads and reinstalls even if already installed.
    /// </summary>
    public async Task<bool> UpdateAllVendorsAsync()
    {
        Logger.Status("This may take several minutes depending on your connection...");
        Logger.NewLine();

        EnsureDownloadDirectoryExists();

        foreach (var vendor in _vendorDefinitions.Values)
        {
            await UpdateVendorAsync(vendor.Name);
            Logger.NewLine();
        }

        CleanupDownloadDirectory();

        return true;
    }

    /// <summary>
    /// Updates a single vendor to the latest version.
    /// For Windows Terminal, extracts over top to preserve LocalState.
    /// For other vendors, removes existing installation before reinstalling.
    /// </summary>
    public async Task<bool> UpdateVendorAsync(string vendorName)
    {
        if (!_vendorDefinitions.TryGetValue(vendorName, out var vendor))
        {
            Logger.Failure($"Unknown vendor: {vendorName}");
            return false;
        }

        var targetDir = Path.Combine(VendorDir, vendor.ExtractDir);
        var isWindowsTerminal = WindowsTerminalConfigurator.IsWindowsTerminal(vendor.Name);

        if (Directory.Exists(targetDir))
        {
            var currentVersion = GetVendorVersion(targetDir);

            if (isWindowsTerminal)
            {
                // Windows Terminal: extract over top to preserve settings/settings.json
                Logger.Info($"Updating {vendor.Name}{(currentVersion != null ? $" (v{currentVersion})" : "")}...");
                Logger.Info($"  Preserving settings configuration");
            }
            else
            {
                // Other vendors: remove and reinstall fresh
                Logger.Info($"Removing existing {vendor.Name} installation{(currentVersion != null ? $" (v{currentVersion})" : "")}...");

                try
                {
                    Directory.Delete(targetDir, true);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to remove existing installation: {ex.Message}");
                    return false;
                }
            }
        }

        // Install (or overwrite for Windows Terminal)
        return await InstallVendorAsync(vendorName, skipIfExists: false);
    }

    // GitHub API response models
    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
    }

    private class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }

    // Node.js API response model
    private class NodeJsRelease
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("files")]
        public List<string>? Files { get; set; }

        [JsonPropertyName("lts")]
        public object? Lts { get; set; }
    }
}

/// <summary>
/// Information about a vendor download.
/// </summary>
public class VendorDownloadInfo
{
    public string Url { get; set; } = "";
    public string FileName { get; set; } = "";
    public string? Version { get; set; }
}
