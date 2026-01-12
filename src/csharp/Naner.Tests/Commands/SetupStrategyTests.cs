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
    public void SetupOptions_DefaultVendorMode_IsDefault()
    {
        // Act
        var options = new SetupOptions();

        // Assert
        options.VendorMode.Should().Be(VendorInstallMode.Default);
        options.DebugMode.Should().BeFalse();
    }

    [Fact]
    public void SetupOptions_CanSetVendorModeToSkip()
    {
        // Act
        var options = new SetupOptions
        {
            VendorMode = VendorInstallMode.Skip
        };

        // Assert
        options.VendorMode.Should().Be(VendorInstallMode.Skip);
    }

    [Fact]
    public void SetupOptions_CanSetVendorModeToInstall()
    {
        // Act
        var options = new SetupOptions
        {
            VendorMode = VendorInstallMode.Install
        };

        // Assert
        options.VendorMode.Should().Be(VendorInstallMode.Install);
    }

    [Fact]
    public void SetupOptions_CanSetDebugMode()
    {
        // Act
        var options = new SetupOptions
        {
            DebugMode = true
        };

        // Assert
        options.DebugMode.Should().BeTrue();
    }

    [Fact]
    public void VendorInstallMode_HasCorrectValues()
    {
        // Assert - Verify enum has expected values
        VendorInstallMode.Default.Should().Be((VendorInstallMode)0);
        VendorInstallMode.Skip.Should().Be((VendorInstallMode)1);
        VendorInstallMode.Install.Should().Be((VendorInstallMode)2);
    }
}
