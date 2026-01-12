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
    /// </summary>
    /// <param name="nanerRoot">Naner root directory path</param>
    public void Verify(string nanerRoot)
    {
        var configPath = Path.Combine(nanerRoot, "config", "naner.json");
        if (File.Exists(configPath))
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
            Logger.Failure($"Configuration file missing: {configPath}");
        }
        Logger.NewLine();
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
