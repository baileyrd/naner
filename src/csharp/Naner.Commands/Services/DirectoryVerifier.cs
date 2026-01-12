using System;
using System.IO;
using Naner.Commands.Abstractions;
using Naner.Core;

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
        // Use centralized constant for essential directories (DRY principle)
        foreach (var dir in NanerConstants.DirectoryNames.Essential)
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
