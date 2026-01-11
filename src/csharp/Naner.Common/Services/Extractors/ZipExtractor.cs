using System;
using System.IO;
using System.IO.Compression;
using Naner.Common.Abstractions;

namespace Naner.Common.Services.Extractors;

/// <summary>
/// Extractor for ZIP archives.
/// Handles directory flattening when a single subdirectory is present.
/// </summary>
public class ZipExtractor : IArchiveExtractor
{
    public bool CanExtract(string archivePath)
    {
        return Path.GetExtension(archivePath).Equals(".zip", StringComparison.OrdinalIgnoreCase);
    }

    public bool Extract(string archivePath, string targetDir, string vendorName)
    {
        try
        {
            Directory.CreateDirectory(targetDir);
            ZipFile.ExtractToDirectory(archivePath, targetDir, overwriteFiles: true);

            // Check if extraction created a single subdirectory (common with vendor ZIPs)
            var entries = Directory.GetFileSystemEntries(targetDir);
            if (entries.Length == 1 && Directory.Exists(entries[0]))
            {
                FlattenSingleSubdirectory(targetDir, entries[0]);
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
    /// Flattens a single subdirectory by moving its contents up one level.
    /// </summary>
    private static void FlattenSingleSubdirectory(string targetDir, string subDir)
    {
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
}
