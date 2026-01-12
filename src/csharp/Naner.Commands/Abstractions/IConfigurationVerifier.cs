namespace Naner.Commands.Abstractions;

/// <summary>
/// Service for verifying Naner configuration file validity.
/// </summary>
public interface IConfigurationVerifier
{
    /// <summary>
    /// Verifies configuration file exists and is valid.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory path</param>
    void Verify(string nanerRoot);
}
