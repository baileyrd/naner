using FluentAssertions;
using Naner.Core;
using Naner.Commands;
using Xunit;

namespace Naner.Tests;

/// <summary>
/// Tests for CommonCommandConstants and DRY compliance.
/// </summary>
public class CommonCommandConstantsTests
{
    [Fact]
    public void CommonCommandConstants_Version_IsCorrect()
    {
        CommonCommandConstants.Version.Should().Be("--version");
    }

    [Fact]
    public void CommonCommandConstants_VersionShort_IsCorrect()
    {
        CommonCommandConstants.VersionShort.Should().Be("-v");
    }

    [Fact]
    public void CommonCommandConstants_Help_IsCorrect()
    {
        CommonCommandConstants.Help.Should().Be("--help");
    }

    [Fact]
    public void CommonCommandConstants_HelpShort_IsCorrect()
    {
        CommonCommandConstants.HelpShort.Should().Be("-h");
    }

    [Fact]
    public void CommonCommandConstants_HelpAlternate_IsCorrect()
    {
        CommonCommandConstants.HelpAlternate.Should().Be("/?");
    }

    [Fact]
    public void CommonCommandConstants_Debug_IsCorrect()
    {
        CommonCommandConstants.Debug.Should().Be("--debug");
    }

    [Fact]
    public void CommandNames_UsesCommonCommandConstants_ForVersion()
    {
        // Verify CommandNames delegates to CommonCommandConstants (DRY)
        CommandNames.Version.Should().Be(CommonCommandConstants.Version);
        CommandNames.VersionShort.Should().Be(CommonCommandConstants.VersionShort);
    }

    [Fact]
    public void CommandNames_UsesCommonCommandConstants_ForHelp()
    {
        // Verify CommandNames delegates to CommonCommandConstants (DRY)
        CommandNames.Help.Should().Be(CommonCommandConstants.Help);
        CommandNames.HelpShort.Should().Be(CommonCommandConstants.HelpShort);
        CommandNames.HelpAlternate.Should().Be(CommonCommandConstants.HelpAlternate);
    }

    [Fact]
    public void CommandNames_UsesCommonCommandConstants_ForDebug()
    {
        // Verify CommandNames delegates to CommonCommandConstants (DRY)
        CommandNames.Debug.Should().Be(CommonCommandConstants.Debug);
    }

    [Fact]
    public void CommandNames_VersionConstants_MatchExpectedValues()
    {
        // Ensure the constants have the expected values
        CommandNames.Version.Should().Be("--version");
        CommandNames.VersionShort.Should().Be("-v");
    }

    [Fact]
    public void CommandNames_HelpConstants_MatchExpectedValues()
    {
        // Ensure the constants have the expected values
        CommandNames.Help.Should().Be("--help");
        CommandNames.HelpShort.Should().Be("-h");
        CommandNames.HelpAlternate.Should().Be("/?");
    }

    [Fact]
    public void CommandNames_DebugConstant_MatchesExpectedValue()
    {
        // Ensure the constant has the expected value
        CommandNames.Debug.Should().Be("--debug");
    }
}
