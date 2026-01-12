using System;
using System.Linq;
using System.Threading.Tasks;
using Naner.Commands.Abstractions;
using Naner.Commands.Implementations.Setup;

namespace Naner.Commands.Implementations;

/// <summary>
/// Command for initializing a new Naner installation.
/// Uses Strategy pattern to delegate to appropriate setup strategy.
/// Extracted from Program.cs to improve SRP compliance.
/// </summary>
public class InitCommand : ICommand
{
    private readonly ISetupStrategy _interactiveStrategy;
    private readonly ISetupStrategy _quickStrategy;

    /// <summary>
    /// Creates a new InitCommand with default setup strategies.
    /// </summary>
    public InitCommand()
        : this(new InteractiveSetupStrategy(), new QuickSetupStrategy())
    {
    }

    /// <summary>
    /// Creates a new InitCommand with custom setup strategies.
    /// Supports dependency injection for testing.
    /// </summary>
    /// <param name="interactiveStrategy">Strategy for interactive setup</param>
    /// <param name="quickStrategy">Strategy for quick setup</param>
    public InitCommand(ISetupStrategy interactiveStrategy, ISetupStrategy quickStrategy)
    {
        _interactiveStrategy = interactiveStrategy ?? throw new ArgumentNullException(nameof(interactiveStrategy));
        _quickStrategy = quickStrategy ?? throw new ArgumentNullException(nameof(quickStrategy));
    }

    /// <summary>
    /// Executes the init command.
    /// </summary>
    /// <param name="args">Command arguments</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public int Execute(string[] args)
    {
        return ExecuteAsync(args).GetAwaiter().GetResult();
    }

    private async Task<int> ExecuteAsync(string[] args)
    {
        // Show deprecation notice
        Logger.NewLine();
        Logger.Warning("NOTICE: The 'naner init' command is deprecated.");
        Logger.Info("Please use 'naner-init' for initialization and updates.");
        Logger.Info("naner-init automatically downloads the latest version from GitHub.");
        Logger.NewLine();
        Console.Write("Continue with legacy init? (y/N): ");
        var response = Console.ReadLine()?.Trim().ToLower();
        if (response != "y" && response != "yes")
        {
            Logger.Info("Cancelled. Please use 'naner-init' instead.");
            return 0;
        }
        Logger.NewLine();

        // Parse arguments into options
        var (targetPath, options, useInteractive) = ParseArguments(args);

        // Select and execute the appropriate strategy
        var strategy = useInteractive ? _interactiveStrategy : _quickStrategy;
        return await strategy.ExecuteAsync(targetPath, options);
    }

    /// <summary>
    /// Parses command-line arguments into setup options.
    /// </summary>
    /// <param name="args">Command arguments</param>
    /// <returns>Tuple of target path, options, and whether to use interactive mode</returns>
    private static (string targetPath, SetupOptions options, bool useInteractive) ParseArguments(string[] args)
    {
        bool useInteractive = !args.Contains("--minimal") && !args.Contains("--quick");

        // Determine vendor mode from arguments
        var vendorMode = DetermineVendorMode(args);

        var options = new SetupOptions
        {
            VendorMode = vendorMode,
            DebugMode = args.Contains("--debug")
        };

        string? targetPath = null;

        // Parse path argument
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--path" && i + 1 < args.Length)
            {
                targetPath = args[i + 1];
                i++;
            }
            else if (!args[i].StartsWith("--") && !args[i].StartsWith("-"))
            {
                targetPath = args[i];
            }
        }

        return (targetPath ?? string.Empty, options, useInteractive);
    }

    /// <summary>
    /// Determines the vendor install mode from command-line arguments.
    /// </summary>
    /// <param name="args">Command arguments</param>
    /// <returns>The appropriate VendorInstallMode</returns>
    private static VendorInstallMode DetermineVendorMode(string[] args)
    {
        // Skip takes precedence if both flags somehow specified
        if (args.Contains("--skip-vendors") || args.Contains("--no-vendors"))
        {
            return VendorInstallMode.Skip;
        }

        if (args.Contains("--with-vendors"))
        {
            return VendorInstallMode.Install;
        }

        return VendorInstallMode.Default;
    }
}
