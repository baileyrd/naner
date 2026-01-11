using System;
using Xunit;
using FluentAssertions;
using Naner.Launcher.Services;
using Naner.Tests.Helpers;

namespace Naner.Tests.Services;

/// <summary>
/// Unit tests for CommandRouter.
/// </summary>
public class CommandRouterTests : IDisposable
{
    private readonly TestLogger _testLogger;

    public CommandRouterTests()
    {
        _testLogger = new TestLogger();
        Logger.SetLogger(_testLogger);
    }

    public void Dispose()
    {
        // Reset logger to console logger after tests
        Logger.SetLogger(new ConsoleLogger());
    }

    [Fact]
    public void CommandRouter_CanBeInstantiated()
    {
        // Arrange & Act
        var router = new CommandRouter();

        // Assert
        router.Should().NotBeNull();
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-v")]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/?")]
    [InlineData("--diagnose")]
    [InlineData("init")]
    [InlineData("setup-vendors")]
    public void CommandRouter_Route_RecognizesRegisteredCommands(string command)
    {
        // Arrange
        var router = new CommandRouter();
        var args = new[] { command };

        // Act
        var result = router.Route(args);

        // Assert
        // Result should not be -1 (which means unrecognized command)
        result.Should().NotBe(-1, $"command '{command}' should be recognized");
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("random-command")]
    [InlineData("--unknown-flag")]
    public void CommandRouter_Route_ReturnsNegativeOneForUnknownCommands(string command)
    {
        // Arrange
        var router = new CommandRouter();
        var args = new[] { command };

        // Act
        var result = router.Route(args);

        // Assert
        result.Should().Be(-1, "unrecognized commands should return -1");
    }

    [Fact]
    public void CommandRouter_Route_WithNoArgs_ReturnsNegativeOne()
    {
        // Arrange
        var router = new CommandRouter();
        var args = Array.Empty<string>();

        // Act
        var result = router.Route(args);

        // Assert
        result.Should().Be(-1, "no arguments should return -1 to trigger launcher");
    }

    [Theory]
    [InlineData("--version", true)]
    [InlineData("-v", true)]
    [InlineData("--help", true)]
    [InlineData("-h", true)]
    [InlineData("/?", true)]
    [InlineData("--diagnose", true)]
    [InlineData("init", true)]
    [InlineData("setup-vendors", true)]
    [InlineData("--debug", true)]
    [InlineData("unknown", false)]
    [InlineData("launch", false)]
    public void CommandRouter_NeedsConsole_ReturnsCorrectValue(string command, bool expected)
    {
        // Arrange
        var args = new[] { command };

        // Act
        var result = CommandRouter.NeedsConsole(args);

        // Assert
        result.Should().Be(expected, $"command '{command}' should {(expected ? "need" : "not need")} console");
    }

    [Fact]
    public void CommandRouter_NeedsConsole_WithNoArgs_ReturnsFalse()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = CommandRouter.NeedsConsole(args);

        // Assert
        result.Should().BeFalse("no arguments should not need console by default");
    }

    [Fact]
    public void CommandRouter_NeedsConsole_IsCaseInsensitive()
    {
        // Arrange
        var argsLower = new[] { "init" };
        var argsUpper = new[] { "INIT" };
        var argsMixed = new[] { "Init" };

        // Act
        var resultLower = CommandRouter.NeedsConsole(argsLower);
        var resultUpper = CommandRouter.NeedsConsole(argsUpper);
        var resultMixed = CommandRouter.NeedsConsole(argsMixed);

        // Assert
        resultLower.Should().BeTrue("lowercase 'init' should need console");
        resultUpper.Should().BeTrue("uppercase 'INIT' should need console");
        resultMixed.Should().BeTrue("mixed case 'Init' should need console");
    }

    [Fact]
    public void CommandRouter_RegistersAllStandardCommands()
    {
        // Arrange
        var router = new CommandRouter();
        var standardCommands = new[]
        {
            "--version", "-v",
            "--help", "-h", "/?",
            "--diagnose",
            "init",
            "setup-vendors"
        };

        // Act & Assert
        foreach (var command in standardCommands)
        {
            var result = router.Route(new[] { command });
            result.Should().NotBe(-1, $"command '{command}' should be registered");
        }
    }
}
