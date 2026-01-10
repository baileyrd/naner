using System;

namespace Naner.Launcher.Commands;

/// <summary>
/// Displays help information.
/// </summary>
public class HelpCommand : ICommand
{
    public int Execute(string[] args)
    {
        Console.WriteLine("Naner - Unified Terminal Environment Manager");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  naner [options]                Launch default terminal profile");
        Console.WriteLine("  naner <profile>                Launch specific profile");
        Console.WriteLine("  naner [command]                Run special command");
        Console.WriteLine();
        Console.WriteLine("PROFILES:");
        Console.WriteLine("  Unified                        Launch unified PowerShell+Bash environment");
        Console.WriteLine("  PowerShell                     Launch PowerShell profile");
        Console.WriteLine("  Bash                           Launch Bash (MSYS2) profile");
        Console.WriteLine("  CMD                            Launch CMD profile");
        Console.WriteLine();
        Console.WriteLine("COMMANDS:");
        Console.WriteLine("  init                           Initialize Naner environment");
        Console.WriteLine("  setup-vendors                  Download and setup vendor tools");
        Console.WriteLine("  --diagnose                     Run system diagnostics");
        Console.WriteLine("  --version, -v                  Show version information");
        Console.WriteLine("  --help, -h, /?                 Show this help message");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  -p, --profile <name>           Profile to launch");
        Console.WriteLine("  -d, --directory <path>         Starting directory");
        Console.WriteLine("  -c, --config <path>            Custom config file path");
        Console.WriteLine("  --debug                        Enable debug output");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  naner                          # Launch default profile");
        Console.WriteLine("  naner Unified                  # Launch Unified profile");
        Console.WriteLine("  naner -p Bash -d ~/projects    # Launch Bash in ~/projects");
        Console.WriteLine("  naner --diagnose               # Run diagnostics");
        Console.WriteLine();
        Console.WriteLine("For more information, see the documentation at:");
        Console.WriteLine("https://github.com/baileyrd/naner");
        return 0;
    }
}
