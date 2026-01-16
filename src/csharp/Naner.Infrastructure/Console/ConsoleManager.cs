using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Naner.Infrastructure.Console;

/// <summary>
/// Service for managing console attachment and allocation on Windows.
/// Provides singleton instance for consistent state tracking across the application.
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

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetFileType(IntPtr hFile);

    private const int ATTACH_PARENT_PROCESS = -1;
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint FILE_TYPE_CHAR = 0x0002;  // Console/character device
    private const uint FILE_TYPE_PIPE = 0x0003;  // Pipe (redirected)

    private static readonly Lazy<ConsoleManager> _instance = new(() => new ConsoleManager());

    /// <summary>
    /// Gets the singleton instance of ConsoleManager.
    /// Ensures consistent console state tracking across the application.
    /// </summary>
    public static ConsoleManager Instance => _instance.Value;

    private bool _isAttached = false;

    /// <summary>
    /// Checks if a console is currently attached.
    /// </summary>
    public bool HasConsole => GetConsoleWindow() != IntPtr.Zero;

    /// <summary>
    /// Checks if stdout is being captured by the parent process.
    /// For a WinExe app, if we have a valid stdout handle before attaching to a console,
    /// it means the parent process has set up redirection (pipe or file).
    /// In this case, we should NOT call AttachConsole as it would bypass the capture.
    /// </summary>
    public bool IsStdoutCaptured
    {
        get
        {
            var handle = GetStdHandle(STD_OUTPUT_HANDLE);
            // For a WinExe with no console, handle is typically INVALID_HANDLE_VALUE (-1) or null.
            // If we have a VALID handle (not -1, not 0), the parent set up redirection for us.
            if (handle == IntPtr.Zero || handle == new IntPtr(-1))
                return false;

            // We have a valid handle - parent is capturing our output
            return true;
        }
    }

    /// <summary>
    /// Ensures a console is attached for output.
    /// Tries to attach to parent console first, then allocates a new one if needed.
    /// </summary>
    /// <returns>True if console is available, false otherwise.</returns>
    public bool EnsureConsoleAttached()
    {
        // If stdout is being captured by parent (piped or redirected), don't attach to console.
        // AttachConsole would bypass the capture and write directly to the parent console,
        // which breaks scenarios like: pwsh -Command "& naner.exe --export-env | Invoke-Expression"
        if (IsStdoutCaptured)
        {
            return true;  // Output will go through the existing pipe/redirect
        }

        if (HasConsole)
        {
            return true;
        }

        // Try to attach to parent console (if launched from command line)
        if (AttachConsole(ATTACH_PARENT_PROCESS))
        {
            _isAttached = true;
            ReinitializeConsoleStreams();
            // Write a newline to move past the shell prompt that was already displayed
            // This is necessary because the parent shell returned to prompt before we attached
            System.Console.WriteLine();
            return true;
        }

        // Allocate a new console (if double-clicked or launched from GUI)
        if (AllocConsole())
        {
            _isAttached = true;
            ReinitializeConsoleStreams();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reinitializes Console.In, Console.Out, and Console.Error streams after
    /// attaching/allocating a console for a WinExe application.
    /// </summary>
    private static void ReinitializeConsoleStreams()
    {
        // Reopen stdout with explicit Windows line endings
        var stdOut = System.Console.OpenStandardOutput();
        var stdOutWriter = new StreamWriter(stdOut, System.Console.OutputEncoding)
        {
            AutoFlush = true,
            NewLine = "\r\n"  // Ensure Windows-style line endings for attached console
        };
        System.Console.SetOut(stdOutWriter);

        // Reopen stderr with explicit Windows line endings
        var stdErr = System.Console.OpenStandardError();
        var stdErrWriter = new StreamWriter(stdErr, System.Console.OutputEncoding)
        {
            AutoFlush = true,
            NewLine = "\r\n"  // Ensure Windows-style line endings for attached console
        };
        System.Console.SetError(stdErrWriter);

        // Reopen stdin
        var stdIn = System.Console.OpenStandardInput();
        var stdInReader = new StreamReader(stdIn, System.Console.InputEncoding);
        System.Console.SetIn(stdInReader);
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
            ReinitializeConsoleStreams();
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
            ReinitializeConsoleStreams();
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
