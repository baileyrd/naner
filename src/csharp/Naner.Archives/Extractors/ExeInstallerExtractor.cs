using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Naner.Archives.Abstractions;

namespace Naner.Archives.Extractors;

/// <summary>
/// Extractor for executable installers (.exe).
/// Runs the installer in silent mode with appropriate arguments.
/// </summary>
public class ExeInstallerExtractor : IArchiveExtractor
{
    private readonly List<string>? _installerArgs;
    private readonly string? _vendorHint;

    /// <summary>
    /// Creates a new ExeInstallerExtractor with optional custom installer arguments.
    /// </summary>
    /// <param name="installerArgs">Custom installer arguments (if null, uses defaults based on vendor)</param>
    /// <param name="vendorHint">Hint about which vendor this is for (to determine default args)</param>
    public ExeInstallerExtractor(List<string>? installerArgs = null, string? vendorHint = null)
    {
        _installerArgs = installerArgs;
        _vendorHint = vendorHint;
    }

    public bool CanExtract(string archivePath)
    {
        return Path.GetExtension(archivePath).Equals(".exe", StringComparison.OrdinalIgnoreCase);
    }

    public bool Extract(string archivePath, string targetDir, string vendorName)
    {
        try
        {
            Directory.CreateDirectory(targetDir);

            var arguments = BuildInstallerArguments(archivePath, targetDir, vendorName);

            Logger.Info($"    Running installer: {Path.GetFileName(archivePath)}");
            Logger.Debug($"    Arguments: {arguments}", debugMode: false);

            var startInfo = new ProcessStartInfo
            {
                FileName = archivePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            // Set environment variables for installers that need them
            SetInstallerEnvironment(startInfo, archivePath, targetDir, vendorName);

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Logger.Failure($"    Failed to start installer process");
                return false;
            }

            // Read output streams to prevent deadlock
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Logger.Failure($"    Installer exited with code {process.ExitCode}");
                if (!string.IsNullOrEmpty(stderr))
                {
                    Logger.Debug($"    Error output: {stderr}", debugMode: false);
                }
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"    Installer execution failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Builds installer arguments based on vendor-specific requirements.
    /// </summary>
    private string BuildInstallerArguments(string installerPath, string targetDir, string vendorName)
    {
        // If custom args were provided, use them (substituting placeholders)
        if (_installerArgs != null && _installerArgs.Count > 0)
        {
            var args = string.Join(" ", _installerArgs);
            // Replace common placeholders
            args = args.Replace("%TARGETDIR%", targetDir);
            args = args.Replace("$TARGETDIR", targetDir);
            return args;
        }

        // Determine default arguments based on vendor/installer type
        var fileName = Path.GetFileName(installerPath).ToLowerInvariant();
        var vendor = vendorName.ToLowerInvariant();

        // Miniconda/Anaconda installer
        if (fileName.Contains("miniconda") || fileName.Contains("anaconda") || vendor.Contains("conda"))
        {
            // Miniconda silent install: /S /D=<path>
            // /S = silent, /D= must be last and without quotes
            return $"/S /D={targetDir}";
        }

        // Rust/rustup installer
        if (fileName.Contains("rustup") || vendor.Contains("rust"))
        {
            // rustup-init.exe args
            return $"-y --default-toolchain stable --profile default --no-modify-path";
        }

        // Default: try common silent install flags
        // Many NSIS installers use /S, InnoSetup uses /VERYSILENT
        return $"/S /D=\"{targetDir}\"";
    }

    /// <summary>
    /// Sets environment variables needed by specific installers.
    /// </summary>
    private void SetInstallerEnvironment(ProcessStartInfo startInfo, string installerPath, string targetDir, string vendorName)
    {
        var fileName = Path.GetFileName(installerPath).ToLowerInvariant();
        var vendor = vendorName.ToLowerInvariant();

        // Rust/rustup needs RUSTUP_HOME and CARGO_HOME to install to custom location
        if (fileName.Contains("rustup") || vendor.Contains("rust"))
        {
            var rustupHome = Path.Combine(targetDir, ".rustup");
            var cargoHome = Path.Combine(targetDir, ".cargo");

            startInfo.Environment["RUSTUP_HOME"] = rustupHome;
            startInfo.Environment["CARGO_HOME"] = cargoHome;

            Logger.Debug($"    RUSTUP_HOME={rustupHome}", debugMode: false);
            Logger.Debug($"    CARGO_HOME={cargoHome}", debugMode: false);
        }
    }
}
