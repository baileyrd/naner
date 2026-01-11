using Naner.Launcher.Services;

namespace Naner.Launcher.Commands;

/// <summary>
/// Runs system diagnostics to verify Naner installation.
/// Delegates to DiagnosticsService for actual diagnostic logic.
/// </summary>
public class DiagnosticsCommand : ICommand
{
    public int Execute(string[] args)
    {
        var diagnosticsService = new DiagnosticsService();
        return diagnosticsService.Run();
    }
}
