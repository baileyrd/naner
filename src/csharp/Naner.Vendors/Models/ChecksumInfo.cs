namespace Naner.Vendors.Models;

/// <summary>
/// Represents checksum information for file verification.
/// </summary>
public class ChecksumInfo
{
    /// <summary>
    /// Gets or sets the checksum algorithm (e.g., "SHA256", "SHA512", "MD5").
    /// </summary>
    public string Algorithm { get; set; } = "SHA256";

    /// <summary>
    /// Gets or sets the expected checksum value (hex string).
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// Gets or sets whether verification is required (failure blocks installation).
    /// If false, verification failure only produces a warning.
    /// </summary>
    public bool Required { get; set; } = false;
}
