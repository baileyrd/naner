using System;
using System.IO;
using FluentAssertions;
using Naner.Launcher.Commands;
using Xunit;

namespace Naner.Tests.Commands;

/// <summary>
/// Tests for VersionCommand.
/// </summary>
public class VersionCommandTests
{
    [Fact]
    public void Execute_ReturnsSuccessExitCode()
    {
        // Arrange
        var command = new VersionCommand();

        // Act
        var exitCode = command.Execute(Array.Empty<string>());

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public void Execute_WritesToConsole()
    {
        // Arrange
        var command = new VersionCommand();
        var output = new StringWriter();
        Console.SetOut(output);

        try
        {
            // Act
            command.Execute(Array.Empty<string>());

            // Assert
            var result = output.ToString();
            result.Should().Contain("Naner");
            result.Should().Contain("1.0.0");
        }
        finally
        {
            // Restore original console output
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);
        }
    }
}
