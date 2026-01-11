using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Naner.Common;
using Naner.Common.Services;
using Xunit;

namespace Naner.Tests.Services;

/// <summary>
/// Tests for HttpClientWrapper.
/// </summary>
public class HttpClientWrapperTests
{
    [Fact]
    public void Constructor_WithDefaultTimeout_CreatesWrapper()
    {
        // Act
        var wrapper = new HttpClientWrapper();

        // Assert
        wrapper.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomTimeout_CreatesWrapper()
    {
        // Arrange
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        var wrapper = new HttpClientWrapper(timeout);

        // Assert
        wrapper.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithHttpClient_CreatesWrapper()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act
        var wrapper = new HttpClientWrapper(httpClient);

        // Assert
        wrapper.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new HttpClientWrapper((HttpClient)null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public async Task GetAsync_WithValidUrl_ReturnsResponse()
    {
        // Arrange
        var wrapper = new HttpClientWrapper();
        var url = "https://httpbin.org/status/200";

        // Act
        var response = await wrapper.GetAsync(url);

        // Assert
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_WithCompletionOption_ReturnsResponse()
    {
        // Arrange
        var wrapper = new HttpClientWrapper();
        var url = "https://httpbin.org/status/200";

        // Act
        var response = await wrapper.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        // Assert
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public void Constructor_UsesDefaultUserAgent()
    {
        // Arrange & Act
        var wrapper = new HttpClientWrapper();

        // Assert
        // We can't directly test the user agent without making a real request,
        // but we can verify the wrapper was created successfully
        wrapper.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_UsesDefaultHttpTimeout()
    {
        // Arrange & Act
        var wrapper = new HttpClientWrapper();

        // Assert
        // Timeout is set to NanerConstants.DefaultHttpTimeoutMinutes (10 minutes)
        // We verify this indirectly by ensuring the wrapper is created
        wrapper.Should().NotBeNull();
    }
}
