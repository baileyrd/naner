using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Naner.Vendors.Abstractions;
using Naner.Archives.Abstractions;
using Naner.Infrastructure.Abstractions;
using Naner.Vendors.Models;

namespace Naner.Vendors.Services;

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
    protected readonly ChecksumVerifier ChecksumVerifier;

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
        ChecksumVerifier = new ChecksumVerifier();
    }

    /// <summary>
    /// Verifies the checksum of a downloaded file.
    /// </summary>
    /// <param name="filePath">Path to the file to verify</param>
    /// <param name="checksumInfo">Checksum information</param>
    /// <returns>True if checksum is valid or verification was skipped</returns>
    protected bool VerifyChecksum(string filePath, ChecksumInfo? checksumInfo)
    {
        if (checksumInfo == null || string.IsNullOrEmpty(checksumInfo.Value))
        {
            Logger.Debug("    No checksum provided, skipping verification", debugMode: false);
            return true;
        }

        Logger.Status($"    Verifying {checksumInfo.Algorithm} checksum...");

        var result = ChecksumVerifier.Verify(filePath, checksumInfo);

        if (result.Skipped)
        {
            Logger.Debug($"    {result.Message}", debugMode: false);
            return true;
        }

        if (result.Success)
        {
            Logger.Success($"    Checksum verified: {result.ActualChecksum?.Substring(0, 16)}...");
            return true;
        }

        // Verification failed
        if (checksumInfo.Required)
        {
            Logger.Failure($"    Checksum verification failed!");
            Logger.Failure($"    Expected: {result.ExpectedChecksum}");
            Logger.Failure($"    Actual:   {result.ActualChecksum}");
            return false;
        }

        // Not required - just warn
        Logger.Warning($"    Checksum mismatch (verification not required)");
        Logger.Warning($"    Expected: {result.ExpectedChecksum}");
        Logger.Warning($"    Actual:   {result.ActualChecksum}");
        return true;
    }

    /// <summary>
    /// Computes and displays the checksum of a file (useful for adding checksums to definitions).
    /// </summary>
    protected void ShowFileChecksum(string filePath, string algorithm = "SHA256")
    {
        try
        {
            var checksum = ChecksumVerifier.ComputeChecksum(filePath, algorithm);
            Logger.Info($"    {algorithm}: {checksum}");
        }
        catch (Exception ex)
        {
            Logger.Debug($"    Could not compute checksum: {ex.Message}", debugMode: false);
        }
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
    /// Delegates to specialized configurators for vendor-specific setup (SRP).
    /// </summary>
    protected void PostInstallConfiguration(string vendorName, string targetDir)
    {
        try
        {
            // Windows Terminal: Create .portable file and settings.json
            if (WindowsTerminalConfigurator.IsWindowsTerminal(vendorName))
            {
                var configurator = new WindowsTerminalConfigurator(NanerRoot);
                configurator.ConfigurePortableMode(targetDir);
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"    Post-install configuration warning: {ex.Message}");
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
