using Naner.Core;

namespace Naner.Commands;

/// <summary>
/// Constants for command-line command names.
/// Centralizes command strings to avoid duplication and typos.
/// Inherits common constants from CommonCommandConstants.
/// </summary>
public static class CommandNames
{
    // Version commands (from shared constants)
    public const string Version = CommonCommandConstants.Version;
    public const string VersionShort = CommonCommandConstants.VersionShort;

    // Help commands (from shared constants)
    public const string Help = CommonCommandConstants.Help;
    public const string HelpShort = CommonCommandConstants.HelpShort;
    public const string HelpAlternate = CommonCommandConstants.HelpAlternate;

    // Diagnostic commands
    public const string Diagnose = "--diagnose";

    // Setup commands
    public const string Init = "init";
    public const string SetupVendors = "setup-vendors";

    // Options (from shared constants)
    public const string Debug = CommonCommandConstants.Debug;

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
