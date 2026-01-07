# Portable SSH Configuration

This directory contains your portable SSH configuration for Naner. When you launch Naner, the `HOME` environment variable is automatically set to `%NANER_ROOT%\home`, making SSH tools use this directory instead of your Windows user profile.

## Directory Structure

```
home/
└── .ssh/
    ├── config           # SSH client configuration
    ├── known_hosts      # Trusted host keys (auto-generated)
    ├── id_rsa           # Private key (gitignored for security)
    ├── id_rsa.pub       # Public key
    ├── id_ed25519       # Private key (gitignored for security)
    └── id_ed25519.pub   # Public key
```

## Setup Instructions

### 1. Generate SSH Keys (if needed)

From within a Naner terminal session:

```bash
# Generate ED25519 key (recommended)
ssh-keygen -t ed25519 -C "your_email@example.com" -f ~/.ssh/id_ed25519

# Or generate RSA key (legacy compatibility)
ssh-keygen -t rsa -b 4096 -C "your_email@example.com" -f ~/.ssh/id_rsa
```

The keys will be created in this portable `.ssh` directory.

### 2. Copy Existing Keys (optional)

If you have existing SSH keys, copy them here:

```powershell
# From PowerShell (outside Naner)
Copy-Item "$env:USERPROFILE\.ssh\id_*" "path\to\naner_launcher\home\.ssh\"
Copy-Item "$env:USERPROFILE\.ssh\config" "path\to\naner_launcher\home\.ssh\"
```

### 3. Create SSH Config (optional)

Create a `config` file for custom SSH settings:

```ssh-config
# Example SSH config
Host github.com
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_ed25519

Host myserver
    HostName example.com
    User myusername
    Port 2222
    IdentityFile ~/.ssh/id_rsa
```

### 4. Set Correct Permissions

**IMPORTANT:** SSH requires specific file permissions. In MSYS2/Git Bash:

```bash
# Set directory permissions
chmod 700 ~/.ssh

# Set private key permissions
chmod 600 ~/.ssh/id_*
chmod 600 ~/.ssh/config

# Public keys can be more permissive
chmod 644 ~/.ssh/*.pub
```

## Security Notes

### Protected Files

The following files are automatically **excluded from git** via `.gitignore`:

- `id_rsa` (private key)
- `id_ed25519` (private key)
- `id_dsa` (private key)
- `known_hosts` (contains server fingerprints)
- Any file without an extension in `.ssh/`

### What Gets Committed

Only these files will be tracked in git:
- `.gitkeep` (to preserve directory structure)
- `README.md` (this file)
- `config` (SSH client configuration - review before committing!)
- `*.pub` files (public keys - safe to share)

### Best Practices

1. **Never commit private keys** - They're gitignored by default
2. **Review `config` file** before committing (may contain hostnames/usernames)
3. **Use strong passphrases** on private keys
4. **Backup your keys** separately from this repository
5. **Use different keys** for different purposes (work vs personal)

## Testing Your Setup

### Verify SSH Agent

```bash
# Start SSH agent (if not running)
eval $(ssh-agent -s)

# Add your key
ssh-add ~/.ssh/id_ed25519

# List loaded keys
ssh-add -l
```

### Test GitHub Connection

```bash
ssh -T git@github.com
# Expected: "Hi username! You've successfully authenticated..."
```

### Test Custom Host

```bash
ssh -v myserver
# Use -v for verbose output to debug connection issues
```

## Troubleshooting

### "Permission denied (publickey)"

1. Check key permissions: `ls -la ~/.ssh`
2. Ensure key is loaded: `ssh-add -l`
3. Verify public key is added to remote server
4. Check SSH config: `ssh -v user@host`

### "Bad owner or permissions"

SSH on Windows can be picky about permissions. In Git Bash:

```bash
# Reset permissions
chmod 700 ~/.ssh
chmod 600 ~/.ssh/id_*
chmod 644 ~/.ssh/*.pub
```

### "Could not resolve hostname"

Check your `config` file for typos in `HostName` entries.

## Environment Variables Set by Naner

When Naner launches, these variables are automatically set:

- `HOME=%NANER_ROOT%\home` - Makes SSH use this directory
- `SSH_HOME=%NANER_ROOT%\home\.ssh` - Explicit SSH directory
- `GIT_SSH_COMMAND=ssh` - Ensures Git uses SSH from PATH

## Using with Git

Git will automatically use the SSH keys from this directory:

```bash
# Clone with SSH
git clone git@github.com:username/repo.git

# Set remote to use SSH
git remote set-url origin git@github.com:username/repo.git
```

## Portable Across Machines

This setup is fully portable. You can:

1. Copy the entire `naner_launcher` folder to another machine
2. Your SSH config and keys move with it
3. No need to reconfigure SSH on the new machine

**Note:** Remember to set correct file permissions after copying to a new machine.

## Additional Resources

- [GitHub SSH Documentation](https://docs.github.com/en/authentication/connecting-to-github-with-ssh)
- [SSH Config File Examples](https://www.ssh.com/academy/ssh/config)
- [SSH Key Types Comparison](https://security.stackexchange.com/questions/5096/rsa-vs-dsa-for-ssh-authentication-keys)
