using Naner.Commands.Abstractions;
using Naner.Commands.Services;

namespace Naner.Commands.Implementations;

/// <summary>
/// Runs system diagnostics to verify Naner installation.
/// Delegates to IDiagnosticsService for actual diagnostic logic.
/// Supports dependency injection for testability.
/// </summary>
public class DiagnosticsCommand : ICommand
{
    private readonly IDiagnosticsService _diagnosticsService;

    /// <summary>
    /// Creates a new DiagnosticsCommand with an injected diagnostics service.
    /// Preferred constructor for DI containers and testing.
    /// </summary>
    public DiagnosticsCommand(IDiagnosticsService diagnosticsService)
    {
        _diagnosticsService = diagnosticsService ?? throw new ArgumentNullException(nameof(diagnosticsService));
    }

    /// <summary>
    /// Creates a new DiagnosticsCommand with the default DiagnosticsService.
    /// Maintains backward compatibility for non-DI usage.
    /// </summary>
    public DiagnosticsCommand()
        : this(new DiagnosticsService())
    {
    }

    public int Execute(string[] args)
    {
        return _diagnosticsService.Run();
    }
}
