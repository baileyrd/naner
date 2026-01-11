using System.Threading.Tasks;

namespace Naner.Infrastructure.Abstractions;

/// <summary>
/// Interface for interacting with GitHub Releases API.
/// Enables dependency injection and testability.
/// </summary>
public interface IGitHubClient
{
    /// <summary>
    /// Gets the latest release information (including prereleases).
    /// </summary>
    /// <returns>Latest release information, or null if not available</returns>
    Task<GitHubRelease?> GetLatestReleaseAsync();

    /// <summary>
    /// Downloads an asset from a release.
    /// </summary>
    /// <param name="downloadUrl">URL to download from</param>
    /// <param name="outputPath">Local path to save the file</param>
    /// <param name="assetName">Display name for the asset</param>
    /// <returns>True if download succeeded, false otherwise</returns>
    Task<bool> DownloadAssetAsync(string downloadUrl, string outputPath, string assetName);
}

/// <summary>
/// Represents a GitHub release.
/// </summary>
public class GitHubRelease
{
    public string? TagName { get; set; }
    public string? Name { get; set; }
    public bool Prerelease { get; set; }
    public System.Collections.Generic.List<GitHubAsset>? Assets { get; set; }
    public string? Body { get; set; }
    public System.DateTime PublishedAt { get; set; }
}

/// <summary>
/// Represents a GitHub release asset.
/// </summary>
public class GitHubAsset
{
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? BrowserDownloadUrl { get; set; }
    public long Size { get; set; }
}
