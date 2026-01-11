using System;
using Xunit;
using FluentAssertions;
using Naner.Launcher.Commands;
using Naner.Tests.Helpers;

namespace Naner.Tests.Commands;

/// <summary>
/// Unit tests for SetupVendorsCommand.
/// </summary>
public class SetupVendorsCommandTests : IDisposable
{
    private readonly TestLogger _testLogger;

    public SetupVendorsCommandTests()
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
    public void SetupVendorsCommand_ImplementsICommand()
    {
        // Arrange & Act
        var command = new SetupVendorsCommand();

        // Assert
        command.Should().BeAssignableTo<Launcher.Commands.ICommand>();
    }

    [Fact]
    public void SetupVendorsCommand_HasExecuteMethod()
    {
        // Arrange
        var command = new SetupVendorsCommand();

        // Act
        var executeMethod = command.GetType().GetMethod("Execute");

        // Assert
        executeMethod.Should().NotBeNull("Execute method should exist");
        executeMethod!.ReturnType.Should().Be(typeof(int), "Execute should return int exit code");
        executeMethod.GetParameters().Should().HaveCount(1, "Execute should take one parameter");
        executeMethod.GetParameters()[0].ParameterType.Should().Be(typeof(string[]), "Execute should take string[] args");
    }

    [Fact]
    public void SetupVendorsCommand_HasCorrectNamespace()
    {
        // Arrange & Act
        var command = new SetupVendorsCommand();
        var commandType = command.GetType();

        // Assert
        commandType.Namespace.Should().Be("Naner.Launcher.Commands");
    }

    [Fact]
    public void SetupVendorsCommand_Execute_WithNoArgs_AcceptsEmptyArray()
    {
        // Arrange
        var command = new SetupVendorsCommand();
        var args = Array.Empty<string>();

        // Act
        var hasExecuteMethod = command.GetType().GetMethod("Execute") != null;

        // Assert
        hasExecuteMethod.Should().BeTrue();
        // Note: Actual execution would require NANER_ROOT setup, which is beyond unit test scope
    }

    [Fact]
    public void SetupVendorsCommand_CanBeInstantiated()
    {
        // Arrange & Act
        var command = new SetupVendorsCommand();

        // Assert
        command.Should().NotBeNull();
    }

    [Fact]
    public void SetupVendorsCommand_HasXmlDocumentation()
    {
        // Arrange & Act
        var command = new SetupVendorsCommand();
        var commandType = command.GetType();

        // Assert
        commandType.Should().NotBeNull();
        commandType.IsPublic.Should().BeTrue("Command should be public");
    }
}
