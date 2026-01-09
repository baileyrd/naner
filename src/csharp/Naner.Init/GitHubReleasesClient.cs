using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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
            Timeout = TimeSpan.FromMinutes(10)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Naner-Init/1.0.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    }

    /// <summary>
    /// Gets the latest release information.
    /// </summary>
    public async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                ConsoleHelper.Warning($"Failed to fetch latest release: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return release;
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error($"Error fetching latest release: {ex.Message}");
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
            ConsoleHelper.Status($"Downloading {assetName}...");

            using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
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
                    if (percent != lastPercent && percent % 10 == 0)
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

            ConsoleHelper.Success($"Downloaded {assetName}");
            return true;
        }
        catch (Exception ex)
        {
            ConsoleHelper.Error($"Download failed: {ex.Message}");
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
    public string? TagName { get; set; }
    public string? Name { get; set; }
    public bool Prerelease { get; set; }
    public List<GitHubAsset>? Assets { get; set; }
    public string? Body { get; set; }
    public DateTime PublishedAt { get; set; }
}

/// <summary>
/// Represents a GitHub release asset.
/// </summary>
public class GitHubAsset
{
    public string? Name { get; set; }
    public string? BrowserDownloadUrl { get; set; }
    public long Size { get; set; }
}
