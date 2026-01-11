using System;
using System.Net.Http;
using System.Threading.Tasks;
using Naner.Common.Abstractions;

namespace Naner.Common.Services;

/// <summary>
/// Default implementation of IHttpClientWrapper using a real HttpClient.
/// </summary>
public class HttpClientWrapper : IHttpClientWrapper
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Creates a new HttpClientWrapper with a configured HttpClient.
    /// </summary>
    /// <param name="timeout">Optional timeout for HTTP requests (default: 10 minutes)</param>
    public HttpClientWrapper(TimeSpan? timeout = null)
    {
        _httpClient = new HttpClient
        {
            Timeout = timeout ?? TimeSpan.FromMinutes(10)
        };

        // Configure default headers
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Naner/1.0.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    }

    /// <summary>
    /// Creates a new HttpClientWrapper using an existing HttpClient.
    /// Useful for dependency injection scenarios.
    /// </summary>
    /// <param name="httpClient">The HttpClient instance to wrap</param>
    public HttpClientWrapper(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetAsync(string url)
    {
        return _httpClient.GetAsync(url);
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetAsync(string url, HttpCompletionOption completionOption)
    {
        return _httpClient.GetAsync(url, completionOption);
    }
}
