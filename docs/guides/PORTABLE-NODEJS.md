# Portable Node.js with Naner

Complete guide to using portable Node.js and npm within the Naner environment.

## Overview

Naner provides a fully portable Node.js runtime with npm package manager, allowing you to develop JavaScript/TypeScript applications without installing Node.js system-wide.

**Benefits:**
- No system-wide Node.js installation required
- Portable npm global packages (`home/.npm-global`)
- Portable npm cache (`home/.npm-cache`)
- Version-locked for team consistency
- Works across multiple machines

## Quick Start

### Enable Node.js Vendor

1. **Edit vendor configuration:**
   ```powershell
   # Open config/vendors.json
   code config/vendors.json
   ```

2. **Set NodeJS enabled to true:**
   ```json
   {
     "NodeJS": {
       "enabled": true,
       ...
     }
   }
   ```

3. **Install Node.js vendor:**
   ```powershell
   .\src\powershell\Setup-NanerVendor.ps1 -VendorId NodeJS
   ```

4. **Verify installation:**
   ```powershell
   node --version
   npm --version
   ```

## Configuration

### Vendor Configuration

Located in `config/vendors.json` (lines 97-116):

```json
{
  "NodeJS": {
    "enabled": false,
    "name": "Node.js",
    "extractDir": "nodejs",
    "releaseSource": {
      "type": "github",
      "repo": "nodejs/node",
      "assetPattern": "*-win-x64.zip",
      "excludePattern": []
    },
    "paths": [
      "nodejs"
    ],
    "postInstall": "Initialize-NodeJS"
  }
}
```

### PostInstall Configuration

The `Initialize-NodeJS` function (Naner.Vendors.psm1:635-704) configures:

1. **Flattens nested directory structure** - Node.js archives contain nested folders
2. **Creates portable npm directories**:
   - `home/.npm-global` - Global packages
   - `home/.npm-cache` - Package cache
3. **Configures npm settings**:
   ```
   npm config set prefix "%NANER_ROOT%\home\.npm-global"
   npm config set cache "%NANER_ROOT%\home\.npm-cache"
   ```
4. **Verifies installation** - Displays Node.js and npm versions

## Usage

### Basic Node.js Commands

```powershell
# Check Node.js version
node --version

# Run JavaScript file
node script.js

# Start Node REPL
node

# Run npm commands
npm --version
npm init
npm install
```

### Installing Global Packages

Global packages install to `home/.npm-global`:

```powershell
# Install globally
npm install -g typescript
npm install -g nodemon
npm install -g eslint

# Run global commands
tsc --version
nodemon --version
eslint --version
```

**Note:** Global binaries are automatically in PATH via Naner's PATH management.

### Project-Specific Packages

```powershell
# Initialize new project
npm init -y

# Install dependencies
npm install express
npm install --save-dev jest

# Install all dependencies from package.json
npm install
```

### npm Scripts

```powershell
# Run scripts defined in package.json
npm run dev
npm run build
npm run test
npm start
```

## Directory Structure

```
naner/
├── vendor/
│   └── nodejs/              # Node.js runtime
│       ├── node.exe
│       └── npm (script)
├── home/
│   ├── .npm-global/         # Global npm packages
│   │   ├── bin/            # Global executables
│   │   └── lib/            # Global packages
│   └── .npm-cache/          # npm package cache
└── projects/
    └── my-project/
        └── node_modules/    # Project dependencies
```

## Common Tasks

### Create React App

```powershell
# Using Naner template (recommended)
New-NanerProject -Type react-vite-ts -Name my-app
cd my-app
npm run dev
```

### Create Express API

```powershell
# Using Naner template (recommended)
New-NanerProject -Type nodejs-express-api -Name my-api
cd my-api
npm run dev

# Or manually
mkdir my-api
cd my-api
npm init -y
npm install express
```

### TypeScript Setup

```powershell
# Install TypeScript globally
npm install -g typescript

# Initialize TypeScript project
mkdir my-ts-project
cd my-ts-project
npm init -y
npm install --save-dev typescript @types/node

# Create tsconfig.json
tsc --init

# Compile TypeScript
tsc
```

### Working with Package.json

```powershell
# Add dependency
npm install lodash

# Add dev dependency
npm install --save-dev @types/lodash

# Update packages
npm update

# Check for outdated packages
npm outdated

# Audit for vulnerabilities
npm audit
npm audit fix
```

## Environment Variables

Naner sets these environment variables:

```powershell
# Node.js is in PATH
$env:PATH  # Contains vendor/nodejs

# npm configuration (set by PostInstall)
npm config get prefix      # Returns home/.npm-global
npm config get cache       # Returns home/.npm-cache
```

## Version Management

### Check Current Version

```powershell
node --version
# Example: v20.11.0
```

### Update to Latest Version

```powershell
# Re-run vendor setup
.\src\powershell\Setup-NanerVendor.ps1 -VendorId NodeJS -ForceDownload

# Update lock file
.\src\powershell\Export-VendorLockFile.ps1 -IncludeHashes
```

### Lock Specific Version

See [VENDOR-LOCK-FILE.md](VENDOR-LOCK-FILE.md) for version locking.

## Troubleshooting

### Node.js Not Found

**Error:** `node: command not found` or `'node' is not recognized`

**Causes:**
- Node.js vendor not installed
- Naner environment not loaded

**Solutions:**
```powershell
# Check if vendor is installed
Test-Path vendor/nodejs/node.exe

# If not installed
.\src\powershell\Setup-NanerVendor.ps1 -VendorId NodeJS

# Reload environment
pwsh  # Start new PowerShell session with Naner profile
```

### npm Global Packages Not Found

**Error:** Global package command not found

**Cause:** npm prefix not configured correctly

**Solution:**
```powershell
# Check npm prefix
npm config get prefix
# Should return: C:\path\to\naner\home\.npm-global

# Reconfigure if needed
.\src\powershell\Setup-NanerVendor.ps1 -VendorId NodeJS -ForceDownload
```

### Permission Errors

**Error:** `EACCES: permission denied`

**Cause:** Trying to install to system directories

**Solution:**
```powershell
# Always use Naner's npm (which is configured for portable directories)
# Never use system npm if it exists

# Verify you're using Naner's npm
Get-Command npm
# Should show: vendor\nodejs\npm
```

### Package Installation Fails

**Error:** Network errors or corrupted packages

**Solutions:**
```powershell
# Clear npm cache
npm cache clean --force

# Retry installation
npm install

# Use different registry (if corporate network)
npm config set registry https://registry.npmjs.org/
```

### Node Version Mismatch

**Error:** Project requires different Node.js version

**Options:**
1. **Update Naner's Node.js** (affects all projects):
   ```powershell
   .\src\powershell\Setup-NanerVendor.ps1 -VendorId NodeJS -ForceDownload
   ```

2. **Use nvm** (Node Version Manager) - Not portable, but available if needed:
   ```powershell
   # Install nvm-windows separately
   # https://github.com/coreybutler/nvm-windows
   ```

## Best Practices

### ✅ DO

- **Use Naner templates** for new projects:
  ```powershell
  New-NanerProject -Type react-vite-ts -Name my-app
  ```

- **Commit package.json and package-lock.json**:
  ```powershell
  git add package.json package-lock.json
  ```

- **Use npm scripts** for project tasks:
  ```json
  {
    "scripts": {
      "dev": "vite",
      "build": "vite build",
      "test": "jest"
    }
  }
  ```

- **Install dev dependencies correctly**:
  ```powershell
  npm install --save-dev jest eslint
  ```

- **Check for vulnerabilities regularly**:
  ```powershell
  npm audit
  ```

### ❌ DON'T

- **Don't install packages with sudo/admin** - Not needed with Naner
- **Don't modify npm prefix manually** - PostInstall handles this
- **Don't commit node_modules** - Always in .gitignore
- **Don't mix global and local package managers** - Use Naner's npm exclusively

## Integration with Other Tools

### VS Code Integration

Naner's VS Code settings (home/.vscode/settings.json) automatically detect Node.js:

```json
{
  "javascript.updateImportsOnFileMove.enabled": "always",
  "typescript.updateImportsOnFileMove.enabled": "always"
}
```

No additional configuration needed.

### TypeScript Integration

```powershell
# Install TypeScript globally
npm install -g typescript

# Project-specific TypeScript
npm install --save-dev typescript @types/node

# VS Code will auto-detect tsconfig.json
```

### ESLint Integration

```powershell
# Install ESLint globally
npm install -g eslint

# Initialize ESLint in project
cd my-project
eslint --init
```

### Prettier Integration

```powershell
# Install Prettier globally
npm install -g prettier

# Format code
prettier --write "src/**/*.js"
```

## Example Workflows

### Full Stack JavaScript Project

```powershell
# Create Express backend
New-NanerProject -Type nodejs-express-api -Name backend
cd backend
npm install

# Create React frontend
cd ..
New-NanerProject -Type react-vite-ts -Name frontend
cd frontend
npm install

# Run both
# Terminal 1
cd backend
npm run dev

# Terminal 2
cd frontend
npm run dev
```

### CLI Tool Development

```powershell
# Create new CLI project
mkdir my-cli
cd my-cli
npm init -y

# Add dependencies
npm install commander chalk

# Create bin entry in package.json
{
  "bin": {
    "my-cli": "./index.js"
  }
}

# Test locally
npm link  # Creates global symlink
my-cli --help
```

### Package Publishing

```powershell
# Login to npm (one-time)
npm login

# Publish package
npm publish

# Update version and publish
npm version patch
npm publish
```

## Performance Tips

### Speed Up npm install

```powershell
# Use npm ci for clean installs (faster)
npm ci

# Install only production dependencies
npm install --production

# Use package-lock.json for deterministic installs
```

### Reduce Package Size

```powershell
# Check package size
npm install -g cost-of-modules
cost-of-modules

# Analyze bundle
npm install -g webpack-bundle-analyzer
```

## Migration from System Node.js

If you have Node.js installed system-wide:

1. **Backup global packages:**
   ```powershell
   npm list -g --depth=0 > global-packages.txt
   ```

2. **Enable Naner Node.js:**
   ```powershell
   # Enable in vendors.json
   .\src\powershell\Setup-NanerVendor.ps1 -VendorId NodeJS
   ```

3. **Reinstall global packages:**
   ```powershell
   # Install needed globals with Naner's npm
   npm install -g typescript eslint prettier
   ```

4. **Existing projects work as-is** - Just run `npm install` in project directories

## Related Documentation

- [Project Templates](../home/Templates/README.md) - React, Express templates
- [VS Code Settings](../home/.vscode/settings.json) - Editor integration
- [Vendor Lock Files](VENDOR-LOCK-FILE.md) - Version control
- [Error Codes](ERROR-CODES.md) - Troubleshooting reference

## References

- [Node.js Official Documentation](https://nodejs.org/docs/)
- [npm Documentation](https://docs.npmjs.com/)
- [package.json Reference](https://docs.npmjs.com/cli/configuring-npm/package-json)

---

**Version:** 1.0
**Last Updated:** 2026-01-07
**Node.js Version Tested:** v20.11.0
