using System;
using System.Linq;

namespace Naner.Init;

/// <summary>
/// Utility for comparing semantic versions.
/// </summary>
public static class VersionComparer
{
    /// <summary>
    /// Compares two version strings.
    /// </summary>
    /// <returns>
    /// -1 if version1 < version2,
    /// 0 if version1 == version2,
    /// 1 if version1 > version2
    /// </returns>
    public static int Compare(string version1, string version2)
    {
        var v1 = ParseVersion(version1);
        var v2 = ParseVersion(version2);

        if (v1.Major != v2.Major)
            return v1.Major.CompareTo(v2.Major);

        if (v1.Minor != v2.Minor)
            return v1.Minor.CompareTo(v2.Minor);

        if (v1.Patch != v2.Patch)
            return v1.Patch.CompareTo(v2.Patch);

        return 0;
    }

    /// <summary>
    /// Checks if version1 is newer than version2.
    /// </summary>
    public static bool IsNewer(string version1, string version2)
    {
        return Compare(version1, version2) > 0;
    }

    /// <summary>
    /// Parses a version string into components.
    /// Handles formats like: "v1.0.0", "1.0.0", "1.0.0-beta"
    /// </summary>
    private static (int Major, int Minor, int Patch) ParseVersion(string version)
    {
        // Remove 'v' prefix if present
        version = version.TrimStart('v', 'V');

        // Remove any suffix after '-' (like -beta, -rc1, etc.)
        var dashIndex = version.IndexOf('-');
        if (dashIndex > 0)
        {
            version = version.Substring(0, dashIndex);
        }

        var parts = version.Split('.');
        var major = parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 0;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;
        var patch = parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : 0;

        return (major, minor, patch);
    }

    /// <summary>
    /// Normalizes a version string (removes 'v' prefix and suffixes).
    /// </summary>
    public static string Normalize(string version)
    {
        version = version.TrimStart('v', 'V');
        var dashIndex = version.IndexOf('-');
        if (dashIndex > 0)
        {
            version = version.Substring(0, dashIndex);
        }
        return version;
    }
}
