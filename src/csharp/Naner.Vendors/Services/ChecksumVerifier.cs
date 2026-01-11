using System;
using System.IO;
using System.Security.Cryptography;
using Naner.Vendors.Models;

namespace Naner.Vendors.Services;

/// <summary>
/// Provides checksum verification for downloaded files.
/// Supports SHA256, SHA512, SHA1, and MD5 algorithms.
/// </summary>
public class ChecksumVerifier
{
    /// <summary>
    /// Verifies the checksum of a file against the expected value.
    /// </summary>
    /// <param name="filePath">Path to the file to verify</param>
    /// <param name="checksumInfo">Checksum information including algorithm and expected value</param>
    /// <returns>Verification result</returns>
    public ChecksumVerificationResult Verify(string filePath, ChecksumInfo checksumInfo)
    {
        if (!File.Exists(filePath))
        {
            return new ChecksumVerificationResult
            {
                Success = false,
                ErrorMessage = $"File not found: {filePath}"
            };
        }

        if (string.IsNullOrEmpty(checksumInfo.Value))
        {
            return new ChecksumVerificationResult
            {
                Success = true,
                Skipped = true,
                Message = "No checksum provided, skipping verification"
            };
        }

        try
        {
            var actualChecksum = ComputeChecksum(filePath, checksumInfo.Algorithm);
            var expectedChecksum = NormalizeChecksum(checksumInfo.Value);
            var actualNormalized = NormalizeChecksum(actualChecksum);

            var matches = string.Equals(expectedChecksum, actualNormalized, StringComparison.OrdinalIgnoreCase);

            return new ChecksumVerificationResult
            {
                Success = matches,
                Algorithm = checksumInfo.Algorithm,
                ExpectedChecksum = expectedChecksum,
                ActualChecksum = actualNormalized,
                ErrorMessage = matches ? null : $"Checksum mismatch: expected {expectedChecksum}, got {actualNormalized}"
            };
        }
        catch (Exception ex)
        {
            return new ChecksumVerificationResult
            {
                Success = false,
                Algorithm = checksumInfo.Algorithm,
                ErrorMessage = $"Checksum verification failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Computes the checksum of a file using the specified algorithm.
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="algorithm">Hash algorithm name (SHA256, SHA512, SHA1, MD5)</param>
    /// <returns>Hex-encoded checksum</returns>
    public string ComputeChecksum(string filePath, string algorithm)
    {
        using var hashAlgorithm = CreateHashAlgorithm(algorithm);
        using var stream = File.OpenRead(filePath);

        var hashBytes = hashAlgorithm.ComputeHash(stream);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Computes the checksum of a file asynchronously.
    /// </summary>
    public async Task<string> ComputeChecksumAsync(string filePath, string algorithm)
    {
        using var hashAlgorithm = CreateHashAlgorithm(algorithm);
        await using var stream = File.OpenRead(filePath);

        var hashBytes = await hashAlgorithm.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes);
    }

    private static HashAlgorithm CreateHashAlgorithm(string algorithm)
    {
        return algorithm.ToUpperInvariant() switch
        {
            "SHA256" => SHA256.Create(),
            "SHA512" => SHA512.Create(),
            "SHA1" => SHA1.Create(),
            "MD5" => MD5.Create(),
            "SHA384" => SHA384.Create(),
            _ => throw new NotSupportedException($"Unsupported hash algorithm: {algorithm}. " +
                                                  "Supported: SHA256, SHA512, SHA384, SHA1, MD5")
        };
    }

    private static string NormalizeChecksum(string checksum)
    {
        // Remove common prefixes and whitespace
        return checksum
            .Replace(" ", "")
            .Replace("-", "")
            .Replace(":", "")
            .ToUpperInvariant();
    }
}

/// <summary>
/// Result of a checksum verification operation.
/// </summary>
public class ChecksumVerificationResult
{
    /// <summary>
    /// Gets or sets whether verification was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets whether verification was skipped (no checksum provided).
    /// </summary>
    public bool Skipped { get; set; }

    /// <summary>
    /// Gets or sets the hash algorithm used.
    /// </summary>
    public string? Algorithm { get; set; }

    /// <summary>
    /// Gets or sets the expected checksum value.
    /// </summary>
    public string? ExpectedChecksum { get; set; }

    /// <summary>
    /// Gets or sets the actual computed checksum.
    /// </summary>
    public string? ActualChecksum { get; set; }

    /// <summary>
    /// Gets or sets the error message if verification failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets an informational message.
    /// </summary>
    public string? Message { get; set; }
}
