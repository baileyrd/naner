using System;
using System.IO;
using System.Text.Json;
using Naner.Configuration.Abstractions;

namespace Naner.Configuration.Providers;

/// <summary>
/// Configuration provider for JSON format files.
/// Supports .json files with the standard Naner configuration schema.
/// </summary>
public class JsonConfigurationProvider : IConfigurationProvider
{
    public int Priority => 10;
    public string Name => "JSON";

    public bool CanLoad(string configPath)
    {
        if (string.IsNullOrEmpty(configPath))
            return false;

        return Path.GetExtension(configPath).Equals(".json", StringComparison.OrdinalIgnoreCase);
    }

    public NanerConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        var jsonContent = File.ReadAllText(configPath);

        // Use source-generated JSON context for trim-safe deserialization
        var config = JsonSerializer.Deserialize(jsonContent, NanerJsonContext.Default.NanerConfig)
            ?? throw new InvalidOperationException("Failed to deserialize JSON configuration");

        return config;
    }

    public void ApplyOverrides(NanerConfig config)
    {
        // JSON provider is the base, no overrides needed
    }
}
