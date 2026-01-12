using System;
using System.Diagnostics;
using System.IO;
using Naner.Archives.Abstractions;
using Naner.Archives.Utilities;

namespace Naner.Archives.Extractors;

/// <summary>
/// Extractor for .tar.xz archives.
/// Uses 7-Zip with two-step extraction: .xz -> .tar -> files.
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

            // Step 1: Extract .xz to get .tar (extracts to same directory as source)
            Logger.Info($"    Extracting .xz...");
            var archiveDir = Path.GetDirectoryName(archivePath)!;
            if (!RunSevenZip($"x \"{archivePath}\" -o\"{archiveDir}\" -y", "extract .xz"))
            {
                return false;
            }

            // Find the .tar file that was extracted
            var tarPath = archivePath.Replace(".tar.xz", ".tar", StringComparison.OrdinalIgnoreCase);
            if (!File.Exists(tarPath))
            {
                Logger.Warning($"    .tar file not found after extraction");
                return false;
            }

            // Step 2: Extract .tar to target directory
            Logger.Info($"    Extracting .tar...");
            if (!RunSevenZip($"x \"{tarPath}\" -o\"{targetDir}\" -y", "extract .tar"))
            {
                return false;
            }

            // Cleanup intermediate .tar file
            try { File.Delete(tarPath); } catch { /* ignore */ }

            // Flatten single subdirectory if present (e.g., msys64/msys64 -> msys64)
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

        // Read stdout/stderr to prevent buffer deadlock
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
}
