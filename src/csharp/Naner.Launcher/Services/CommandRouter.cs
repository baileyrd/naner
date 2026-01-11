using System;
using System.Collections.Generic;
using Naner.Launcher.Commands;

namespace Naner.Launcher.Services;

/// <summary>
/// Routes command-line arguments to appropriate command handlers.
/// Implements the Command pattern for better separation of concerns.
/// </summary>
public class CommandRouter
{
    private readonly Dictionary<string, ICommand> _commands;

    public CommandRouter()
    {
        _commands = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase)
        {
            ["--version"] = new VersionCommand(),
            ["-v"] = new VersionCommand(),
            ["--help"] = new HelpCommand(),
            ["-h"] = new HelpCommand(),
            ["/?"] = new HelpCommand(),
            ["--diagnose"] = new DiagnosticsCommand(),
            ["init"] = new InitCommand(),
            ["setup-vendors"] = new SetupVendorsCommand()
        };
    }

    /// <summary>
    /// Routes command-line arguments to the appropriate command.
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public int Route(string[] args)
    {
        if (args.Length == 0)
        {
            // No arguments - return special code to indicate launcher should run
            return -1;
        }

        var command = args[0];

        // Check if it's a registered command
        if (_commands.TryGetValue(command, out var handler))
        {
            // Execute the command with remaining arguments
            var commandArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();
            return handler.Execute(commandArgs);
        }

        // Not a registered command - return special code to indicate launcher should run
        return -1;
    }

    /// <summary>
    /// Checks if the command requires console output.
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    /// <returns>True if console is needed, false otherwise</returns>
    public static bool NeedsConsole(string[] args)
    {
        if (args.Length > 0)
        {
            var firstArg = args[0].ToLower();

            return firstArg switch
            {
                "--version" or "-v" => true,
                "--help" or "-h" or "/?" => true,
                "--diagnose" => true,
                "init" => true,
                "setup-vendors" => true,
                "--debug" => true,
                _ => false
            };
        }

        return false;
    }
}
