using Xunit;
using FluentAssertions;
using Naner.Commands;

namespace Naner.Tests;

/// <summary>
/// Unit tests for CommandNames constants.
/// Ensures command name constants are defined correctly.
/// </summary>
public class CommandNamesTests
{
    [Fact]
    public void CommandNames_AllConstantsAreNotNull()
    {
        // Assert
        CommandNames.Version.Should().NotBeNullOrEmpty();
        CommandNames.VersionShort.Should().NotBeNullOrEmpty();
        CommandNames.Help.Should().NotBeNullOrEmpty();
        CommandNames.HelpShort.Should().NotBeNullOrEmpty();
        CommandNames.HelpAlternate.Should().NotBeNullOrEmpty();
        CommandNames.Diagnose.Should().NotBeNullOrEmpty();
        CommandNames.UpdateVendors.Should().NotBeNullOrEmpty();
        CommandNames.Debug.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CommandNames_NoCommandMatch_IsNegativeOne()
    {
        // Arrange & Assert
        CommandNames.NoCommandMatch.Should().Be(-1);
    }

    [Fact]
    public void CommandNames_VersionCommands_AreDifferent()
    {
        // Arrange & Assert
        CommandNames.Version.Should().NotBe(CommandNames.VersionShort);
        CommandNames.Version.Should().Be("--version");
        CommandNames.VersionShort.Should().Be("-v");
    }

    [Fact]
    public void CommandNames_HelpCommands_AreDifferent()
    {
        // Arrange & Assert
        CommandNames.Help.Should().NotBe(CommandNames.HelpShort);
        CommandNames.Help.Should().NotBe(CommandNames.HelpAlternate);
        CommandNames.HelpShort.Should().NotBe(CommandNames.HelpAlternate);
    }

    [Fact]
    public void CommandNames_UpdateVendors_HasCorrectValue()
    {
        // Arrange & Assert
        CommandNames.UpdateVendors.Should().Be("update-vendors");
    }
}
