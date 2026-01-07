using System.Management.Automation;
using System.Reflection;

namespace Naner.Launcher;

/// <summary>
/// Manages PowerShell script extraction and execution
/// </summary>
public static class PowerShellHost
{
    private static string? _scriptDirectory;

    /// <summary>
    /// Extracts embedded PowerShell scripts to a temporary directory
    /// </summary>
    /// <returns>Path to the directory containing extracted scripts</returns>
    public static string ExtractEmbeddedScripts()
    {
        // Create unique temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), $"naner_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        foreach (var resourceName in resourceNames)
        {
            // Extract .ps1 and .psm1 files
            if (resourceName.EndsWith(".ps1") || resourceName.EndsWith(".psm1"))
            {
                // Get the file name from the resource name
                // Resource names are like: Naner.Launcher.Resources.Invoke-Naner.ps1
                var fileName = resourceName.Split('.').TakeLast(2).Aggregate((a, b) => a + "." + b);
                var outputPath = Path.Combine(tempDir, fileName);

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var fileStream = File.Create(outputPath);
                    stream.CopyTo(fileStream);
                }
            }
        }

        _scriptDirectory = tempDir;
        return tempDir;
    }

    /// <summary>
    /// Executes a PowerShell script with arguments
    /// </summary>
    /// <param name="scriptPath">Path to the PowerShell script</param>
    /// <param name="arguments">List of arguments to pass</param>
    /// <param name="debugMode">Enable verbose output</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int ExecuteScript(string scriptPath, List<string> arguments, bool debugMode = false)
    {
        try
        {
            using var ps = PowerShell.Create();

            // Import Common module first
            var commonModule = Path.Combine(Path.GetDirectoryName(scriptPath)!, "Common.psm1");
            ps.AddScript($"Import-Module '{commonModule}' -Force -Global");
            ps.Invoke();

            if (ps.HadErrors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] Failed to import Common module");
                Console.ResetColor();
                foreach (var error in ps.Streams.Error)
                {
                    Console.WriteLine($"  {error}");
                }
                return 1;
            }

            // Clear previous commands
            ps.Commands.Clear();

            // Build the invocation command
            var argString = string.Join(" ", arguments);
            var command = $"& '{scriptPath}' {argString}";

            if (debugMode)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[DEBUG] Executing PowerShell: {command}");
                Console.ResetColor();
            }

            ps.AddScript(command);

            // Execute and capture output
            var results = ps.Invoke();

            // Display output
            foreach (var result in results)
            {
                if (result != null)
                {
                    Console.WriteLine(result.ToString());
                }
            }

            // Handle errors
            if (ps.HadErrors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var error in ps.Streams.Error)
                {
                    Console.Error.WriteLine($"Error: {error}");
                }
                Console.ResetColor();
                return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FATAL] PowerShell execution failed: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    /// <summary>
    /// Cleans up temporary script directory
    /// </summary>
    /// <param name="scriptDirectory">Directory to clean up (optional, uses last extracted if not provided)</param>
    public static void Cleanup(string? scriptDirectory = null)
    {
        var dirToClean = scriptDirectory ?? _scriptDirectory;
        if (dirToClean != null && Directory.Exists(dirToClean))
        {
            try
            {
                Directory.Delete(dirToClean, true);
            }
            catch
            {
                // Ignore cleanup errors - Windows will clean up temp files eventually
            }
        }
    }
}
