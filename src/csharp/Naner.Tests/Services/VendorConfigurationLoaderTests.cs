using System.IO;
using FluentAssertions;
using Naner.Vendors.Services;
using Naner.Tests.Helpers;
using Xunit;

namespace Naner.Tests.Services;

/// <summary>
/// Tests for VendorConfigurationLoader service.
/// </summary>
public class VendorConfigurationLoaderTests
{
    [Fact]
    public void LoadVendors_WhenFileDoesNotExist_ReturnsDefaultVendors()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var logger = new TestLogger();
        var loader = new VendorConfigurationLoader(tempDir, logger);

        try
        {
            // Act
            var vendors = loader.LoadVendors();

            // Assert
            vendors.Should().NotBeNull();
            vendors.Should().HaveCount(4); // Default vendors: 7-Zip, PowerShell, Windows Terminal, MSYS2
            vendors.Should().Contain(v => v.Name == "7-Zip");
            vendors.Should().Contain(v => v.Name == "PowerShell");
            vendors.Should().Contain(v => v.Name == "Windows Terminal");
            vendors.Should().Contain(v => v.Name == "MSYS2 (Git/Bash)");

            logger.WarningMessages.Should().Contain(m => m.Contains("Vendor configuration not found"));
            logger.InfoMessages.Should().Contain(m => m.Contains("Using default vendor definitions"));
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
    public void LoadVendors_WhenFileIsInvalid_ReturnsDefaultVendors()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var configDir = Path.Combine(tempDir, "config");
        Directory.CreateDirectory(configDir);
        var vendorsFile = Path.Combine(configDir, "vendors.json");
        File.WriteAllText(vendorsFile, "{ invalid json }");

        var logger = new TestLogger();
        var loader = new VendorConfigurationLoader(tempDir, logger);

        try
        {
            // Act
            var vendors = loader.LoadVendors();

            // Assert
            vendors.Should().NotBeNull();
            vendors.Should().HaveCount(4); // Default vendors
            logger.WarningMessages.Should().Contain(m => m.Contains("Failed to load vendor configuration"));
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
    public void LoadVendors_ParsesConfigFile_WhenConfigurationExists()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var configDir = Path.Combine(tempDir, "config");
        Directory.CreateDirectory(configDir);
        var vendorsFile = Path.Combine(configDir, "vendors.json");

        var json = @"{
  ""$schema"": ""./vendors-schema.json"",
  ""vendors"": {
    ""TestVendor"": {
      ""name"": ""Test"",
      ""extractDir"": ""test""
    }
  }
}";
        File.WriteAllText(vendorsFile, json);

        var logger = new TestLogger();
        var loader = new VendorConfigurationLoader(tempDir, logger);

        try
        {
            // Act
            var vendors = loader.LoadVendors();

            // Assert - Should parse the config file successfully
            vendors.Should().NotBeNull();
            vendors.Should().HaveCount(1);
            vendors.Should().Contain(v => v.Name == "Test");
            vendors.Should().Contain(v => v.Key == "TestVendor");
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
