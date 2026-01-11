using System;
using System.IO;
using Naner.Configuration.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Naner.Configuration.Providers;

/// <summary>
/// Configuration provider for YAML format files.
/// Supports .yaml and .yml files with the standard Naner configuration schema.
/// </summary>
public class YamlConfigurationProvider : IConfigurationProvider
{
    private readonly IDeserializer _deserializer;

    public int Priority => 20;
    public string Name => "YAML";

    public YamlConfigurationProvider()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public bool CanLoad(string configPath)
    {
        if (string.IsNullOrEmpty(configPath))
            return false;

        var extension = Path.GetExtension(configPath);
        return extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".yml", StringComparison.OrdinalIgnoreCase);
    }

    public NanerConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        var yamlContent = File.ReadAllText(configPath);

        try
        {
            var config = _deserializer.Deserialize<NanerConfig>(yamlContent)
                ?? throw new InvalidOperationException("Failed to deserialize YAML configuration");

            return config;
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new InvalidOperationException($"Invalid YAML format: {ex.Message}", ex);
        }
    }

    public void ApplyOverrides(NanerConfig config)
    {
        // YAML provider can be used as base config, no overrides needed
    }
}
