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
    /// Creates vendor definitions with static URLs (legacy mode).
    /// </summary>
    public static VendorDefinition[] GetStaticUrlVendors()
    {
        return new[]
        {
            new VendorDefinition
            {
                Name = "7-Zip",
                ExtractDir = "7zip",
                SourceType = VendorSourceType.StaticUrl,
                StaticUrl = "https://www.7-zip.org/a/7z2408-x64.msi",
                FileName = "7z2408-x64.msi",
                FallbackUrl = "https://www.7-zip.org/a/7z2408-x64.msi"
            },
            new VendorDefinition
            {
                Name = "PowerShell",
                ExtractDir = "powershell",
                SourceType = VendorSourceType.StaticUrl,
                StaticUrl = "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/PowerShell-7.4.6-win-x64.zip",
                FileName = "PowerShell-7.4.6-win-x64.zip",
                FallbackUrl = "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/PowerShell-7.4.6-win-x64.zip"
            },
            new VendorDefinition
            {
                Name = "Windows Terminal",
                ExtractDir = "terminal",
                SourceType = VendorSourceType.StaticUrl,
                StaticUrl = "https://github.com/microsoft/terminal/releases/download/v1.21.2361.0/Microsoft.WindowsTerminal_1.21.2361.0_x64.zip",
                FileName = "Microsoft.WindowsTerminal_1.21.2361.0_x64.zip",
                FallbackUrl = "https://github.com/microsoft/terminal/releases/download/v1.21.2361.0/Microsoft.WindowsTerminal_1.21.2361.0_x64.zip"
            },
            new VendorDefinition
            {
                Name = "MSYS2 (Git/Bash)",
                ExtractDir = "msys64",
                SourceType = VendorSourceType.StaticUrl,
                StaticUrl = "https://repo.msys2.org/distrib/x86_64/msys2-base-x86_64-20240727.tar.xz",
                FileName = "msys2-base-x86_64-20240727.tar.xz",
                FallbackUrl = "https://repo.msys2.org/distrib/x86_64/msys2-base-x86_64-20240727.tar.xz"
            }
        };
    }

    /// <summary>
    /// Creates 7-Zip vendor definition with web scraping for latest version.
    /// </summary>
    public static VendorDefinition Create7ZipDefinition()
    {
        return new VendorDefinition
        {
            Name = "7-Zip",
            ExtractDir = "7zip",
            SourceType = VendorSourceType.WebScrape,
            WebScrapeConfig = new WebScrapeConfig
            {
                Url = "https://www.7-zip.org/download.html",
                Pattern = @"href=""(a/7z\d+-x64\.msi)""",
                BaseUrl = "https://www.7-zip.org"
            },
            FallbackUrl = "https://www.7-zip.org/a/7z2408-x64.msi"
        };
    }

    /// <summary>
    /// Creates PowerShell vendor definition with GitHub releases.
    /// </summary>
    public static VendorDefinition CreatePowerShellDefinition()
    {
        return new VendorDefinition
        {
            Name = "PowerShell",
            ExtractDir = "powershell",
            SourceType = VendorSourceType.GitHub,
            GitHubOwner = "PowerShell",
            GitHubRepo = "PowerShell",
            AssetPattern = "win-x64.zip",
            FallbackUrl = "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/PowerShell-7.4.6-win-x64.zip"
        };
    }

    /// <summary>
    /// Creates Windows Terminal vendor definition with GitHub releases.
    /// </summary>
    public static VendorDefinition CreateWindowsTerminalDefinition()
    {
        return new VendorDefinition
        {
            Name = "Windows Terminal",
            ExtractDir = "terminal",
            SourceType = VendorSourceType.GitHub,
            GitHubOwner = "microsoft",
            GitHubRepo = "terminal",
            AssetPattern = "Microsoft.WindowsTerminal_",
            AssetPatternEnd = "_x64.zip",
            FallbackUrl = "https://github.com/microsoft/terminal/releases/download/v1.21.2361.0/Microsoft.WindowsTerminal_1.21.2361.0_x64.zip"
        };
    }

    /// <summary>
    /// Creates MSYS2 vendor definition with web scraping for latest version.
    /// </summary>
    public static VendorDefinition CreateMSYS2Definition()
    {
        return new VendorDefinition
        {
            Name = "MSYS2 (Git/Bash)",
            ExtractDir = "msys64",
            SourceType = VendorSourceType.WebScrape,
            WebScrapeConfig = new WebScrapeConfig
            {
                Url = "https://repo.msys2.org/distrib/x86_64/",
                Pattern = @"href=""(msys2-base-x86_64-\d+\.tar\.xz)""",
                BaseUrl = "https://repo.msys2.org/distrib/x86_64/"
            },
            FallbackUrl = "https://repo.msys2.org/distrib/x86_64/msys2-base-x86_64-20240727.tar.xz"
        };
    }
}
