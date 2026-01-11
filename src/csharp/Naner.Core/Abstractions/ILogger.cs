namespace Naner.Core.Abstractions;

/// <summary>
/// Logging abstraction for Naner applications.
/// </summary>
public interface ILogger
{
    void Status(string message);
    void Success(string message);
    void Failure(string message);
    void Info(string message);
    void Debug(string message, bool debugMode = false);
    void Warning(string message);
    void NewLine();
    void Header(string header);
}

/// <summary>
/// Console-based implementation of ILogger.
/// </summary>
public class ConsoleLogger : ILogger
{
    public void Status(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[*] {message}");
        Console.ResetColor();
    }

    public void Success(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK] {message}");
        Console.ResetColor();
    }

    public void Failure(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[✗] {message}");
        Console.ResetColor();
    }

    public void Info(string message)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"    {message}");
        Console.ResetColor();
    }

    public void Debug(string message, bool debugMode = false)
    {
        if (!debugMode) return;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[DEBUG] {message}");
        Console.ResetColor();
    }

    public void Warning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[!] {message}");
        Console.ResetColor();
    }

    public void NewLine()
    {
        Console.WriteLine();
    }

    public void Header(string header)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(header);
        Console.WriteLine(new string('=', header.Length));
        Console.ResetColor();
        Console.WriteLine();
    }
}
