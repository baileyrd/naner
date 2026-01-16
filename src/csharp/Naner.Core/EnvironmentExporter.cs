using System;
using System.Collections.Generic;
using System.Text;

namespace Naner.Core;

/// <summary>
/// Exports environment configuration as shell commands for integration with external terminals.
/// Supports PowerShell, Bash, and CMD output formats.
/// </summary>
public class EnvironmentExporter
{
    /// <summary>
    /// Supported shell formats for environment export.
    /// </summary>
    public enum ShellFormat
    {
        PowerShell,
        Bash,
        Cmd
    }

    /// <summary>
    /// Parses a format string into a ShellFormat enum.
    /// </summary>
    /// <param name="format">Format string (powershell, bash, cmd)</param>
    /// <returns>Parsed ShellFormat</returns>
    /// <exception cref="ArgumentException">Thrown when format is not recognized</exception>
    public static ShellFormat ParseFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "powershell" or "ps" or "ps1" => ShellFormat.PowerShell,
            "bash" or "sh" or "zsh" => ShellFormat.Bash,
            "cmd" or "bat" or "batch" => ShellFormat.Cmd,
            _ => throw new ArgumentException($"Unknown format: {format}. Supported formats: powershell, bash, cmd")
        };
    }

    /// <summary>
    /// Exports environment variables as shell commands.
    /// </summary>
    /// <param name="environmentVariables">Dictionary of environment variables to export</param>
    /// <param name="path">The PATH value to set</param>
    /// <param name="format">Output shell format</param>
    /// <param name="noComments">If true, omit comment lines (useful for piping to Invoke-Expression)</param>
    /// <returns>Shell commands as a string</returns>
    public static string Export(
        IDictionary<string, string> environmentVariables,
        string path,
        ShellFormat format,
        bool noComments = false)
    {
        var sb = new StringBuilder();

        if (!noComments)
        {
            // Add header comment
            sb.AppendLine(FormatComment($"Naner Environment Setup - Generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}", format));
            sb.AppendLine(FormatComment("Source this file or execute these commands to configure your shell", format));
            sb.AppendLine(FormatComment("PATH Configuration", format));
        }

        // Export PATH first (most important)
        sb.AppendLine(FormatSetVariable("PATH", path, format));

        if (!noComments)
        {
            // Export other environment variables
            sb.AppendLine(FormatComment("Environment Variables", format));
        }

        foreach (var (key, value) in environmentVariables)
        {
            // Skip PATH as we already handled it
            if (key.Equals("PATH", StringComparison.OrdinalIgnoreCase))
                continue;

            sb.AppendLine(FormatSetVariable(key, value, format));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a comment for the specified shell.
    /// </summary>
    private static string FormatComment(string comment, ShellFormat format)
    {
        return format switch
        {
            ShellFormat.PowerShell => $"# {comment}",
            ShellFormat.Bash => $"# {comment}",
            ShellFormat.Cmd => $"REM {comment}",
            _ => $"# {comment}"
        };
    }

    /// <summary>
    /// Formats an environment variable assignment for the specified shell.
    /// </summary>
    private static string FormatSetVariable(string name, string value, ShellFormat format)
    {
        // Escape the value appropriately for each shell
        return format switch
        {
            ShellFormat.PowerShell => FormatPowerShellVariable(name, value),
            ShellFormat.Bash => FormatBashVariable(name, value),
            ShellFormat.Cmd => FormatCmdVariable(name, value),
            _ => FormatPowerShellVariable(name, value)
        };
    }

    /// <summary>
    /// Formats a PowerShell environment variable assignment.
    /// </summary>
    private static string FormatPowerShellVariable(string name, string value)
    {
        // Escape single quotes by doubling them
        var escaped = value.Replace("'", "''");
        return $"$env:{name} = '{escaped}'";
    }

    /// <summary>
    /// Formats a Bash environment variable assignment with export.
    /// </summary>
    private static string FormatBashVariable(string name, string value)
    {
        // For bash, we need to:
        // 1. Convert Windows paths to Unix-style if this is PATH
        // 2. Escape special characters

        if (name.Equals("PATH", StringComparison.OrdinalIgnoreCase))
        {
            // Convert Windows PATH to Unix-style
            value = ConvertPathToUnix(value);
        }
        else
        {
            // Convert Windows paths in other variables
            value = ConvertSinglePathToUnix(value);
        }

        // Escape special bash characters: $, `, \, ", !
        var escaped = value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("$", "\\$")
            .Replace("`", "\\`")
            .Replace("!", "\\!");

        return $"export {name}=\"{escaped}\"";
    }

    /// <summary>
    /// Formats a CMD environment variable assignment.
    /// </summary>
    private static string FormatCmdVariable(string name, string value)
    {
        // CMD uses SET command, need to escape special characters
        // In CMD, we escape % by doubling it
        var escaped = value.Replace("%", "%%");
        return $"SET \"{name}={escaped}\"";
    }

    /// <summary>
    /// Converts a Windows PATH string (semicolon-separated) to Unix-style (colon-separated with Unix paths).
    /// </summary>
    private static string ConvertPathToUnix(string windowsPath)
    {
        var paths = windowsPath.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var unixPaths = new List<string>();

        foreach (var path in paths)
        {
            unixPaths.Add(ConvertSinglePathToUnix(path));
        }

        return string.Join(":", unixPaths);
    }

    /// <summary>
    /// Converts a single Windows path to Unix-style.
    /// </summary>
    private static string ConvertSinglePathToUnix(string windowsPath)
    {
        if (string.IsNullOrEmpty(windowsPath))
            return windowsPath;

        // Convert backslashes to forward slashes
        var result = windowsPath.Replace('\\', '/');

        // Convert drive letters (C:\ -> /c/)
        if (result.Length >= 2 && char.IsLetter(result[0]) && result[1] == ':')
        {
            result = "/" + char.ToLower(result[0]) + result.Substring(2);
        }

        return result;
    }
}
