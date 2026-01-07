# .NET SDK Plugin

Portable .NET SDK for Naner.

## Description

This plugin provides a portable .NET SDK for C#, F#, and VB.NET development. It includes the full SDK with compilers, runtime, and development tools.

## Features

- ✅ Portable .NET 8.0 SDK (LTS)
- ✅ Automatic DOTNET_ROOT configuration
- ✅ PATH integration for dotnet CLI
- ✅ NuGet packages in portable location
- ✅ Telemetry disabled by default
- ✅ No system installation required

## Installation

```powershell
# Install the plugin
Install-NanerPlugin -PluginPath "plugins/dotnet-sdk"

# Enable the plugin
Enable-NanerPlugin -PluginId "dotnet-sdk"
```

## Usage

After enabling, restart your terminal. .NET commands will be available:

```bash
dotnet --version
dotnet new console -n MyApp
dotnet build
dotnet run
```

## Environment Variables

This plugin sets the following environment variables:

- `DOTNET_ROOT` - .NET installation directory
- `DOTNET_CLI_TELEMETRY_OPTOUT` - Disables telemetry (1)
- `DOTNET_SKIP_FIRST_TIME_EXPERIENCE` - Skips first-run experience (1)
- `DOTNET_MULTILEVEL_LOOKUP` - Prevents system .NET lookup (0)
- `NUGET_PACKAGES` - NuGet packages cache location
- `PATH` - Adds `$DOTNET_ROOT` and global tools to PATH

## Configuration

Edit `plugin.json` to customize:

- `dotnetVersion` - .NET version to use (default: 8.0)
- `disableTelemetry` - Disable Microsoft telemetry (default: true)

## Global Tools

Install .NET global tools portably:

```bash
dotnet tool install -g dotnet-ef
dotnet tool install -g dotnet-format
```

Tools are installed to `%NANER_ROOT%\home\.dotnet\tools`

## Requirements

- Naner 1.0.0+
- ~220MB disk space

## License

MIT
