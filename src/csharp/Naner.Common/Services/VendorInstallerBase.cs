using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Naner.Common.Abstractions;
using Naner.Common.Models;

namespace Naner.Common.Services;

/// <summary>
/// Base class for vendor installation with shared download and extraction logic.
/// Eliminates duplication between static and dynamic vendor installers.
/// </summary>
public abstract class VendorInstallerBase : IVendorInstaller
{
    protected readonly string NanerRoot;
    protected readonly string VendorDir;
    protected readonly string DownloadDir;
    protected readonly IHttpClientWrapper HttpClient;

    /// <summary>
    /// Creates a new vendor installer with dependency injection support.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory</param>
    /// <param name="httpClient">Optional HTTP client wrapper for dependency injection</param>
    protected VendorInstallerBase(string nanerRoot, IHttpClientWrapper? httpClient = null)
    {
        NanerRoot = nanerRoot ?? throw new ArgumentNullException(nameof(nanerRoot));
        VendorDir = Path.Combine(nanerRoot, "vendor");
        DownloadDir = Path.Combine(VendorDir, ".downloads");
        HttpClient = httpClient ?? new HttpClientWrapper();
    }

    public abstract Task<bool> InstallVendorAsync(string vendorName);

    public bool IsInstalled(string vendorName)
    {
        var vendorPath = GetVendorPath(vendorName);
        return vendorPath != null && Directory.Exists(vendorPath) &&
               Directory.GetFileSystemEntries(vendorPath).Length > 0;
    }

    public virtual string? GetVendorPath(string vendorName)
    {
        // This should be overridden by implementations that know their vendor definitions
        return null;
    }

    /// <summary>
    /// Downloads a file from URL to local path with progress indication.
    /// </summary>
    protected async Task<bool> DownloadFileAsync(string url, string outputPath)
    {
        try
        {
            using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
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
                    if (percent != lastPercent && percent % NanerConstants.ProgressUpdateInterval == 0)
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
    /// Extracts an archive using the appropriate extractor strategy.
    /// </summary>
    protected bool ExtractArchive(string archivePath, string targetDir, string vendorName)
    {
        var sevenZipPath = Path.Combine(VendorDir, "7zip", "7z.exe");
        var extractorService = new ArchiveExtractorService(sevenZipPath);

        return extractorService.ExtractArchive(archivePath, targetDir, vendorName);
    }

    /// <summary>
    /// Performs post-installation configuration for specific vendors.
    /// </summary>
    protected void PostInstallConfiguration(string vendorName, string targetDir)
    {
        try
        {
            // Windows Terminal: Create .portable file and settings.json
            if (vendorName.Contains("Windows Terminal", StringComparison.OrdinalIgnoreCase))
            {
                // Create .portable marker file
                var portableFile = Path.Combine(targetDir, ".portable");
                File.WriteAllText(portableFile, "");
                Logger.Info($"    Created .portable file for portable mode");

                // Create LocalState directory and settings.json with Naner profiles
                var localStateDir = Path.Combine(targetDir, "LocalState");
                Directory.CreateDirectory(localStateDir);

                var settingsFile = Path.Combine(localStateDir, "settings.json");
                CreateWindowsTerminalSettings(settingsFile);
                Logger.Info($"    Created LocalState/settings.json with Naner profiles");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"    Post-install configuration warning: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates Windows Terminal settings.json with Naner profiles.
    /// Copies from template in home/.config/windows-terminal/settings.json and expands paths.
    /// </summary>
    protected void CreateWindowsTerminalSettings(string settingsPath)
    {
        // Try to copy from template
        var templatePath = Path.Combine(NanerRoot, "home", ".config", "windows-terminal", "settings.json");

        if (File.Exists(templatePath))
        {
            // Read template and expand %NANER_ROOT% to actual path
            var templateContent = File.ReadAllText(templatePath);
            var expandedContent = templateContent.Replace("%NANER_ROOT%", NanerRoot.Replace("\\", "\\\\"));
            File.WriteAllText(settingsPath, expandedContent);
        }
        else
        {
            // Fallback: create basic settings inline
            var settings = @"{
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
            File.WriteAllText(settingsPath, settings);
        }
    }

    /// <summary>
    /// Ensures the download directory exists.
    /// </summary>
    protected void EnsureDownloadDirectoryExists()
    {
        if (!Directory.Exists(DownloadDir))
        {
            Directory.CreateDirectory(DownloadDir);
        }
    }

    /// <summary>
    /// Cleans up the download directory.
    /// </summary>
    protected void CleanupDownloadDirectory()
    {
        try
        {
            if (Directory.Exists(DownloadDir))
            {
                Directory.Delete(DownloadDir, true);
            }
        }
        catch (Exception ex)
        {
            // Non-critical: Log and continue
            Logger.Debug($"Failed to cleanup download directory: {ex.Message}", debugMode: false);
        }
    }
}
