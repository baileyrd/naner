namespace Naner.Vendors.Models;

/// <summary>
/// Defines how to fetch and install a vendor package.
/// </summary>
public class VendorDefinition
{
    public string Name { get; set; } = "";
    public string ExtractDir { get; set; } = "";
    public VendorSourceType SourceType { get; set; }

    // For static URLs
    public string? StaticUrl { get; set; }
    public string? FileName { get; set; }

    // For GitHub releases
    public string? GitHubOwner { get; set; }
    public string? GitHubRepo { get; set; }
    public string? AssetPattern { get; set; }
    public string? AssetPatternEnd { get; set; }

    // For web scraping
    public WebScrapeConfig? WebScrapeConfig { get; set; }

    // Fallback
    public string? FallbackUrl { get; set; }
}

public enum VendorSourceType
{
    StaticUrl,
    GitHub,
    WebScrape
}

public class WebScrapeConfig
{
    public string Url { get; set; } = "";
    public string Pattern { get; set; } = "";
    public string BaseUrl { get; set; } = "";
}
