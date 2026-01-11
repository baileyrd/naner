using System;
using System.IO;
using Naner.Common;
using Naner.Configuration;

namespace Naner.Launcher.Commands;

/// <summary>
/// Runs system diagnostics to verify Naner installation.
/// </summary>
public class DiagnosticsCommand : ICommand
{
    public int Execute(string[] args)
    {
        Logger.Header("Naner Diagnostics");
        Console.WriteLine($"Version: {NanerConstants.Version}");
        Console.WriteLine($"Phase: {NanerConstants.PhaseName}");
        Logger.NewLine();

        // Executable location
        Logger.Status("Executable Information:");
        Logger.Info($"  Location: {AppContext.BaseDirectory}");
        Logger.Info($"  Command Line: {Environment.CommandLine}");
        Logger.NewLine();

        // NANER_ROOT search
        Logger.Status("Searching for NANER_ROOT...");
        try
        {
            var nanerRoot = PathResolver.FindNanerRoot();
            Logger.Success($"  Found: {nanerRoot}");
            Logger.NewLine();

            // Verify structure
            VerifyDirectoryStructure(nanerRoot);

            // Config check
            CheckConfiguration(nanerRoot);

            // Environment variables
            CheckEnvironmentVariables();

            Logger.NewLine();
            Logger.Success("Diagnostics complete - Naner installation appears healthy");
            return 0;
        }
        catch (Exception ex)
        {
            Logger.Failure("NANER_ROOT not found");
            Logger.NewLine();
            Logger.Info("Details:");
            Logger.Info($"  {ex.Message}");
            Logger.NewLine();
            Logger.Info("This usually means:");
            Logger.Info("  1. You're running naner.exe outside the Naner directory");
            Logger.Info("  2. The Naner directory structure is incomplete");
            Logger.Info("  3. You need to set NANER_ROOT environment variable");
            Logger.NewLine();
            Logger.Info("Try:");
            Logger.Info("  1. cd <your-naner-directory>");
            Logger.Info("  2. .\\vendor\\bin\\naner.exe --diagnose");
            return 1;
        }
    }

    private void VerifyDirectoryStructure(string nanerRoot)
    {
        Logger.Status("Verifying directory structure:");
        var dirs = new[] { "bin", "vendor", "config", "home" };
        foreach (var dir in dirs)
        {
            var path = Path.Combine(nanerRoot, dir);
            var exists = Directory.Exists(path);
            var symbol = exists ? "✓" : "✗";
            var color = exists ? ConsoleColor.Green : ConsoleColor.Red;

            Console.ForegroundColor = color;
            Console.WriteLine($"  {symbol} {dir}/");
            Console.ResetColor();
        }
        Logger.NewLine();
    }

    private void CheckConfiguration(string nanerRoot)
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

                CheckVendorPaths(config);
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

    private void CheckVendorPaths(NanerConfig config)
    {
        Logger.Status("Vendor Paths:");

        CheckVendorPath(config, "WindowsTerminal");
        CheckVendorPath(config, "PowerShell");
        CheckVendorPath(config, "GitBash");
    }

    private void CheckVendorPath(NanerConfig config, string vendorName)
    {
        if (config.VendorPaths.TryGetValue(vendorName, out var vendorPath))
        {
            var exists = File.Exists(vendorPath);
            var symbol = exists ? "✓" : "✗";
            var color = exists ? ConsoleColor.Green : ConsoleColor.Red;
            Console.ForegroundColor = color;
            Console.WriteLine($"  {symbol} {vendorName}: {vendorPath}");
            Console.ResetColor();
        }
    }

    private void CheckEnvironmentVariables()
    {
        Logger.Status("Environment Variables:");
        var envVars = new[] { "NANER_ROOT", "NANER_ENVIRONMENT", "HOME", "PATH" };
        foreach (var envVar in envVars)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            if (value != null)
            {
                if (envVar == "PATH")
                {
                    value = value.Substring(0, Math.Min(100, value.Length)) + "...";
                }
                Logger.Info($"  {envVar}={value}");
            }
            else
            {
                Logger.Info($"  {envVar}=(not set)");
            }
        }
    }
}
