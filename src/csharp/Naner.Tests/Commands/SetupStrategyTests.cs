using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Naner.Commands.Abstractions;
using Naner.Commands.Implementations;
using Xunit;

namespace Naner.Tests.Commands;

/// <summary>
/// Tests for InitCommand strategy pattern implementation.
/// </summary>
public class SetupStrategyTests
{
    [Fact]
    public void InitCommand_Constructor_WithNullInteractiveStrategy_ThrowsArgumentNullException()
    {
        // Arrange
        var mockQuickStrategy = new Mock<ISetupStrategy>();

        // Act & Assert
        var action = () => new InitCommand(null!, mockQuickStrategy.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("interactiveStrategy");
    }

    [Fact]
    public void InitCommand_Constructor_WithNullQuickStrategy_ThrowsArgumentNullException()
    {
        // Arrange
        var mockInteractiveStrategy = new Mock<ISetupStrategy>();

        // Act & Assert
        var action = () => new InitCommand(mockInteractiveStrategy.Object, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("quickStrategy");
    }

    [Fact]
    public void InitCommand_Constructor_Parameterless_CreatesDefaultStrategies()
    {
        // Act - Should not throw
        var command = new InitCommand();

        // Assert - Command was created successfully
        command.Should().NotBeNull();
    }

    [Fact]
    public void InitCommand_Constructor_WithValidStrategies_CreatesCommand()
    {
        // Arrange
        var mockInteractiveStrategy = new Mock<ISetupStrategy>();
        var mockQuickStrategy = new Mock<ISetupStrategy>();

        // Act
        var command = new InitCommand(mockInteractiveStrategy.Object, mockQuickStrategy.Object);

        // Assert
        command.Should().NotBeNull();
    }

    [Fact]
    public void SetupOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new SetupOptions();

        // Assert
        options.SkipVendors.Should().BeFalse();
        options.WithVendors.Should().BeFalse();
        options.DebugMode.Should().BeFalse();
    }

    [Fact]
    public void SetupOptions_CanSetAllProperties()
    {
        // Act
        var options = new SetupOptions
        {
            SkipVendors = true,
            WithVendors = true,
            DebugMode = true
        };

        // Assert
        options.SkipVendors.Should().BeTrue();
        options.WithVendors.Should().BeTrue();
        options.DebugMode.Should().BeTrue();
    }
}
