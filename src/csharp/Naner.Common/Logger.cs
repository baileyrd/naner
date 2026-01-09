using System;

namespace Naner.Common;

/// <summary>
/// Console logger with color-coded output for Naner messages.
/// Migrated from Common.psm1 logging functions.
/// </summary>
public static class Logger
{
    /// <summary>
    /// Writes a status message in cyan with [*] prefix.
    /// Used for informational progress updates.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public static void Status(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[*] {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Writes a success message in green with [OK] prefix.
    /// Used for completed operations.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public static void Success(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK] {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Writes a failure message in red with [✗] prefix.
    /// Used for errors and failures.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public static void Failure(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[✗] {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Writes an info message in gray with indentation.
    /// Used for detailed information under status messages.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public static void Info(string message)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"    {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Writes a debug message in yellow with [DEBUG] prefix.
    /// Only outputs if debugMode is true.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="debugMode">Whether debug mode is enabled.</param>
    public static void Debug(string message, bool debugMode = false)
    {
        if (!debugMode)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[DEBUG] {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Writes a warning message in yellow with [!] prefix.
    /// Used for warnings that don't stop execution.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public static void Warning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[!] {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Writes a blank line for spacing.
    /// </summary>
    public static void NewLine()
    {
        Console.WriteLine();
    }

    /// <summary>
    /// Writes a section header in cyan with separator line.
    /// </summary>
    /// <param name="header">The header text.</param>
    public static void Header(string header)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(header);
        Console.WriteLine(new string('=', header.Length));
        Console.ResetColor();
        Console.WriteLine();
    }
}
