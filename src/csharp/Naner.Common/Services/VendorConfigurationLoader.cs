using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Naner.Common.Abstractions;
using Naner.Common.Models;

namespace Naner.Common.Services;

/// <summary>
/// Loads vendor configuration from vendors.json file.
/// Provides fallback to default vendors if configuration is missing or invalid.
/// </summary>
public class VendorConfigurationLoader
{
    private readonly string _configPath;
    private readonly ILogger _logger;

    public VendorConfigurationLoader(string nanerRoot, ILogger logger)
    {
        _configPath = Path.Combine(nanerRoot, NanerConstants.DirectoryNames.Config, NanerConstants.VendorsConfigFileName);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads vendor definitions from configuration file.
    /// Falls back to default vendors if file doesn't exist or is invalid.
    /// </summary>
    public List<VendorDefinition> LoadVendors()
    {
        if (!File.Exists(_configPath))
        {
            _logger.Warning($"Vendor configuration not found: {_configPath}");
            _logger.Info("Using default vendor definitions");
            return GetDefaultVendors();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var config = JsonSerializer.Deserialize<VendorConfiguration>(json, options);

            if (config?.Vendors == null || config.Vendors.Count == 0)
            {
                _logger.Warning("Vendor configuration is empty");
                _logger.Info("Using default vendor definitions");
                return GetDefaultVendors();
            }

            _logger.Debug($"Loaded {config.Vendors.Count} vendor definitions from configuration", true);
            return config.Vendors;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to load vendor configuration: {ex.Message}");
            _logger.Info("Using default vendor definitions");
            return GetDefaultVendors();
        }
    }

    /// <summary>
    /// Returns default vendor definitions (fallback).
    /// </summary>
    private List<VendorDefinition> GetDefaultVendors()
    {
        return new List<VendorDefinition>
        {
            new VendorDefinition
            {
                Name = NanerConstants.VendorNames.SevenZip,
                ExtractDir = "7zip",
                SourceType = VendorSourceType.StaticUrl,
                StaticUrl = "https://www.7-zip.org/a/7z2408-x64.msi",
                FileName = "7z2408-x64.msi"
            },
            new VendorDefinition
            {
                Name = NanerConstants.VendorNames.PowerShell,
                ExtractDir = "powershell",
                SourceType = VendorSourceType.GitHub,
                GitHubOwner = "PowerShell",
                GitHubRepo = "PowerShell",
                AssetPattern = "win-x64.zip",
                FallbackUrl = "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/PowerShell-7.4.6-win-x64.zip"
            },
            new VendorDefinition
            {
                Name = NanerConstants.VendorNames.WindowsTerminal,
                ExtractDir = "terminal",
                SourceType = VendorSourceType.GitHub,
                GitHubOwner = "microsoft",
                GitHubRepo = "terminal",
                AssetPattern = "Microsoft.WindowsTerminal_",
                AssetPatternEnd = "_x64.zip",
                FallbackUrl = "https://github.com/microsoft/terminal/releases/download/v1.21.2361.0/Microsoft.WindowsTerminal_1.21.2361.0_x64.zip"
            },
            new VendorDefinition
            {
                Name = NanerConstants.VendorNames.MSYS2,
                ExtractDir = "msys64",
                SourceType = VendorSourceType.StaticUrl,
                StaticUrl = "https://repo.msys2.org/distrib/x86_64/msys2-base-x86_64-20240727.tar.xz",
                FileName = "msys2-base-x86_64-20240727.tar.xz"
            }
        };
    }
}
