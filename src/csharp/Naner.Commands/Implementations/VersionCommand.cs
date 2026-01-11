using System;

namespace Naner.Commands.Implementations;

/// <summary>
/// Displays version information.
/// </summary>
public class VersionCommand : ICommand
{
    public int Execute(string[] args)
    {
        Console.WriteLine($"Naner Terminal Environment Manager - Version {NanerConstants.Version}");
        Console.WriteLine($"Phase: {NanerConstants.PhaseName}");
        Console.WriteLine();
        Console.WriteLine("A unified terminal environment for Windows development");
        Console.WriteLine("Copyright © 2026");
        return 0;
    }
}
