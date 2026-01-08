# User Binaries Directory

This directory is reserved for user-added executables after Naner deployment.

## Purpose

The `bin/` directory is automatically added to your PATH when you launch Naner, allowing you to place any custom executables, scripts, or tools here that you want available in your terminal sessions.

## Usage

Simply drop any `.exe`, `.bat`, `.ps1`, or other executable files into this directory, and they will be accessible from any Naner terminal session.

### Examples

```
bin/
├── my-custom-tool.exe
├── deploy-script.bat
├── helper-script.ps1
└── python-script.py
```

## Note

- This directory is gitignored by default (except for this README)
- The Naner executable (`naner.exe`) is located in `vendor/bin/` not here
- User-specific tools go here; Naner's own tools go in `vendor/bin/`

## PATH Priority

The `bin/` directory is added to PATH with high priority, so your custom tools will be found before system tools if there are naming conflicts.
