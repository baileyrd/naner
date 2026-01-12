using System;
using System.IO;
using System.IO.Compression;
using Naner.Archives.Abstractions;
using Naner.Archives.Utilities;

namespace Naner.Archives.Extractors;

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
            ArchiveUtilities.FlattenSingleSubdirectory(targetDir);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"    ZIP extraction failed: {ex.Message}");
            return false;
        }
    }
}
