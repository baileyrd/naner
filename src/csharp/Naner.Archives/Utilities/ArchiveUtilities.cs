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
    /// Uses robocopy on Windows for better handling of symlinks and special files.
    /// </summary>
    /// <param name="targetDir">The parent directory</param>
    /// <param name="subDir">The subdirectory whose contents should be moved up</param>
    private static void FlattenDirectory(string targetDir, string subDir)
    {
        // Strategy: Rename parent to temp, rename inner to final name, delete temp
        // This avoids copying large directory trees and handles symlinks better
        var parentDir = Path.GetDirectoryName(targetDir)!;
        var targetName = Path.GetFileName(targetDir);
        var tempParentName = targetName + "_flatten_temp";
        var tempParentPath = Path.Combine(parentDir, tempParentName);

        // Step 1: Rename the target directory (e.g., msys64 -> msys64_flatten_temp)
        Directory.Move(targetDir, tempParentPath);

        // Step 2: Get the inner directory path (e.g., msys64_flatten_temp/msys64)
        var innerDirName = Path.GetFileName(subDir);
        var innerDirPath = Path.Combine(tempParentPath, innerDirName);

        // Step 3: Move the inner directory to the final location (e.g., msys64_flatten_temp/msys64 -> msys64)
        Directory.Move(innerDirPath, targetDir);

        // Step 4: Delete the now-empty temp parent directory
        try
        {
            Directory.Delete(tempParentPath, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors - the important move succeeded
        }
    }
}
