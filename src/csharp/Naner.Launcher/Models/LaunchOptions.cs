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
        HelpText = "Path to config file (supports .json, .yaml, .yml)")]
    public string? ConfigPath { get; set; }

    [Option("debug", Required = false,
        HelpText = "Enable debug/verbose output")]
    public bool Debug { get; set; }

    [Option('v', "version", Required = false,
        HelpText = "Display version information")]
    public bool Version { get; set; }

    [Option("setup-only", Required = false,
        HelpText = "Setup environment without launching terminal (outputs nothing, sets process env)")]
    public bool SetupOnly { get; set; }

    [Option("export-env", Required = false,
        HelpText = "Export environment setup commands for shell integration")]
    public bool ExportEnv { get; set; }

    [Option('f', "format", Required = false, Default = "powershell",
        HelpText = "Output format for --export-env (powershell, bash, cmd)")]
    public string Format { get; set; } = "powershell";

    [Option("no-comments", Required = false,
        HelpText = "Omit comments from --export-env output (useful when piping to Invoke-Expression)")]
    public bool NoComments { get; set; }
}
