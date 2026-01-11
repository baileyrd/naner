using CommandLine;

namespace Naner.Launcher.Models;

/// <summary>
/// Command-line options for launching Naner terminal.
/// Parsed using CommandLineParser library.
/// </summary>
public class LaunchOptions
{
    [Option('p', "profile", Required = false,
        HelpText = "Terminal profile to launch (Unified, PowerShell, Bash, CMD)")]
    public string? Profile { get; set; }

    [Option('e', "environment", Required = false,
        HelpText = "Environment name (default, work, personal, etc.)")]
    public string Environment { get; set; } = "default";

    [Option('d', "directory", Required = false,
        HelpText = "Starting directory for terminal session")]
    public string? Directory { get; set; }

    [Option('c', "config", Required = false,
        HelpText = "Path to naner.json config file")]
    public string? ConfigPath { get; set; }

    [Option("debug", Required = false,
        HelpText = "Enable debug/verbose output")]
    public bool Debug { get; set; }

    [Option('v', "version", Required = false,
        HelpText = "Display version information")]
    public bool Version { get; set; }
}
