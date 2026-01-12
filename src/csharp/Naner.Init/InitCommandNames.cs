namespace Naner.Init;

/// <summary>
/// Constants for naner-init command-line command names.
/// Centralizes command strings to avoid duplication and typos.
/// </summary>
public static class InitCommandNames
{
    // Version commands
    public const string Version = "--version";
    public const string VersionShort = "-v";

    // Help commands
    public const string Help = "--help";
    public const string HelpShort = "-h";

    // Init-specific commands
    public const string Init = "init";
    public const string Update = "update";
    public const string CheckUpdate = "check-update";
    public const string UpdateVendors = "update-vendors";

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
        CheckUpdate,
        UpdateVendors
    };
}
