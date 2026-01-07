# Portable Go Workspace

This directory contains your portable Go workspace (GOPATH) for the Naner environment.

## Directory Structure

```
go/
├── bin/        # Compiled executables from 'go install'
├── pkg/        # Package objects (cache)
└── src/        # Source code (for Go modules, this is typically empty)
```

## Usage

### Create a New Go Module

```bash
# Create project directory
mkdir myproject
cd myproject

# Initialize Go module
go mod init github.com/username/myproject

# Create main.go
echo 'package main

import "fmt"

func main() {
    fmt.Println("Hello from Naner!")
}' > main.go

# Run the program
go run main.go

# Build executable
go build
```

### Install Go Tools

```bash
# Install popular Go tools
go install golang.org/x/tools/gopls@latest           # Language server
go install github.com/go-delve/delve/cmd/dlv@latest  # Debugger
go install golang.org/x/tools/cmd/goimports@latest   # Import formatter
go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest  # Linter

# Installed binaries go to: %NANER_ROOT%\home\go\bin
# This directory is already in your PATH
```

### Download Dependencies

```bash
# Download module dependencies
go mod download

# Add missing dependencies
go mod tidy

# Verify dependencies
go mod verify
```

### Build and Install

```bash
# Build for current platform
go build

# Build for specific platform
GOOS=linux GOARCH=amd64 go build

# Install to $GOPATH/bin
go install
```

## Environment Variables

Naner automatically sets these Go environment variables:

- **GOROOT**: `%NANER_ROOT%\vendor\go` - Go installation directory
- **GOPATH**: `%NANER_ROOT%\home\go` - Go workspace (this directory)
- **GOCACHE**: `%NANER_ROOT%\home\.cache\go-build` - Build cache

### Verify Environment

```bash
# Check Go environment
go env GOROOT
go env GOPATH
go env GOCACHE

# Or see all Go environment variables
go env
```

## Go Modules (Recommended)

Go modules are the standard way to manage dependencies in modern Go projects:

```bash
# Initialize module
go mod init example.com/myproject

# Add dependency (automatically adds to go.mod)
go get github.com/gin-gonic/gin

# Update dependencies
go get -u ./...

# Remove unused dependencies
go mod tidy
```

Your `go.mod` file defines your project and its dependencies:

```go
module example.com/myproject

go 1.21

require (
    github.com/gin-gonic/gin v1.9.1
)
```

## Popular Go Packages

### Web Frameworks

```bash
go get github.com/gin-gonic/gin              # Fast HTTP web framework
go get github.com/gofiber/fiber/v2           # Express-inspired framework
go get github.com/labstack/echo/v4           # High performance framework
```

### Database

```bash
go get gorm.io/gorm                          # ORM library
go get github.com/jmoiron/sqlx               # SQL extensions
go get github.com/lib/pq                     # PostgreSQL driver
go get github.com/go-sql-driver/mysql        # MySQL driver
```

### CLI Tools

```bash
go get github.com/spf13/cobra                # CLI framework
go get github.com/urfave/cli/v2              # CLI library
go get github.com/AlecAivazis/survey/v2      # Terminal UI prompts
```

### Utilities

```bash
go get github.com/joho/godotenv              # .env file support
go get github.com/stretchr/testify           # Testing toolkit
go get go.uber.org/zap                       # Fast logging
```

## Common Commands

```bash
# Run tests
go test ./...

# Run tests with coverage
go test -cover ./...

# Format code
go fmt ./...

# Run linter (requires golangci-lint installed)
golangci-lint run

# Clean module cache
go clean -modcache

# List dependencies
go list -m all

# Check for updates
go list -u -m all
```

## IDE Integration

### VS Code

Install the official Go extension:
1. Open VS Code
2. Install "Go" extension (by Go Team at Google)
3. The extension will automatically detect your GOROOT and GOPATH
4. Install recommended tools when prompted (gopls, dlv, etc.)

### GoLand

1. File → Settings → Go → GOROOT
2. Point to: `%NANER_ROOT%\vendor\go`
3. GOPATH should auto-detect to: `%NANER_ROOT%\home\go`

## Cross-Compilation

Go makes it easy to build for different platforms:

```bash
# Build for Windows
GOOS=windows GOARCH=amd64 go build -o app.exe

# Build for Linux
GOOS=linux GOARCH=amd64 go build -o app

# Build for macOS
GOOS=darwin GOARCH=amd64 go build -o app

# Build for ARM (Raspberry Pi)
GOOS=linux GOARCH=arm go build -o app
```

## Troubleshooting

### Command Not Found

If `go` command is not found:
```bash
# Verify Go is installed
ls %NANER_ROOT%\vendor\go\bin\go.exe

# Check PATH includes Go
echo $PATH | grep go
```

### Module Issues

```bash
# Clear module cache
go clean -modcache

# Re-download dependencies
go mod download

# Verify module integrity
go mod verify
```

### Proxy Issues

If behind a corporate firewall:
```bash
# Set Go proxy
go env -w GOPROXY=https://proxy.golang.org,direct

# Or disable proxy
go env -w GOPROXY=direct

# Set private module patterns
go env -w GOPRIVATE=github.com/yourcompany/*
```

## Resources

- **Official Docs**: https://go.dev/doc/
- **Go by Example**: https://gobyexample.com/
- **Effective Go**: https://go.dev/doc/effective_go
- **Go Tour**: https://go.dev/tour/
- **Standard Library**: https://pkg.go.dev/std
- **Awesome Go**: https://github.com/avelino/awesome-go

## Portability

All Go workspace data is portable:
- ✅ Source code (if stored in `home/go/src/`)
- ✅ Go modules cache
- ✅ Build cache
- ✅ Installed binaries (in `home/go/bin/`)

When you move Naner to a new machine, your entire Go development environment travels with it.
