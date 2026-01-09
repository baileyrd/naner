using System;

namespace Naner.Init;

/// <summary>
/// Helper class for console output formatting.
/// </summary>
public static class ConsoleHelper
{
    public static void Header(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n=== {message} ===");
        Console.ResetColor();
    }

    public static void Success(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
    }

    public static void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
    }

    public static void Warning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {message}");
        Console.ResetColor();
    }

    public static void Info(string message)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"ℹ {message}");
        Console.ResetColor();
    }

    public static void Status(string message)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"→ {message}");
        Console.ResetColor();
    }

    public static void NewLine()
    {
        Console.WriteLine();
    }
}
