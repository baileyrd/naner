using System.Collections.Generic;
using Naner.Commands.Abstractions;

namespace Naner.Commands.Plugins;

/// <summary>
/// Interface for Naner plugins that can provide additional commands.
/// Plugins are loaded dynamically from DLL files in the plugins directory.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Gets the unique identifier for this plugin.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of this plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of this plugin.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets an optional description of what this plugin does.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the commands provided by this plugin.
    /// The dictionary key is the command name (e.g., "my-command").
    /// </summary>
    IReadOnlyDictionary<string, ICommand> Commands { get; }

    /// <summary>
    /// Called when the plugin is loaded.
    /// Use this for initialization tasks.
    /// </summary>
    void OnLoad();

    /// <summary>
    /// Called when the plugin is unloaded.
    /// Use this for cleanup tasks.
    /// </summary>
    void OnUnload();
}

/// <summary>
/// Base class for plugins that provides default implementations.
/// </summary>
public abstract class PluginBase : IPlugin
{
    private readonly Dictionary<string, ICommand> _commands = new();

    public abstract string Id { get; }
    public abstract string Name { get; }
    public virtual string Version => "1.0.0";
    public virtual string? Description => null;

    public IReadOnlyDictionary<string, ICommand> Commands => _commands;

    /// <summary>
    /// Registers a command with the plugin.
    /// </summary>
    /// <param name="commandName">The name to register the command under</param>
    /// <param name="command">The command implementation</param>
    protected void RegisterCommand(string commandName, ICommand command)
    {
        _commands[commandName] = command;
    }

    public virtual void OnLoad() { }
    public virtual void OnUnload() { }
}
