using System.Collections.Generic;
using Naner.Common.Abstractions;

namespace Naner.Tests.Helpers;

/// <summary>
/// Test implementation of ILogger that captures log messages for verification.
/// </summary>
public class TestLogger : ILogger
{
    public List<string> StatusMessages { get; } = new();
    public List<string> SuccessMessages { get; } = new();
    public List<string> FailureMessages { get; } = new();
    public List<string> InfoMessages { get; } = new();
    public List<string> DebugMessages { get; } = new();
    public List<string> WarningMessages { get; } = new();
    public List<string> HeaderMessages { get; } = new();
    public int NewLineCount { get; private set; }

    public void Status(string message) => StatusMessages.Add(message);
    public void Success(string message) => SuccessMessages.Add(message);
    public void Failure(string message) => FailureMessages.Add(message);
    public void Info(string message) => InfoMessages.Add(message);
    public void Debug(string message, bool debugMode = false)
    {
        if (debugMode)
        {
            DebugMessages.Add(message);
        }
    }
    public void Warning(string message) => WarningMessages.Add(message);
    public void NewLine() => NewLineCount++;
    public void Header(string header) => HeaderMessages.Add(header);

    /// <summary>
    /// Clears all captured messages.
    /// </summary>
    public void Clear()
    {
        StatusMessages.Clear();
        SuccessMessages.Clear();
        FailureMessages.Clear();
        InfoMessages.Clear();
        DebugMessages.Clear();
        WarningMessages.Clear();
        HeaderMessages.Clear();
        NewLineCount = 0;
    }
}
