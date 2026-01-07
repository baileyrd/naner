# Portable Cargo Home

This directory contains your portable Cargo configuration and installed tools for the Naner environment.

## Directory Structure

```
.cargo/
├── bin/            # Installed cargo binaries (cargo install)
├── registry/       # Downloaded crate registry cache
├── git/            # Git dependencies cache
├── config.toml     # Cargo configuration
└── .package-cache  # Package metadata cache
```

## Usage

### Create a New Rust Project

```bash
# Create binary project
cargo new myproject
cd myproject

# Create library project
cargo new --lib mylib

# Run the project
cargo run

# Build for release
cargo build --release
```

### Build and Run

```bash
# Check code without building
cargo check

# Build in debug mode (faster compile, slower runtime)
cargo build

# Build in release mode (slower compile, optimized runtime)
cargo build --release

# Run with arguments
cargo run -- arg1 arg2

# Run specific binary
cargo run --bin mybin

# Run examples
cargo run --example example_name
```

### Testing

```bash
# Run all tests
cargo test

# Run specific test
cargo test test_name

# Run tests with output
cargo test -- --nocapture

# Run doc tests
cargo test --doc

# Run benchmarks
cargo bench
```

### Managing Dependencies

Add dependencies to `Cargo.toml`:

```toml
[dependencies]
serde = "1.0"
tokio = { version = "1.35", features = ["full"] }
rand = "0.8"
```

Then:

```bash
# Update dependencies
cargo update

# Check for outdated dependencies
cargo outdated

# Generate Cargo.lock
cargo generate-lockfile
```

### Installing Cargo Tools

```bash
# Install popular cargo extensions
cargo install cargo-edit        # cargo add, cargo rm, cargo upgrade
cargo install cargo-watch       # Auto-rebuild on file changes
cargo install cargo-expand      # Show macro expansions
cargo install cargo-audit       # Security vulnerability scanner
cargo install cargo-outdated    # Check for outdated dependencies
cargo install cargo-tree        # Visualize dependency tree
cargo install cargo-bloat       # Find what takes space in binary
cargo install cargo-geiger      # Detect unsafe code usage

# Installed binaries go to: %NANER_ROOT%\home\.cargo\bin
# This directory is already in your PATH
```

### Cargo Watch (Auto-Rebuild)

```bash
# Install cargo-watch
cargo install cargo-watch

# Watch for changes and run
cargo watch -x run

# Watch and run tests
cargo watch -x test

# Watch with clear screen
cargo watch -c -x run
```

## Popular Rust Crates

### Web Frameworks

```toml
# Actix Web - Fast and powerful
actix-web = "4.4"

# Axum - Ergonomic and modular
axum = "0.7"

# Rocket - Easy to use with great documentation
rocket = "0.5"

# Warp - Composable filters-based framework
warp = "0.3"
```

### Async Runtime

```toml
# Tokio - Most popular async runtime
tokio = { version = "1.35", features = ["full"] }

# async-std - Alternative async runtime
async-std = "1.12"
```

### Serialization

```toml
# Serde - De facto serialization framework
serde = { version = "1.0", features = ["derive"] }
serde_json = "1.0"
serde_yaml = "0.9"
toml = "0.8"
```

### CLI Tools

```toml
# Clap - Command line argument parser
clap = { version = "4.4", features = ["derive"] }

# colored - Terminal colors
colored = "2.1"

# indicatif - Progress bars
indicatif = "0.17"
```

### HTTP Clients

```toml
# reqwest - High-level HTTP client
reqwest = { version = "0.11", features = ["json"] }

# hyper - Low-level HTTP library
hyper = "1.1"
```

### Database

```toml
# SQLx - Async SQL toolkit
sqlx = { version = "0.7", features = ["runtime-tokio-native-tls", "postgres"] }

# diesel - ORM and query builder
diesel = { version = "2.1", features = ["postgres"] }
```

### Error Handling

```toml
# anyhow - Flexible error handling
anyhow = "1.0"

# thiserror - Derive error types
thiserror = "1.0"
```

### Utilities

```toml
# log - Logging facade
log = "0.4"

# env_logger - Logger implementation
env_logger = "0.11"

# chrono - Date and time library
chrono = "0.4"

# uuid - UUID generation
uuid = { version = "1.6", features = ["v4"] }

# regex - Regular expressions
regex = "1.10"
```

## Cargo Configuration

The `config.toml` file configures Cargo behavior. Common settings:

```toml
# Portable Cargo configuration for Naner

[build]
# Number of parallel jobs
jobs = 4

# Incremental compilation (faster rebuilds)
incremental = true

[term]
# Colored output
color = 'auto'

# Verbosity
verbose = false

[net]
# Git fetch with CLI (for better SSH key support)
git-fetch-with-cli = true

[profile.dev]
# Faster linking on Windows
split-debuginfo = "unpacked"

[profile.release]
# Optimize for size (smaller binaries)
# opt-level = "z"
# lto = true
# strip = true
```

## Cross-Compilation

Rust supports cross-compilation to different targets:

```bash
# List installed targets
rustup target list --installed

# Add target (requires rustup, not in portable mode)
# Instead, manually download std libraries for target

# Build for specific target
cargo build --target x86_64-pc-windows-msvc
cargo build --target x86_64-unknown-linux-gnu
cargo build --target x86_64-apple-darwin

# Common targets
# - x86_64-pc-windows-msvc (Windows 64-bit)
# - x86_64-pc-windows-gnu (Windows 64-bit with GNU toolchain)
# - x86_64-unknown-linux-gnu (Linux 64-bit)
# - x86_64-apple-darwin (macOS 64-bit)
# - aarch64-unknown-linux-gnu (ARM64 Linux)
```

## Environment Variables

Naner automatically sets these Rust environment variables:

- **CARGO_HOME**: `%NANER_ROOT%\home\.cargo` - Cargo configuration and cache
- **RUSTUP_HOME**: `%NANER_ROOT%\home\.rustup` - Rustup data (if installed)

### Verify Environment

```bash
# Check Rust version
rustc --version

# Check Cargo version
cargo --version

# Show Cargo home
echo $CARGO_HOME

# Show all Cargo config
cargo config get
```

## Common Commands Reference

```bash
# Project Management
cargo new <name>              # Create new project
cargo init                    # Initialize project in current directory
cargo build                   # Build project
cargo run                     # Build and run
cargo clean                   # Remove build artifacts

# Testing and Quality
cargo test                    # Run tests
cargo bench                   # Run benchmarks
cargo doc                     # Build documentation
cargo doc --open              # Build and open docs
cargo clippy                  # Lint code (requires clippy)
cargo fmt                     # Format code (requires rustfmt)

# Dependencies
cargo add <crate>             # Add dependency (requires cargo-edit)
cargo rm <crate>              # Remove dependency (requires cargo-edit)
cargo update                  # Update dependencies
cargo tree                    # Show dependency tree

# Publishing (crates.io)
cargo login                   # Login to crates.io
cargo publish                 # Publish crate
cargo yank                    # Yank published version

# Utilities
cargo search <query>          # Search crates.io
cargo install <crate>         # Install binary crate
cargo uninstall <crate>       # Uninstall binary crate
```

## IDE Integration

### VS Code

Install the official Rust extension:
1. Install "rust-analyzer" extension
2. Extension auto-detects Cargo projects
3. Provides IntelliSense, code completion, and inline errors
4. Supports debugging with CodeLLDB extension

### RustRover / IntelliJ IDEA

1. Install Rust plugin
2. Open Cargo project
3. IDE auto-detects Cargo.toml
4. Full support for debugging and profiling

### Visual Studio

1. Install "Rust for Visual Studio" extension
2. Open project folder containing Cargo.toml
3. Full IntelliSense and debugging support

## Cargo Workspaces

For projects with multiple crates:

```toml
# Root Cargo.toml
[workspace]
members = [
    "crate1",
    "crate2",
    "lib/*"
]

[workspace.dependencies]
serde = "1.0"
```

Then in member crates:

```toml
[dependencies]
serde = { workspace = true }
```

## Troubleshooting

### Cargo Command Not Found

```bash
# Verify cargo is installed
ls %NANER_ROOT%\vendor\rust\cargo\bin\cargo.exe

# Check PATH includes cargo
echo $PATH | grep cargo
```

### Slow Compilation

```bash
# Use cargo check instead of build for faster feedback
cargo check

# Enable parallel compilation (in .cargo/config.toml)
[build]
jobs = 8

# Use cargo-watch for incremental builds
cargo watch -x check
```

### Dependency Resolution Issues

```bash
# Clean and rebuild
cargo clean
cargo build

# Update Cargo.lock
cargo update

# Remove target directory
rm -rf target/
cargo build
```

### Linker Errors on Windows

If you encounter linker errors, ensure you have the MSVC toolchain:
1. Install Visual Studio Build Tools
2. Select "Desktop development with C++"
3. Restart your terminal

Alternatively, use the GNU toolchain (MinGW) which comes with MSYS2 in Naner.

### Network Issues

```bash
# Use git CLI for fetching (better SSH support)
# Add to .cargo/config.toml:
[net]
git-fetch-with-cli = true

# Use alternative registry mirror
[source.crates-io]
replace-with = "mirror"

[source.mirror]
registry = "https://your-mirror.com/crates.io-index"
```

## Resources

- **Official Docs**: https://doc.rust-lang.org/
- **Rust Book**: https://doc.rust-lang.org/book/
- **Rust by Example**: https://doc.rust-lang.org/rust-by-example/
- **Cargo Book**: https://doc.rust-lang.org/cargo/
- **Crates.io**: https://crates.io/ (Package registry)
- **Docs.rs**: https://docs.rs/ (Package documentation)
- **Awesome Rust**: https://github.com/rust-unofficial/awesome-rust

## Portability

All Cargo data is portable with Naner:
- ✅ Cargo configuration (config.toml)
- ✅ Installed binaries (.cargo/bin/)
- ✅ Registry cache (downloaded crates)
- ✅ Git dependencies cache
- ✅ Package metadata

When you move Naner to a new machine, your entire Rust development environment travels with it. Just rebuild your projects with `cargo build`.
