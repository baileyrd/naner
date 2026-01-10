using System;

namespace Naner.Launcher.Commands;

/// <summary>
/// Displays version information.
/// </summary>
public class VersionCommand : ICommand
{
    private const string Version = "1.0.0";
    private const string PhaseName = "Production Release - Pure C# Implementation";

    public int Execute(string[] args)
    {
        Console.WriteLine($"Naner Terminal Environment Manager - Version {Version}");
        Console.WriteLine($"Phase: {PhaseName}");
        Console.WriteLine();
        Console.WriteLine("A unified terminal environment for Windows development");
        Console.WriteLine("Copyright © 2026");
        return 0;
    }
}
