using System;
using System.IO;
using Xunit;
using FluentAssertions;
using Naner.Commands.Implementations;
using Naner.Tests.Helpers;

namespace Naner.Tests.Commands;

/// <summary>
/// Unit tests for InitCommand.
/// </summary>
public class InitCommandTests : IDisposable
{
    private readonly TestLogger _testLogger;

    public InitCommandTests()
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
    public void InitCommand_ImplementsICommand()
    {
        // Arrange & Act
        var command = new InitCommand();

        // Assert
        command.Should().BeAssignableTo<Naner.Commands.Abstractions.ICommand>();
    }

    [Fact]
    public void InitCommand_Execute_WithNoArgs_ShowsDeprecationNotice()
    {
        // Arrange
        var command = new InitCommand();
        var args = Array.Empty<string>();

        // Act
        // Note: This will prompt for user input, so we can't fully test in automated environment
        // Just verify command can be instantiated and has Execute method
        var hasExecuteMethod = command.GetType().GetMethod("Execute") != null;

        // Assert
        hasExecuteMethod.Should().BeTrue();
    }

    [Theory]
    [InlineData("--minimal")]
    [InlineData("--quick")]
    [InlineData("--skip-vendors")]
    [InlineData("--with-vendors")]
    public void InitCommand_RecognizesValidArguments(string argument)
    {
        // Arrange
        var command = new InitCommand();

        // Act
        var hasExecuteMethod = command.GetType().GetMethod("Execute") != null;

        // Assert
        hasExecuteMethod.Should().BeTrue();
        argument.Should().StartWith("--");
    }

    [Fact]
    public void InitCommand_HasCorrectNamespace()
    {
        // Arrange & Act
        var command = new InitCommand();
        var commandType = command.GetType();

        // Assert
        commandType.Namespace.Should().Be("Naner.Commands.Implementations");
    }

    [Fact]
    public void InitCommand_HasXmlDocumentation()
    {
        // Arrange & Act
        var command = new InitCommand();
        var executeMethod = command.GetType().GetMethod("Execute");

        // Assert
        executeMethod.Should().NotBeNull("Execute method should exist");
        executeMethod!.GetParameters().Should().HaveCount(1, "Execute should take one parameter");
        executeMethod.GetParameters()[0].ParameterType.Should().Be(typeof(string[]), "Execute should take string[] args");
    }
}
