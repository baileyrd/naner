using System.Net.Http;
using System.Threading.Tasks;

namespace Naner.Infrastructure.Abstractions;

/// <summary>
/// Abstraction for HttpClient to enable testing and dependency injection.
/// Wraps HttpClient operations used in Naner vendor installation.
/// </summary>
public interface IHttpClientWrapper
{
    /// <summary>
    /// Sends a GET request to the specified URL.
    /// </summary>
    /// <param name="url">The URL to request</param>
    /// <returns>HTTP response message</returns>
    Task<HttpResponseMessage> GetAsync(string url);

    /// <summary>
    /// Sends a GET request with specified completion option.
    /// </summary>
    /// <param name="url">The URL to request</param>
    /// <param name="completionOption">HTTP completion option</param>
    /// <returns>HTTP response message</returns>
    Task<HttpResponseMessage> GetAsync(string url, HttpCompletionOption completionOption);
}
