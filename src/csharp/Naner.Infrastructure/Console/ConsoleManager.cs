using System;
using System.Runtime.InteropServices;

namespace Naner.Infrastructure.Console;

/// <summary>
/// Service for managing console attachment and allocation on Windows.
/// Consolidates duplicate console management logic.
/// </summary>
public class ConsoleManager
{
    // Import Windows API for console attachment
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetConsoleWindow();

    private const int ATTACH_PARENT_PROCESS = -1;

    private bool _isAttached = false;

    /// <summary>
    /// Checks if a console is currently attached.
    /// </summary>
    public bool HasConsole => GetConsoleWindow() != IntPtr.Zero;

    /// <summary>
    /// Ensures a console is attached for output.
    /// Tries to attach to parent console first, then allocates a new one if needed.
    /// </summary>
    /// <returns>True if console is available, false otherwise.</returns>
    public bool EnsureConsoleAttached()
    {
        if (HasConsole)
        {
            return true;
        }

        // Try to attach to parent console (if launched from command line)
        if (AttachConsole(ATTACH_PARENT_PROCESS))
        {
            _isAttached = true;
            return true;
        }

        // Allocate a new console (if double-clicked or launched from GUI)
        if (AllocConsole())
        {
            _isAttached = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attaches to the parent process console.
    /// </summary>
    /// <returns>True if attached successfully, false otherwise.</returns>
    public bool AttachToParentConsole()
    {
        if (HasConsole)
        {
            return true;
        }

        if (AttachConsole(ATTACH_PARENT_PROCESS))
        {
            _isAttached = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Allocates a new console window.
    /// </summary>
    /// <returns>True if allocated successfully, false otherwise.</returns>
    public bool AllocateNewConsole()
    {
        if (HasConsole)
        {
            return true;
        }

        if (AllocConsole())
        {
            _isAttached = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Detaches from the current console (if we attached it).
    /// </summary>
    /// <returns>True if detached successfully, false otherwise.</returns>
    public bool DetachConsole()
    {
        if (!_isAttached || !HasConsole)
        {
            return false;
        }

        if (FreeConsole())
        {
            _isAttached = false;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if a console is needed based on command-line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="consoleCommands">Array of commands that require console output.</param>
    /// <returns>True if console is needed, false otherwise.</returns>
    public static bool NeedsConsole(string[] args, string[] consoleCommands)
    {
        if (args == null || args.Length == 0)
        {
            return false;
        }

        var firstArg = args[0].ToLower();

        foreach (var command in consoleCommands)
        {
            if (firstArg == command.ToLower())
            {
                return true;
            }
        }

        return false;
    }
}
