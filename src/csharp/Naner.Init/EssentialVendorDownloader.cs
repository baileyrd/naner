using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace Naner.Init;

/// <summary>
/// Downloads essential vendors needed for Naner to function.
/// This is a minimal implementation that downloads only critical dependencies.
/// </summary>
public class EssentialVendorDownloader
{
    private readonly string _nanerRoot;
    private readonly string _vendorDir;
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    public EssentialVendorDownloader(string nanerRoot)
    {
        _nanerRoot = nanerRoot;
        _vendorDir = Path.Combine(nanerRoot, "vendor");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Naner-Init/1.0.0");
    }

    /// <summary>
    /// Downloads PowerShell (essential for terminal functionality).
    /// </summary>
    public async Task<bool> DownloadPowerShellAsync()
    {
        const string url = "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/PowerShell-7.4.6-win-x64.zip";
        const string fileName = "PowerShell-7.4.6-win-x64.zip";
        var extractDir = Path.Combine(_vendorDir, "powershell");

        if (Directory.Exists(extractDir) && File.Exists(Path.Combine(extractDir, "pwsh.exe")))
        {
            ConsoleHelper.Info("PowerShell already installed, skipping...");
            return true;
        }

        ConsoleHelper.Status("Downloading PowerShell...");

        try
        {
            var downloadPath = Path.Combine(_vendorDir, ".downloads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath)!);

            if (!await DownloadFileAsync(url, downloadPath, "PowerShell"))
            {
                return false;
            }

            ConsoleHelper.Status("Extracting PowerShell...");
            Directory.CreateDirectory(extractDir);
            ZipFile.ExtractToDirectory(downloadPath, extractDir, overwriteFiles: true);

            // Clean up download
            File.Delete(downloadPath);

            ConsoleHelper.Success("PowerShell installed");
            return true;
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error($"Failed to download PowerShell: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Downloads a file with progress indication.
    /// </summary>
    private async Task<bool> DownloadFileAsync(string url, string outputPath, string displayName)
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
                        Console.Write($"\r  Progress: {percent}%");
                        lastPercent = percent;
                    }
                }
            }

            if (totalBytes > 0)
            {
                Console.Write("\r  Progress: 100%");
                Console.WriteLine();
            }

            return true;
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error($"Download failed: {ex.Message}");
            return false;
        }
    }
}
