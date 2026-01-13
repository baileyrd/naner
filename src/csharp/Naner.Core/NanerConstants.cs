using System.Reflection;

namespace Naner.Core;

/// <summary>
/// Centralized constants for Naner application.
/// Eliminates magic strings and provides single source of truth for configuration.
/// </summary>
public static class NanerConstants
{
    /// <summary>
    /// Gets the version from assembly metadata (set in Directory.Build.props).
    /// </summary>
    public static string Version { get; } = GetAssemblyVersion();

    public const string ProductName = "Naner Terminal Launcher";
    public const string PhaseName = "Production Release - Pure C# Implementation";

    private static string GetAssemblyVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        // Strip git hash suffix if present (e.g., "0.4.3+abc123" -> "0.4.3")
        if (infoVersion != null)
        {
            var plusIndex = infoVersion.IndexOf('+');
            return plusIndex > 0 ? infoVersion[..plusIndex] : infoVersion;
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    public const string InitializationMarkerFile = ".naner-initialized";
    public const string VersionFile = ".naner-version";
    public const string ConfigFileName = "naner.json";  // Default, but YAML also supported
    public const string VendorsConfigFileName = "vendors.json";

    /// <summary>
    /// Supported configuration file names in priority order.
    /// </summary>
    public static readonly string[] ConfigFileNames = new[] { "naner.json", "naner.yaml", "naner.yml" };

    /// <summary>
    /// GitHub repository information for releases and updates.
    /// </summary>
    public static class GitHub
    {
        public const string Owner = "baileyrd";
        public const string Repo = "naner";
        public static string UserAgent => $"Naner/{Version}";
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

        /// <summary>
        /// Essential directories required for a valid Naner installation.
        /// Used for validation and first-run detection.
        /// </summary>
        public static readonly string[] Essential = new[] { Bin, Vendor, Config, Home };

        /// <summary>
        /// All directories created during setup (includes optional directories).
        /// </summary>
        public static readonly string[] All = new[] { Bin, Vendor, VendorBin, Config, Home, Plugins, Logs };
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

    // ===== HTTP Configuration =====

    /// <summary>
    /// Default HTTP timeout for downloads and API requests, in minutes.
    /// </summary>
    public const int DefaultHttpTimeoutMinutes = 10;

    /// <summary>
    /// Buffer size for HTTP downloads, in bytes (8 KB).
    /// </summary>
    public const int HttpDownloadBufferSize = 8192;

    /// <summary>
    /// Progress update interval for downloads (report every N percent).
    /// </summary>
    public const int ProgressUpdateInterval = 10;

    // ===== Path Resolution =====

    /// <summary>
    /// Maximum depth to traverse when searching for Naner root directory.
    /// </summary>
    public const int MaxNanerRootSearchDepth = 10;

    /// <summary>
    /// Maximum length of PATH string to display in debug output.
    /// </summary>
    public const int MaxPathDisplayLength = 200;

    /// <summary>
    /// Default User-Agent string for HTTP requests.
    /// </summary>
    public static string DefaultUserAgent => $"Naner/{Version}";
}
