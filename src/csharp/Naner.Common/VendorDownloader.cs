using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Naner.Common;

/// <summary>
/// Handles downloading and installing vendor dependencies.
/// </summary>
public class VendorDownloader
{
    private readonly string _nanerRoot;
    private readonly string _vendorDir;
    private readonly string _downloadDir;
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    public VendorDownloader(string nanerRoot)
    {
        _nanerRoot = nanerRoot;
        _vendorDir = Path.Combine(nanerRoot, "vendor");
        _downloadDir = Path.Combine(_vendorDir, ".downloads");

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Naner/1.0.0");
    }

    /// <summary>
    /// Downloads and installs required vendors for Naner.
    /// </summary>
    public async Task<bool> SetupRequiredVendorsAsync()
    {
        Logger.Header("Downloading Vendor Dependencies");
        Logger.NewLine();
        Logger.Status("This may take several minutes depending on your connection...");
        Logger.NewLine();

        // Create download directory
        if (!Directory.Exists(_downloadDir))
        {
            Directory.CreateDirectory(_downloadDir);
        }

        // Download required vendors in order (respecting dependencies)
        var vendors = new[]
        {
            new VendorInfo("7-Zip", "https://www.7-zip.org/a/7z2408-x64.msi", "7z2408-x64.msi", "7zip"),
            new VendorInfo("PowerShell", "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/PowerShell-7.4.6-win-x64.zip", "PowerShell-7.4.6-win-x64.zip", "powershell"),
            new VendorInfo("Windows Terminal", "https://github.com/microsoft/terminal/releases/download/v1.21.2361.0/Microsoft.WindowsTerminal_1.21.2361.0_x64.zip", "Microsoft.WindowsTerminal_1.21.2361.0_x64.zip", "terminal"),
            new VendorInfo("MSYS2 (Git/Bash)", "https://repo.msys2.org/distrib/x86_64/msys2-base-x86_64-20240727.tar.xz", "msys2-base-x86_64-20240727.tar.xz", "msys64")
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

            Logger.Status($"Downloading {vendor.Name}...");

            var downloadPath = Path.Combine(_downloadDir, vendor.FileName);

            try
            {
                // Download file
                if (!await DownloadFileAsync(vendor.Url, downloadPath))
                {
                    Logger.Warning($"Failed to download {vendor.Name}, skipping...");
                    continue;
                }

                Logger.Success($"  Downloaded {vendor.FileName}");

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
    /// Downloads a file from URL to local path with progress indication.
    /// </summary>
    private async Task<bool> DownloadFileAsync(string url, string outputPath)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            var lastPercent = -1;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
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

    /// <summary>
    /// Extracts a ZIP archive and flattens single-directory structures.
    /// </summary>
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
                // Move contents of subdirectory up one level
                var subDir = entries[0];
                var tempDir = targetDir + "_temp";

                // Move subdirectory to temp location
                Directory.Move(subDir, tempDir);

                // Move all contents from temp to target
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

                // Remove temp directory
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

    /// <summary>
    /// Extracts an MSI installer using msiexec.
    /// </summary>
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

                // Cleanup
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

    /// <summary>
    /// Extracts a .tar.xz archive using 7-Zip (must be installed first).
    /// </summary>
    private bool ExtractTarXz(string tarXzPath, string targetDir, string vendorName)
    {
        try
        {
            // Find 7z.exe (should be installed first)
            var sevenZipPath = Path.Combine(_vendorDir, "7zip", "7z.exe");
            if (!File.Exists(sevenZipPath))
            {
                Logger.Warning($"    7-Zip not found at {sevenZipPath}");
                Logger.Info($"    {vendorName} downloaded to: {tarXzPath}");
                Logger.Info($"    Please extract manually to: {targetDir}");
                return false;
            }

            Directory.CreateDirectory(targetDir);

            // Step 1: Extract .xz to get .tar (to same directory as .tar.xz)
            Logger.Info($"    Extracting .xz archive...");
            var tarPath = tarXzPath.Replace(".tar.xz", ".tar");

            var xzStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = sevenZipPath,
                Arguments = $"e \"{tarXzPath}\" -o\"{Path.GetDirectoryName(tarXzPath)}\" -y",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var xzProcess = System.Diagnostics.Process.Start(xzStartInfo))
            {
                xzProcess?.WaitForExit();
                if (xzProcess?.ExitCode != 0)
                {
                    Logger.Warning($"    Failed to extract .xz (exit code {xzProcess?.ExitCode})");
                    return false;
                }
            }

            // Step 2: Extract .tar to target directory
            Logger.Info($"    Extracting .tar archive...");

            var tarStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = sevenZipPath,
                Arguments = $"x \"{tarPath}\" -o\"{targetDir}\" -y",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var tarProcess = System.Diagnostics.Process.Start(tarStartInfo))
            {
                tarProcess?.WaitForExit();
                if (tarProcess?.ExitCode != 0)
                {
                    Logger.Warning($"    Failed to extract .tar (exit code {tarProcess?.ExitCode})");
                    return false;
                }
            }

            // Cleanup intermediate .tar file
            try
            {
                if (File.Exists(tarPath))
                {
                    File.Delete(tarPath);
                }
            }
            catch { /* Ignore cleanup errors */ }

            // Check if extraction created a single subdirectory and flatten
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

    /// <summary>
    /// Performs post-installation configuration for specific vendors.
    /// </summary>
    private void PostInstallConfiguration(string vendorName, string targetDir)
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

                // Create settings directory and settings.json with Naner profiles
                // Note: Windows Terminal portable mode looks for settings/settings.json
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
            // Non-critical, don't fail the installation
        }
    }

    /// <summary>
    /// Creates Windows Terminal settings.json with Naner profiles.
    /// </summary>
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
            },
            {
                ""guid"": ""{574e775e-4f2a-5b96-ac1e-a2962a402336}"",
                ""name"": ""PowerShell"",
                ""commandline"": ""%NANER_ROOT%\\vendor\\powershell\\pwsh.exe -NoExit -Command \""$env:PATH='%NANER_ROOT%\\bin;%NANER_ROOT%\\vendor\\powershell;'+$env:PATH\"""",
                ""startingDirectory"": ""%USERPROFILE%"",
                ""icon"": ""ms-appx:///ProfileIcons/{574e775e-4f2a-5b96-ac1e-a2962a402336}.png"",
                ""colorScheme"": ""Campbell""
            },
            {
                ""guid"": ""{2c4de342-38b7-51cf-b940-2309a097f518}"",
                ""name"": ""Git Bash"",
                ""commandline"": ""%NANER_ROOT%\\vendor\\msys64\\usr\\bin\\bash.exe --login -i"",
                ""startingDirectory"": ""%USERPROFILE%"",
                ""icon"": ""ms-appx:///ProfileIcons/{2c4de342-38b7-51cf-b940-2309a097f518}.png"",
                ""colorScheme"": ""One Half Dark""
            },
            {
                ""guid"": ""{0caa0dad-35be-5f56-a8ff-afceeeaa6101}"",
                ""name"": ""Command Prompt"",
                ""commandline"": ""cmd.exe"",
                ""startingDirectory"": ""%USERPROFILE%"",
                ""icon"": ""ms-appx:///ProfileIcons/{0caa0dad-35be-5f56-a8ff-afceeeaa6101}.png"",
                ""colorScheme"": ""Campbell""
            }
        ]
    },
    ""schemes"": [],
    ""actions"": [
        { ""command"": { ""action"": ""copy"", ""singleLine"": false }, ""keys"": ""ctrl+c"" },
        { ""command"": ""paste"", ""keys"": ""ctrl+v"" },
        { ""command"": ""find"", ""keys"": ""ctrl+shift+f"" },
        { ""command"": { ""action"": ""splitPane"", ""split"": ""auto"", ""splitMode"": ""duplicate"" }, ""keys"": ""alt+shift+d"" }
    ]
}";
        File.WriteAllText(settingsPath, settings);
    }

    /// <summary>
    /// Represents information about a vendor package.
    /// </summary>
    private class VendorInfo
    {
        public string Name { get; }
        public string Url { get; }
        public string FileName { get; }
        public string ExtractDir { get; }

        public VendorInfo(string name, string url, string fileName, string extractDir)
        {
            Name = name;
            Url = url;
            FileName = fileName;
            ExtractDir = extractDir;
        }
    }
}
