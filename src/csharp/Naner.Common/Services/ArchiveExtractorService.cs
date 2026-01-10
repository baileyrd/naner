using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Naner.Common.Abstractions;

namespace Naner.Common.Services;

/// <summary>
/// Service for extracting various archive formats using the Strategy pattern.
/// Supports .zip, .tar.xz, .msi, and other formats.
/// </summary>
public class ArchiveExtractorService
{
    private readonly ILogger _logger;
    private readonly string? _sevenZipPath;

    /// <summary>
    /// Creates a new ArchiveExtractorService.
    /// </summary>
    /// <param name="logger">Logger for output messages.</param>
    /// <param name="sevenZipPath">Optional path to 7z.exe (required for .tar.xz and .msi).</param>
    public ArchiveExtractorService(ILogger logger, string? sevenZipPath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sevenZipPath = sevenZipPath;
    }

    /// <summary>
    /// Extracts an archive to a destination directory.
    /// </summary>
    /// <param name="archivePath">Path to the archive file.</param>
    /// <param name="destinationPath">Directory to extract to.</param>
    /// <param name="displayName">Optional display name for progress messages.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if extraction succeeded, false otherwise.</returns>
    public async Task<bool> ExtractArchiveAsync(
        string archivePath,
        string destinationPath,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(archivePath))
            throw new ArgumentException("Archive path cannot be null or empty", nameof(archivePath));
        if (string.IsNullOrEmpty(destinationPath))
            throw new ArgumentException("Destination path cannot be null or empty", nameof(destinationPath));
        if (!File.Exists(archivePath))
            throw new FileNotFoundException($"Archive not found: {archivePath}", archivePath);

        displayName ??= Path.GetFileName(archivePath);
        var extension = Path.GetExtension(archivePath).ToLowerInvariant();

        _logger.Status($"Extracting {displayName}...");

        try
        {
            Directory.CreateDirectory(destinationPath);

            var success = extension switch
            {
                ".zip" => await ExtractZipAsync(archivePath, destinationPath, cancellationToken),
                ".xz" when archivePath.EndsWith(".tar.xz", StringComparison.OrdinalIgnoreCase)
                    => await ExtractTarXzAsync(archivePath, destinationPath, cancellationToken),
                ".msi" => await ExtractMsiAsync(archivePath, destinationPath, cancellationToken),
                _ => throw new NotSupportedException($"Unsupported archive format: {extension}")
            };

            if (success)
            {
                _logger.Success($"Extracted {displayName}");
            }

            return success;
        }
        catch (NotSupportedException ex)
        {
            _logger.Failure(ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.Failure($"Extraction failed for {displayName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Extracts a .zip archive using .NET's built-in ZipFile.
    /// </summary>
    private Task<bool> ExtractZipAsync(string archivePath, string destinationPath, CancellationToken cancellationToken)
    {
        try
        {
            ZipFile.ExtractToDirectory(archivePath, destinationPath, overwriteFiles: true);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.Failure($"ZIP extraction failed: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Extracts a .tar.xz archive using 7-Zip (two-pass extraction).
    /// </summary>
    private async Task<bool> ExtractTarXzAsync(string archivePath, string destinationPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_sevenZipPath) || !File.Exists(_sevenZipPath))
        {
            _logger.Failure("7-Zip is required to extract .tar.xz archives but was not found");
            return false;
        }

        try
        {
            var workDir = Path.GetDirectoryName(archivePath) ?? Path.GetTempPath();
            var tarPath = Path.Combine(workDir, Path.GetFileNameWithoutExtension(archivePath));

            // First pass: .tar.xz -> .tar
            _logger.Info("Decompressing .xz archive...");
            if (!await Run7ZipAsync(_sevenZipPath, $"x \"{archivePath}\" -o\"{workDir}\" -y", cancellationToken))
            {
                return false;
            }

            // Second pass: .tar -> files
            _logger.Info("Extracting .tar archive...");
            if (!await Run7ZipAsync(_sevenZipPath, $"x \"{tarPath}\" -o\"{destinationPath}\" -y", cancellationToken))
            {
                return false;
            }

            // Clean up intermediate .tar file
            if (File.Exists(tarPath))
            {
                File.Delete(tarPath);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Failure($"TAR.XZ extraction failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Extracts an .msi installer using 7-Zip or msiexec.
    /// </summary>
    private async Task<bool> ExtractMsiAsync(string archivePath, string destinationPath, CancellationToken cancellationToken)
    {
        try
        {
            // Create temporary extraction directory
            var tempExtractDir = Path.Combine(Path.GetTempPath(), $"msi_extract_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempExtractDir);

            try
            {
                // Use msiexec for administrative install (extracts files)
                _logger.Info("Extracting MSI using msiexec...");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "msiexec",
                    Arguments = $"/a \"{archivePath}\" /qn TARGETDIR=\"{tempExtractDir}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    _logger.Failure("Failed to start msiexec process");
                    return false;
                }

                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    _logger.Failure($"msiexec failed with exit code {process.ExitCode}");
                    return false;
                }

                // MSI typically extracts to Files/<ProductName> subdirectory
                // Find the actual files and move them to destination
                var filesDir = Path.Combine(tempExtractDir, "Files");
                if (Directory.Exists(filesDir))
                {
                    var subDirs = Directory.GetDirectories(filesDir);
                    if (subDirs.Length > 0)
                    {
                        // Move files from first subdirectory to destination
                        CopyDirectory(subDirs[0], destinationPath);
                    }
                    else
                    {
                        // No subdirectory, copy from Files directly
                        CopyDirectory(filesDir, destinationPath);
                    }
                }
                else
                {
                    // No Files directory, copy everything
                    CopyDirectory(tempExtractDir, destinationPath);
                }

                return true;
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempExtractDir))
                {
                    try
                    {
                        Directory.Delete(tempExtractDir, recursive: true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Failure($"MSI extraction failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Runs 7-Zip with specified arguments.
    /// </summary>
    private async Task<bool> Run7ZipAsync(string sevenZipPath, string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = sevenZipPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.Failure("Failed to start 7-Zip process");
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                _logger.Failure($"7-Zip failed with exit code {process.ExitCode}: {error}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Failure($"7-Zip execution failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Recursively copies a directory.
    /// </summary>
    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destPath = Path.Combine(destDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(file, destPath, overwrite: true);
        }
    }
}
