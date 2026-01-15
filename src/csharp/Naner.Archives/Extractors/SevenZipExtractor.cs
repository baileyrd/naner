using System;
using System.Diagnostics;
using System.IO;
using Naner.Archives.Abstractions;
using Naner.Archives.Utilities;

namespace Naner.Archives.Extractors;

/// <summary>
/// Extractor for .7z archives using 7-Zip.
/// </summary>
public class SevenZipExtractor : IArchiveExtractor
{
    private readonly string _sevenZipPath;

    public SevenZipExtractor(string sevenZipPath)
    {
        _sevenZipPath = sevenZipPath ?? throw new ArgumentNullException(nameof(sevenZipPath));
    }

    public bool CanExtract(string archivePath)
    {
        return archivePath.EndsWith(".7z", StringComparison.OrdinalIgnoreCase);
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

            Logger.Info($"    Extracting .7z archive...");
            if (!RunSevenZip($"x \"{archivePath}\" -o\"{targetDir}\" -y", "extract .7z"))
            {
                return false;
            }

            // Flatten single subdirectory if present (e.g., ruby/rubyinstaller-4.0.0-1 -> ruby)
            ArchiveUtilities.FlattenSingleSubdirectory(targetDir);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"    .7z extraction failed: {ex.Message}");
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
