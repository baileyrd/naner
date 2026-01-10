using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Naner.Common;

namespace Naner.Init;

/// <summary>
/// Downloads essential vendors needed for Naner to function.
/// This is a minimal implementation that downloads only critical dependencies.
/// </summary>
public class EssentialVendorDownloader
{
    private readonly string _nanerRoot;
    private readonly string _vendorDir;
    private readonly bool _forceUpdate;
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    public EssentialVendorDownloader(string nanerRoot, bool forceUpdate = false)
    {
        _nanerRoot = nanerRoot;
        _vendorDir = Path.Combine(nanerRoot, "vendor");
        _forceUpdate = forceUpdate;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Naner-Init/1.0.0");
    }

    /// <summary>
    /// Downloads all essential vendors for a minimal working setup.
    /// </summary>
    public async Task<bool> DownloadAllEssentialsAsync()
    {
        Logger.Info("Downloading essential vendors...");
        Logger.NewLine();

        var success = true;

        // Download 7-Zip first (needed for extracting other archives)
        if (!await Download7ZipAsync())
        {
            Logger.Warning("7-Zip download failed, will use fallback for other vendors");
            success = false;
        }

        // Download PowerShell
        if (!await DownloadPowerShellAsync())
        {
            Logger.Warning("PowerShell download failed");
            success = false;
        }

        // Download Windows Terminal
        if (!await DownloadWindowsTerminalAsync())
        {
            Logger.Warning("Windows Terminal download failed");
            success = false;
        }

        // Download MSYS2 (Git Bash) - requires 7-Zip for extraction
        if (!await DownloadMSYS2Async())
        {
            Logger.Warning("MSYS2/Git Bash download failed");
            success = false;
        }

        return success;
    }

    /// <summary>
    /// Downloads 7-Zip (needed for extracting .tar.xz archives).
    /// </summary>
    private async Task<bool> Download7ZipAsync()
    {
        var extractDir = Path.Combine(_vendorDir, "7zip");

        if (Directory.Exists(extractDir) && File.Exists(Path.Combine(extractDir, "7z.exe")))
        {
            Logger.Info("7-Zip already installed, skipping...");
            return true;
        }

        Logger.Status("Downloading 7-Zip...");

        try
        {
            // Fetch the latest 7-Zip download URL
            var (url, fileName) = await GetLatest7ZipUrlAsync();
            if (url == null || fileName == null)
            {
                Logger.Failure("Failed to find latest 7-Zip release");
                return false;
            }

            Logger.Info($"Latest 7-Zip package: {fileName}");

            var downloadPath = Path.Combine(_vendorDir, ".downloads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath)!);

            if (!await DownloadFileAsync(url, downloadPath, "7-Zip"))
            {
                return false;
            }

            Logger.Status("Extracting 7-Zip from MSI...");
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

            Logger.Success("7-Zip installed");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Failed to download 7-Zip: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Fetches the latest 7-Zip download URL by parsing the 7-zip.org download page.
    /// </summary>
    private async Task<(string? Url, string? FileName)> GetLatest7ZipUrlAsync()
    {
        try
        {
            const string downloadPage = "https://www.7-zip.org/download.html";
            var response = await _httpClient.GetStringAsync(downloadPage);

            // Look for patterns like: 7z2408-x64.msi
            // The pattern matches version like 24.08 (year.month format)
            var pattern = @"a/(7z\d{4}-x64\.msi)";
            var matches = System.Text.RegularExpressions.Regex.Matches(response, pattern);

            if (matches.Count == 0)
            {
                return (null, null);
            }

            // Take the first match (should be the latest)
            var fileName = matches[0].Groups[1].Value;
            var url = $"https://www.7-zip.org/a/{fileName}";

            return (url, fileName);
        }
        catch
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Downloads PowerShell (essential for terminal functionality).
    /// </summary>
    private async Task<bool> DownloadPowerShellAsync()
    {
        var extractDir = Path.Combine(_vendorDir, "powershell");
        var pwshExe = Path.Combine(extractDir, "pwsh.exe");

        try
        {
            // Fetch latest PowerShell release from GitHub
            var githubClient = new GitHubReleasesClient("PowerShell", "PowerShell");
            var latestRelease = await githubClient.GetLatestReleaseAsync();

            if (latestRelease == null)
            {
                Logger.Failure("Failed to fetch latest PowerShell release");
                return false;
            }

            var latestVersion = latestRelease.TagName?.TrimStart('v');

            // Check if already installed and up to date
            if (!_forceUpdate && Directory.Exists(extractDir) && File.Exists(pwshExe))
            {
                var installedVersion = GetPowerShellVersion(pwshExe);
                if (installedVersion != null && installedVersion == latestVersion)
                {
                    Logger.Info($"PowerShell {installedVersion} already installed (latest)");
                    return true;
                }
                else if (installedVersion != null)
                {
                    Logger.Info($"PowerShell {installedVersion} installed, updating to {latestVersion}...");
                }
            }

            Logger.Status("Downloading PowerShell...");

            // Find the win-x64.zip asset
            var asset = latestRelease.Assets?.FirstOrDefault(a =>
                a.Name != null && a.Name.Contains("win-x64.zip", StringComparison.OrdinalIgnoreCase) &&
                !a.Name.Contains("arm", StringComparison.OrdinalIgnoreCase));

            if (asset == null)
            {
                Logger.Failure("PowerShell win-x64.zip asset not found in release");
                return false;
            }

            var url = asset.BrowserDownloadUrl;
            var fileName = asset.Name!;

            if (string.IsNullOrEmpty(url))
            {
                Logger.Failure("PowerShell download URL is missing");
                return false;
            }

            var downloadPath = Path.Combine(_vendorDir, ".downloads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath)!);

            Logger.Info($"Latest PowerShell version: {latestVersion}");

            if (!await DownloadFileAsync(url, downloadPath, "PowerShell"))
            {
                return false;
            }

            Logger.Status("Extracting PowerShell...");

            // Delete old installation if exists
            if (Directory.Exists(extractDir))
            {
                Directory.Delete(extractDir, recursive: true);
            }

            Directory.CreateDirectory(extractDir);
            ZipFile.ExtractToDirectory(downloadPath, extractDir, overwriteFiles: true);

            // Clean up download
            File.Delete(downloadPath);

            Logger.Success($"PowerShell {latestVersion} installed");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Failed to download PowerShell: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the installed PowerShell version.
    /// </summary>
    private string? GetPowerShellVersion(string pwshExe)
    {
        try
        {
            var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(pwshExe);
            return versionInfo.ProductVersion?.Split('+')[0]; // Remove git hash if present
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Downloads Windows Terminal (recommended terminal for best experience).
    /// </summary>
    private async Task<bool> DownloadWindowsTerminalAsync()
    {
        var extractDir = Path.Combine(_vendorDir, "terminal");

        if (Directory.Exists(extractDir) && File.Exists(Path.Combine(extractDir, "wt.exe")))
        {
            Logger.Info("Windows Terminal already installed, skipping...");
            return true;
        }

        Logger.Status("Downloading Windows Terminal...");

        try
        {
            // Fetch latest Windows Terminal release from GitHub
            var githubClient = new GitHubReleasesClient("microsoft", "terminal");
            var latestRelease = await githubClient.GetLatestReleaseAsync();

            if (latestRelease == null)
            {
                Logger.Failure("Failed to fetch latest Windows Terminal release");
                return false;
            }

            // Find the x64.zip asset (not the .msixbundle)
            var asset = latestRelease.Assets?.FirstOrDefault(a =>
                a.Name != null && a.Name.Contains("x64.zip", StringComparison.OrdinalIgnoreCase) &&
                !a.Name.Contains("arm", StringComparison.OrdinalIgnoreCase));

            if (asset == null)
            {
                Logger.Failure("Windows Terminal x64.zip asset not found in release");
                return false;
            }

            var url = asset.BrowserDownloadUrl;
            var fileName = asset.Name!;

            if (string.IsNullOrEmpty(url))
            {
                Logger.Failure("Windows Terminal download URL is missing");
                return false;
            }

            var downloadPath = Path.Combine(_vendorDir, ".downloads", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath)!);

            Logger.Info($"Latest Windows Terminal version: {latestRelease.TagName}");

            if (!await DownloadFileAsync(url, downloadPath, "Windows Terminal"))
            {
                return false;
            }

            Logger.Status("Extracting Windows Terminal...");
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

            Logger.Success("Windows Terminal installed");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Failed to download Windows Terminal: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Downloads MSYS2 (Git Bash and UNIX tools).
    /// </summary>
    private async Task<bool> DownloadMSYS2Async()
    {
        var extractDir = Path.Combine(_vendorDir, "msys64");

        if (Directory.Exists(extractDir) && File.Exists(Path.Combine(extractDir, "usr", "bin", "bash.exe")))
        {
            Logger.Info("MSYS2 already installed, skipping...");
            return true;
        }

        Logger.Status("Downloading MSYS2 (Git Bash)...");

        try
        {
            // Fetch the latest MSYS2 base archive URL
            var (url, fileName) = await GetLatestMSYS2UrlAsync();
            if (url == null || fileName == null)
            {
                Logger.Failure("Failed to find latest MSYS2 release");
                return false;
            }

            Logger.Info($"Latest MSYS2 package: {fileName}");

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
                Logger.Warning("7-Zip not found, cannot extract MSYS2 archive");
                return false;
            }

            Logger.Status("Extracting MSYS2 (this may take a while)...");

            // First extraction: .tar.xz -> .tar
            var tarFileName = fileName.Replace(".tar.xz", ".tar");
            var tarPath = Path.Combine(_vendorDir, ".downloads", tarFileName);
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
                    Logger.Failure("Failed to extract .tar.xz archive");
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
                    Logger.Failure("Failed to extract .tar archive");
                    return false;
                }
            }

            // Clean up downloads
            File.Delete(downloadPath);
            File.Delete(tarPath);

            Logger.Success("MSYS2 installed");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Failed to download MSYS2: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Fetches the latest MSYS2 base archive URL by parsing the repo directory listing.
    /// </summary>
    private async Task<(string? Url, string? FileName)> GetLatestMSYS2UrlAsync()
    {
        try
        {
            const string repoUrl = "https://repo.msys2.org/distrib/x86_64/";
            var response = await _httpClient.GetStringAsync(repoUrl);

            // Parse HTML to find msys2-base-x86_64-*.tar.xz files
            // Look for patterns like: msys2-base-x86_64-20240727.tar.xz
            var pattern = @"msys2-base-x86_64-(\d{8})\.tar\.xz";
            var matches = System.Text.RegularExpressions.Regex.Matches(response, pattern);

            if (matches.Count == 0)
            {
                return (null, null);
            }

            // Find the most recent date
            string? latestFileName = null;
            int latestDate = 0;

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var dateStr = match.Groups[1].Value;
                if (int.TryParse(dateStr, out var date) && date > latestDate)
                {
                    latestDate = date;
                    latestFileName = match.Value;
                }
            }

            if (latestFileName == null)
            {
                return (null, null);
            }

            var url = $"{repoUrl}{latestFileName}";
            return (url, latestFileName);
        }
        catch
        {
            return (null, null);
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
            Logger.Failure($"Download failed: {ex.Message}");
            return false;
        }
    }
}
