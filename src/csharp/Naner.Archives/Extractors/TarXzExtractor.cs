using System;
using System.Diagnostics;
using System.IO;
using Naner.Archives.Abstractions;
using Naner.Archives.Utilities;

namespace Naner.Archives.Extractors;

/// <summary>
/// Extractor for .tar.xz archives.
/// Requires 7-Zip to be installed first (two-stage extraction: .xz -> .tar -> files).
/// </summary>
public class TarXzExtractor : IArchiveExtractor
{
    private readonly string _sevenZipPath;

    public TarXzExtractor(string sevenZipPath)
    {
        _sevenZipPath = sevenZipPath ?? throw new ArgumentNullException(nameof(sevenZipPath));
    }

    public bool CanExtract(string archivePath)
    {
        return archivePath.EndsWith(".tar.xz", StringComparison.OrdinalIgnoreCase);
    }

    public bool Extract(string archivePath, string targetDir, string vendorName)
    {
        try
        {
            if (!File.Exists(_sevenZipPath))
            {
                Logger.Warning($"    7-Zip not found at {_sevenZipPath}");
                Logger.Info($"    {vendorName} downloaded to: {archivePath}");
                Logger.Info($"    Please extract manually to: {targetDir}");
                return false;
            }

            Directory.CreateDirectory(targetDir);

            // Step 1: Extract .xz to get .tar
            Logger.Info($"    Extracting .xz archive...");
            var tarPath = archivePath.Replace(".tar.xz", ".tar");

            if (!ExtractXz(archivePath, tarPath))
            {
                return false;
            }

            // Step 2: Extract .tar to target directory
            Logger.Info($"    Extracting .tar archive...");

            if (!ExtractTar(tarPath, targetDir))
            {
                return false;
            }

            // Cleanup intermediate .tar file
            CleanupTarFile(tarPath);

            // Flatten single subdirectory if present
            ArchiveUtilities.FlattenSingleSubdirectory(targetDir);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"    .tar.xz extraction failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Extracts .xz archive to .tar file.
    /// </summary>
    private bool ExtractXz(string xzPath, string tarPath)
    {
        return RunSevenZip($"e \"{xzPath}\" -o\"{Path.GetDirectoryName(xzPath)}\" -y", "extract .xz");
    }

    /// <summary>
    /// Extracts .tar archive to target directory.
    /// </summary>
    private bool ExtractTar(string tarPath, string targetDir)
    {
        return RunSevenZip($"x \"{tarPath}\" -o\"{targetDir}\" -y", "extract .tar");
    }

    /// <summary>
    /// Runs 7-Zip with the specified arguments, properly handling stdout/stderr to prevent deadlocks.
    /// </summary>
    private bool RunSevenZip(string arguments, string operationName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _sevenZipPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            Logger.Warning($"    Failed to start 7-Zip for {operationName}");
            return false;
        }

        // Read stdout/stderr asynchronously to prevent buffer deadlock
        // This is critical for large archives like MSYS2 which output thousands of file names
        process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Logger.Warning($"    Failed to {operationName} (exit code {process.ExitCode})");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Removes intermediate .tar file after extraction.
    /// </summary>
    private static void CleanupTarFile(string tarPath)
    {
        try
        {
            if (File.Exists(tarPath))
            {
                File.Delete(tarPath);
            }
        }
        catch (Exception ex)
        {
            // Non-critical - ignore cleanup errors but log for diagnostics
            Logger.Debug($"Could not delete temporary tar file '{tarPath}': {ex.Message}", debugMode: false);
        }
    }
}
