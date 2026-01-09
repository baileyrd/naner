using System.Text.Json;
using System.Text.Json.Serialization;

namespace Naner.Configuration;

/// <summary>
/// JSON source generation context for Naner configuration.
/// This enables trim-safe JSON serialization/deserialization.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    WriteIndented = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip)]
[JsonSerializable(typeof(NanerConfig))]
[JsonSerializable(typeof(EnvironmentConfig))]
[JsonSerializable(typeof(ProfileConfig))]
[JsonSerializable(typeof(WindowsTerminalConfig))]
[JsonSerializable(typeof(AdvancedConfig))]
internal partial class NanerJsonContext : JsonSerializerContext
{
}
