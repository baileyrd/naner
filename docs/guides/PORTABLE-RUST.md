# Portable Rust with Naner

Complete guide to using portable Rust toolchain within the Naner environment.

## Overview

Naner provides a fully portable Rust development environment via rustup, including cargo, rustc, and the complete Rust toolchain, allowing you to develop Rust applications without installing Rust system-wide.

**Benefits:**
- No system-wide Rust installation required
- Full rustup toolchain manager
- Portable CARGO_HOME (`home/.cargo`)
- Portable RUSTUP_HOME (`home/.rustup`)
- Version-locked for team consistency
- Works across multiple machines

## Quick Start

### Enable Rust Vendor

1. **Edit vendor configuration:**
   ```powershell
   # Open config/vendors.json
   code config/vendors.json
   ```

2. **Set Rust enabled to true:**
   ```json
   {
     "Rust": {
       "enabled": true,
       ...
     }
   }
   ```

3. **Install Rust vendor:**
   ```powershell
   .\src\powershell\Setup-NanerVendor.ps1 -VendorId Rust
   ```

4. **Verify installation:**
   ```powershell
   rustc --version
   cargo --version
   rustup --version
   ```

## Configuration

### Vendor Configuration

Located in `config/vendors.json` (lines 152-176):

```json
{
  "Rust": {
    "enabled": false,
    "name": "Rust",
    "extractDir": "rust",
    "releaseSource": {
      "type": "static",
      "url": "https://static.rust-lang.org/rustup/dist/x86_64-pc-windows-msvc/rustup-init.exe"
    },
    "paths": [
      "..\\home\\.cargo\\bin"
    ],
    "postInstall": "Initialize-Rust"
  }
}
```

### PostInstall Configuration

The `Initialize-Rust` function (Naner.Vendors.psm1:822-922) configures:

1. **Sets environment variables**:
   - `CARGO_HOME=%NANER_ROOT%\home\.cargo`
   - `RUSTUP_HOME=%NANER_ROOT%\home\.rustup`
2. **Silent installation** with default stable toolchain
3. **Creates portable cargo config**:
   - Registry cache in portable location
4. **Disables PATH modification** (Naner manages PATH)
5. **Verifies installation** - Displays cargo, rustc, and rustup versions

## Usage

### Basic Rust Commands

```powershell
# Check Rust version
rustc --version

# Check Cargo version
cargo --version

# Compile Rust file
rustc main.rs

# Run Rust file
rustc main.rs && .\main.exe
```

### Cargo (Rust Package Manager)

```powershell
# Create new project
cargo new my-project
cd my-project

# Create new library
cargo new --lib my-lib

# Build project
cargo build

# Build with optimizations (release)
cargo build --release

# Run project
cargo run

# Run with args
cargo run -- arg1 arg2

# Test project
cargo test

# Check for errors without building
cargo check

# Format code
cargo fmt

# Run linter
cargo clippy
```

### Managing Dependencies

Edit `Cargo.toml`:

```toml
[dependencies]
serde = "1.0"
tokio = { version = "1.0", features = ["full"] }
```

Then run:
```powershell
# Download and compile dependencies
cargo build
```

### Installing Tools

```powershell
# Install tool from crates.io
cargo install ripgrep

# Install specific version
cargo install cargo-edit@0.11.0

# Tools install to home/.cargo/bin (in PATH)
rg --version

# List installed tools
cargo install --list
```

## Directory Structure

```
naner/
├── home/
│   ├── .cargo/              # CARGO_HOME
│   │   ├── bin/            # Installed binaries
│   │   ├── registry/       # Package registry cache
│   │   └── config.toml     # Cargo configuration
│   └── .rustup/            # RUSTUP_HOME
│       ├── toolchains/     # Installed toolchains
│       ├── downloads/      # Rustup downloads
│       └── settings.toml   # Rustup configuration
└── projects/
    └── my-rust-project/
        ├── Cargo.toml      # Project manifest
        ├── Cargo.lock      # Dependency lock file
        ├── src/
        │   └── main.rs
        └── target/         # Build artifacts
```

## Common Tasks

### Create CLI Application

```powershell
# Create new project
cargo new my-cli
cd my-cli

# Edit src/main.rs
@"
use std::env;

fn main() {
    let args: Vec<String> = env::args().collect();

    if args.len() < 2 {
        eprintln!("Usage: {} <name>", args[0]);
        return;
    }

    println!("Hello, {}!", args[1]);
}
"@ | Out-File -Encoding UTF8 src/main.rs

# Run
cargo run -- World

# Build release
cargo build --release
```

### Create Library

```powershell
# Create new library
cargo new --lib my-lib
cd my-lib

# Edit src/lib.rs
@"
pub fn add(a: i32, b: i32) -> i32 {
    a + b
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_add() {
        assert_eq!(add(2, 3), 5);
    }
}
"@ | Out-File -Encoding UTF8 src/lib.rs

# Run tests
cargo test
```

### Create Web Server (Actix)

```powershell
# Create project
cargo new my-server
cd my-server

# Add dependencies to Cargo.toml
[dependencies]
actix-web = "4"

# Edit src/main.rs
@"
use actix_web::{get, App, HttpServer, Responder};

#[get("/")]
async fn index() -> impl Responder {
    "Hello, World!"
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    HttpServer::new(|| {
        App::new().service(index)
    })
    .bind(("127.0.0.1", 8080))?
    .run()
    .await
}
"@ | Out-File -Encoding UTF8 src/main.rs

# Run server
cargo run
```

### Working with Workspaces

```powershell
# Create workspace directory
mkdir my-workspace
cd my-workspace

# Create Cargo.toml for workspace
@"
[workspace]
members = [
    "app",
    "lib",
]
"@ | Out-File Cargo.toml

# Create member projects
cargo new app
cargo new --lib lib

# Build entire workspace
cargo build
```

### Cross-Compilation

```powershell
# Install target
rustup target add x86_64-unknown-linux-gnu

# Build for Linux
cargo build --target x86_64-unknown-linux-gnu

# List installed targets
rustup target list --installed
```

## Environment Variables

Naner sets these environment variables:

```powershell
# Cargo binaries in PATH
$env:PATH  # Contains home/.cargo/bin

# Cargo and Rustup homes (set by PostInstall)
$env:CARGO_HOME  # Set to home/.cargo
$env:RUSTUP_HOME # Set to home/.rustup

# Verify environment
cargo --version
rustc --version
```

## Toolchain Management

### Using rustup

```powershell
# Show current toolchain
rustup show

# List installed toolchains
rustup toolchain list

# Install nightly toolchain
rustup toolchain install nightly

# Set default toolchain
rustup default stable

# Update toolchains
rustup update

# Add component
rustup component add rustfmt clippy

# Add target
rustup target add wasm32-unknown-unknown
```

### Switching Toolchains

```powershell
# Use nightly for current command
cargo +nightly build

# Set project-specific toolchain
rustup override set nightly

# Remove override
rustup override unset
```

## Version Management

### Check Current Version

```powershell
rustc --version
# Example: rustc 1.75.0 (82e1608df 2023-12-21)

cargo --version
# Example: cargo 1.75.0 (1d8b05cdd 2023-11-20)
```

### Update Rust

```powershell
# Update all toolchains
rustup update

# Or reinstall vendor
.\src\powershell\Setup-NanerVendor.ps1 -VendorId Rust -ForceDownload

# Update lock file
.\src\powershell\Export-VendorLockFile.ps1 -IncludeHashes
```

## Troubleshooting

### Rust Not Found

**Error:** `rustc: command not found` or `'cargo' is not recognized`

**Causes:**
- Rust vendor not installed
- Naner environment not loaded

**Solutions:**
```powershell
# Check if vendor is installed
Test-Path home/.cargo/bin/cargo.exe

# If not installed
.\src\powershell\Setup-NanerVendor.ps1 -VendorId Rust

# Reload environment
pwsh  # Start new PowerShell session with Naner profile
```

### CARGO_HOME Issues

**Error:** CARGO_HOME not set correctly

**Solutions:**
```powershell
# Check CARGO_HOME
$env:CARGO_HOME
# Should return: C:\path\to\naner\home\.cargo

# If incorrect, reinstall
.\src\powershell\Setup-NanerVendor.ps1 -VendorId Rust -ForceDownload
```

### Build Errors

**Error:** `linker 'link.exe' not found`

**Cause:** Missing MSVC build tools

**Solution:**
```powershell
# Install Visual Studio Build Tools
# Download from: https://visualstudio.microsoft.com/downloads/
# Or install Visual Studio Community with "Desktop development with C++"
```

**Error:** `error: could not compile`

**Solutions:**
```powershell
# Clean and rebuild
cargo clean
cargo build

# Check for detailed errors
cargo build --verbose

# Update dependencies
cargo update
```

### Dependency Download Fails

**Error:** `failed to download`

**Solutions:**
```powershell
# Check internet connection

# Use alternative registry (if corporate network)
# Edit home/.cargo/config.toml
[source.crates-io]
replace-with = "mirror"

[source.mirror]
registry = "https://your-mirror.com"

# Clear cargo cache
Remove-Item -Recurse -Force $env:CARGO_HOME/registry/cache
Remove-Item -Recurse -Force $env:CARGO_HOME/registry/index
```

## Best Practices

### ✅ DO

- **Use Cargo.toml for dependencies**:
  ```toml
  [dependencies]
  serde = "1.0"
  ```

- **Commit Cargo.lock** for applications:
  ```powershell
  git add Cargo.lock
  ```

- **Run clippy regularly**:
  ```powershell
  cargo clippy
  ```

- **Format code before committing**:
  ```powershell
  cargo fmt
  ```

- **Write tests**:
  ```rust
  #[cfg(test)]
  mod tests {
      #[test]
      fn it_works() {
          assert_eq!(2 + 2, 4);
      }
  }
  ```

- **Use semantic versioning**:
  ```toml
  [dependencies]
  serde = "~1.0.0"  # Compatible with 1.0.x
  ```

### ❌ DON'T

- **Don't modify CARGO_HOME manually** - PostInstall handles this
- **Don't commit target/ directory** - Always in .gitignore
- **Don't commit Cargo.lock for libraries** - Only for applications
- **Don't mix global and local Rust installations**

## Integration with Other Tools

### VS Code Integration

Naner's VS Code settings (home/.vscode/settings.json) automatically detect Rust:

```json
{
  "rust-analyzer.server.path": "${env:NANER_ROOT}\\home\\.cargo\\bin\\rust-analyzer.exe",
  "[rust]": {
    "editor.defaultFormatter": "rust-lang.rust-analyzer",
    "editor.formatOnSave": true
  }
}
```

Install rust-analyzer:
```powershell
rustup component add rust-analyzer
```

Install VS Code extension:
- Extension ID: `rust-lang.rust-analyzer`

### Clippy (Linter)

```powershell
# Install clippy
rustup component add clippy

# Run clippy
cargo clippy

# Run with warnings as errors
cargo clippy -- -D warnings

# Fix automatically (where possible)
cargo clippy --fix
```

### rustfmt (Formatter)

```powershell
# Install rustfmt
rustup component add rustfmt

# Format code
cargo fmt

# Check formatting without modifying
cargo fmt -- --check

# Configure in rustfmt.toml
max_width = 100
tab_spaces = 4
```

### Cargo Edit

```powershell
# Install cargo-edit
cargo install cargo-edit

# Add dependency
cargo add serde

# Add dev dependency
cargo add --dev tokio-test

# Remove dependency
cargo rm serde

# Upgrade dependencies
cargo upgrade
```

## Example Workflows

### Command-Line Tool

```powershell
# Create CLI project
cargo new my-tool
cd my-tool

# Add CLI dependencies
cargo add clap --features derive

# Build and test
cargo build
cargo test

# Install locally
cargo install --path .

# Publish to crates.io
cargo publish
```

### Web API with Actix-web

```powershell
# Create project
cargo new my-api
cd my-api

# Add dependencies
cargo add actix-web
cargo add serde --features derive

# Develop
cargo watch -x run  # Install: cargo install cargo-watch
```

### WebAssembly Project

```powershell
# Add wasm target
rustup target add wasm32-unknown-unknown

# Install wasm-pack
cargo install wasm-pack

# Create project
cargo new --lib my-wasm
cd my-wasm

# Build for wasm
wasm-pack build --target web
```

### Async Runtime with Tokio

```powershell
# Create project
cargo new my-async
cd my-async

# Add tokio
cargo add tokio --features full

# Edit src/main.rs
@"
#[tokio::main]
async fn main() {
    println!("Hello from async!");
}
"@ | Out-File -Encoding UTF8 src/main.rs

# Run
cargo run
```

## Performance Tips

### Optimize Build Time

```powershell
# Use cargo check instead of build for quick feedback
cargo check

# Parallel compilation (automatic, but can specify)
# Edit Cargo.toml or use environment variable
$env:CARGO_BUILD_JOBS = "8"

# Use sccache for caching
cargo install sccache
$env:RUSTC_WRAPPER = "sccache"
```

### Optimize Binary Size

```powershell
# Use release profile with size optimizations
# Edit Cargo.toml
[profile.release]
opt-level = 'z'     # Optimize for size
lto = true          # Enable Link Time Optimization
codegen-units = 1   # Better optimization
strip = true        # Strip symbols

# Build
cargo build --release
```

### Speed Up Incremental Builds

```powershell
# Enable incremental compilation (default in dev)
# Edit Cargo.toml
[profile.dev]
incremental = true
```

## Migration from System Rust

If you have Rust installed system-wide:

1. **Note current toolchains:**
   ```powershell
   rustup toolchain list
   ```

2. **Enable Naner Rust:**
   ```powershell
   # Enable in vendors.json
   .\src\powershell\Setup-NanerVendor.ps1 -VendorId Rust
   ```

3. **Existing projects work immediately:**
   ```powershell
   cd my-rust-project
   cargo build  # Uses Naner's Rust
   ```

4. **Reinstall tools if needed:**
   ```powershell
   cargo install cargo-edit cargo-watch
   ```

## Related Documentation

- [VS Code Settings](../home/.vscode/settings.json) - Rust editor integration
- [Vendor Lock Files](VENDOR-LOCK-FILE.md) - Version control
- [Error Codes](ERROR-CODES.md) - Troubleshooting reference

## References

- [Rust Official Documentation](https://www.rust-lang.org/learn)
- [The Rust Book](https://doc.rust-lang.org/book/)
- [Cargo Book](https://doc.rust-lang.org/cargo/)
- [Rust by Example](https://doc.rust-lang.org/rust-by-example/)
- [crates.io](https://crates.io/) - Rust package registry

---

**Version:** 1.0
**Last Updated:** 2026-01-07
**Rust Version Tested:** 1.75.0
