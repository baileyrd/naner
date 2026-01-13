using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Naner.Vendors.Models;

namespace Naner.Vendors.Services;

/// <summary>
/// Loads vendor configuration from vendors.json file.
/// Provides fallback to default vendors if configuration is missing or invalid.
/// </summary>
public class VendorConfigurationLoader
{
    private readonly string _configPath;
    private readonly string _vendorDir;
    private readonly ILogger _logger;
    private List<VendorDefinition>? _cachedVendors;

    public VendorConfigurationLoader(string nanerRoot, ILogger logger)
    {
        _configPath = Path.Combine(nanerRoot, NanerConstants.DirectoryNames.Config, NanerConstants.VendorsConfigFileName);
        _vendorDir = Path.Combine(nanerRoot, NanerConstants.DirectoryNames.Vendor);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads vendor definitions from configuration file.
    /// Falls back to default vendors if file doesn't exist or is invalid.
    /// </summary>
    public List<VendorDefinition> LoadVendors()
    {
        if (_cachedVendors != null)
        {
            return _cachedVendors;
        }

        if (!File.Exists(_configPath))
        {
            _logger.Warning($"Vendor configuration not found: {_configPath}");
            _logger.Info("Using default vendor definitions");
            _cachedVendors = GetDefaultVendors();
            return _cachedVendors;
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

            var root = JsonSerializer.Deserialize<VendorsJsonRoot>(json, options);

            if (root?.Vendors == null || root.Vendors.Count == 0)
            {
                _logger.Warning("Vendor configuration is empty");
                _logger.Info("Using default vendor definitions");
                _cachedVendors = GetDefaultVendors();
                return _cachedVendors;
            }

            _cachedVendors = ConvertToVendorDefinitions(root.Vendors);
            _logger.Debug($"Loaded {_cachedVendors.Count} vendor definitions from configuration", true);
            return _cachedVendors;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to load vendor configuration: {ex.Message}");
            _logger.Info("Using default vendor definitions");
            _cachedVendors = GetDefaultVendors();
            return _cachedVendors;
        }
    }

    /// <summary>
    /// Gets all optional (non-required) vendors.
    /// </summary>
    public List<VendorDefinition> GetOptionalVendors()
    {
        return LoadVendors().Where(v => !v.Required).ToList();
    }

    /// <summary>
    /// Gets all essential (required) vendors.
    /// </summary>
    public List<VendorDefinition> GetEssentialVendors()
    {
        return LoadVendors().Where(v => v.Required).ToList();
    }

    /// <summary>
    /// Gets a vendor by its key (case-insensitive).
    /// </summary>
    public VendorDefinition? GetVendorByKey(string key)
    {
        return LoadVendors().FirstOrDefault(v =>
            v.Key.Equals(key, StringComparison.OrdinalIgnoreCase) ||
            v.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a vendor is installed by looking for its extract directory.
    /// </summary>
    public bool IsVendorInstalled(VendorDefinition vendor)
    {
        var targetDir = Path.Combine(_vendorDir, vendor.ExtractDir);
        return Directory.Exists(targetDir) && Directory.GetFileSystemEntries(targetDir).Length > 0;
    }

    /// <summary>
    /// Converts JSON vendor entries to VendorDefinition objects.
    /// </summary>
    private List<VendorDefinition> ConvertToVendorDefinitions(Dictionary<string, VendorJsonEntry> vendors)
    {
        var definitions = new List<VendorDefinition>();

        foreach (var kvp in vendors)
        {
            var entry = kvp.Value;
            var definition = new VendorDefinition
            {
                Key = kvp.Key,
                Name = entry.Name,
                Description = entry.Description,
                ExtractDir = entry.ExtractDir,
                Enabled = entry.Enabled,
                Required = entry.Required,
                Dependencies = entry.Dependencies ?? new List<string>()
            };

            // Parse release source
            if (entry.ReleaseSource != null)
            {
                var source = entry.ReleaseSource;
                definition.SourceType = ParseSourceType(source.Type);

                switch (definition.SourceType)
                {
                    case VendorSourceType.GitHub:
                        if (!string.IsNullOrEmpty(source.Repo))
                        {
                            var parts = source.Repo.Split('/');
                            if (parts.Length == 2)
                            {
                                definition.GitHubOwner = parts[0];
                                definition.GitHubRepo = parts[1];
                            }
                        }
                        definition.AssetPattern = source.AssetPattern;
                        break;

                    case VendorSourceType.WebScrape:
                        definition.WebScrapeConfig = new WebScrapeConfig
                        {
                            Url = source.Url ?? "",
                            Pattern = source.Pattern ?? "",
                            BaseUrl = GetBaseUrl(source.Url)
                        };
                        break;

                    case VendorSourceType.StaticUrl:
                        definition.StaticUrl = source.Url;
                        definition.FileName = source.FileName;
                        break;
                }

                // Set fallback URL
                if (source.Fallback != null)
                {
                    definition.FallbackUrl = source.Fallback.Url;
                }
            }

            // Parse installer configuration
            definition.InstallType = entry.InstallType;
            definition.InstallerArgs = entry.InstallerArgs;

            definitions.Add(definition);
        }

        return definitions;
    }

    /// <summary>
    /// Parses source type string to enum.
    /// </summary>
    private VendorSourceType ParseSourceType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "github" => VendorSourceType.GitHub,
            "web-scrape" => VendorSourceType.WebScrape,
            "static" => VendorSourceType.StaticUrl,
            "golang-api" => VendorSourceType.StaticUrl, // Treat as static with fallback
            _ => VendorSourceType.StaticUrl
        };
    }

    /// <summary>
    /// Extracts base URL from a full URL.
    /// </summary>
    private string GetBaseUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return "";

        try
        {
            var uri = new Uri(url);
            return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf('/') + 1)}";
        }
        catch
        {
            return url;
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
