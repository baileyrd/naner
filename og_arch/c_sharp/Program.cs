using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;

namespace CmderLauncher;

/// <summary>
/// Cmder Windows Terminal Launcher - C# Edition
/// Port of the PowerShell prototype with performance optimizations
/// </summary>
class Program
{
    private const string Version = "2.0.0-csharp";
    
    static async Task<int> Main(string[] args)
    {
        // TODO: Port from PowerShell - See Launch-Cmder.ps1
        
        // Command-line argument parsing
        var rootCommand = BuildCommandLine();
        
        // Execute
        return await rootCommand.InvokeAsync(args);
    }
    
    static RootCommand BuildCommandLine()
    {
        // TODO: Implement command-line parsing
        // Reference: PowerShell param() block in Launch-Cmder.ps1
        
        var rootCommand = new RootCommand("Cmder Windows Terminal Launcher");
        
        // Add options
        var startDirOption = new Option<string?>(
            aliases: new[] { "--start", "-s" },
            description: "Starting directory for the terminal");
        
        var profileOption = new Option<string>(
            aliases: new[] { "--profile", "-p" },
            getDefaultValue: () => "Cmder",
            description: "Windows Terminal profile to use");
        
        var singleOption = new Option<bool>(
            aliases: new[] { "--single" },
            description: "Open in existing window if possible");
        
        var registerOption = new Option<string?>(
            aliases: new[] { "--register" },
            description: "Register shell integration (USER or ALL)");
        
        var unregisterOption = new Option<string?>(
            aliases: new[] { "--unregister" },
            description: "Unregister shell integration (USER or ALL)");
        
        rootCommand.AddOption(startDirOption);
        rootCommand.AddOption(profileOption);
        rootCommand.AddOption(singleOption);
        rootCommand.AddOption(registerOption);
        rootCommand.AddOption(unregisterOption);
        
        rootCommand.SetHandler(
            (startDir, profile, single, register, unregister) =>
            {
                return LaunchCmder(startDir, profile, single, register, unregister);
            },
            startDirOption, profileOption, singleOption, registerOption, unregisterOption);
        
        return rootCommand;
    }
    
    static int LaunchCmder(
        string? startDir,
        string profile,
        bool single,
        string? register,
        string? unregister)
    {
        // TODO: Port main logic from PowerShell Main function
        
        Console.WriteLine($"Cmder Windows Terminal Launcher v{Version}");
        Console.WriteLine("================================================\n");
        
        // Handle registration
        if (!string.IsNullOrEmpty(register))
        {
            return HandleRegistration(register, isRegister: true);
        }
        
        if (!string.IsNullOrEmpty(unregister))
        {
            return HandleRegistration(unregister, isRegister: false);
        }
        
        // Initialize environment
        InitializeCmderEnvironment();
        
        // Find Windows Terminal
        var wtPath = FindWindowsTerminal();
        if (wtPath == null)
        {
            Console.WriteLine("ERROR: Windows Terminal not found!");
            Console.WriteLine("\nPlease install from:");
            Console.WriteLine("  - Microsoft Store: https://aka.ms/terminal");
            Console.WriteLine("  - GitHub: https://github.com/microsoft/terminal/releases\n");
            return 1;
        }
        
        // Determine starting directory
        var targetDir = startDir ?? Directory.GetCurrentDirectory();
        
        // Launch Windows Terminal
        return LaunchWindowsTerminal(wtPath, profile, targetDir, single) ? 0 : 1;
    }
    
    static void InitializeCmderEnvironment()
    {
        // TODO: Port from PowerShell Initialize-CmderEnvironment
        
        var cmderRoot = GetCmderRoot();
        
        Environment.SetEnvironmentVariable("CMDER_ROOT", cmderRoot);
        Environment.SetEnvironmentVariable("CMDER_USER_CONFIG", 
            Path.Combine(cmderRoot, "config"));
        Environment.SetEnvironmentVariable("CMDER_USER_BIN", 
            Path.Combine(cmderRoot, "bin"));
        
        // Create directories if needed
        Directory.CreateDirectory(Path.Combine(cmderRoot, "config"));
        Directory.CreateDirectory(Path.Combine(cmderRoot, "bin"));
    }
    
    static string GetCmderRoot()
    {
        // TODO: Implement smart detection
        var exePath = Environment.ProcessPath ?? 
                     System.Reflection.Assembly.GetExecutingAssembly().Location;
        return Path.GetDirectoryName(exePath) ?? Directory.GetCurrentDirectory();
    }
    
    static string? FindWindowsTerminal()
    {
        // TODO: Port from PowerShell Find-WindowsTerminal
        
        var searchPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\WindowsApps\wt.exe"),
            @"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_*\wt.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\WindowsApps\wtd.exe")
        };
        
        foreach (var path in searchPaths)
        {
            if (path.Contains("*"))
            {
                // Handle wildcards
                var dir = Path.GetDirectoryName(path);
                var pattern = Path.GetFileName(path);
                
                if (Directory.Exists(dir))
                {
                    var matches = Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);
                    if (matches.Length > 0)
                        return matches.OrderByDescending(File.GetLastWriteTime).First();
                }
            }
            else if (File.Exists(path))
            {
                return path;
            }
        }
        
        // Check PATH
        var wtPath = FindInPath("wt.exe");
        if (wtPath != null)
            return wtPath;
        
        return null;
    }
    
    static string? FindInPath(string executable)
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? Array.Empty<string>();
        
        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path.Trim(), executable);
            if (File.Exists(fullPath))
                return fullPath;
        }
        
        return null;
    }
    
    static bool LaunchWindowsTerminal(
        string wtPath,
        string profile,
        string startDir,
        bool openInExisting)
    {
        // TODO: Port from PowerShell Start-WindowsTerminal
        
        try
        {
            var args = new List<string>();
            
            if (openInExisting)
                args.AddRange(new[] { "-w", "0" });
            else
                args.Add("new-tab");
            
            args.AddRange(new[] { "-p", $"\"{profile}\"" });
            args.AddRange(new[] { "-d", $"\"{startDir}\"" });
            
            var psi = new ProcessStartInfo
            {
                FileName = wtPath,
                Arguments = string.Join(" ", args),
                UseShellExecute = false
            };
            
            Process.Start(psi);
            
            Console.WriteLine("\n✓ Launched successfully!\n");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: Failed to launch Windows Terminal: {ex.Message}\n");
            return false;
        }
    }
    
    static int HandleRegistration(string scope, bool isRegister)
    {
        // TODO: Port from PowerShell Register/Unregister functions
        
        Console.WriteLine($"{(isRegister ? "Registering" : "Unregistering")} " +
                         $"shell integration ({scope})...");
        
        // Check admin rights for ALL scope
        if (scope.ToUpper() == "ALL" && !IsAdministrator())
        {
            Console.WriteLine("ERROR: Administrator rights required for ALL scope");
            return 1;
        }
        
        // TODO: Implement registry operations
        Console.WriteLine("TODO: Registry operations not yet implemented in C# version");
        Console.WriteLine("Please use the PowerShell version for shell integration");
        
        return 1;
    }
    
    static bool IsAdministrator()
    {
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
}
