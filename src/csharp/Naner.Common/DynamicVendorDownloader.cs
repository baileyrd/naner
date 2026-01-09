using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Naner.Common;

/// <summary>
/// Handles downloading and installing vendor dependencies using dynamic GitHub API.
/// </summary>
public class DynamicVendorDownloader
{
    private readonly string _nanerRoot;
    private readonly string _vendorDir;
    private readonly string _downloadDir;
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    public DynamicVendorDownloader(string nanerRoot)
    {
        _nanerRoot = nanerRoot;
        _vendorDir = Path.Combine(nanerRoot, "vendor");
        _downloadDir = Path.Combine(_vendorDir, ".downloads");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Naner/1.0.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    }

    /// <summary>
    /// Downloads and installs required vendors for Naner using dynamic GitHub API.
    /// </summary>
    public async Task<bool> SetupRequiredVendorsAsync()
    {
        Logger.Header("Downloading Vendor Dependencies");
        Logger.NewLine();
        Logger.Status("Fetching latest versions from GitHub...");
        Logger.NewLine();

        // Create download directory
        if (!Directory.Exists(_downloadDir))
        {
            Directory.CreateDirectory(_downloadDir);
        }

        // Define vendors with dynamic fetching
        var vendors = new[]
        {
            new DynamicVendorInfo
            {
                Name = "7-Zip",
                ExtractDir = "7zip",
                FetchType = VendorFetchType.WebScrape,
                FallbackUrl = "https://www.7-zip.org/a/7z2408-x64.msi",
                WebScrapeConfig = new WebScrapeConfig
                {
                    Url = "https://www.7-zip.org/download.html",
                    Pattern = @"href=""(/a/7z\d+-x64\.msi)""",
                    BaseUrl = "https://www.7-zip.org"
                }
            },
            new DynamicVendorInfo
            {
                Name = "PowerShell",
                ExtractDir = "powershell",
                FetchType = VendorFetchType.GitHub,
                GitHubOwner = "PowerShell",
                GitHubRepo = "PowerShell",
                AssetPattern = "win-x64.zip",
                FallbackUrl = "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/PowerShell-7.4.6-win-x64.zip"
            },
            new DynamicVendorInfo
            {
                Name = "Windows Terminal",
                ExtractDir = "terminal",
                FetchType = VendorFetchType.GitHub,
                GitHubOwner = "microsoft",
                GitHubRepo = "terminal",
                AssetPattern = "Microsoft.WindowsTerminal_",
                AssetPatternEnd = "_x64.zip",
                FallbackUrl = "https://github.com/microsoft/terminal/releases/download/v1.21.2361.0/Microsoft.WindowsTerminal_1.21.2361.0_x64.zip"
            },
            new DynamicVendorInfo
            {
                Name = "MSYS2 (Git/Bash)",
                ExtractDir = "msys64",
                FetchType = VendorFetchType.WebScrape,
                FallbackUrl = "https://repo.msys2.org/distrib/x86_64/msys2-base-x86_64-20240727.tar.xz",
                WebScrapeConfig = new WebScrapeConfig
                {
                    Url = "https://repo.msys2.org/distrib/x86_64/",
                    Pattern = @"href=""(msys2-base-x86_64-\d+\.tar\.xz)""",
                    BaseUrl = "https://repo.msys2.org/distrib/x86_64/"
                }
            }
        };

        foreach (var vendor in vendors)
        {
            var targetDir = Path.Combine(_vendorDir, vendor.ExtractDir);

            // Skip if already installed
            if (Directory.Exists(targetDir) && Directory.GetFileSystemEntries(targetDir).Length > 0)
            {
                Logger.Info($"Skipping {vendor.Name} (already installed)");
                continue;
            }

            Logger.Status($"Fetching latest {vendor.Name}...");

            try
            {
                // Fetch download URL
                var downloadInfo = await FetchVendorDownloadInfoAsync(vendor);
                if (downloadInfo == null)
                {
                    Logger.Warning($"Failed to fetch {vendor.Name}, skipping...");
                    continue;
                }

                Logger.Info($"  Latest version: {downloadInfo.Version ?? "Unknown"}");
                Logger.Status($"  Downloading {downloadInfo.FileName}...");

                var downloadPath = Path.Combine(_downloadDir, downloadInfo.FileName);

                // Download file
                if (!await DownloadFileAsync(downloadInfo.Url, downloadPath))
                {
                    Logger.Warning($"Failed to download {vendor.Name}, skipping...");
                    continue;
                }

                Logger.Success($"  Downloaded {downloadInfo.FileName}");

                // Extract file
                Logger.Status($"  Installing {vendor.Name}...");

                if (!ExtractArchive(downloadPath, targetDir, vendor.Name))
                {
                    Logger.Warning($"Failed to install {vendor.Name}, skipping...");
                    continue;
                }

                // Post-install configuration
                PostInstallConfiguration(vendor.Name, targetDir);

                Logger.Success($"  Installed {vendor.Name}");
                Logger.NewLine();
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error setting up {vendor.Name}: {ex.Message}");
                Logger.NewLine();
            }
        }

        // Cleanup downloads
        try
        {
            if (Directory.Exists(_downloadDir))
            {
                Directory.Delete(_downloadDir, true);
            }
        }
        catch { /* Ignore cleanup errors */ }

        Logger.NewLine();
        Logger.Success("Vendor setup completed!");
        Logger.Info("Note: MSYS2 packages (git, make, gcc) will be installed on first terminal launch");

        return true;
    }

    /// <summary>
    /// Fetches download information for a vendor.
    /// </summary>
    private async Task<VendorDownloadInfo?> FetchVendorDownloadInfoAsync(DynamicVendorInfo vendor)
    {
        try
        {
            return vendor.FetchType switch
            {
                VendorFetchType.GitHub => await FetchFromGitHubAsync(vendor),
                VendorFetchType.WebScrape => await FetchFromWebScrapeAsync(vendor),
                _ => null
            };
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
                    FileName = Path.GetFileName(vendor.FallbackUrl),
                    Version = "fallback"
                };
            }

            return null;
        }
    }

    /// <summary>
    /// Fetches download info from GitHub releases API.
    /// </summary>
    private async Task<VendorDownloadInfo?> FetchFromGitHubAsync(DynamicVendorInfo vendor)
    {
        var url = $"https://api.github.com/repos/{vendor.GitHubOwner}/{vendor.GitHubRepo}/releases/latest";
        var response = await _httpClient.GetAsync(url);

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
    private async Task<VendorDownloadInfo?> FetchFromWebScrapeAsync(DynamicVendorInfo vendor)
    {
        if (vendor.WebScrapeConfig == null)
        {
            return null;
        }

        var response = await _httpClient.GetAsync(vendor.WebScrapeConfig.Url);
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

        // Extract version from filename if possible
        var versionMatch = Regex.Match(fileName, @"(\d+\.?\d*\.?\d*\.?\d*)");
        var version = versionMatch.Success ? versionMatch.Groups[1].Value : "latest";

        return new VendorDownloadInfo
        {
            Url = fullUrl,
            FileName = fileName,
            Version = version
        };
    }

    /// <summary>
    /// Downloads a file from URL to local path with progress indication.
    /// </summary>
    private async Task<bool> DownloadFileAsync(string url, string outputPath)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            var lastPercent = -1;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    var percent = (int)((totalRead * 100) / totalBytes);
                    if (percent != lastPercent && percent % 10 == 0)
                    {
                        Console.Write($"\r    Progress: {percent}%");
                        lastPercent = percent;
                    }
                }
            }

            if (totalBytes > 0)
            {
                Console.Write("\r    Progress: 100%");
                Console.WriteLine();
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"    Download error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Extracts an archive to the target directory.
    /// </summary>
    private bool ExtractArchive(string archivePath, string targetDir, string vendorName)
    {
        try
        {
            var extension = Path.GetExtension(archivePath).ToLower();

            if (extension == ".zip")
            {
                return ExtractZip(archivePath, targetDir);
            }
            else if (extension == ".msi")
            {
                return ExtractMsi(archivePath, targetDir);
            }
            else if (archivePath.EndsWith(".tar.xz", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractTarXz(archivePath, targetDir, vendorName);
            }
            else
            {
                Logger.Warning($"    Unsupported archive format: {extension}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Failure($"    Extraction error: {ex.Message}");
            return false;
        }
    }

    // Include extraction methods from VendorDownloader (ExtractZip, ExtractMsi, ExtractTarXz)
    private bool ExtractZip(string zipPath, string targetDir)
    {
        try
        {
            Directory.CreateDirectory(targetDir);
            ZipFile.ExtractToDirectory(zipPath, targetDir, overwriteFiles: true);

            // Check if extraction created a single subdirectory (common with vendor ZIPs)
            var entries = Directory.GetFileSystemEntries(targetDir);
            if (entries.Length == 1 && Directory.Exists(entries[0]))
            {
                var subDir = entries[0];
                var tempDir = targetDir + "_temp";
                Directory.Move(subDir, tempDir);

                foreach (var file in Directory.GetFiles(tempDir))
                {
                    var destFile = Path.Combine(targetDir, Path.GetFileName(file));
                    File.Move(file, destFile, overwrite: true);
                }

                foreach (var dir in Directory.GetDirectories(tempDir))
                {
                    var destDir = Path.Combine(targetDir, Path.GetFileName(dir));
                    Directory.Move(dir, destDir);
                }

                Directory.Delete(tempDir, recursive: true);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"    ZIP extraction failed: {ex.Message}");
            return false;
        }
    }

    private bool ExtractMsi(string msiPath, string targetDir)
    {
        try
        {
            Directory.CreateDirectory(targetDir);

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = $"/a \"{msiPath}\" /qn TARGETDIR=\"{targetDir}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit();

            // MSI extracts to Files/7-Zip subdirectory, move contents up
            var filesDir = Path.Combine(targetDir, "Files", "7-Zip");
            if (Directory.Exists(filesDir))
            {
                foreach (var file in Directory.GetFiles(filesDir))
                {
                    var destFile = Path.Combine(targetDir, Path.GetFileName(file));
                    File.Move(file, destFile, overwrite: true);
                }

                var files7ZipDir = Path.Combine(targetDir, "Files");
                if (Directory.Exists(files7ZipDir))
                {
                    Directory.Delete(files7ZipDir, recursive: true);
                }
            }

            return process?.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Logger.Failure($"    MSI extraction failed: {ex.Message}");
            return false;
        }
    }

    private bool ExtractTarXz(string tarXzPath, string targetDir, string vendorName)
    {
        try
        {
            var sevenZipPath = Path.Combine(_vendorDir, "7zip", "7z.exe");
            if (!File.Exists(sevenZipPath))
            {
                Logger.Warning($"    7-Zip not found at {sevenZipPath}");
                return false;
            }

            Directory.CreateDirectory(targetDir);

            Logger.Info($"    Extracting .xz archive...");
            var tarPath = tarXzPath.Replace(".tar.xz", ".tar");

            var xzStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = sevenZipPath,
                Arguments = $"e \"{tarXzPath}\" -o\"{Path.GetDirectoryName(tarXzPath)}\" -y",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var xzProcess = System.Diagnostics.Process.Start(xzStartInfo))
            {
                xzProcess?.WaitForExit();
                if (xzProcess?.ExitCode != 0)
                {
                    return false;
                }
            }

            Logger.Info($"    Extracting .tar archive...");

            var tarStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = sevenZipPath,
                Arguments = $"x \"{tarPath}\" -o\"{targetDir}\" -y",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var tarProcess = System.Diagnostics.Process.Start(tarStartInfo))
            {
                tarProcess?.WaitForExit();
                if (tarProcess?.ExitCode != 0)
                {
                    return false;
                }
            }

            try
            {
                if (File.Exists(tarPath))
                {
                    File.Delete(tarPath);
                }
            }
            catch { }

            // Flatten single subdirectory
            var entries = Directory.GetFileSystemEntries(targetDir);
            if (entries.Length == 1 && Directory.Exists(entries[0]))
            {
                var subDir = entries[0];
                var tempDir = targetDir + "_temp";
                Directory.Move(subDir, tempDir);

                foreach (var file in Directory.GetFiles(tempDir))
                {
                    var destFile = Path.Combine(targetDir, Path.GetFileName(file));
                    File.Move(file, destFile, overwrite: true);
                }

                foreach (var dir in Directory.GetDirectories(tempDir))
                {
                    var destDir = Path.Combine(targetDir, Path.GetFileName(dir));
                    Directory.Move(dir, destDir);
                }

                Directory.Delete(tempDir, recursive: true);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"    .tar.xz extraction failed: {ex.Message}");
            return false;
        }
    }

    private void PostInstallConfiguration(string vendorName, string targetDir)
    {
        try
        {
            if (vendorName.Contains("Windows Terminal", StringComparison.OrdinalIgnoreCase))
            {
                var portableFile = Path.Combine(targetDir, ".portable");
                File.WriteAllText(portableFile, "");
                Logger.Info($"    Created .portable file for portable mode");

                var settingsDir = Path.Combine(targetDir, "settings");
                Directory.CreateDirectory(settingsDir);

                var settingsFile = Path.Combine(settingsDir, "settings.json");
                CreateWindowsTerminalSettings(settingsFile);
                Logger.Info($"    Created settings/settings.json with Naner profiles");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"    Post-install configuration warning: {ex.Message}");
        }
    }

    private void CreateWindowsTerminalSettings(string settingsPath)
    {
        var settings = @"{
    ""$schema"": ""https://aka.ms/terminal-profiles-schema"",
    ""defaultProfile"": ""{61c54bbd-c2c6-5271-96e7-009a87ff44bf}"",
    ""copyOnSelect"": false,
    ""copyFormatting"": false,
    ""profiles"": {
        ""defaults"": {},
        ""list"": [
            {
                ""guid"": ""{61c54bbd-c2c6-5271-96e7-009a87ff44bf}"",
                ""name"": ""Naner (Unified)"",
                ""commandline"": ""%NANER_ROOT%\\vendor\\powershell\\pwsh.exe -NoExit -Command \""$env:PATH='%NANER_ROOT%\\bin;%NANER_ROOT%\\vendor\\bin;%NANER_ROOT%\\vendor\\powershell;%NANER_ROOT%\\vendor\\msys64\\usr\\bin;%NANER_ROOT%\\vendor\\msys64\\mingw64\\bin;'+$env:PATH; $env:HOME='%NANER_ROOT%\\home'\"""",
                ""startingDirectory"": ""%NANER_ROOT%\\home"",
                ""icon"": ""ms-appx:///ProfileIcons/{61c54bbd-c2c6-5271-96e7-009a87ff44bf}.png"",
                ""colorScheme"": ""Campbell""
            }
        ]
    }
}";
        File.WriteAllText(settingsPath, settings);
    }

    private class GitHubRelease
    {
        public string? TagName { get; set; }
        public List<GitHubAsset>? Assets { get; set; }
    }

    private class GitHubAsset
    {
        public string? Name { get; set; }
        public string? BrowserDownloadUrl { get; set; }
    }
}

public class DynamicVendorInfo
{
    public string Name { get; set; } = "";
    public string ExtractDir { get; set; } = "";
    public VendorFetchType FetchType { get; set; }
    public string? GitHubOwner { get; set; }
    public string? GitHubRepo { get; set; }
    public string? AssetPattern { get; set; }
    public string? AssetPatternEnd { get; set; }
    public string? FallbackUrl { get; set; }
    public WebScrapeConfig? WebScrapeConfig { get; set; }
}

public class WebScrapeConfig
{
    public string Url { get; set; } = "";
    public string Pattern { get; set; } = "";
    public string BaseUrl { get; set; } = "";
}

public class VendorDownloadInfo
{
    public string Url { get; set; } = "";
    public string FileName { get; set; } = "";
    public string? Version { get; set; }
}

public enum VendorFetchType
{
    GitHub,
    WebScrape
}
