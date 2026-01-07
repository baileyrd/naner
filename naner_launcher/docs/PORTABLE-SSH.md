# Portable SSH Configuration Guide

Naner supports a fully portable SSH setup, allowing you to carry your SSH keys, config, and known hosts with your Naner installation.

## Overview

When Naner launches, it automatically sets:
- `HOME=%NANER_ROOT%\home` - Makes all tools use the portable home directory
- `SSH_HOME=%NANER_ROOT%\home\.ssh` - Explicit SSH directory location

This means SSH, Git, and other tools will use `naner_launcher/home/.ssh/` instead of your Windows profile directory (`%USERPROFILE%\.ssh`).

## Quick Setup

### Option 1: Generate New Keys (Recommended for New Setup)

1. **Launch Naner** (either profile: Unified, Bash, or PowerShell)

2. **Generate SSH key:**
   ```bash
   # ED25519 (modern, recommended)
   ssh-keygen -t ed25519 -C "your_email@example.com"

   # When prompted for file location, press Enter (uses default: ~/.ssh/id_ed25519)
   # Set a strong passphrase when prompted
   ```

3. **Add public key to your services:**
   ```bash
   # Display your public key
   cat ~/.ssh/id_ed25519.pub

   # Or copy to clipboard (PowerShell)
   Get-Content ~/.ssh/id_ed25519.pub | Set-Clipboard
   ```

4. **Add to GitHub/GitLab/Bitbucket:**
   - GitHub: Settings → SSH and GPG keys → New SSH key
   - GitLab: Preferences → SSH Keys
   - Bitbucket: Personal settings → SSH keys

5. **Test connection:**
   ```bash
   ssh -T git@github.com
   ```

### Option 2: Copy Existing Keys

If you have existing SSH keys in your Windows profile:

**From PowerShell (outside Naner):**
```powershell
# Copy entire .ssh directory
Copy-Item "$env:USERPROFILE\.ssh\*" "C:\path\to\naner_launcher\home\.ssh\" -Recurse

# Or copy specific files
Copy-Item "$env:USERPROFILE\.ssh\id_*" "C:\path\to\naner_launcher\home\.ssh\"
Copy-Item "$env:USERPROFILE\.ssh\config" "C:\path\to\naner_launcher\home\.ssh\"
```

**Set correct permissions (from Naner Bash):**
```bash
chmod 700 ~/.ssh
chmod 600 ~/.ssh/id_*
chmod 644 ~/.ssh/*.pub
```

## SSH Config File

Create `home/.ssh/config` for custom SSH settings. See `home/.ssh/config.example` for examples.

### Example Config

```ssh-config
# GitHub with specific key
Host github.com
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_ed25519

# Work server
Host work
    HostName work.example.com
    User myusername
    Port 2222
    IdentityFile ~/.ssh/id_work_rsa
```

## Directory Structure

```
naner_launcher/
└── home/
    └── .ssh/
        ├── .gitkeep              # Preserves directory in git
        ├── README.md             # Detailed documentation
        ├── config.example        # Example SSH config
        ├── config                # Your SSH config (optional)
        ├── id_ed25519            # Private key (gitignored)
        ├── id_ed25519.pub        # Public key (tracked in git)
        └── known_hosts           # Known hosts (gitignored)
```

## Security

### What's Protected (gitignored)

These files are **automatically excluded** from git:
- Private keys: `id_rsa`, `id_ed25519`, etc.
- `known_hosts` files
- `authorized_keys`

### What's Tracked (safe to commit)

- Public keys: `*.pub` files
- `config` file (review for sensitive info!)
- Documentation: `README.md`, `.gitkeep`

### Best Practices

1. ✅ **Use strong passphrases** on private keys
2. ✅ **Review `config` file** before committing (may contain server names)
3. ✅ **Backup keys separately** from this repository
4. ✅ **Use different keys** for different purposes
5. ❌ **Never commit private keys** (already gitignored)
6. ❌ **Don't share your naner installation** with private keys included

## Using SSH with Git

Once your SSH keys are set up, Git will automatically use them:

```bash
# Clone repository
git clone git@github.com:username/repo.git

# Change existing repo to use SSH
cd existing-repo
git remote set-url origin git@github.com:username/repo.git

# Verify
git remote -v
```

## Troubleshooting

### Test SSH Connection

```bash
# Verbose mode for debugging
ssh -vvv git@github.com

# Check which key is being used
ssh -vT git@github.com 2>&1 | grep identity
```

### Permission Errors

**"Bad owner or permissions"**

From Naner Bash:
```bash
# Reset all permissions
chmod 700 ~/.ssh
chmod 600 ~/.ssh/id_*
chmod 600 ~/.ssh/config
chmod 644 ~/.ssh/*.pub
```

### SSH Agent

Start SSH agent and add your key:

```bash
# Start agent
eval $(ssh-agent -s)

# Add key
ssh-add ~/.ssh/id_ed25519

# List loaded keys
ssh-add -l
```

### Verify HOME Variable

From within Naner:

**PowerShell:**
```powershell
$env:HOME
# Should output: C:\path\to\naner_launcher\home
```

**Bash:**
```bash
echo $HOME
# Should output: /c/path/to/naner_launcher/home
```

### Git Not Using SSH Key

Ensure Git is using SSH (not HTTPS):

```bash
# Check remote URL
git remote -v

# Should be: git@github.com:user/repo.git
# Not: https://github.com/user/repo.git

# Fix if needed
git remote set-url origin git@github.com:user/repo.git
```

## Portability

### Moving to Another Machine

1. Copy entire `naner_launcher` folder to new machine
2. Launch Naner
3. Set file permissions (from Bash):
   ```bash
   chmod 700 ~/.ssh
   chmod 600 ~/.ssh/id_*
   ```
4. Test SSH connection:
   ```bash
   ssh -T git@github.com
   ```

### USB Drive / Cloud Sync

You can run Naner from:
- ✅ USB drive
- ✅ Synced folder (OneDrive, Dropbox, etc.)
- ✅ Network share

**Important:** Ensure private keys remain secure when using cloud sync. Consider:
- Encrypting the `.ssh` directory
- Using cloud provider's encryption
- Excluding `.ssh` from sync and copying manually

## Advanced: Multiple Identities

Use different SSH keys for different services:

**config:**
```ssh-config
# Personal GitHub
Host github-personal
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_personal_ed25519

# Work GitHub
Host github-work
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_work_ed25519
```

**Usage:**
```bash
# Clone with personal key
git clone git@github-personal:personal-account/repo.git

# Clone with work key
git clone git@github-work:work-account/repo.git
```

## Related Documentation

- [home/.ssh/README.md](../home/.ssh/README.md) - Detailed SSH directory documentation
- [config/naner.json](../config/naner.json) - Environment variable configuration
- [GitHub SSH Documentation](https://docs.github.com/en/authentication/connecting-to-github-with-ssh)

## Summary

Naner's portable SSH setup provides:
- ✅ **Complete portability** - Move your SSH config between machines
- ✅ **Automatic configuration** - HOME variable set automatically
- ✅ **Security** - Private keys gitignored by default
- ✅ **Flexibility** - Use multiple keys, custom configs
- ✅ **Compatibility** - Works with Git, SSH, and all standard tools
