namespace Naner.Common;

/// <summary>
/// Centralized constants for Naner application.
/// Eliminates magic strings and provides single source of truth for configuration.
/// </summary>
public static class NanerConstants
{
    public const string Version = "1.0.0";
    public const string ProductName = "Naner Terminal Launcher";
    public const string PhaseName = "Production Release - Pure C# Implementation";

    public const string InitializationMarkerFile = ".naner-initialized";
    public const string VersionFile = ".naner-version";
    public const string ConfigFileName = "naner.json";
    public const string VendorsConfigFileName = "vendors.json";

    /// <summary>
    /// GitHub repository information for releases and updates.
    /// </summary>
    public static class GitHub
    {
        public const string Owner = "baileyrd";
        public const string Repo = "naner";
        public const string UserAgent = "Naner/1.0.0";
    }

    /// <summary>
    /// Standard directory names within Naner installation.
    /// </summary>
    public static class DirectoryNames
    {
        public const string Bin = "bin";
        public const string Vendor = "vendor";
        public const string VendorBin = "vendor/bin";
        public const string Config = "config";
        public const string Home = "home";
        public const string Plugins = "plugins";
        public const string Logs = "logs";
        public const string Downloads = ".downloads";
    }

    /// <summary>
    /// Common executable file names.
    /// </summary>
    public static class Executables
    {
        public const string Naner = "naner.exe";
        public const string NanerInit = "naner-init.exe";
        public const string WindowsTerminal = "wt.exe";
        public const string PowerShell = "pwsh.exe";
        public const string Bash = "bash.exe";
        public const string SevenZip = "7z.exe";
    }

    /// <summary>
    /// Default vendor package names.
    /// </summary>
    public static class VendorNames
    {
        public const string SevenZip = "7-Zip";
        public const string PowerShell = "PowerShell";
        public const string WindowsTerminal = "Windows Terminal";
        public const string MSYS2 = "MSYS2 (Git/Bash)";
    }
}
