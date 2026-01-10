using FluentAssertions;
using Naner.Common;
using Xunit;

namespace Naner.Tests;

/// <summary>
/// Tests for NanerConstants to ensure values are set correctly.
/// </summary>
public class NanerConstantsTests
{
    [Fact]
    public void Version_IsNotEmpty()
    {
        // Assert
        NanerConstants.Version.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ProductName_IsNotEmpty()
    {
        // Assert
        NanerConstants.ProductName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ConfigFileName_IsCorrect()
    {
        // Assert
        NanerConstants.ConfigFileName.Should().Be("naner.json");
    }

    [Fact]
    public void VendorsConfigFileName_IsCorrect()
    {
        // Assert
        NanerConstants.VendorsConfigFileName.Should().Be("vendors.json");
    }

    [Fact]
    public void GitHub_Owner_IsSet()
    {
        // Assert
        NanerConstants.GitHub.Owner.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GitHub_Repo_IsSet()
    {
        // Assert
        NanerConstants.GitHub.Repo.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void DirectoryNames_AreSet()
    {
        // Assert
        NanerConstants.DirectoryNames.Bin.Should().Be("bin");
        NanerConstants.DirectoryNames.Vendor.Should().Be("vendor");
        NanerConstants.DirectoryNames.Config.Should().Be("config");
        NanerConstants.DirectoryNames.Home.Should().Be("home");
    }

    [Fact]
    public void Executables_AreSet()
    {
        // Assert
        NanerConstants.Executables.Naner.Should().Be("naner.exe");
        NanerConstants.Executables.NanerInit.Should().Be("naner-init.exe");
        NanerConstants.Executables.PowerShell.Should().Be("pwsh.exe");
    }

    [Fact]
    public void VendorNames_AreSet()
    {
        // Assert
        NanerConstants.VendorNames.SevenZip.Should().Be("7-Zip");
        NanerConstants.VendorNames.PowerShell.Should().Be("PowerShell");
        NanerConstants.VendorNames.WindowsTerminal.Should().Be("Windows Terminal");
    }
}
