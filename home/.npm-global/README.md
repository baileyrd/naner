# Portable npm Global Packages

This directory contains globally installed npm packages for the portable Node.js environment.

## Usage

```bash
# Install global package
npm install -g <package-name>

# List global packages
npm list -g --depth=0

# Update global package
npm update -g <package-name>

# Uninstall global package
npm uninstall -g <package-name>
```

## Popular Global Packages

```bash
npm install -g typescript        # TypeScript compiler
npm install -g nodemon           # Auto-restart for development
npm install -g pm2               # Process manager
npm install -g eslint            # JavaScript linter
npm install -g prettier          # Code formatter
npm install -g @angular/cli      # Angular CLI
npm install -g create-react-app  # React project generator
npm install -g vue-cli           # Vue CLI
npm install -g http-server       # Simple HTTP server
```

## Location

- Global packages: `%NANER_ROOT%\home\.npm-global`
- npm cache: `%NANER_ROOT%\home\.npm-cache`
- Executables: `%NANER_ROOT%\home\.npm-global` (in PATH)

All global packages are portable and travel with Naner.
