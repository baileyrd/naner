namespace Naner.Commands;

/// <summary>
/// Constants for command-line command names.
/// Centralizes command strings to avoid duplication and typos.
/// </summary>
public static class CommandNames
{
    // Version commands
    public const string Version = "--version";
    public const string VersionShort = "-v";

    // Help commands
    public const string Help = "--help";
    public const string HelpShort = "-h";
    public const string HelpAlternate = "/?";

    // Diagnostic commands
    public const string Diagnose = "--diagnose";

    // Setup commands
    public const string Init = "init";
    public const string SetupVendors = "setup-vendors";

    // Options
    public const string Debug = "--debug";

    // Special return codes
    public const int NoCommandMatch = -1;

    /// <summary>
    /// Commands that require console output.
    /// Used by ConsoleManager and CommandRouter to determine console attachment.
    /// </summary>
    public static readonly string[] ConsoleCommands = new[]
    {
        Version,
        VersionShort,
        Help,
        HelpShort,
        HelpAlternate,
        Diagnose,
        Init,
        SetupVendors,
        Debug
    };
}
