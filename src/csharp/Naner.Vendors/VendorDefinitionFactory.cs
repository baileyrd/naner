using Naner.Vendors.Models;

namespace Naner.Vendors;

/// <summary>
/// Factory for creating standard vendor definitions.
/// Centralizes vendor configuration to eliminate duplication.
/// </summary>
public static class VendorDefinitionFactory
{
    /// <summary>
    /// Creates vendor definitions for all essential vendors (7-Zip, PowerShell, Windows Terminal, MSYS2).
    /// </summary>
    public static VendorDefinition[] GetEssentialVendors()
    {
        return new[]
        {
            Create7ZipDefinition(),
            CreatePowerShellDefinition(),
            CreateWindowsTerminalDefinition(),
            CreateMSYS2Definition()
        };
    }

    /// <summary>
    /// Creates 7-Zip vendor definition with web scraping for latest version.
    /// </summary>
    public static VendorDefinition Create7ZipDefinition()
    {
        return new VendorDefinition
        {
            Name = NanerConstants.VendorNames.SevenZip,
            ExtractDir = "7zip",
            SourceType = VendorSourceType.WebScrape,
            WebScrapeConfig = new WebScrapeConfig
            {
                Url = NanerConstants.VendorWebScrape.SevenZip.Url,
                Pattern = NanerConstants.VendorWebScrape.SevenZip.Pattern,
                BaseUrl = NanerConstants.VendorWebScrape.SevenZip.BaseUrl
            },
            FallbackUrl = NanerConstants.VendorFallbackUrls.SevenZip
        };
    }

    /// <summary>
    /// Creates PowerShell vendor definition with GitHub releases.
    /// </summary>
    public static VendorDefinition CreatePowerShellDefinition()
    {
        return new VendorDefinition
        {
            Name = NanerConstants.VendorNames.PowerShell,
            ExtractDir = "powershell",
            SourceType = VendorSourceType.GitHub,
            GitHubOwner = "PowerShell",
            GitHubRepo = "PowerShell",
            AssetPattern = "win-x64.zip",
            FallbackUrl = NanerConstants.VendorFallbackUrls.PowerShell
        };
    }

    /// <summary>
    /// Creates Windows Terminal vendor definition with GitHub releases.
    /// </summary>
    public static VendorDefinition CreateWindowsTerminalDefinition()
    {
        return new VendorDefinition
        {
            Name = NanerConstants.VendorNames.WindowsTerminal,
            ExtractDir = "terminal",
            SourceType = VendorSourceType.GitHub,
            GitHubOwner = "microsoft",
            GitHubRepo = "terminal",
            AssetPattern = "Microsoft.WindowsTerminal_",
            AssetPatternEnd = "_x64.zip",
            FallbackUrl = NanerConstants.VendorFallbackUrls.WindowsTerminal
        };
    }

    /// <summary>
    /// Creates MSYS2 vendor definition with web scraping for latest version.
    /// </summary>
    public static VendorDefinition CreateMSYS2Definition()
    {
        return new VendorDefinition
        {
            Name = NanerConstants.VendorNames.MSYS2,
            ExtractDir = "msys64",
            SourceType = VendorSourceType.WebScrape,
            WebScrapeConfig = new WebScrapeConfig
            {
                Url = NanerConstants.VendorWebScrape.MSYS2.Url,
                Pattern = NanerConstants.VendorWebScrape.MSYS2.Pattern,
                BaseUrl = NanerConstants.VendorWebScrape.MSYS2.BaseUrl
            },
            FallbackUrl = NanerConstants.VendorFallbackUrls.MSYS2
        };
    }
}
