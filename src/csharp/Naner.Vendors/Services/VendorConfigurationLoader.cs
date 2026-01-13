using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Naner.Vendors.Abstractions;
using Naner.Archives.Abstractions;
using Naner.Infrastructure.Abstractions;
using Naner.Vendors.Models;

namespace Naner.Vendors.Services;

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
    /// Delegates to VendorDefinitionFactory to avoid duplication.
    /// </summary>
    private List<VendorDefinition> GetDefaultVendors()
    {
        return new List<VendorDefinition>(VendorDefinitionFactory.GetEssentialVendors());
    }
}
