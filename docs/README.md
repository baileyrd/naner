# Naner Documentation

Welcome to the Naner documentation! This guide will help you find the information you need.

## Quick Links

- **New User?** Start with the [Quick Start Guide](guides/QUICK-START.md)
- **Want to understand the architecture?** See [Architecture Overview](reference/ARCHITECTURE.md)
- **Developer?** Check out the [Development Documentation](development/)
- **Having issues?** See archived [Troubleshooting Guide](archived/TROUBLESHOOTING.md)

## Documentation Structure

### User Guides (`guides/`)

Documentation for end users setting up and using Naner:

- [Quick Start Guide](guides/QUICK-START.md) - Get up and running quickly
- [Multi-Environment Setup](guides/MULTI-ENVIRONMENT.md) - Managing multiple environments
- [Plugin Development](guides/PLUGIN-DEVELOPMENT.md) - Creating custom plugins
- [Sync & Backup](guides/SYNC-BACKUP.md) - Syncing your environment across machines

**Portable Tool Guides:**
- [Portable Bash](guides/PORTABLE-BASH.md) - Setting up portable Bash
- [Portable Git](guides/PORTABLE-GIT.md) - Portable Git configuration
- [Portable PowerShell](guides/PORTABLE-POWERSHELL.md) - PowerShell profiles and modules
- [Portable SSH](guides/PORTABLE-SSH.md) - SSH configuration and keys
- [Portable Editors](guides/PORTABLE-EDITORS.md) - Portable text editors
- [Portable Python](guides/PORTABLE-PYTHON.md) - Python environment
- [Portable Node.js](guides/PORTABLE-NODEJS.md) - Node.js and npm
- [Portable Ruby](guides/PORTABLE-RUBY.md) - Ruby and gems
- [Portable Rust](guides/PORTABLE-RUST.md) - Rust and Cargo
- [Portable Go](guides/PORTABLE-GO.md) - Go toolchain

### Technical Reference (`reference/`)

Technical documentation and API references:

- [Architecture Overview](reference/ARCHITECTURE.md) - System design and architecture
- [Implementation Guide](reference/IMPLEMENTATION-GUIDE.md) - Implementation details
- [Error Codes](reference/ERROR-CODES.md) - Error code reference
- [Vendor Dependencies](reference/README-VENDOR.md) - Third-party dependencies

### Development (`development/`)

Documentation for contributors and developers working on Naner:

- [Capability Roadmap](development/CAPABILITY-ROADMAP.md) - Feature roadmap and planning
- [C# Migration Roadmap](development/CSHARP-MIGRATION-ROADMAP.md) - Migration to pure C#
- [Migration Guide](development/MIGRATION_GUIDE.md) - Detailed migration documentation
- [Migration Quick Start](development/MIGRATION-QUICK-START.md) - Fast-track migration guide
- [Testing Guide](development/TESTING_GUIDE.md) - Testing procedures
- [Code Quality Analysis](development/CODE-QUALITY-ANALYSIS.md) - Code quality standards

**Phase 10 (C# Migration) Documentation:**
- [Phase 10.1: C# Wrapper](development/PHASE-10.1-CSHARP-WRAPPER.md)
- [Phase 10.3: Optimization Analysis](development/PHASE-10.3-OPTIMIZATION-ANALYSIS.md)
- [Phase 10.4: Usability Improvements](development/PHASE-10.4-USABILITY-IMPROVEMENTS.md)
- [Phase 10.5: First-Run Experience](development/PHASE-10.5-FIRST-RUN-EXPERIENCE.md)
- [Phase 10: Next Steps](development/PHASE-10-NEXT-STEPS.md)

### Archived (`archived/`)

Historical documentation from the PowerShell era (pre-v1.0.0):

**PowerShell Implementation (Archived):**
- [PowerShell Assessment](archived/POWERSHELL-ASSESSMENT.md)
- [Implementation Summary](archived/IMPLEMENTATION-SUMMARY.md)
- [Setup Instructions](archived/SETUP-INSTRUCTIONS.md)
- [User Settings Examples](archived/USER-SETTINGS-EXAMPLES.md)

**Refactoring History:**
- [Refactoring Summary](archived/REFACTORING-SUMMARY.md)
- [Phase 1 Summary](archived/REFACTORING-PHASE1-SUMMARY.md)
- [Phase 2 Plan](archived/REFACTORING-PHASE2-PLAN.md)
- [Phase 2 Summary](archived/PHASE2-REFACTORING-SUMMARY.md)

**Technical Analysis:**
- [7-Zip Bundling](archived/7ZIP-BUNDLING.md)
- [Cmder Analysis](archived/CMDER-ANALYSIS.md)
- [Launcher Comparison](archived/LAUNCHER-COMPARISON.md)
- [Dynamic URLs](archived/DYNAMIC-URLS.md)
- [Terminal Launch Issues](archived/TERMINAL-LAUNCH-ISSUES.md)
- [Windows Terminal Portable](archived/WINDOWS-TERMINAL-PORTABLE.md)

**Archived Features:**
- [GUI Configuration Manager](archived/GUI-CONFIGURATION-MANAGER.md) (Not implemented)
- [Vendor Lock File](archived/VENDOR-LOCK-FILE.md) (Not implemented)

**Troubleshooting (PowerShell-era):**
- [General Troubleshooting](archived/TROUBLESHOOTING.md)
- [Custom Profiles Troubleshooting](archived/TROUBLESHOOTING-CUSTOM-PROFILES.md)

## Version Information

This documentation is for **Naner v1.0.0** - Production Release (Pure C# Implementation).

For information about earlier versions and the PowerShell implementation, see the [archived documentation](archived/).

## Contributing to Documentation

Documentation improvements are welcome! Please ensure:

1. User guides go in `guides/`
2. Technical references go in `reference/`
3. Developer documentation goes in `development/`
4. Historical docs stay in `archived/` (read-only)

## Getting Help

- **Commands:** Run `naner --help` for command-line help
- **Diagnostics:** Run `naner --diagnose` to troubleshoot issues
- **Release Notes:** See [RELEASE-NOTES-v1.0.0.md](../RELEASE-NOTES-v1.0.0.md) in the repository root

## External Resources

- **Repository:** https://github.com/baileyrd/naner
- **Issues:** https://github.com/baileyrd/naner/issues
- **Releases:** https://github.com/baileyrd/naner/releases
