# Portable Ruby with Naner

Complete guide to using portable Ruby within the Naner environment.

## Overview

Naner provides a fully portable Ruby development environment via RubyInstaller2 with DevKit, including gem and bundler package managers, allowing you to develop Ruby applications without installing Ruby system-wide.

**Benefits:**
- No system-wide Ruby installation required
- RubyInstaller2 with MSYS2 DevKit for native extensions
- Portable gem directory (`home/.gem`)
- Bundler included for dependency management
- Version-locked for team consistency
- Works across multiple machines

## Quick Start

### Enable Ruby Vendor

1. **Edit vendor configuration:**
   ```powershell
   # Open config/vendors.json
   code config/vendors.json
   ```

2. **Set Ruby enabled to true:**
   ```json
   {
     "Ruby": {
       "enabled": true,
       ...
     }
   }
   ```

3. **Install Ruby vendor:**
   ```powershell
   .\src\powershell\Setup-NanerVendor.ps1 -VendorId Ruby
   ```

4. **Verify installation:**
   ```powershell
   ruby --version
   gem --version
   bundle --version
   ```

## Configuration

### Vendor Configuration

Located in `config/vendors.json` (lines 177-196):

```json
{
  "Ruby": {
    "enabled": false,
    "name": "Ruby",
    "extractDir": "ruby",
    "releaseSource": {
      "type": "github",
      "repo": "oneclick/rubyinstaller2",
      "assetPattern": "*-x64.7z",
      "excludePattern": ["*without-devkit*"]
    },
    "paths": [
      "ruby\\bin"
    ],
    "postInstall": "Initialize-Ruby"
  }
}
```

### PostInstall Configuration

The `Initialize-Ruby` function (Naner.Vendors.psm1:924-990) configures:

1. **Sets environment variable**:
   - `GEM_HOME=%NANER_ROOT%\home\.gem`
2. **Creates portable .gemrc**:
   - Disables documentation generation (faster installs)
   - Sets gem install directory
3. **Installs bundler** if not present
4. **Verifies installation** - Displays Ruby, gem, and bundler versions

## Usage

### Basic Ruby Commands

```powershell
# Check Ruby version
ruby --version

# Run Ruby script
ruby script.rb

# Start Interactive Ruby (irb)
irb

# Check syntax without running
ruby -c script.rb

# Run with warnings
ruby -w script.rb
```

### Package Management with gem

```powershell
# Install gem
gem install rails

# Install specific version
gem install sinatra -v 3.0.0

# List installed gems
gem list

# Show gem info
gem info rails

# Uninstall gem
gem uninstall sinatra

# Update gem
gem update rails

# Update all gems
gem update

# Search for gems
gem search rails
```

### Bundler for Dependency Management

```powershell
# Install bundler (if not already installed)
gem install bundler

# Create Gemfile
bundle init

# Install gems from Gemfile
bundle install

# Update gems
bundle update

# Execute command with bundled gems
bundle exec ruby script.rb

# Show installed gems
bundle list

# Check for outdated gems
bundle outdated
```

## Directory Structure

```
naner/
├── vendor/
│   └── ruby/                # Ruby installation
│       ├── bin/
│       │   ├── ruby.exe
│       │   ├── gem.bat
│       │   └── bundle.bat
│       └── lib/
├── home/
│   ├── .gem/               # GEM_HOME
│   │   └── ruby/
│       │       └── 3.2.0/
│       │           ├── bin/        # Installed executables
│       │           └── gems/       # Installed gems
│   └── .gemrc              # Gem configuration
└── projects/
    └── my-ruby-project/
        ├── Gemfile         # Dependency specification
        ├── Gemfile.lock    # Dependency lock file
        └── app.rb
```

## Common Tasks

### Create Ruby Script

```powershell
# Create simple Ruby script
@"
#!/usr/bin/env ruby

def greet(name)
  puts "Hello, #{name}!"
end

if ARGV.empty?
  puts "Usage: #{$0} <name>"
  exit 1
end

greet(ARGV[0])
"@ | Out-File -Encoding UTF8 hello.rb

# Run script
ruby hello.rb World
```

### Create Sinatra Web App

```powershell
# Create project directory
mkdir my-sinatra-app
cd my-sinatra-app

# Create Gemfile
@"
source 'https://rubygems.org'

gem 'sinatra', '~> 3.0'
gem 'puma', '~> 6.0'
"@ | Out-File Gemfile

# Install dependencies
bundle install

# Create app.rb
@"
require 'sinatra'

get '/' do
  'Hello, World!'
end

get '/greet/:name' do
  "Hello, #{params[:name]}!"
end
"@ | Out-File app.rb

# Run app
ruby app.rb
```

### Create Rails Application

```powershell
# Install Rails
gem install rails

# Create new Rails app
rails new my-rails-app
cd my-rails-app

# Install dependencies
bundle install

# Start server
rails server

# Or use bundle exec
bundle exec rails server
```

### Working with Gemfile

```ruby
# Gemfile example
source 'https://rubygems.org'

# Ruby version
ruby '3.2.0'

# Gems
gem 'sinatra', '~> 3.0'
gem 'pg', '~> 1.5'
gem 'redis', '~> 5.0'

# Development only
group :development do
  gem 'pry'
  gem 'rubocop'
end

# Test only
group :test do
  gem 'rspec'
  gem 'rack-test'
end

# Development and test
group :development, :test do
  gem 'dotenv'
end
```

### Running Tests with RSpec

```powershell
# Add RSpec to Gemfile
bundle add rspec --group test

# Initialize RSpec
rspec --init

# Create test file
@"
require 'rspec'

describe 'Math' do
  it 'adds numbers' do
    expect(2 + 2).to eq(4)
  end
end
"@ | Out-File spec/math_spec.rb

# Run tests
rspec

# Or with bundle exec
bundle exec rspec
```

## Environment Variables

Naner sets these environment variables:

```powershell
# Ruby binaries in PATH
$env:PATH  # Contains vendor/ruby/bin and home/.gem/ruby/3.2.0/bin

# Gem home (set by PostInstall)
$env:GEM_HOME  # Set to home/.gem

# Verify environment
gem env
```

## Version Management

### Check Current Version

```powershell
ruby --version
# Example: ruby 3.2.2 (2023-03-30 revision e51014f9c0) [x64-mingw-ucrt]

gem --version
# Example: 3.4.10

bundle --version
# Example: Bundler version 2.4.10
```

### Update Ruby

```powershell
# Re-run vendor setup for new version
.\src\powershell\Setup-NanerVendor.ps1 -VendorId Ruby -ForceDownload

# Update lock file
.\src\powershell\Export-VendorLockFile.ps1 -IncludeHashes
```

### Update Gems

```powershell
# Update all gems
gem update

# Update specific gem
gem update rails

# Update bundler itself
gem update bundler
```

## Troubleshooting

### Ruby Not Found

**Error:** `ruby: command not found` or `'ruby' is not recognized`

**Causes:**
- Ruby vendor not installed
- Naner environment not loaded

**Solutions:**
```powershell
# Check if vendor is installed
Test-Path vendor/ruby/bin/ruby.exe

# If not installed
.\src\powershell\Setup-NanerVendor.ps1 -VendorId Ruby

# Reload environment
pwsh  # Start new PowerShell session with Naner profile
```

### GEM_HOME Issues

**Error:** Gems installing to wrong location

**Solutions:**
```powershell
# Check GEM_HOME
$env:GEM_HOME
# Should return: C:\path\to\naner\home\.gem

# Check gem environment
gem env

# If incorrect, reinstall
.\src\powershell\Setup-NanerVendor.ps1 -VendorId Ruby -ForceDownload
```

### Native Extension Build Fails

**Error:** `extconf.rb failed` or `compilation failed`

**Cause:** Missing build tools

**Solutions:**
```powershell
# Ruby with DevKit should have MSYS2 tools included

# Verify DevKit
Test-Path vendor/ruby/msys64

# If issues persist, reinstall Ruby vendor
.\src\powershell\Setup-NanerVendor.ps1 -VendorId Ruby -ForceDownload
```

### Bundle Install Fails

**Error:** `Could not find gem` or network errors

**Solutions:**
```powershell
# Check Gemfile for typos

# Update bundler
gem update bundler

# Clean bundle cache
bundle clean --force

# Retry installation
bundle install

# Use specific source
bundle config set --local mirror.https://rubygems.org https://rubygems.org
```

### Command Not Found After Gem Install

**Error:** Gem executable not found after installation

**Cause:** Gem bin directory not in PATH

**Solutions:**
```powershell
# Check if gem was installed
gem list | Select-String gem-name

# Reload environment (gem bin directory should be in PATH via Naner)
pwsh

# Or run with bundle exec
bundle exec command-name
```

## Best Practices

### ✅ DO

- **Use Bundler for all projects**:
  ```powershell
  bundle init
  bundle add sinatra
  ```

- **Commit Gemfile and Gemfile.lock**:
  ```powershell
  git add Gemfile Gemfile.lock
  ```

- **Use bundle exec** to ensure correct gem versions:
  ```powershell
  bundle exec ruby app.rb
  bundle exec rspec
  ```

- **Specify Ruby version in Gemfile**:
  ```ruby
  ruby '3.2.0'
  ```

- **Use semantic versioning**:
  ```ruby
  gem 'rails', '~> 7.0.0'  # Compatible with 7.0.x
  ```

- **Group gems by environment**:
  ```ruby
  group :development, :test do
    gem 'pry'
  end
  ```

### ❌ DON'T

- **Don't modify GEM_HOME manually** - PostInstall handles this
- **Don't use sudo** for gem install (not needed with Naner)
- **Don't commit Gemfile.lock for gems** - Only for applications
- **Don't install gems with --user-install** - Conflicts with GEM_HOME

## Integration with Other Tools

### VS Code Integration

Naner's VS Code settings (home/.vscode/settings.json) automatically detect Ruby:

```json
{
  "[ruby]": {
    "editor.defaultFormatter": "Shopify.ruby-lsp",
    "editor.formatOnSave": true
  }
}
```

Install Ruby LSP:
```powershell
gem install ruby-lsp
```

Install VS Code extension:
- Extension ID: `Shopify.ruby-lsp`

### RuboCop (Linter)

```powershell
# Install RuboCop
gem install rubocop

# Run RuboCop
rubocop

# Auto-correct issues
rubocop -A

# Configure in .rubocop.yml
AllCops:
  TargetRubyVersion: 3.2
  NewCops: enable

Style/StringLiterals:
  Enabled: true
  EnforcedStyle: single_quotes
```

### Pry (Debugger)

```powershell
# Install Pry
gem install pry

# Use in code
require 'pry'

def my_method
  x = 10
  binding.pry  # Execution stops here
  x * 2
end

# Run script and debug
ruby script.rb
```

### Rake (Build Tool)

```powershell
# Install Rake
gem install rake

# Create Rakefile
@"
task default: %w[test]

task :test do
  ruby 'test/test_helper.rb'
end

task :clean do
  rm_f 'tmp/*'
end
"@ | Out-File Rakefile

# Run tasks
rake              # Runs default task
rake test         # Runs test task
rake clean        # Runs clean task
```

## Example Workflows

### Sinatra API Development

```powershell
# Create project
mkdir my-api
cd my-api

# Create Gemfile
bundle init
bundle add sinatra
bundle add puma
bundle add json

# Create config.ru
@"
require './app'
run Sinatra::Application
"@ | Out-File config.ru

# Create app.rb
@"
require 'sinatra'
require 'json'

get '/api/status' do
  content_type :json
  { status: 'ok', timestamp: Time.now }.to_json
end
"@ | Out-File app.rb

# Run with Puma
bundle exec puma
```

### Rails API Development

```powershell
# Install Rails
gem install rails

# Create API-only app
rails new my-api --api
cd my-api

# Generate model
rails generate model User name:string email:string

# Run migrations
rails db:migrate

# Generate controller
rails generate controller Users

# Start server
rails server
```

### Ruby Gem Development

```powershell
# Create gem skeleton
bundle gem my_gem
cd my_gem

# Edit lib/my_gem.rb
# Edit spec files

# Run tests
bundle exec rspec

# Build gem
gem build my_gem.gemspec

# Install locally
gem install ./my_gem-0.1.0.gem

# Publish to RubyGems (requires account)
gem push my_gem-0.1.0.gem
```

### Scripting and Automation

```powershell
# Create utility script
@"
#!/usr/bin/env ruby

require 'fileutils'
require 'optparse'

options = {}
OptionParser.new do |opts|
  opts.banner = 'Usage: script.rb [options]'

  opts.on('-v', '--verbose', 'Run verbosely') do |v|
    options[:verbose] = v
  end
end.parse!

puts 'Running...' if options[:verbose]
"@ | Out-File -Encoding UTF8 script.rb

# Run script
ruby script.rb --verbose
```

## Performance Tips

### Speed Up Gem Installation

```powershell
# Disable documentation (already done by PostInstall)
# But you can verify in home/.gemrc:
---
gem: --no-document

# Use parallel gem installation
bundle install --jobs=4

# Use specific gemserver if available
bundle config set --local mirror.https://rubygems.org http://your-gem-server
```

### Reduce Memory Usage

```powershell
# Use Ruby with jemalloc (included in RubyInstaller2)

# Set memory limits for specific apps
$env:RUBY_GC_HEAP_INIT_SLOTS = "10000"
$env:RUBY_GC_HEAP_GROWTH_FACTOR = "1.1"
```

## Migration from System Ruby

If you have Ruby installed system-wide:

1. **Note installed gems:**
   ```powershell
   gem list > installed-gems.txt
   ```

2. **Enable Naner Ruby:**
   ```powershell
   # Enable in vendors.json
   .\src\powershell\Setup-NanerVendor.ps1 -VendorId Ruby
   ```

3. **Existing projects work immediately:**
   ```powershell
   cd my-ruby-project
   bundle install  # Uses Naner's Ruby
   ```

4. **Reinstall needed global gems:**
   ```powershell
   gem install rails pry rubocop
   ```

## Related Documentation

- [VS Code Settings](../home/.vscode/settings.json) - Ruby editor integration
- [Vendor Lock Files](VENDOR-LOCK-FILE.md) - Version control
- [Error Codes](ERROR-CODES.md) - Troubleshooting reference

## References

- [Ruby Official Documentation](https://www.ruby-lang.org/en/documentation/)
- [RubyGems Documentation](https://guides.rubygems.org/)
- [Bundler Documentation](https://bundler.io/)
- [Ruby on Rails Guides](https://guides.rubyonrails.org/)
- [Sinatra Documentation](http://sinatrarb.com/)

---

**Version:** 1.0
**Last Updated:** 2026-01-07
**Ruby Version Tested:** 3.2.2 (RubyInstaller2 with DevKit)
