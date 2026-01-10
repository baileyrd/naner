using Naner.Common.Abstractions;

namespace Naner.Common;

/// <summary>
/// Static facade for console logging (maintains backward compatibility).
/// Delegates to ILogger implementation using the Adapter pattern.
/// </summary>
/// <remarks>
/// This class provides a static interface for all existing code while allowing
/// dependency injection of ILogger implementations for testing and modularity.
/// </remarks>
public static class Logger
{
    private static ILogger _instance = new ConsoleLogger();

    /// <summary>
    /// Sets custom logger implementation (for testing or custom output).
    /// </summary>
    /// <param name="logger">The logger implementation to use.</param>
    public static void SetLogger(ILogger logger)
    {
        _instance = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Writes a status message in cyan with [*] prefix.
    /// Used for informational progress updates.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public static void Status(string message) => _instance.Status(message);

    /// <summary>
    /// Writes a success message in green with [OK] prefix.
    /// Used for completed operations.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public static void Success(string message) => _instance.Success(message);

    /// <summary>
    /// Writes a failure message in red with [✗] prefix.
    /// Used for errors and failures.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public static void Failure(string message) => _instance.Failure(message);

    /// <summary>
    /// Writes an info message in gray with indentation.
    /// Used for detailed information under status messages.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public static void Info(string message) => _instance.Info(message);

    /// <summary>
    /// Writes a debug message in yellow with [DEBUG] prefix.
    /// Only outputs if debugMode is true.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="debugMode">Whether debug mode is enabled.</param>
    public static void Debug(string message, bool debugMode = false)
        => _instance.Debug(message, debugMode);

    /// <summary>
    /// Writes a warning message in yellow with [!] prefix.
    /// Used for warnings that don't stop execution.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public static void Warning(string message) => _instance.Warning(message);

    /// <summary>
    /// Writes a blank line for spacing.
    /// </summary>
    public static void NewLine() => _instance.NewLine();

    /// <summary>
    /// Writes a section header in cyan with separator line.
    /// </summary>
    /// <param name="header">The header text.</param>
    public static void Header(string header) => _instance.Header(header);
}
