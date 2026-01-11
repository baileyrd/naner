using System;
using System.Diagnostics;
using System.IO;
using Naner.Archives.Abstractions;

namespace Naner.Archives.Extractors;

/// <summary>
/// Extractor for MSI installer packages.
/// Uses msiexec administrative installation to extract contents.
/// </summary>
public class MsiExtractor : IArchiveExtractor
{
    public bool CanExtract(string archivePath)
    {
        return Path.GetExtension(archivePath).Equals(".msi", StringComparison.OrdinalIgnoreCase);
    }

    public bool Extract(string archivePath, string targetDir, string vendorName)
    {
        try
        {
            Directory.CreateDirectory(targetDir);

            var startInfo = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = $"/a \"{archivePath}\" /qn TARGETDIR=\"{targetDir}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();

            // MSI extracts to Files/7-Zip subdirectory, move contents up
            var filesDir = Path.Combine(targetDir, "Files", "7-Zip");
            if (Directory.Exists(filesDir))
            {
                MoveFilesToRoot(filesDir, targetDir);

                // Cleanup
                var parentFilesDir = Path.Combine(targetDir, "Files");
                if (Directory.Exists(parentFilesDir))
                {
                    Directory.Delete(parentFilesDir, recursive: true);
                }
            }

            return process?.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Logger.Failure($"    MSI extraction failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Moves all files from source directory to target directory root.
    /// </summary>
    private static void MoveFilesToRoot(string sourceDir, string targetDir)
    {
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Move(file, destFile, overwrite: true);
        }
    }
}
