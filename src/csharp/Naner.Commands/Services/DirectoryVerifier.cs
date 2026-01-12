using System;
using System.IO;
using Naner.Commands.Abstractions;

namespace Naner.Commands.Services;

/// <summary>
/// Verifies Naner directory structure integrity.
/// Single Responsibility: Directory structure validation.
/// </summary>
public class DirectoryVerifier : IDirectoryVerifier
{
    /// <summary>
    /// Verifies the Naner directory structure exists.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory path</param>
    public void Verify(string nanerRoot)
    {
        Logger.Status("Verifying directory structure:");
        var dirs = new[] { "bin", "vendor", "config", "home" };
        foreach (var dir in dirs)
        {
            var path = Path.Combine(nanerRoot, dir);
            var exists = Directory.Exists(path);
            var symbol = exists ? "✓" : "✗";
            var color = exists ? ConsoleColor.Green : ConsoleColor.Red;

            Console.ForegroundColor = color;
            Console.WriteLine($"  {symbol} {dir}/");
            Console.ResetColor();
        }
        Logger.NewLine();
    }
}
