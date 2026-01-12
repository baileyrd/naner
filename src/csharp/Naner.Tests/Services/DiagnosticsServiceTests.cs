using System;
using FluentAssertions;
using Moq;
using Naner.Commands.Abstractions;
using Naner.Commands.Services;
using Xunit;

namespace Naner.Tests.Services;

/// <summary>
/// Tests for DiagnosticsService with DI support.
/// </summary>
public class DiagnosticsServiceTests
{
    [Fact]
    public void Constructor_WithNullDirectoryVerifier_ThrowsArgumentNullException()
    {
        // Arrange
        var mockConfigVerifier = new Mock<IConfigurationVerifier>();
        var mockEnvReporter = new Mock<IEnvironmentReporter>();

        // Act & Assert
        var action = () => new DiagnosticsService(null!, mockConfigVerifier.Object, mockEnvReporter.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("directoryVerifier");
    }

    [Fact]
    public void Constructor_WithNullConfigurationVerifier_ThrowsArgumentNullException()
    {
        // Arrange
        var mockDirVerifier = new Mock<IDirectoryVerifier>();
        var mockEnvReporter = new Mock<IEnvironmentReporter>();

        // Act & Assert
        var action = () => new DiagnosticsService(mockDirVerifier.Object, null!, mockEnvReporter.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configurationVerifier");
    }

    [Fact]
    public void Constructor_WithNullEnvironmentReporter_ThrowsArgumentNullException()
    {
        // Arrange
        var mockDirVerifier = new Mock<IDirectoryVerifier>();
        var mockConfigVerifier = new Mock<IConfigurationVerifier>();

        // Act & Assert
        var action = () => new DiagnosticsService(mockDirVerifier.Object, mockConfigVerifier.Object, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("environmentReporter");
    }

    [Fact]
    public void Constructor_Parameterless_CreatesDefaultServices()
    {
        // Act - Should not throw
        var service = new DiagnosticsService();

        // Assert - Service was created successfully
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesService()
    {
        // Arrange
        var mockDirVerifier = new Mock<IDirectoryVerifier>();
        var mockConfigVerifier = new Mock<IConfigurationVerifier>();
        var mockEnvReporter = new Mock<IEnvironmentReporter>();

        // Act
        var service = new DiagnosticsService(
            mockDirVerifier.Object,
            mockConfigVerifier.Object,
            mockEnvReporter.Object);

        // Assert
        service.Should().NotBeNull();
    }
}
