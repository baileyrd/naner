using System;
using Naner.Common;

namespace Naner.Launcher.Services;

/// <summary>
/// Provides formatted help text for the Naner launcher.
/// Centralizes help content to separate presentation from logic.
/// </summary>
public class HelpTextProvider
{
    /// <summary>
    /// Displays the complete help text for Naner.
    /// </summary>
    public void ShowHelp()
    {
        Logger.Header("Naner Terminal Launcher");
        Console.WriteLine($"Version {NanerConstants.Version} - {NanerConstants.PhaseName}");
        Console.WriteLine();

        ShowUsage();
        ShowCommands();
        ShowOptions();
        ShowExamples();
        ShowRequirements();
        ShowDocumentation();
    }

    /// <summary>
    /// Displays usage information.
    /// </summary>
    private void ShowUsage()
    {
        Console.WriteLine("USAGE:");
        Console.WriteLine("  naner.exe [OPTIONS]");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays available commands.
    /// </summary>
    private void ShowCommands()
    {
        Console.WriteLine("COMMANDS:");
        Console.WriteLine("  init [PATH]                Initialize Naner in specified directory");
        Console.WriteLine("                             Options: --minimal, --quick, --skip-vendors, --with-vendors");
        Console.WriteLine("  setup-vendors              Download and install vendor dependencies");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays available options.
    /// </summary>
    private void ShowOptions()
    {
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  -p, --profile <NAME>       Terminal profile to launch");
        Console.WriteLine("                             (Unified, PowerShell, Bash, CMD)");
        Console.WriteLine("  -e, --environment <NAME>   Environment name (default, work, etc.)");
        Console.WriteLine("  -d, --directory <PATH>     Starting directory for terminal");
        Console.WriteLine("  -c, --config <PATH>        Path to naner.json config file");
        Console.WriteLine("  --debug                    Enable debug/verbose output");
        Console.WriteLine("  -v, --version              Display version information");
        Console.WriteLine("  -h, --help                 Display this help message");
        Console.WriteLine("  --diagnose                 Run diagnostic checks");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays usage examples.
    /// </summary>
    private void ShowExamples()
    {
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  naner.exe init                     # Interactive setup wizard");
        Console.WriteLine("  naner.exe init --minimal           # Quick setup in current dir");
        Console.WriteLine("  naner.exe init --minimal --with-vendors  # Setup with auto vendor download");
        Console.WriteLine("  naner.exe init C:\\MyNaner          # Setup in specific directory");
        Console.WriteLine("  naner.exe                          # Launch default profile");
        Console.WriteLine("  naner.exe --profile PowerShell     # Launch PowerShell profile");
        Console.WriteLine("  naner.exe -p Bash -d C:\\projects   # Launch Bash in specific dir");
        Console.WriteLine("  naner.exe --debug                  # Show detailed diagnostics");
        Console.WriteLine("  naner.exe --diagnose               # Check installation health");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays requirements.
    /// </summary>
    private void ShowRequirements()
    {
        Console.WriteLine("REQUIREMENTS:");
        Console.WriteLine("  naner.exe must be run from within a Naner installation that");
        Console.WriteLine("  contains bin/, vendor/, and config/ subdirectories.");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays documentation links.
    /// </summary>
    private void ShowDocumentation()
    {
        Console.WriteLine("DOCUMENTATION:");
        Console.WriteLine("  https://github.com/yourusername/naner");
        Console.WriteLine();
    }
}
