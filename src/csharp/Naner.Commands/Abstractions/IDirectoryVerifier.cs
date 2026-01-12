namespace Naner.Commands.Abstractions;

/// <summary>
/// Service for verifying Naner directory structure integrity.
/// </summary>
public interface IDirectoryVerifier
{
    /// <summary>
    /// Verifies the Naner directory structure exists.
    /// </summary>
    /// <param name="nanerRoot">Naner root directory path</param>
    void Verify(string nanerRoot);
}
