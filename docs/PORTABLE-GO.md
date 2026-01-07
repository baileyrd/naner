# Portable Go with Naner

Complete guide to using portable Go toolchain within the Naner environment.

## Overview

Naner provides a fully portable Go development environment, allowing you to develop Go applications without installing Go system-wide.

**Benefits:**
- No system-wide Go installation required
- Portable GOPATH (`home/go`)
- Portable module cache
- Version-locked for team consistency
- Works across multiple machines

## Quick Start

### Enable Go Vendor

1. **Edit vendor configuration:**
   ```powershell
   # Open config/vendors.json
   code config/vendors.json
   ```

2. **Set Go enabled to true:**
   ```json
   {
     "Go": {
       "enabled": true,
       ...
     }
   }
   ```

3. **Install Go vendor:**
   ```powershell
   .\src\powershell\Setup-NanerVendor.ps1 -VendorId Go
   ```

4. **Verify installation:**
   ```powershell
   go version
   go env
   ```

## Configuration

### Vendor Configuration

Located in `config/vendors.json` (lines 133-151):

```json
{
  "Go": {
    "enabled": false,
    "name": "Go",
    "extractDir": "go",
    "releaseSource": {
      "type": "go-api",
      "platform": "windows",
      "arch": "amd64"
    },
    "paths": [
      "go\\bin"
    ],
    "postInstall": "Initialize-Go"
  }
}
```

### PostInstall Configuration

The `Initialize-Go` function (Naner.Vendors.psm1:782-820) configures:

1. **Creates GOPATH structure**:
   - `home/go/bin` - Installed binaries
   - `home/go/pkg` - Package objects
   - `home/go/src` - Source code (legacy GOPATH projects)
2. **Sets environment variables**:
   - `GOPATH=%NANER_ROOT%\home\go`
3. **Displays version** - Shows installed Go version

## Usage

### Basic Go Commands

```powershell
# Check Go version
go version

# Show Go environment
go env

# Build current package
go build

# Run Go program
go run main.go

# Test packages
go test ./...

# Get dependencies
go get package-name

# Install program
go install package-name
```

### Go Modules (Recommended)

Go modules are the standard way to manage dependencies (no GOPATH needed for source):

```powershell
# Initialize new module
go mod init github.com/username/project

# Add dependency
go get github.com/gorilla/mux

# Update dependencies
go get -u ./...

# Tidy dependencies
go mod tidy

# Download dependencies
go mod download

# Verify dependencies
go mod verify
```

### Building Applications

```powershell
# Build for current platform
go build -o myapp.exe

# Build with optimizations
go build -ldflags="-s -w" -o myapp.exe

# Cross-compile for Linux
$env:GOOS="linux"; $env:GOARCH="amd64"; go build -o myapp

# Cross-compile for macOS
$env:GOOS="darwin"; $env:GOARCH="amd64"; go build -o myapp
```

### Installing Tools

```powershell
# Install Go tool globally
go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest

# Tool installs to home/go/bin (in PATH)
golangci-lint --version
```

## Directory Structure

```
naner/
├── vendor/
│   └── go/                  # Go installation
│       ├── bin/
│       │   ├── go.exe
│       │   └── gofmt.exe
│       └── pkg/
├── home/
│   └── go/                  # GOPATH
│       ├── bin/            # Installed tools
│       ├── pkg/            # Package objects
│       │   └── mod/       # Module cache
│       └── src/            # Legacy GOPATH source
└── projects/
    └── my-go-project/
        ├── go.mod          # Module definition
        ├── go.sum          # Dependency checksums
        └── main.go
```

## Common Tasks

### Create CLI Application

```powershell
# Create project directory
mkdir my-cli
cd my-cli

# Initialize module
go mod init github.com/username/my-cli

# Create main.go
@"
package main

import (
    "fmt"
    "os"
)

func main() {
    if len(os.Args) < 2 {
        fmt.Println("Usage: my-cli <name>")
        return
    }
    fmt.Printf("Hello, %s!\n", os.Args[1])
}
"@ | Out-File -Encoding UTF8 main.go

# Run
go run main.go World

# Build
go build -o my-cli.exe
```

### Create Web Server

```powershell
# Create project
mkdir my-server
cd my-server
go mod init github.com/username/my-server

# Add Gorilla Mux router
go get github.com/gorilla/mux

# Create main.go
@"
package main

import (
    "fmt"
    "log"
    "net/http"
    "github.com/gorilla/mux"
)

func main() {
    r := mux.NewRouter()
    r.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
        fmt.Fprintf(w, "Hello, World!")
    })

    log.Println("Server starting on :8080")
    log.Fatal(http.ListenAndServe(":8080", r))
}
"@ | Out-File -Encoding UTF8 main.go

# Run server
go run main.go
```

### Create REST API

```powershell
# Create project
mkdir my-api
cd my-api
go mod init github.com/username/my-api

# Add dependencies
go get github.com/gorilla/mux
go get github.com/rs/cors

# Create structured project
mkdir handlers models

# Build and run
go build -o api.exe
.\api.exe
```

### Working with Tests

```powershell
# Create main.go and main_test.go
@"
package main

func Add(a, b int) int {
    return a + b
}
"@ | Out-File -Encoding UTF8 math.go

@"
package main

import "testing"

func TestAdd(t *testing.T) {
    result := Add(2, 3)
    if result != 5 {
        t.Errorf("Add(2, 3) = %d; want 5", result)
    }
}
"@ | Out-File -Encoding UTF8 math_test.go

# Run tests
go test

# Run with coverage
go test -cover

# Generate coverage report
go test -coverprofile=coverage.out
go tool cover -html=coverage.out
```

### Working with go.mod

```powershell
# Add specific version
go get github.com/pkg/errors@v0.9.1

# Update all dependencies
go get -u ./...

# Update specific dependency
go get -u github.com/gorilla/mux

# Remove unused dependencies
go mod tidy

# View dependency graph
go mod graph

# Explain why dependency is needed
go mod why github.com/pkg/errors
```

## Environment Variables

Naner sets these environment variables:

```powershell
# Go binaries in PATH
$env:PATH  # Contains vendor/go/bin and home/go/bin

# GOPATH configuration (set by PostInstall)
$env:GOPATH  # Set to home/go

# Check Go environment
go env GOPATH     # Returns C:\path\to\naner\home\go
go env GOMODCACHE # Returns C:\path\to\naner\home\go\pkg\mod
```

## Version Management

### Check Current Version

```powershell
go version
# Example: go version go1.21.5 windows/amd64
```

### Update to Latest Version

```powershell
# Re-run vendor setup
.\src\powershell\Setup-NanerVendor.ps1 -VendorId Go -ForceDownload

# Update lock file
.\src\powershell\Export-VendorLockFile.ps1 -IncludeHashes
```

### Lock Specific Version

See [VENDOR-LOCK-FILE.md](VENDOR-LOCK-FILE.md) for version locking.

## Troubleshooting

### Go Not Found

**Error:** `go: command not found` or `'go' is not recognized`

**Causes:**
- Go vendor not installed
- Naner environment not loaded

**Solutions:**
```powershell
# Check if vendor is installed
Test-Path vendor/go/bin/go.exe

# If not installed
.\src\powershell\Setup-NanerVendor.ps1 -VendorId Go

# Reload environment
pwsh  # Start new PowerShell session with Naner profile
```

### GOPATH Issues

**Error:** GOPATH not set correctly

**Solutions:**
```powershell
# Check GOPATH
$env:GOPATH
# Should return: C:\path\to\naner\home\go

# Check Go environment
go env GOPATH

# If incorrect, reinstall
.\src\powershell\Setup-NanerVendor.ps1 -VendorId Go -ForceDownload
```

### Module Download Fails

**Error:** `go: module download failed`

**Causes:**
- Network issues
- Proxy settings
- Private repositories

**Solutions:**
```powershell
# Check proxy settings
go env GOPROXY
# Default: https://proxy.golang.org,direct

# Use direct connection
go env -w GOPROXY=direct

# For private repos, use GOPRIVATE
go env -w GOPRIVATE=github.com/mycompany/*

# Clear module cache
go clean -modcache
```

### Build Errors

**Error:** `cannot find package`

**Solutions:**
```powershell
# Download dependencies
go mod download

# Tidy modules
go mod tidy

# Verify modules
go mod verify

# Update dependencies
go get -u ./...
```

### Cross-Compilation Issues

**Error:** Cross-compilation not working

**Solutions:**
```powershell
# Set environment variables explicitly
$env:GOOS = "linux"
$env:GOARCH = "amd64"
$env:CGO_ENABLED = "0"  # Disable CGO for pure Go
go build -o myapp

# Reset after building
Remove-Item Env:\GOOS
Remove-Item Env:\GOARCH
Remove-Item Env:\CGO_ENABLED
```

## Best Practices

### ✅ DO

- **Use Go modules** for all projects:
  ```powershell
  go mod init github.com/username/project
  ```

- **Run go mod tidy** regularly:
  ```powershell
  go mod tidy
  ```

- **Commit go.mod and go.sum**:
  ```powershell
  git add go.mod go.sum
  ```

- **Use semantic import versioning**:
  ```powershell
  go get github.com/pkg/errors/v2
  ```

- **Format code before committing**:
  ```powershell
  go fmt ./...
  ```

- **Run tests frequently**:
  ```powershell
  go test ./...
  ```

### ❌ DON'T

- **Don't modify GOPATH manually** - PostInstall handles this
- **Don't commit vendor/ directory** unless using vendoring
- **Don't use legacy GOPATH mode** - Use Go modules instead
- **Don't mix global and local Go installations**

## Integration with Other Tools

### VS Code Integration

Naner's VS Code settings (home/.vscode/settings.json) automatically detect Go:

```json
{
  "go.goroot": "${env:NANER_ROOT}\\vendor\\go",
  "go.gopath": "${env:NANER_ROOT}\\home\\go",
  "[go]": {
    "editor.defaultFormatter": "golang.go",
    "editor.formatOnSave": true,
    "editor.codeActionsOnSave": {
      "source.organizeImports": "explicit"
    }
  }
}
```

Install Go extension in VS Code:
- Extension ID: `golang.go`

### golangci-lint Integration

```powershell
# Install golangci-lint
go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest

# Run linter
golangci-lint run

# Run specific linters
golangci-lint run --enable=gofmt,govet,staticcheck

# Generate config
golangci-lint config > .golangci.yml
```

### Delve Debugger

```powershell
# Install Delve
go install github.com/go-delve/delve/cmd/dlv@latest

# Debug program
dlv debug

# Debug with args
dlv debug -- arg1 arg2

# Debug tests
dlv test
```

### Air (Live Reload)

```powershell
# Install Air
go install github.com/cosmtrek/air@latest

# Initialize Air
air init

# Run with live reload
air
```

## Example Workflows

### Microservice Development

```powershell
# Create service
mkdir my-service
cd my-service
go mod init github.com/username/my-service

# Add dependencies
go get github.com/gorilla/mux
go get github.com/sirupsen/logrus

# Create structure
mkdir handlers models config

# Build
go build -o service.exe

# Run
.\service.exe
```

### CLI Tool with Cobra

```powershell
# Install Cobra generator
go install github.com/spf13/cobra-cli@latest

# Create new CLI
cobra-cli init

# Add command
cobra-cli add serve

# Build
go build -o mycli.exe
```

### gRPC Service

```powershell
# Install protoc compiler separately
# Download from: https://github.com/protocolbuffers/protobuf/releases

# Install Go plugins
go install google.golang.org/protobuf/cmd/protoc-gen-go@latest
go install google.golang.org/grpc/cmd/protoc-gen-go-grpc@latest

# Create proto file and generate
protoc --go_out=. --go-grpc_out=. proto/*.proto
```

## Performance Tips

### Optimize Build Time

```powershell
# Use build cache (enabled by default)
go build

# Parallel compilation (automatic)
go build -p 8

# Disable CGO if not needed
$env:CGO_ENABLED="0"; go build
```

### Reduce Binary Size

```powershell
# Strip debug info and symbol table
go build -ldflags="-s -w" -o myapp.exe

# Use UPX compressor (install separately)
go build -ldflags="-s -w" -o myapp.exe
upx --best myapp.exe
```

## Migration from System Go

If you have Go installed system-wide:

1. **Note current GOPATH projects** (if any):
   ```powershell
   $env:GOPATH
   ls $env:GOPATH/src
   ```

2. **Enable Naner Go:**
   ```powershell
   # Enable in vendors.json
   .\src\powershell\Setup-NanerVendor.ps1 -VendorId Go
   ```

3. **Module-based projects** work immediately:
   ```powershell
   cd my-module-project
   go build  # Uses Naner's Go
   ```

4. **For GOPATH projects**, migrate to modules:
   ```powershell
   cd $old_gopath/src/github.com/user/project
   go mod init github.com/user/project
   go mod tidy
   ```

## Related Documentation

- [VS Code Settings](../home/.vscode/settings.json) - Go editor integration
- [Vendor Lock Files](VENDOR-LOCK-FILE.md) - Version control
- [Error Codes](ERROR-CODES.md) - Troubleshooting reference

## References

- [Go Official Documentation](https://go.dev/doc/)
- [Go Modules Reference](https://go.dev/ref/mod)
- [Effective Go](https://go.dev/doc/effective_go)
- [Go by Example](https://gobyexample.com/)

---

**Version:** 1.0
**Last Updated:** 2026-01-07
**Go Version Tested:** 1.21.5
