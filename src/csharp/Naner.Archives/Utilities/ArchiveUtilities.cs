using System;
using System.IO;

namespace Naner.Archives.Utilities;

/// <summary>
/// Shared utility methods for archive extraction operations.
/// Eliminates duplication between different archive extractors.
/// </summary>
public static class ArchiveUtilities
{
    /// <summary>
    /// Flattens a single subdirectory by moving its contents up one level.
    /// Common operation when archives contain a single root folder.
    /// </summary>
    /// <param name="targetDir">The target directory to flatten</param>
    public static void FlattenSingleSubdirectory(string targetDir)
    {
        var entries = Directory.GetFileSystemEntries(targetDir);
        if (entries.Length == 1 && Directory.Exists(entries[0]))
        {
            FlattenDirectory(targetDir, entries[0]);
        }
    }

    /// <summary>
    /// Moves all contents from a subdirectory up to the target directory.
    /// </summary>
    /// <param name="targetDir">The parent directory</param>
    /// <param name="subDir">The subdirectory whose contents should be moved up</param>
    private static void FlattenDirectory(string targetDir, string subDir)
    {
        var tempDir = targetDir + "_temp";

        // Move subdirectory to temp location
        Directory.Move(subDir, tempDir);

        // Move all files from temp to target
        foreach (var file in Directory.GetFiles(tempDir))
        {
            var destFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Move(file, destFile, overwrite: true);
        }

        // Move all directories from temp to target
        foreach (var dir in Directory.GetDirectories(tempDir))
        {
            var destDir = Path.Combine(targetDir, Path.GetFileName(dir));

            // If destination exists, delete it first (handles case where archive
            // has nested folder with same name as target, e.g. msys64/msys64)
            if (Directory.Exists(destDir))
            {
                Directory.Delete(destDir, recursive: true);
            }

            Directory.Move(dir, destDir);
        }

        // Remove temp directory
        Directory.Delete(tempDir, recursive: true);
    }
}
