using System;
using System.IO;
using FluentAssertions;
using Naner.Vendors.Services;
using Xunit;

namespace Naner.Tests.Services;

/// <summary>
/// Tests for WindowsTerminalConfigurator service.
/// </summary>
public class WindowsTerminalConfiguratorTests : IDisposable
{
    private readonly string _testDir;

    public WindowsTerminalConfiguratorTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"naner-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public void IsWindowsTerminal_WithWindowsTerminalName_ReturnsTrue()
    {
        // Act & Assert
        WindowsTerminalConfigurator.IsWindowsTerminal("Windows Terminal").Should().BeTrue();
        WindowsTerminalConfigurator.IsWindowsTerminal("windows terminal").Should().BeTrue();
        WindowsTerminalConfigurator.IsWindowsTerminal("WINDOWS TERMINAL").Should().BeTrue();
        WindowsTerminalConfigurator.IsWindowsTerminal("Microsoft Windows Terminal").Should().BeTrue();
    }

    [Fact]
    public void IsWindowsTerminal_WithNonWindowsTerminalName_ReturnsFalse()
    {
        // Act & Assert
        WindowsTerminalConfigurator.IsWindowsTerminal("PowerShell").Should().BeFalse();
        WindowsTerminalConfigurator.IsWindowsTerminal("Git").Should().BeFalse();
        WindowsTerminalConfigurator.IsWindowsTerminal("7zip").Should().BeFalse();
        WindowsTerminalConfigurator.IsWindowsTerminal("").Should().BeFalse();
        WindowsTerminalConfigurator.IsWindowsTerminal(null!).Should().BeFalse();
    }

    [Fact]
    public void ConfigurePortableMode_CreatesPortableFile()
    {
        // Arrange
        var targetDir = Path.Combine(_testDir, "WindowsTerminal");
        Directory.CreateDirectory(targetDir);
        var configurator = new WindowsTerminalConfigurator(_testDir);

        // Act
        configurator.ConfigurePortableMode(targetDir);

        // Assert
        var portableFile = Path.Combine(targetDir, ".portable");
        File.Exists(portableFile).Should().BeTrue();
    }

    [Fact]
    public void ConfigurePortableMode_CreatesLocalStateDirectory()
    {
        // Arrange
        var targetDir = Path.Combine(_testDir, "WindowsTerminal");
        Directory.CreateDirectory(targetDir);
        var configurator = new WindowsTerminalConfigurator(_testDir);

        // Act
        configurator.ConfigurePortableMode(targetDir);

        // Assert
        var localStateDir = Path.Combine(targetDir, "LocalState");
        Directory.Exists(localStateDir).Should().BeTrue();
    }

    [Fact]
    public void ConfigurePortableMode_CreatesSettingsJson()
    {
        // Arrange
        var targetDir = Path.Combine(_testDir, "WindowsTerminal");
        Directory.CreateDirectory(targetDir);
        var configurator = new WindowsTerminalConfigurator(_testDir);

        // Act
        configurator.ConfigurePortableMode(targetDir);

        // Assert
        var settingsFile = Path.Combine(targetDir, "LocalState", "settings.json");
        File.Exists(settingsFile).Should().BeTrue();
    }

    [Fact]
    public void CreateSettings_WithTemplate_UsesTemplate()
    {
        // Arrange
        var homeDir = Path.Combine(_testDir, "home", ".config", "windows-terminal");
        Directory.CreateDirectory(homeDir);
        var templateContent = @"{ ""test"": ""%NANER_ROOT%/test"" }";
        File.WriteAllText(Path.Combine(homeDir, "settings.json"), templateContent);

        var configurator = new WindowsTerminalConfigurator(_testDir);
        var settingsPath = Path.Combine(_testDir, "output-settings.json");

        // Act
        configurator.CreateSettings(settingsPath);

        // Assert
        var content = File.ReadAllText(settingsPath);
        content.Should().Contain(_testDir.Replace("\\", "\\\\"));
        content.Should().NotContain("%NANER_ROOT%");
    }

    [Fact]
    public void CreateSettings_WithoutTemplate_CreatesDefaultSettings()
    {
        // Arrange - no template file exists
        var configurator = new WindowsTerminalConfigurator(_testDir);
        var settingsPath = Path.Combine(_testDir, "output-settings.json");

        // Act
        configurator.CreateSettings(settingsPath);

        // Assert
        var content = File.ReadAllText(settingsPath);
        content.Should().Contain("$schema");
        content.Should().Contain("defaultProfile");
        content.Should().Contain("naner-unified");
    }

    [Fact]
    public void Constructor_WithNullNanerRoot_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new WindowsTerminalConfigurator(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("nanerRoot");
    }

    [Fact]
    public void ConfigurePortableMode_WithNullTargetDir_ThrowsArgumentException()
    {
        // Arrange
        var configurator = new WindowsTerminalConfigurator(_testDir);

        // Act
        var act = () => configurator.ConfigurePortableMode(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("targetDir");
    }
}
