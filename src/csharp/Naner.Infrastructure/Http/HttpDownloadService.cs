using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Naner.Infrastructure.Abstractions;

namespace Naner.Infrastructure.Http;

/// <summary>
/// Service for downloading files over HTTP with progress tracking.
/// Consolidates duplicate download logic across the codebase.
/// </summary>
public class HttpDownloadService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new HttpDownloadService with default settings.
    /// </summary>
    /// <param name="logger">Logger for output messages.</param>
    /// <param name="timeoutMinutes">HTTP timeout in minutes (default: 10).</param>
    /// <param name="userAgent">User-Agent header (default: Naner/{version}).</param>
    public HttpDownloadService(ILogger logger, int timeoutMinutes = NanerConstants.DefaultHttpTimeoutMinutes, string? userAgent = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(timeoutMinutes)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent ?? NanerConstants.DefaultUserAgent);
    }

    /// <summary>
    /// Creates a new HttpDownloadService with custom HttpClient.
    /// </summary>
    /// <param name="httpClient">Custom HTTP client instance.</param>
    /// <param name="logger">Logger for output messages.</param>
    public HttpDownloadService(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Downloads a file from a URL to a local path with progress tracking.
    /// </summary>
    /// <param name="url">The URL to download from.</param>
    /// <param name="outputPath">The local file path to save to.</param>
    /// <param name="displayName">Optional display name for progress messages (defaults to filename).</param>
    /// <param name="showProgress">Whether to show download progress (default: true).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if download succeeded, false otherwise.</returns>
    public async Task<bool> DownloadFileAsync(
        string url,
        string outputPath,
        string? displayName = null,
        bool showProgress = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

        displayName ??= Path.GetFileName(outputPath);

        try
        {
            _logger.Status($"Downloading {displayName}...");

            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, NanerConstants.HttpDownloadBufferSize, true);

            await CopyStreamWithProgressAsync(contentStream, fileStream, totalBytes, displayName, showProgress, cancellationToken);

            _logger.Success($"Downloaded {displayName}");
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.Failure($"HTTP error downloading {displayName}: {ex.Message}");
            return false;
        }
        catch (TaskCanceledException)
        {
            _logger.Warning($"Download of {displayName} was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Failure($"Download failed for {displayName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Downloads a file with custom HTTP headers (e.g., for GitHub API assets).
    /// </summary>
    /// <param name="url">The URL to download from.</param>
    /// <param name="outputPath">The local file path to save to.</param>
    /// <param name="displayName">Optional display name for progress messages.</param>
    /// <param name="configureRequest">Action to configure the HTTP request (e.g., add headers).</param>
    /// <param name="showProgress">Whether to show download progress (default: true).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if download succeeded, false otherwise.</returns>
    public async Task<bool> DownloadFileWithCustomHeadersAsync(
        string url,
        string outputPath,
        string? displayName = null,
        Action<HttpRequestMessage>? configureRequest = null,
        bool showProgress = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

        displayName ??= Path.GetFileName(outputPath);

        try
        {
            _logger.Status($"Downloading {displayName}...");

            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            configureRequest?.Invoke(request);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, NanerConstants.HttpDownloadBufferSize, true);

            await CopyStreamWithProgressAsync(contentStream, fileStream, totalBytes, displayName, showProgress, cancellationToken);

            _logger.Success($"Downloaded {displayName}");
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.Failure($"HTTP error downloading {displayName}: {ex.Message}");
            return false;
        }
        catch (TaskCanceledException)
        {
            _logger.Warning($"Download of {displayName} was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Failure($"Download failed for {displayName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Copies a stream to another stream with progress tracking.
    /// </summary>
    private async Task CopyStreamWithProgressAsync(
        Stream source,
        Stream destination,
        long totalBytes,
        string displayName,
        bool showProgress,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[NanerConstants.HttpDownloadBufferSize];
        long totalRead = 0;
        int bytesRead;
        var lastPercent = -1;

        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;

            if (showProgress && totalBytes > 0)
            {
                var percent = (int)((totalRead * 100) / totalBytes);
                if (percent != lastPercent && percent % NanerConstants.ProgressUpdateInterval == 0)
                {
                    System.Console.Write($"\r  Progress: {percent}%");
                    lastPercent = percent;
                }
            }
        }

        if (showProgress && totalBytes > 0)
        {
            System.Console.Write("\r  Progress: 100%");
            System.Console.WriteLine();
        }
    }

    /// <summary>
    /// Adds an HTTP header to all requests.
    /// </summary>
    /// <param name="name">Header name.</param>
    /// <param name="value">Header value.</param>
    public void AddHeader(string name, string value)
    {
        if (_httpClient.DefaultRequestHeaders.Contains(name))
        {
            _httpClient.DefaultRequestHeaders.Remove(name);
        }
        _httpClient.DefaultRequestHeaders.Add(name, value);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
