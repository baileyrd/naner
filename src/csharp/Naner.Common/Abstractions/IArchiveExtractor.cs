namespace Naner.Common.Abstractions;

/// <summary>
/// Interface for archive extraction strategies.
/// Follows the Strategy pattern to handle different archive formats.
/// </summary>
public interface IArchiveExtractor
{
    /// <summary>
    /// Determines if this extractor can handle the given archive file.
    /// </summary>
    /// <param name="archivePath">Path to the archive file</param>
    /// <returns>True if this extractor supports the archive format</returns>
    bool CanExtract(string archivePath);

    /// <summary>
    /// Extracts the archive to the target directory.
    /// </summary>
    /// <param name="archivePath">Path to the archive file</param>
    /// <param name="targetDir">Directory to extract contents to</param>
    /// <param name="vendorName">Name of the vendor (for logging)</param>
    /// <returns>True if extraction succeeded, false otherwise</returns>
    bool Extract(string archivePath, string targetDir, string vendorName);
}
