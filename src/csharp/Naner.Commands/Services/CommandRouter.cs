using System;
using System.Collections.Generic;
using System.Linq;
using Naner.Commands.Abstractions;
using Naner.Commands.Plugins;
using Naner.Infrastructure.Console;

namespace Naner.Commands.Services;

/// <summary>
/// Routes command-line arguments to appropriate command handlers.
/// Implements the Command pattern for better separation of concerns.
/// Supports dynamic plugin loading for extensibility.
/// </summary>
public class CommandRouter : IDisposable
{
    private readonly Dictionary<string, ICommand> _commands;
    private readonly PluginLoader? _pluginLoader;
    private bool _disposed;

    /// <summary>
    /// Creates a new command router with built-in commands only.
    /// </summary>
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
            [CommandNames.SetupVendors] = new SetupVendorsCommand(),
            [CommandNames.UpdateVendors] = new UpdateVendorsCommand()
        };
    }

    /// <summary>
    /// Creates a new command router with plugin support.
    /// </summary>
    /// <param name="nanerRoot">The Naner root directory for plugin loading</param>
    /// <param name="loadPlugins">Whether to automatically load plugins</param>
    public CommandRouter(string nanerRoot, bool loadPlugins = true) : this()
    {
        _pluginLoader = new PluginLoader(nanerRoot);

        if (loadPlugins)
        {
            LoadPlugins();
        }
    }

    /// <summary>
    /// Gets the plugin loader for managing plugins.
    /// </summary>
    public PluginLoader? PluginLoader => _pluginLoader;

    /// <summary>
    /// Gets all registered command names (built-in and plugin).
    /// </summary>
    public IEnumerable<string> RegisteredCommands =>
        _commands.Keys.Concat(_pluginLoader?.Commands.Keys ?? Enumerable.Empty<string>()).Distinct();

    /// <summary>
    /// Loads plugins from the plugins directory.
    /// </summary>
    /// <returns>The number of plugins loaded</returns>
    public int LoadPlugins()
    {
        return _pluginLoader?.LoadPlugins() ?? 0;
    }

    /// <summary>
    /// Registers a custom command.
    /// </summary>
    /// <param name="commandName">The command name to register</param>
    /// <param name="command">The command implementation</param>
    public void RegisterCommand(string commandName, ICommand command)
    {
        _commands[commandName] = command;
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
        var commandArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();

        // Check built-in commands first
        if (_commands.TryGetValue(command, out var handler))
        {
            return handler.Execute(commandArgs);
        }

        // Check plugin commands
        var pluginCommand = _pluginLoader?.GetCommand(command);
        if (pluginCommand != null)
        {
            return pluginCommand.Execute(commandArgs);
        }

        // Not a registered command - return special code to indicate launcher should run
        return CommandNames.NoCommandMatch;
    }

    /// <summary>
    /// Checks if a command is registered (built-in or plugin).
    /// </summary>
    /// <param name="commandName">The command name to check</param>
    /// <returns>True if the command is registered</returns>
    public bool IsCommandRegistered(string commandName)
    {
        return _commands.ContainsKey(commandName) ||
               (_pluginLoader?.Commands.ContainsKey(commandName) ?? false);
    }

    /// <summary>
    /// Checks if the command requires console output.
    /// Delegates to ConsoleManager.NeedsConsole with CommandNames.ConsoleCommands.
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    /// <returns>True if console is needed, false otherwise</returns>
    public static bool NeedsConsole(string[] args)
    {
        // Delegate to ConsoleManager to eliminate duplication (DRY principle)
        return ConsoleManager.NeedsConsole(args, CommandNames.ConsoleCommands);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _pluginLoader?.Dispose();
        _disposed = true;
    }
}
