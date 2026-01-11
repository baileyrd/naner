using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Naner.Commands.Abstractions;

namespace Naner.Commands.Plugins;

/// <summary>
/// Loads and manages Naner plugins from the plugins directory.
/// Plugins are loaded from DLL files that implement the IPlugin interface.
/// </summary>
public class PluginLoader : IDisposable
{
    private readonly string _pluginsDirectory;
    private readonly List<LoadedPlugin> _loadedPlugins = new();
    private readonly Dictionary<string, ICommand> _pluginCommands = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new plugin loader.
    /// </summary>
    /// <param name="nanerRoot">The Naner root directory path</param>
    public PluginLoader(string nanerRoot)
    {
        _pluginsDirectory = Path.Combine(nanerRoot, "plugins");
    }

    /// <summary>
    /// Gets all loaded plugins.
    /// </summary>
    public IReadOnlyList<IPlugin> Plugins => _loadedPlugins.Select(p => p.Plugin).ToList().AsReadOnly();

    /// <summary>
    /// Gets all commands provided by plugins.
    /// </summary>
    public IReadOnlyDictionary<string, ICommand> Commands => _pluginCommands;

    /// <summary>
    /// Loads all plugins from the plugins directory.
    /// </summary>
    /// <returns>The number of plugins loaded</returns>
    public int LoadPlugins()
    {
        if (!Directory.Exists(_pluginsDirectory))
        {
            Logger.Debug($"Plugins directory not found: {_pluginsDirectory}", debugMode: false);
            return 0;
        }

        var dllFiles = Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.AllDirectories);
        var loadedCount = 0;

        foreach (var dllPath in dllFiles)
        {
            try
            {
                if (LoadPlugin(dllPath))
                {
                    loadedCount++;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load plugin from {Path.GetFileName(dllPath)}: {ex.Message}");
            }
        }

        return loadedCount;
    }

    /// <summary>
    /// Loads a single plugin from a DLL file.
    /// </summary>
    /// <param name="dllPath">Path to the plugin DLL</param>
    /// <returns>True if plugin was loaded successfully</returns>
    public bool LoadPlugin(string dllPath)
    {
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException($"Plugin file not found: {dllPath}");
        }

        // Create isolated load context for the plugin
        var loadContext = new PluginLoadContext(dllPath);
        var assembly = loadContext.LoadFromAssemblyPath(dllPath);

        // Find types that implement IPlugin
        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        if (pluginTypes.Count == 0)
        {
            Logger.Debug($"No plugin types found in {Path.GetFileName(dllPath)}", debugMode: false);
            return false;
        }

        var pluginsLoaded = false;

        foreach (var pluginType in pluginTypes)
        {
            try
            {
                var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;

                // Check for duplicate plugin IDs
                if (_loadedPlugins.Any(p => p.Plugin.Id == plugin.Id))
                {
                    Logger.Warning($"Plugin with ID '{plugin.Id}' is already loaded, skipping");
                    continue;
                }

                // Load the plugin
                plugin.OnLoad();

                // Register plugin commands
                foreach (var (commandName, command) in plugin.Commands)
                {
                    if (_pluginCommands.ContainsKey(commandName))
                    {
                        Logger.Warning($"Command '{commandName}' from plugin '{plugin.Name}' conflicts with existing command");
                        continue;
                    }

                    _pluginCommands[commandName] = command;
                    Logger.Debug($"Registered plugin command: {commandName}", debugMode: false);
                }

                _loadedPlugins.Add(new LoadedPlugin(plugin, loadContext, dllPath));
                Logger.Info($"Loaded plugin: {plugin.Name} v{plugin.Version}");
                pluginsLoaded = true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to create plugin instance {pluginType.Name}: {ex.Message}");
            }
        }

        return pluginsLoaded;
    }

    /// <summary>
    /// Unloads a plugin by its ID.
    /// </summary>
    /// <param name="pluginId">The plugin ID to unload</param>
    /// <returns>True if plugin was unloaded</returns>
    public bool UnloadPlugin(string pluginId)
    {
        var loadedPlugin = _loadedPlugins.FirstOrDefault(p => p.Plugin.Id == pluginId);
        if (loadedPlugin == null)
        {
            return false;
        }

        // Remove plugin commands
        foreach (var commandName in loadedPlugin.Plugin.Commands.Keys)
        {
            _pluginCommands.Remove(commandName);
        }

        // Unload the plugin
        loadedPlugin.Plugin.OnUnload();
        loadedPlugin.LoadContext.Unload();
        _loadedPlugins.Remove(loadedPlugin);

        Logger.Info($"Unloaded plugin: {loadedPlugin.Plugin.Name}");
        return true;
    }

    /// <summary>
    /// Gets a plugin by its ID.
    /// </summary>
    /// <param name="pluginId">The plugin ID</param>
    /// <returns>The plugin if found, null otherwise</returns>
    public IPlugin? GetPlugin(string pluginId)
    {
        return _loadedPlugins.FirstOrDefault(p => p.Plugin.Id == pluginId)?.Plugin;
    }

    /// <summary>
    /// Gets a command from loaded plugins.
    /// </summary>
    /// <param name="commandName">The command name</param>
    /// <returns>The command if found, null otherwise</returns>
    public ICommand? GetCommand(string commandName)
    {
        return _pluginCommands.TryGetValue(commandName, out var command) ? command : null;
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var loadedPlugin in _loadedPlugins)
        {
            try
            {
                loadedPlugin.Plugin.OnUnload();
                loadedPlugin.LoadContext.Unload();
            }
            catch (Exception ex)
            {
                Logger.Debug($"Error unloading plugin {loadedPlugin.Plugin.Name}: {ex.Message}", debugMode: false);
            }
        }

        _loadedPlugins.Clear();
        _pluginCommands.Clear();
        _disposed = true;
    }

    private record LoadedPlugin(IPlugin Plugin, PluginLoadContext LoadContext, string FilePath);
}

/// <summary>
/// Isolated assembly load context for plugins.
/// Enables plugin unloading and isolation from the main application.
/// </summary>
internal class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
