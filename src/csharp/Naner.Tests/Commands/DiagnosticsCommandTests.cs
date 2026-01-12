using System;
using FluentAssertions;
using Moq;
using Naner.Commands.Abstractions;
using Naner.Commands.Implementations;
using Xunit;

namespace Naner.Tests.Commands;

/// <summary>
/// Tests for DiagnosticsCommand with DI support.
/// </summary>
public class DiagnosticsCommandTests
{
    [Fact]
    public void Execute_WithInjectedService_CallsServiceRun()
    {
        // Arrange
        var mockDiagnosticsService = new Mock<IDiagnosticsService>();
        mockDiagnosticsService.Setup(s => s.Run()).Returns(0);

        var command = new DiagnosticsCommand(mockDiagnosticsService.Object);

        // Act
        var exitCode = command.Execute(Array.Empty<string>());

        // Assert
        mockDiagnosticsService.Verify(s => s.Run(), Times.Once);
        exitCode.Should().Be(0);
    }

    [Fact]
    public void Execute_WithInjectedService_ReturnsServiceExitCode()
    {
        // Arrange
        var mockDiagnosticsService = new Mock<IDiagnosticsService>();
        mockDiagnosticsService.Setup(s => s.Run()).Returns(42);

        var command = new DiagnosticsCommand(mockDiagnosticsService.Object);

        // Act
        var exitCode = command.Execute(Array.Empty<string>());

        // Assert
        exitCode.Should().Be(42);
    }

    [Fact]
    public void Constructor_WithNullService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new DiagnosticsCommand(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("diagnosticsService");
    }

    [Fact]
    public void Constructor_Parameterless_CreatesDefaultService()
    {
        // Act - Should not throw
        var command = new DiagnosticsCommand();

        // Assert - Command was created successfully
        command.Should().NotBeNull();
    }
}
