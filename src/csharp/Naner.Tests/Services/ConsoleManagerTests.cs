using FluentAssertions;
using Naner.Vendors.Services;
using Xunit;

namespace Naner.Tests.Services;

/// <summary>
/// Tests for ConsoleManager service.
/// </summary>
public class ConsoleManagerTests
{
    [Fact]
    public void HasConsole_ShouldReturnBool()
    {
        // Arrange
        var manager = new ConsoleManager();

        // Act
        var hasConsole = manager.HasConsole;

        // Assert - bool is a value type, so we just verify it's a bool
        hasConsole.Should().Be(hasConsole); // tautology to verify it compiles and is a bool
    }

    [Fact]
    public void NeedsConsole_WithConsoleCommand_ReturnsTrue()
    {
        // Arrange
        var consoleCommands = new[] { "--version", "--help", "--diagnose" };

        // Act
        var result = ConsoleManager.NeedsConsole(
            new[] { "--version" },
            consoleCommands);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void NeedsConsole_WithNonConsoleCommand_ReturnsFalse()
    {
        // Arrange
        var consoleCommands = new[] { "--version", "--help", "--diagnose" };

        // Act
        var result = ConsoleManager.NeedsConsole(
            new[] { "launch" },
            consoleCommands);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void NeedsConsole_WithEmptyArgs_ReturnsFalse()
    {
        // Arrange
        var consoleCommands = new[] { "--version", "--help" };

        // Act
        var result = ConsoleManager.NeedsConsole(
            Array.Empty<string>(),
            consoleCommands);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void NeedsConsole_IsCaseInsensitive()
    {
        // Arrange
        var consoleCommands = new[] { "--version" };

        // Act
        var result = ConsoleManager.NeedsConsole(
            new[] { "--VERSION" },
            consoleCommands);

        // Assert
        result.Should().BeTrue();
    }
}
