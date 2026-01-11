using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Naner.Init;

/// <summary>
/// Client for interacting with GitHub Releases API.
/// </summary>
public class GitHubReleasesClient
{
    private readonly HttpClient _httpClient;
    private readonly string _owner;
    private readonly string _repo;

    public GitHubReleasesClient(string owner, string repo)
    {
        _owner = owner;
        _repo = repo;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(NanerConstants.DefaultHttpTimeoutMinutes)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", NanerConstants.DefaultUserAgent);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");

        // Add GitHub token if available (for private repos)
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }
    }

    /// <summary>
    /// Gets the latest release information (including prereleases).
    /// </summary>
    public async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        try
        {
            // Try to get the latest non-prerelease first
            var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (release != null)
                {
                    return release;
                }
            }

            // Fallback: get all releases and return the most recent one (including prereleases)
            url = $"https://api.github.com/repos/{_owner}/{_repo}/releases";
            response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Warning($"Failed to fetch latest release: {response.StatusCode}");
                return null;
            }

            var releasesJson = await response.Content.ReadAsStringAsync();
            var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(releasesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Return the first release (most recent)
            return releases?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Logger.Failure($"Error fetching latest release: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Downloads an asset from a release.
    /// </summary>
    public async Task<bool> DownloadAssetAsync(string downloadUrl, string outputPath, string assetName)
    {
        try
        {
            Logger.Status($"Downloading {assetName}...");

            // For private repos, use the API URL with Accept header for octet-stream
            using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            if (downloadUrl.StartsWith("https://api.github.com/"))
            {
                // Override Accept header to get the binary content
                request.Headers.Remove("Accept");
                request.Headers.Add("Accept", "application/octet-stream");
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            var lastPercent = -1;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    var percent = (int)((totalRead * 100) / totalBytes);
                    if (percent != lastPercent && percent % NanerConstants.ProgressUpdateInterval == 0)
                    {
                        Console.Write($"\r  Progress: {percent}%");
                        lastPercent = percent;
                    }
                }
            }

            if (totalBytes > 0)
            {
                Console.Write("\r  Progress: 100%");
                Console.WriteLine();
            }

            Logger.Success($"Downloaded {assetName}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Failure($"Download failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Fetches the latest release for a GitHub project using different API patterns.
    /// </summary>
    public static async Task<string?> GetLatestReleaseUrlAsync(string owner, string repo, string assetPattern)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Naner-Init/1.0.0");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");

            var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (release?.Assets == null)
            {
                return null;
            }

            // Find asset matching pattern
            var asset = release.Assets.FirstOrDefault(a =>
                a.Name != null && a.Name.Contains(assetPattern, StringComparison.OrdinalIgnoreCase));

            return asset?.BrowserDownloadUrl;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Represents a GitHub release.
/// </summary>
public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubAsset>? Assets { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }
}

/// <summary>
/// Represents a GitHub release asset.
/// </summary>
public class GitHubAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
