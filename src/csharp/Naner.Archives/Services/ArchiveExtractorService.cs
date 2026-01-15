using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Naner.Archives.Abstractions;
using Naner.Archives.Extractors;

namespace Naner.Archives.Services;

/// <summary>
/// Service for extracting various archive formats using the Strategy pattern.
/// Supports .zip, .7z, .tar.xz, .msi, and other formats.
/// </summary>
public class ArchiveExtractorService
{
    private readonly List<IArchiveExtractor> _extractors;

    /// <summary>
    /// Creates a new ArchiveExtractorService.
    /// </summary>
    /// <param name="sevenZipPath">Optional path to 7z.exe (required for .tar.xz).</param>
    /// <param name="installerArgs">Optional custom installer arguments for .exe files.</param>
    /// <param name="vendorHint">Optional vendor hint for determining default installer args.</param>
    public ArchiveExtractorService(string? sevenZipPath = null, List<string>? installerArgs = null, string? vendorHint = null)
    {
        _extractors = new List<IArchiveExtractor>
        {
            new ZipExtractor(),
            new MsiExtractor(),
            new ExeInstallerExtractor(installerArgs, vendorHint)
        };

        // Only add 7-Zip based extractors if 7-Zip is available
        if (!string.IsNullOrEmpty(sevenZipPath) && File.Exists(sevenZipPath))
        {
            _extractors.Add(new TarXzExtractor(sevenZipPath));
            _extractors.Add(new SevenZipExtractor(sevenZipPath));
        }
    }

    /// <summary>
    /// Extracts an archive to a destination directory using the Strategy pattern.
    /// </summary>
    /// <param name="archivePath">Path to the archive file.</param>
    /// <param name="destinationPath">Directory to extract to.</param>
    /// <param name="vendorName">Name of the vendor (for logging).</param>
    /// <returns>True if extraction succeeded, false otherwise.</returns>
    public bool ExtractArchive(string archivePath, string destinationPath, string vendorName)
    {
        if (string.IsNullOrEmpty(archivePath))
            throw new ArgumentException("Archive path cannot be null or empty", nameof(archivePath));
        if (string.IsNullOrEmpty(destinationPath))
            throw new ArgumentException("Destination path cannot be null or empty", nameof(destinationPath));
        if (!File.Exists(archivePath))
            throw new FileNotFoundException($"Archive not found: {archivePath}", archivePath);

        try
        {
            // Find an extractor that can handle this archive
            var extractor = _extractors.FirstOrDefault(e => e.CanExtract(archivePath));

            if (extractor == null)
            {
                var extension = Path.GetExtension(archivePath);
                Logger.Warning($"    Unsupported archive format: {extension}");
                return false;
            }

            return extractor.Extract(archivePath, destinationPath, vendorName);
        }
        catch (Exception ex)
        {
            Logger.Failure($"    Extraction error: {ex.Message}");
            return false;
        }
    }

}
