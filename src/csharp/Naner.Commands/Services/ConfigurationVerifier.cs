using System;
using System.IO;
using Naner.Commands.Abstractions;
using Naner.Configuration;

namespace Naner.Commands.Services;

/// <summary>
/// Verifies Naner configuration file validity.
/// Single Responsibility: Configuration validation.
/// </summary>
public class ConfigurationVerifier : IConfigurationVerifier
{
    /// <summary>
    /// Verifies configuration file exists and is valid.
    /// Supports both JSON and YAML configuration files.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory path</param>
    public void Verify(string nanerRoot)
    {
        var configPath = FindConfigFile(nanerRoot);
        if (configPath != null)
        {
            Logger.Success($"Configuration file found");
            Logger.Info($"  Path: {configPath}");
            try
            {
                var configManager = new ConfigurationManager(nanerRoot);
                var config = configManager.Load(configPath);
                Logger.Info($"  Default Profile: {config.DefaultProfile}");
                Logger.Info($"  Vendor Paths: {config.VendorPaths.Count}");
                Logger.Info($"  Profiles: {config.Profiles.Count}");
                Logger.NewLine();

                VerifyVendorPaths(config);
            }
            catch (Exception ex)
            {
                Logger.Failure($"Configuration error: {ex.Message}");
            }
        }
        else
        {
            Logger.Failure($"Configuration file missing. Expected one of: naner.json, naner.yaml, or naner.yml in config/");
        }
        Logger.NewLine();
    }

    /// <summary>
    /// Finds the first available configuration file.
    /// </summary>
    private string? FindConfigFile(string nanerRoot)
    {
        var configDir = Path.Combine(nanerRoot, "config");
        var configFileNames = new[] { "naner.json", "naner.yaml", "naner.yml" };

        foreach (var fileName in configFileNames)
        {
            var fullPath = Path.Combine(configDir, fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Verifies vendor paths exist and are accessible.
    /// </summary>
    private void VerifyVendorPaths(NanerConfig config)
    {
        Logger.Status("Vendor Paths:");

        var vendorsToCheck = new[] { "WindowsTerminal", "PowerShell", "GitBash" };

        foreach (var vendorKey in vendorsToCheck)
        {
            if (config.VendorPaths.TryGetValue(vendorKey, out var vendorPath))
            {
                var exists = File.Exists(vendorPath);
                var symbol = exists ? "✓" : "✗";
                var color = exists ? ConsoleColor.Green : ConsoleColor.Red;

                Console.ForegroundColor = color;
                Console.WriteLine($"  {symbol} {vendorKey}: {vendorPath}");
                Console.ResetColor();
            }
        }
    }
}
