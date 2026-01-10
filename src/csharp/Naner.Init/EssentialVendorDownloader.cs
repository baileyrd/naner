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
    /// Downloads all essential vendors for a minimal working setup.
    /// </summary>
    public async Task<bool> DownloadAllEssentialsAsync()
    {
        ConsoleHelper.Info("Downloading essential vendors...");
        ConsoleHelper.NewLine();

        var success = true;

        // Download 7-Zip first (needed for extracting other archives)
        if (!await Download7ZipAsync())
        {
            ConsoleHelper.Warning("7-Zip download failed, will use fallback for other vendors");
            success = false;
        }

        // Download PowerShell
        if (!await DownloadPowerShellAsync())
        {
            ConsoleHelper.Warning("PowerShell download failed");
            success = false;
        }

        // Download Windows Terminal
        if (!await DownloadWindowsTerminalAsync())
        {
            ConsoleHelper.Warning("Windows Terminal download failed");
            success = false;
        }

        // Download MSYS2 (Git Bash) - requires 7-Zip for extraction
        if (!await DownloadMSYS2Async())
        {
            ConsoleHelper.Warning("MSYS2/Git Bash download failed");
            success = false;
        }

        return success;
    }

    /// <summary>
    /// Downloads 7-Zip (needed for extracting .tar.xz archives).
    /// </summary>
    private async Task<bool> Download7ZipAsync()
    {
        const string url = "https://www.7-zip.org/a/7z2408-x64.msi";
        const string fileName = "7z2408-x64.msi";
        var extractDir = Path.Combine(_vendorDir, "7zip");

        if (Directory.Exists(extractDir) && File.Exists(Path.Combine(extractDir, "7z.exe")))
        {
            ConsoleHelper.Info("7-Zip already installed, skipping...");
            return true;
        }

        ConsoleHelper.Status("Downloading 7-Zip...");

        try
        {
            var downloadPath = Path.Combine(_vendorDir, ".downloads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath)!);

            if (!await DownloadFileAsync(url, downloadPath, "7-Zip"))
            {
                return false;
            }

            ConsoleHelper.Status("Extracting 7-Zip from MSI...");
            Directory.CreateDirectory(extractDir);

            // Extract MSI using msiexec
            var msiextractPath = Path.Combine(_vendorDir, ".downloads", "7zip-temp");
            Directory.CreateDirectory(msiextractPath);

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "msiexec",
                Arguments = $"/a \"{downloadPath}\" /qn TARGETDIR=\"{msiextractPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                process?.WaitForExit();
            }

            // MSI extracts to Files/7-Zip subdirectory
            var filesDir = Path.Combine(msiextractPath, "Files", "7-Zip");
            if (Directory.Exists(filesDir))
            {
                // Move all files from Files/7-Zip to vendor/7zip
                foreach (var file in Directory.GetFiles(filesDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(filesDir, file);
                    var destPath = Path.Combine(extractDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                    File.Copy(file, destPath, overwrite: true);
                }
            }

            // Clean up
            File.Delete(downloadPath);
            Directory.Delete(msiextractPath, recursive: true);

            ConsoleHelper.Success("7-Zip installed");
            return true;
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error($"Failed to download 7-Zip: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Downloads PowerShell (essential for terminal functionality).
    /// </summary>
    private async Task<bool> DownloadPowerShellAsync()
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
    /// Downloads Windows Terminal (recommended terminal for best experience).
    /// </summary>
    private async Task<bool> DownloadWindowsTerminalAsync()
    {
        const string url = "https://github.com/microsoft/terminal/releases/download/v1.21.2361.0/Microsoft.WindowsTerminal_1.21.2361.0_x64.zip";
        const string fileName = "Microsoft.WindowsTerminal_1.21.2361.0_x64.zip";
        var extractDir = Path.Combine(_vendorDir, "terminal");

        if (Directory.Exists(extractDir) && File.Exists(Path.Combine(extractDir, "wt.exe")))
        {
            ConsoleHelper.Info("Windows Terminal already installed, skipping...");
            return true;
        }

        ConsoleHelper.Status("Downloading Windows Terminal...");

        try
        {
            var downloadPath = Path.Combine(_vendorDir, ".downloads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath)!);

            if (!await DownloadFileAsync(url, downloadPath, "Windows Terminal"))
            {
                return false;
            }

            ConsoleHelper.Status("Extracting Windows Terminal...");
            var tempExtractDir = Path.Combine(_vendorDir, ".downloads", "terminal-temp");
            Directory.CreateDirectory(tempExtractDir);
            ZipFile.ExtractToDirectory(downloadPath, tempExtractDir, overwriteFiles: true);

            // Find the subdirectory containing the terminal files
            var terminalSubDir = Directory.GetDirectories(tempExtractDir).FirstOrDefault();
            if (terminalSubDir != null && Directory.Exists(terminalSubDir))
            {
                // Move all files from subdirectory to vendor/terminal
                Directory.CreateDirectory(extractDir);
                foreach (var file in Directory.GetFiles(terminalSubDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(terminalSubDir, file);
                    var destPath = Path.Combine(extractDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                    File.Copy(file, destPath, overwrite: true);
                }
            }

            // Create .portable file for portable mode
            File.WriteAllText(Path.Combine(extractDir, ".portable"), "");

            // Create settings directory
            var settingsDir = Path.Combine(extractDir, "settings");
            Directory.CreateDirectory(settingsDir);

            // Clean up
            File.Delete(downloadPath);
            Directory.Delete(tempExtractDir, recursive: true);

            ConsoleHelper.Success("Windows Terminal installed");
            return true;
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error($"Failed to download Windows Terminal: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Downloads MSYS2 (Git Bash and UNIX tools).
    /// </summary>
    private async Task<bool> DownloadMSYS2Async()
    {
        const string url = "https://repo.msys2.org/distrib/x86_64/msys2-base-x86_64-20240727.tar.xz";
        const string fileName = "msys2-base-x86_64-20240727.tar.xz";
        var extractDir = Path.Combine(_vendorDir, "msys64");

        if (Directory.Exists(extractDir) && File.Exists(Path.Combine(extractDir, "usr", "bin", "bash.exe")))
        {
            ConsoleHelper.Info("MSYS2 already installed, skipping...");
            return true;
        }

        ConsoleHelper.Status("Downloading MSYS2 (Git Bash)...");

        try
        {
            var downloadPath = Path.Combine(_vendorDir, ".downloads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath)!);

            if (!await DownloadFileAsync(url, downloadPath, "MSYS2"))
            {
                return false;
            }

            // Check if 7-Zip is available
            var sevenZipPath = Path.Combine(_vendorDir, "7zip", "7z.exe");
            if (!File.Exists(sevenZipPath))
            {
                ConsoleHelper.Warning("7-Zip not found, cannot extract MSYS2 archive");
                return false;
            }

            ConsoleHelper.Status("Extracting MSYS2 (this may take a while)...");

            // First extraction: .tar.xz -> .tar
            var tarPath = Path.Combine(_vendorDir, ".downloads", "msys2-base-x86_64-20240727.tar");
            var extractStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = sevenZipPath,
                Arguments = $"x \"{downloadPath}\" -o\"{Path.Combine(_vendorDir, ".downloads")}\" -y",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = System.Diagnostics.Process.Start(extractStartInfo))
            {
                process?.WaitForExit();
                if (process?.ExitCode != 0)
                {
                    ConsoleHelper.Error("Failed to extract .tar.xz archive");
                    return false;
                }
            }

            // Second extraction: .tar -> files
            extractStartInfo.Arguments = $"x \"{tarPath}\" -o\"{_vendorDir}\" -y";
            using (var process = System.Diagnostics.Process.Start(extractStartInfo))
            {
                process?.WaitForExit();
                if (process?.ExitCode != 0)
                {
                    ConsoleHelper.Error("Failed to extract .tar archive");
                    return false;
                }
            }

            // Clean up downloads
            File.Delete(downloadPath);
            File.Delete(tarPath);

            ConsoleHelper.Success("MSYS2 installed");
            return true;
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error($"Failed to download MSYS2: {ex.Message}");
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
