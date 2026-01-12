using Naner.Core;

namespace Naner.Init;

/// <summary>
/// Constants for naner-init command-line command names.
/// Centralizes command strings to avoid duplication and typos.
/// Inherits common constants from CommonCommandConstants.
/// </summary>
public static class InitCommandNames
{
    // Version commands (from shared constants)
    public const string Version = CommonCommandConstants.Version;
    public const string VersionShort = CommonCommandConstants.VersionShort;

    // Help commands (from shared constants)
    public const string Help = CommonCommandConstants.Help;
    public const string HelpShort = CommonCommandConstants.HelpShort;

    // Init-specific commands
    public const string Init = "init";
    public const string Update = "update";
    public const string CheckUpdate = "check-update";

    /// <summary>
    /// Commands that require console output.
    /// Used by ConsoleManager to determine console attachment.
    /// </summary>
    public static readonly string[] ConsoleCommands = new[]
    {
        Version,
        VersionShort,
        Help,
        HelpShort,
        Init,
        Update,
        CheckUpdate
    };
}
