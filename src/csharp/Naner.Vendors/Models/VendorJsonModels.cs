using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Naner.Vendors.Models;

/// <summary>
/// Root object for vendors.json file deserialization.
/// </summary>
public class VendorsJsonRoot
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("vendors")]
    public Dictionary<string, VendorJsonEntry>? Vendors { get; set; }

    [JsonPropertyName("metadata")]
    public VendorMetadata? Metadata { get; set; }
}

/// <summary>
/// Individual vendor entry in vendors.json.
/// </summary>
public class VendorJsonEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("extractDir")]
    public string ExtractDir { get; set; } = "";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("dependencies")]
    public List<string>? Dependencies { get; set; }

    [JsonPropertyName("releaseSource")]
    public ReleaseSourceJson? ReleaseSource { get; set; }

    [JsonPropertyName("postInstallFunction")]
    public string? PostInstallFunction { get; set; }

    [JsonPropertyName("packages")]
    public List<string>? Packages { get; set; }

    [JsonPropertyName("installType")]
    public string? InstallType { get; set; }

    [JsonPropertyName("installerArgs")]
    public List<string>? InstallerArgs { get; set; }
}

/// <summary>
/// Release source configuration in vendors.json.
/// </summary>
public class ReleaseSourceJson
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    // For GitHub releases
    [JsonPropertyName("repo")]
    public string? Repo { get; set; }

    [JsonPropertyName("assetPattern")]
    public string? AssetPattern { get; set; }

    // For web scraping
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    // For static URLs
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    // Fallback configuration
    [JsonPropertyName("fallback")]
    public FallbackJson? Fallback { get; set; }
}

/// <summary>
/// Fallback URL configuration.
/// </summary>
public class FallbackJson
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("size")]
    public string? Size { get; set; }
}

/// <summary>
/// Metadata section of vendors.json.
/// </summary>
public class VendorMetadata
{
    [JsonPropertyName("createdDate")]
    public string? CreatedDate { get; set; }

    [JsonPropertyName("lastModified")]
    public string? LastModified { get; set; }

    [JsonPropertyName("configVersion")]
    public string? ConfigVersion { get; set; }

    [JsonPropertyName("notes")]
    public List<string>? Notes { get; set; }
}
