using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Naner.Common;
using Xunit;

namespace Naner.Tests;

/// <summary>
/// Tests for PathBuilder utility class.
/// </summary>
public class PathBuilderTests
{
    [Fact]
    public void BuildUnifiedPath_WithValidPaths_ReturnsCorrectPath()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var path1 = Path.Combine(tempDir, "bin");
        var path2 = Path.Combine(tempDir, "tools");
        Directory.CreateDirectory(path1);
        Directory.CreateDirectory(path2);

        var pathPrecedence = new List<string> { path1, path2 };

        try
        {
            // Act
            var result = PathBuilder.BuildUnifiedPath(pathPrecedence, tempDir, includeSystemPath: false);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain(path1);
            result.Should().Contain(path2);
            result.Should().StartWith(path1); // First path should be first
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
    public void BuildUnifiedPath_WithNonExistentPaths_SkipsThem()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var existingPath = Path.Combine(tempDir, "bin");
        var nonExistentPath = Path.Combine(tempDir, "nonexistent");
        Directory.CreateDirectory(existingPath);

        var pathPrecedence = new List<string> { existingPath, nonExistentPath };

        try
        {
            // Act
            var result = PathBuilder.BuildUnifiedPath(pathPrecedence, tempDir, includeSystemPath: false);

            // Assert
            result.Should().Contain(existingPath);
            result.Should().NotContain(nonExistentPath);
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
    public void BuildUnifiedPath_WithSystemPath_AppendsSystemPath()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var customPath = Path.Combine(tempDir, "bin");
        Directory.CreateDirectory(customPath);

        var pathPrecedence = new List<string> { customPath };
        var originalSystemPath = Environment.GetEnvironmentVariable("PATH");

        try
        {
            // Act
            var result = PathBuilder.BuildUnifiedPath(pathPrecedence, tempDir, includeSystemPath: true);

            // Assert
            result.Should().StartWith(customPath);
            if (!string.IsNullOrEmpty(originalSystemPath))
            {
                result.Should().Contain(originalSystemPath);
            }
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
    public void BuildUnifiedPath_WithNullPathPrecedence_ThrowsArgumentNullException()
    {
        // Arrange
        var tempDir = Path.GetTempPath();

        // Act & Assert
        var act = () => PathBuilder.BuildUnifiedPath(null!, tempDir, includeSystemPath: false);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pathPrecedence");
    }

    [Fact]
    public void BuildUnifiedPath_WithNullNanerRoot_ThrowsArgumentException()
    {
        // Arrange
        var pathPrecedence = new List<string> { "test" };

        // Act & Assert
        var act = () => PathBuilder.BuildUnifiedPath(pathPrecedence, null!, includeSystemPath: false);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("nanerRoot");
    }

    [Fact]
    public void SetProcessPath_SetsPathEnvironmentVariable()
    {
        // Arrange
        var testPath = "C:\\test\\path";
        var originalPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);

        try
        {
            // Act
            PathBuilder.SetProcessPath(testPath);

            // Assert
            var newPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
            newPath.Should().Be(testPath);
        }
        finally
        {
            // Restore
            if (originalPath != null)
            {
                Environment.SetEnvironmentVariable("PATH", originalPath, EnvironmentVariableTarget.Process);
            }
        }
    }

    [Fact]
    public void BuildAndSetPath_BuildsAndSetsPath()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var customPath = Path.Combine(tempDir, "bin");
        Directory.CreateDirectory(customPath);

        var pathPrecedence = new List<string> { customPath };
        var originalPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);

        try
        {
            // Act
            var result = PathBuilder.BuildAndSetPath(pathPrecedence, tempDir, includeSystemPath: false);

            // Assert
            result.Should().Contain(customPath);
            var envPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
            envPath.Should().Be(result);
        }
        finally
        {
            // Restore
            if (originalPath != null)
            {
                Environment.SetEnvironmentVariable("PATH", originalPath, EnvironmentVariableTarget.Process);
            }
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
