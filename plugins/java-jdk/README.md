# Java JDK Plugin

Portable Java Development Kit for Naner.

## Description

This plugin provides a portable Eclipse Temurin JDK (formerly AdoptOpenJDK) for Java development. It includes the full JDK with compiler, runtime, and development tools.

## Features

- ✅ Portable Java JDK 21 (LTS)
- ✅ Automatic JAVA_HOME configuration
- ✅ PATH integration for java, javac, jar, etc.
- ✅ CLASSPATH configuration
- ✅ No system installation required

## Installation

```powershell
# Install the plugin
Install-NanerPlugin -PluginPath "plugins/java-jdk"

# Enable the plugin
Enable-NanerPlugin -PluginId "java-jdk"
```

## Usage

After enabling, restart your terminal. Java commands will be available:

```bash
java -version
javac -version
jar --version
```

## Environment Variables

This plugin sets the following environment variables:

- `JAVA_HOME` - Java installation directory
- `JDK_HOME` - Same as JAVA_HOME
- `CLASSPATH` - Default classpath (current directory + JDK libs)
- `PATH` - Adds `$JAVA_HOME\bin` to PATH

## Configuration

Edit `plugin.json` to customize:

- `javaVersion` - Java version to use (default: 21)
- `enableJavaFX` - Enable JavaFX support (default: false)

## Requirements

- Naner 1.0.0+
- ~200MB disk space

## License

MIT
