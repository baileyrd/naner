# Portable Gem Home

This directory contains your portable Ruby gems and configuration for the Naner environment.

## Directory Structure

```
.gem/
├── bin/              # Gem executables (gem install creates scripts here)
├── cache/            # Downloaded gem files (.gem archives)
├── doc/              # RDoc and YARD documentation
├── extensions/       # Native extensions (compiled C code)
└── specifications/   # Gem metadata (.gemspec files)
```

## Usage

### Installing Gems

```bash
# Install a gem
gem install rails

# Install specific version
gem install nokogiri -v 1.15.0

# Install from local file
gem install path/to/gem-file.gem

# Install without documentation (faster)
gem install sinatra --no-document

# Install with dependencies
gem install bundler
```

### Managing Gems

```bash
# List installed gems
gem list

# Search for gems
gem search rails

# Show gem information
gem info rails

# Update gem
gem update rails

# Update all gems
gem update

# Uninstall gem
gem uninstall nokogiri

# Clean up old versions
gem cleanup
```

### Bundler (Recommended for Projects)

Bundler manages project dependencies via a `Gemfile`:

```bash
# Install bundler (already installed by Naner setup)
gem install bundler

# Create a new project with Gemfile
bundle init

# Install gems from Gemfile
bundle install

# Update gems in Gemfile.lock
bundle update

# Execute command with bundled gems
bundle exec ruby script.rb
bundle exec rails server

# Check for outdated gems
bundle outdated
```

### Creating a Gemfile

```ruby
# Gemfile
source 'https://rubygems.org'

ruby '3.2.3'

# Web framework
gem 'sinatra', '~> 3.1'

# Database
gem 'sqlite3', '~> 1.6'

# Utilities
gem 'rake', '~> 13.0'

group :development do
  gem 'pry', '~> 0.14'
  gem 'rubocop', '~> 1.50'
end

group :test do
  gem 'rspec', '~> 3.12'
  gem 'minitest', '~> 5.18'
end
```

## Popular Ruby Gems

### Web Frameworks

```bash
# Rails - Full-stack framework
gem install rails

# Sinatra - Lightweight framework
gem install sinatra

# Roda - Fast and simple routing tree framework
gem install roda

# Hanami - Modern web framework
gem install hanami
```

### Database & ORM

```bash
# ActiveRecord - Rails ORM (can be used standalone)
gem install activerecord

# Sequel - Flexible database toolkit
gem install sequel

# ROM - Ruby Object Mapper
gem install rom

# Database drivers
gem install sqlite3        # SQLite
gem install pg             # PostgreSQL
gem install mysql2         # MySQL
```

### Testing

```bash
# RSpec - BDD testing framework
gem install rspec

# Minitest - Ruby's built-in testing (gem enhances it)
gem install minitest

# Cucumber - BDD with natural language
gem install cucumber

# Factory Bot - Test data factories
gem install factory_bot

# Faker - Fake data generator
gem install faker
```

### CLI Tools

```bash
# Thor - CLI builder used by Rails
gem install thor

# TTY - Toolkit for terminal apps
gem install tty

# OptionParser - Command-line parsing (built-in, but gem adds features)
gem install optparse

# Commander - Command-line interface builder
gem install commander
```

### HTTP Clients

```bash
# HTTParty - Simple HTTP requests
gem install httparty

# Faraday - HTTP client abstraction
gem install faraday

# RestClient - Simple REST API client
gem install rest-client

# HTTP.rb - Chainable HTTP client
gem install http
```

### JSON & Serialization

```bash
# JSON - Fast JSON parsing (built-in, but gem may be newer)
gem install json

# Oj - Optimized JSON
gem install oj

# YAML - YAML parsing (built-in)
gem install psych

# MultiJSON - JSON abstraction
gem install multi_json
```

### Background Jobs

```bash
# Sidekiq - Background processing with Redis
gem install sidekiq

# Resque - Redis-backed job queue
gem install resque

# Delayed Job - Database-backed jobs
gem install delayed_job

# Que - PostgreSQL-backed job queue
gem install que
```

### Code Quality

```bash
# RuboCop - Linter and formatter
gem install rubocop

# Reek - Code smell detector
gem install reek

# Brakeman - Security scanner for Rails
gem install brakeman

# SimpleCov - Code coverage
gem install simplecov
```

### Utilities

```bash
# Pry - Enhanced IRB console
gem install pry

# Rake - Build automation (like Make)
gem install rake

# Dotenv - Load environment variables from .env
gem install dotenv

# Chronic - Natural language date parser
gem install chronic

# Colorize - String colorization
gem install colorize

# Progress Bar - Terminal progress bars
gem install ruby-progressbar
```

### Template Engines

```bash
# ERB - Embedded Ruby (built-in)
# (No gem needed)

# Haml - Beautiful markup
gem install haml

# Slim - Fast template language
gem install slim

# Liquid - Safe template language
gem install liquid
```

## Configuration

### .gemrc File

The `.gemrc` file (in `home/` directory) configures gem behavior:

```yaml
# Portable gem configuration for Naner
gem: --no-document
install: --env-shebang
update: --env-shebang
```

Common `.gemrc` options:

```yaml
# Don't install documentation (faster installs)
gem: --no-document

# Use system shebang for scripts
install: --env-shebang
update: --env-shebang

# Set default gem sources
:sources:
  - https://rubygems.org/

# Verbose output
:verbose: true

# Concurrent downloads
:concurrent_downloads: 8
```

## Common Commands

```bash
# Gem Management
gem install <gem>          # Install gem
gem uninstall <gem>        # Remove gem
gem update <gem>           # Update specific gem
gem update --system        # Update RubyGems itself
gem list                   # List installed gems
gem search <query>         # Search rubygems.org
gem info <gem>             # Show gem details
gem cleanup                # Remove old versions

# Building & Publishing
gem build <gemspec>        # Build gem from .gemspec
gem push <gem-file>        # Publish to rubygems.org
gem yank <gem> -v <ver>    # Remove published version

# Environment
gem env                    # Show gem environment
gem which <gem>            # Show path to gem
gem contents <gem>         # List gem files

# Documentation
gem server                 # Start local documentation server
gem rdoc <gem>             # Generate RDoc for gem

# Bundler (Project Dependencies)
bundle install             # Install gems from Gemfile
bundle update              # Update gems
bundle exec <cmd>          # Run command with bundled gems
bundle show <gem>          # Show path to bundled gem
bundle outdated            # Check for outdated gems
bundle clean               # Remove unused gems
```

## Environment Variables

Naner automatically sets these Ruby environment variables:

- **GEM_HOME**: `%NANER_ROOT%\home\.gem` - Where gems are installed
- **GEM_PATH**: `%NANER_ROOT%\home\.gem` - Where gems are loaded from

### Verify Environment

```bash
# Check Ruby version
ruby --version

# Check gem version
gem --version

# Show gem environment
gem env

# Check bundler version
bundle --version
```

## Creating a Ruby Project

### Simple Script

```ruby
#!/usr/bin/env ruby
# script.rb

require 'json'
require 'net/http'

url = URI('https://api.github.com/repos/ruby/ruby')
response = Net::HTTP.get(url)
data = JSON.parse(response)

puts "Ruby repository stars: #{data['stargazers_count']}"
```

Run with:
```bash
ruby script.rb
```

### Gem Project

```bash
# Create gem scaffold
bundle gem my_gem

# Or manually create structure:
my_gem/
├── lib/
│   └── my_gem.rb
├── spec/
│   └── my_gem_spec.rb
├── Gemfile
├── my_gem.gemspec
├── README.md
└── Rakefile
```

### Sinatra Web App

```bash
# Install Sinatra
gem install sinatra

# Create app.rb
cat > app.rb << 'EOF'
require 'sinatra'

get '/' do
  'Hello from Naner!'
end

get '/about' do
  erb :about
end
EOF

# Run server
ruby app.rb

# Or with auto-reload
gem install sinatra-contrib
ruby -r sinatra/reloader app.rb
```

### Rails Application

```bash
# Install Rails
gem install rails

# Create new Rails app
rails new myapp

# Or API-only
rails new myapi --api

# Start server
cd myapp
rails server

# Generate scaffold
rails generate scaffold Post title:string body:text

# Run migrations
rails db:migrate
```

## IDE Integration

### VS Code

Install Ruby extensions:
1. "Ruby" by Shopify (language support)
2. "Ruby Solargraph" (IntelliSense and linting)
3. "Ruby Test Explorer" (test integration)

Install Solargraph:
```bash
gem install solargraph
```

### RubyMine

1. Open project folder
2. IDE auto-detects Ruby SDK from Naner
3. Configure interpreter: File → Settings → Languages → Ruby
4. Point to: `%NANER_ROOT%\vendor\ruby\bin\ruby.exe`

### Sublime Text

Install Package Control, then:
1. Install "Ruby" package
2. Install "RuboCop" package
3. Configure Ruby path in settings

## Testing

### RSpec

```bash
# Install RSpec
gem install rspec

# Initialize RSpec in project
rspec --init

# Run tests
rspec

# Run specific file
rspec spec/my_spec.rb

# Run with documentation format
rspec --format documentation
```

### Minitest

```bash
# Install Minitest
gem install minitest

# Create test file
cat > test_sample.rb << 'EOF'
require 'minitest/autorun'

class TestSample < Minitest::Test
  def test_addition
    assert_equal 4, 2 + 2
  end
end
EOF

# Run tests
ruby test_sample.rb
```

## Ruby Interactive Console

### IRB (Built-in)

```bash
# Start IRB
irb

# Load a file in IRB
irb -r ./lib/my_file.rb

# Run IRB with gems loaded
bundle exec irb
```

### Pry (Enhanced Console)

```bash
# Install Pry
gem install pry

# Start Pry
pry

# Use Pry in code for debugging
require 'pry'
binding.pry  # Execution stops here
```

## Troubleshooting

### Gem Command Not Found

```bash
# Verify gem is available
where gem

# Check Ruby installation
ruby --version

# Reinstall bundler
gem install bundler
```

### Permission Errors

Since Naner uses portable GEM_HOME, you shouldn't encounter permission errors. If you do:

```bash
# Verify GEM_HOME is set
echo $GEM_HOME

# Should show: C:\path\to\naner\home\.gem
```

### Native Extension Compilation Errors

Ruby gems with C extensions need a compiler. Naner includes MSYS2 with MinGW:

```bash
# Ensure MSYS2 is in PATH (Naner does this automatically)
# Install gem with native extensions
gem install nokogiri

# If issues persist, install DevKit gems
ridk install
```

### Slow Gem Installation

```bash
# Skip documentation for faster installs
gem install rails --no-document

# Or set permanently in ~/.gemrc
echo "gem: --no-document" >> ~/.gemrc

# Use concurrent downloads
echo ":concurrent_downloads: 8" >> ~/.gemrc
```

### Bundler Issues

```bash
# Update bundler
gem update bundler

# Clean bundler cache
bundle clean --force

# Reinstall all gems
rm Gemfile.lock
bundle install
```

### Version Conflicts

```bash
# List all versions of a gem
gem list nokogiri --all

# Uninstall specific version
gem uninstall nokogiri -v 1.14.0

# Or uninstall all versions
gem uninstall nokogiri --all
```

## Resources

- **Official Docs**: https://www.ruby-lang.org/en/documentation/
- **Ruby Guides**: https://www.rubyguides.com/
- **RubyGems**: https://rubygems.org/ (Gem repository)
- **Bundler Docs**: https://bundler.io/docs.html
- **Awesome Ruby**: https://github.com/markets/awesome-ruby
- **Ruby Style Guide**: https://rubystyle.guide/
- **Rails Guides**: https://guides.rubyonrails.org/

## Portability

All gem data is portable with Naner:
- ✅ Installed gems and their files
- ✅ Gem executables (bin/)
- ✅ Downloaded gem files (cache/)
- ✅ Native extensions (compiled code)
- ✅ Gem configuration (.gemrc)

When you move Naner to a new machine, your entire Ruby environment travels with it. Native extensions may need recompilation on different architectures.
