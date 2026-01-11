using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Naner.Configuration;
using Xunit;

namespace Naner.Tests.Configuration;

/// <summary>
/// Tests for ConfigurationValidator.
/// </summary>
public class ConfigurationValidatorTests
{
    [Fact]
    public void Validate_WithValidConfiguration_ReturnsTrue()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var config = new NanerConfig
        {
            DefaultProfile = "TestProfile",
            Profiles = new Dictionary<string, ProfileConfig>
            {
                ["TestProfile"] = new ProfileConfig
                {
                    Name = "Test",
                    Shell = "PowerShell",
                    StartingDirectory = "%USERPROFILE%"
                }
            },
            Environment = new EnvironmentConfig
            {
                PathPrecedence = new List<string> { tempDir },
                EnvironmentVariables = new Dictionary<string, string>()
            }
        };

        var validator = new ConfigurationValidator(tempDir);

        try
        {
            // Act
            var result = validator.Validate(config);

            // Assert
            result.Should().BeTrue();
            validator.Errors.Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Validate_WithMissingDefaultProfile_ReturnsFalse()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var config = new NanerConfig
        {
            DefaultProfile = "",
            Profiles = new Dictionary<string, ProfileConfig>()
        };

        var validator = new ConfigurationValidator(tempDir);

        // Act
        var result = validator.Validate(config);

        // Assert
        result.Should().BeFalse();
        validator.Errors.Should().Contain(e => e.Contains("DefaultProfile cannot be empty"));
    }

    [Fact]
    public void Validate_WithNonExistentDefaultProfile_ReturnsFalse()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var config = new NanerConfig
        {
            DefaultProfile = "NonExistent",
            Profiles = new Dictionary<string, ProfileConfig>
            {
                ["OtherProfile"] = new ProfileConfig
                {
                    Name = "Other",
                    Shell = "PowerShell",
                    StartingDirectory = "%USERPROFILE%"
                }
            }
        };

        var validator = new ConfigurationValidator(tempDir);

        // Act
        var result = validator.Validate(config);

        // Assert
        result.Should().BeFalse();
        validator.Errors.Should().Contain(e => e.Contains("DefaultProfile 'NonExistent' does not exist"));
    }

    [Fact]
    public void Validate_WithNoProfiles_ReturnsFalse()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var config = new NanerConfig
        {
            DefaultProfile = "Test",
            Profiles = new Dictionary<string, ProfileConfig>(),
            CustomProfiles = new Dictionary<string, ProfileConfig>()
        };

        var validator = new ConfigurationValidator(tempDir);

        // Act
        var result = validator.Validate(config);

        // Assert
        result.Should().BeFalse();
        validator.Errors.Should().Contain(e => e.Contains("At least one profile must be defined"));
    }

    [Fact]
    public void Validate_WithEmptyProfileName_ReturnsFalse()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var config = new NanerConfig
        {
            DefaultProfile = "Test",
            Profiles = new Dictionary<string, ProfileConfig>
            {
                ["Test"] = new ProfileConfig
                {
                    Name = "",
                    Shell = "PowerShell",
                    StartingDirectory = "%USERPROFILE%"
                }
            }
        };

        var validator = new ConfigurationValidator(tempDir);

        // Act
        var result = validator.Validate(config);

        // Assert
        result.Should().BeFalse();
        validator.Errors.Should().Contain(e => e.Contains("[Test].Name cannot be empty"));
    }

    [Fact]
    public void Validate_WithCustomShellButNoExecutablePath_ReturnsFalse()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var config = new NanerConfig
        {
            DefaultProfile = "Test",
            Profiles = new Dictionary<string, ProfileConfig>
            {
                ["Test"] = new ProfileConfig
                {
                    Name = "Test",
                    Shell = "Custom",
                    StartingDirectory = "%USERPROFILE%",
                    CustomShell = new CustomShellConfig
                    {
                        ExecutablePath = ""
                    }
                }
            }
        };

        var validator = new ConfigurationValidator(tempDir);

        // Act
        var result = validator.Validate(config);

        // Assert
        result.Should().BeFalse();
        validator.Errors.Should().Contain(e => e.Contains("CustomShell.ExecutablePath cannot be empty"));
    }

    [Fact]
    public void Validate_WithNonExistentVendorPath_AddsWarning()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var config = new NanerConfig
        {
            DefaultProfile = "Test",
            Profiles = new Dictionary<string, ProfileConfig>
            {
                ["Test"] = new ProfileConfig
                {
                    Name = "Test",
                    Shell = "PowerShell",
                    StartingDirectory = "%USERPROFILE%"
                }
            },
            VendorPaths = new Dictionary<string, string>
            {
                ["Git"] = Path.Combine(tempDir, "nonexistent")
            }
        };

        var validator = new ConfigurationValidator(tempDir);

        try
        {
            // Act
            var result = validator.Validate(config);

            // Assert
            result.Should().BeTrue(); // Warnings don't fail validation
            validator.Warnings.Should().Contain(w => w.Contains("VendorPaths[Git]") && w.Contains("does not exist"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void ThrowIfInvalid_WithErrors_ThrowsInvalidOperationException()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var config = new NanerConfig
        {
            DefaultProfile = "",
            Profiles = new Dictionary<string, ProfileConfig>()
        };

        var validator = new ConfigurationValidator(tempDir);
        validator.Validate(config);

        // Act & Assert
        var act = () => validator.ThrowIfInvalid();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Configuration validation failed*");
    }

    [Fact]
    public void ThrowIfInvalid_WithoutErrors_DoesNotThrow()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var config = new NanerConfig
        {
            DefaultProfile = "Test",
            Profiles = new Dictionary<string, ProfileConfig>
            {
                ["Test"] = new ProfileConfig
                {
                    Name = "Test",
                    Shell = "PowerShell",
                    StartingDirectory = "%USERPROFILE%"
                }
            }
        };

        var validator = new ConfigurationValidator(tempDir);
        validator.Validate(config);

        try
        {
            // Act & Assert
            var act = () => validator.ThrowIfInvalid();
            act.Should().NotThrow();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
