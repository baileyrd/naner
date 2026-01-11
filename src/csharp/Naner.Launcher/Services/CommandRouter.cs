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
            [CommandNames.Version] = new VersionCommand(),
            [CommandNames.VersionShort] = new VersionCommand(),
            [CommandNames.Help] = new HelpCommand(),
            [CommandNames.HelpShort] = new HelpCommand(),
            [CommandNames.HelpAlternate] = new HelpCommand(),
            [CommandNames.Diagnose] = new DiagnosticsCommand(),
            [CommandNames.Init] = new InitCommand(),
            [CommandNames.SetupVendors] = new SetupVendorsCommand()
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
            return CommandNames.NoCommandMatch;
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
        return CommandNames.NoCommandMatch;
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
                CommandNames.Version or CommandNames.VersionShort => true,
                CommandNames.Help or CommandNames.HelpShort or CommandNames.HelpAlternate => true,
                CommandNames.Diagnose => true,
                CommandNames.Init => true,
                CommandNames.SetupVendors => true,
                CommandNames.Debug => true,
                _ => false
            };
        }

        return false;
    }
}
