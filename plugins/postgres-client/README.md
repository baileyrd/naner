# PostgreSQL Client Plugin

Portable PostgreSQL client tools for Naner.

## Description

This plugin provides portable PostgreSQL client tools including psql, pg_dump, pg_restore, and other database utilities. Perfect for database administration and development without installing a full PostgreSQL server.

## Features

- ✅ Portable PostgreSQL 16.x client tools
- ✅ psql interactive terminal
- ✅ pg_dump and pg_restore for backups
- ✅ Portable .pgpass credentials file
- ✅ Automatic PATH configuration
- ✅ No system installation required

## Installation

```powershell
# Install the plugin
Install-NanerPlugin -PluginPath "plugins/postgres-client"

# Enable the plugin
Enable-NanerPlugin -PluginId "postgres-client"
```

## Usage

After enabling, restart your terminal. PostgreSQL client commands will be available:

```bash
# Connect to a database
psql -h localhost -U myuser -d mydb

# Dump a database
pg_dump -h localhost -U myuser mydb > backup.sql

# Restore a database
pg_restore -h localhost -U myuser -d mydb backup.dump

# Check version
psql --version
```

## Environment Variables

This plugin sets the following environment variables:

- `PGDATA` - PostgreSQL data directory (for local clusters)
- `PGUSER` - Default PostgreSQL username
- `PGPASSFILE` - Password file location (~/.pgpass)
- `PATH` - Adds PostgreSQL bin directory to PATH

## Credentials Management

Store connection credentials in `home\.pgpass`:

```
# Format: hostname:port:database:username:password
localhost:5432:mydb:myuser:mypassword
localhost:5432:*:myuser:mypassword
```

**Security Note:** Set appropriate file permissions on `.pgpass`

## Common Commands

```bash
# List databases
psql -h localhost -U postgres -l

# Execute SQL file
psql -h localhost -U myuser -d mydb -f script.sql

# Interactive terminal
psql -h localhost -U myuser -d mydb

# CSV export
psql -h localhost -U myuser -d mydb -c "COPY (SELECT * FROM users) TO STDOUT CSV HEADER" > users.csv
```

## Configuration

Edit `plugin.json` to customize:

- `defaultHost` - Default PostgreSQL host (default: localhost)
- `defaultPort` - Default PostgreSQL port (default: 5432)

## Requirements

- Naner 1.0.0+
- ~150MB disk space

## License

MIT
