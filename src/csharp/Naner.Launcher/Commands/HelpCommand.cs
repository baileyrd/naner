using Naner.Launcher.Services;

namespace Naner.Launcher.Commands;

/// <summary>
/// Displays help information.
/// Delegates to HelpTextProvider for formatted help content.
/// </summary>
public class HelpCommand : ICommand
{
    public int Execute(string[] args)
    {
        var helpProvider = new HelpTextProvider();
        helpProvider.ShowHelp();
        return 0;
    }
}
