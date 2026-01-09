# Naner Plugins

This directory contains official and community plugins for Naner.

## Available Plugins

### Java JDK
**ID:** `java-jdk`
**Description:** Portable Eclipse Temurin JDK for Java development
**Size:** ~200MB
**Documentation:** [plugins/java-jdk/README.md](java-jdk/README.md)

```powershell
Install-NanerPlugin -PluginPath "plugins/java-jdk"
Enable-NanerPlugin -PluginId "java-jdk"
```

### .NET SDK
**ID:** `dotnet-sdk`
**Description:** Portable .NET SDK for C#, F#, and VB.NET development
**Size:** ~220MB
**Documentation:** [plugins/dotnet-sdk/README.md](dotnet-sdk/README.md)

```powershell
Install-NanerPlugin -PluginPath "plugins/dotnet-sdk"
Enable-NanerPlugin -PluginId "dotnet-sdk"
```

### PostgreSQL Client
**ID:** `postgres-client`
**Description:** Portable PostgreSQL client tools (psql, pg_dump, pg_restore)
**Size:** ~150MB
**Documentation:** [plugins/postgres-client/README.md](postgres-client/README.md)

```powershell
Install-NanerPlugin -PluginPath "plugins/postgres-client"
Enable-NanerPlugin -PluginId "postgres-client"
```

## Plugin Management

### List Installed Plugins
```powershell
Get-NanerPlugin
```

### Install a Plugin
```powershell
# From directory
Install-NanerPlugin -PluginPath "path/to/plugin"

# From ZIP
Install-NanerPlugin -PluginPath "path/to/plugin.zip"
```

### Enable/Disable Plugins
```powershell
Enable-NanerPlugin -PluginId "plugin-id"
Disable-NanerPlugin -PluginId "plugin-id"
```

### Uninstall a Plugin
```powershell
Uninstall-NanerPlugin -PluginId "plugin-id"
```

## Creating Your Own Plugin

See the comprehensive [Plugin Development Guide](../docs/PLUGIN-DEVELOPMENT.md) for detailed instructions on creating, testing, and distributing plugins.

### Quick Start

1. Create plugin directory structure:
```
plugins/my-plugin/
├── plugin.json
├── README.md
└── hooks/
    └── env-setup.ps1
```

2. Create `plugin.json` manifest:
```json
{
  "$schema": "../../config/plugin-schema.json",
  "id": "my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "description": "My custom plugin",
  "author": "Your Name",
  "enabled": false
}
```

3. Create environment setup hook (`hooks/env-setup.ps1`):
```powershell
param([hashtable]$Context)

$env:MY_VAR = "value"
$env:PATH = "C:\my\path;$env:PATH"
```

4. Install and enable:
```powershell
Install-NanerPlugin -PluginPath "plugins/my-plugin"
Enable-NanerPlugin -PluginId "my-plugin"
```

## Documentation

- **Plugin Development Guide:** [docs/PLUGIN-DEVELOPMENT.md](../docs/PLUGIN-DEVELOPMENT.md)
- **Plugin Schema:** [config/plugin-schema.json](../config/plugin-schema.json)
- **Plugin Module:** [src/powershell/Naner.Plugins.psm1](../src/powershell/Naner.Plugins.psm1)

## Contributing

Want to share your plugin with the community? Create a pull request or publish it to your own repository!

### Guidelines

- Follow the [Plugin Development Guide](../docs/PLUGIN-DEVELOPMENT.md)
- Include comprehensive README
- Add tests where applicable
- Use semantic versioning
- Specify appropriate license

## Support

For issues, questions, or feature requests related to plugins:
- Check the [Plugin Development Guide](../docs/PLUGIN-DEVELOPMENT.md)
- Review existing plugin examples
- Open an issue on the repository

---

**Last Updated:** 2026-01-07
**Plugin System Version:** 1.0.0
