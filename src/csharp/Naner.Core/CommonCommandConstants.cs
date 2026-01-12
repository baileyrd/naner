namespace Naner.Core;

/// <summary>
/// Common command-line constants shared across Naner executables.
/// Eliminates duplication between CommandNames and InitCommandNames.
/// </summary>
public static class CommonCommandConstants
{
    // Version commands
    public const string Version = "--version";
    public const string VersionShort = "-v";

    // Help commands
    public const string Help = "--help";
    public const string HelpShort = "-h";
    public const string HelpAlternate = "/?";

    // Debug option
    public const string Debug = "--debug";
}
