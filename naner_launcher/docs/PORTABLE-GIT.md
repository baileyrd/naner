# Portable Git Configuration Guide

Naner provides a fully portable Git configuration that travels with your installation, ensuring consistent Git identity, aliases, and settings across all machines.

## Overview

When you launch any shell in Naner, Git automatically uses your portable configuration:
- **Git Config**: `%NANER_ROOT%\home\.gitconfig`
- **Global Ignore**: `%NANER_ROOT%\home\.config\git\ignore`
- **Credentials**: `%NANER_ROOT%\home\.git-credentials` (gitignored for security)

This means your Git user identity, aliases, and preferences are completely portable across machines.

## Quick Start

### 1. Configure Your Identity

**REQUIRED:** Set your name and email before making commits.

```bash
# From any Naner shell (Bash, PowerShell, CMD)
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"
```

This updates `home/.gitconfig` automatically.

### 2. Verify Portable Configuration

```bash
# Check that Git is using portable config
git config --list --show-origin | grep NANER

# Should show paths like:
# file:C:/path/to/naner_launcher/home/.gitconfig

# Check your identity
git config user.name
git config user.email
```

### 3. Start Using Git

```bash
# All Git operations now use portable config
git clone git@github.com:username/repo.git
cd repo
git commit -m "My portable Git config works!"
```

## Directory Structure

```
home/
├── .gitconfig                   # Main Git configuration (tracked)
├── .git-credentials             # Stored credentials (gitignored)
└── .config/
    └── git/
        ├── ignore               # Global gitignore patterns (tracked)
        ├── credentials          # Alternate credential storage (gitignored)
        └── .gitkeep            # Preserves directory
```

## Configuration File

### Main Config: `home/.gitconfig`

The portable `.gitconfig` includes:

**User Identity:**
```ini
[user]
    name = Your Name
    email = your.email@example.com
```

**Core Settings:**
- Line ending handling (CRLF ↔ LF)
- Default editor (nano, code, vim)
- Long path support for Windows

**Default Behaviors:**
- Default branch: `main`
- Pull strategy: fast-forward or merge
- Auto-setup remote tracking
- Auto-prune deleted remote branches

**Useful Aliases:**
```bash
git s          # Short status
git l          # Pretty log graph
git amend      # Amend last commit
git undo       # Undo last commit (keep changes)
git branches   # List branches by date
git cleanup    # Delete merged branches
```

See the full list in [`home/.gitconfig`](../home/.gitconfig).

## Customizing Your Git Config

### Edit Directly

```bash
# Open in editor
nano ~/.gitconfig
# or
code ~/.gitconfig
# or
notepad ~/.gitconfig
```

### Use Git Commands

```bash
# Set values
git config --global user.name "New Name"
git config --global core.editor "code --wait"
git config --global init.defaultBranch "main"

# Add aliases
git config --global alias.st "status -sb"
git config --global alias.co "checkout"

# List all settings
git config --global --list
```

## Portable vs. Local Config

Git configuration has 3 levels:

1. **System** - Applies to all users (rarely used)
2. **Global** - User-specific (← **Naner uses this**)
3. **Local** - Repository-specific

### Naner's Approach

Naner sets `GIT_CONFIG_GLOBAL` to point to `home/.gitconfig`, making all `git config --global` commands portable.

**Local configs** (per-repository) are still stored in each repo's `.git/config` and are NOT portable.

### Per-Repository Overrides

You can still override settings per-repository:

```bash
cd my-work-repo
git config user.email "work@company.com"  # Local override

cd my-personal-repo
git config user.email "personal@gmail.com"  # Local override
```

These stay in each repo's `.git/config` and don't affect the portable config.

## Global Gitignore

### File: `home/.config/git/ignore`

Patterns here are ignored in **all repositories**.

**Current patterns include:**
- OS files (`.DS_Store`, `Thumbs.db`)
- Editor files (`.vscode`, `.idea`, `*.swp`)
- Build outputs (`node_modules/`, `dist/`, `bin/`)
- Logs and databases (`*.log`, `*.sqlite`)
- Environment files (`.env`, `*.key`)

### Adding Custom Patterns

```bash
# Edit global ignore
nano ~/.config/git/ignore

# Or use git command
git config --global core.excludesfile ~/.config/git/ignore
```

**Example additions:**
```gitignore
# My custom patterns
*.bak
*.backup
.scratch/
TODO.txt
```

## Credential Storage

### Option 1: Windows Credential Manager (Recommended)

**Pros:**
- Secure (Windows-encrypted storage)
- No files with passwords

**Cons:**
- NOT portable (credentials stay on the machine)

**Configuration (default in Naner):**
```ini
[credential]
    helper = manager-core
```

**Usage:**
```bash
# First time you push/pull, Git prompts for credentials
git push

# Credentials are stored in Windows Credential Manager
# Future operations use stored credentials automatically
```

### Option 2: Portable Credential Storage

**Pros:**
- Credentials travel with Naner
- Works on any machine

**Cons:**
- Less secure (plaintext file, gitignored)
- File-based storage

**Configuration:**
```bash
# Edit .gitconfig, change credential helper
git config --global credential.helper "store --file ~/.git-credentials"
```

**File format:** `~/.git-credentials`
```
https://username:token@github.com
https://username:token@gitlab.com
```

**⚠️ Security Warning:**
- `.git-credentials` is **plaintext**
- Use **Personal Access Tokens**, not passwords
- The file is gitignored but not encrypted
- Anyone with file access can read your tokens

### Option 3: No Credential Storage

Always type credentials manually:

```bash
git config --global --unset credential.helper
```

## Git Aliases

The portable config includes many useful aliases:

### Status & Inspection
```bash
git s              # Short status with branch
git l              # One-line log with graph
git lg             # Pretty colored log
git last           # Show last commit
git contributors   # List contributors
```

### Branching
```bash
git co <branch>    # Checkout branch
git cob <branch>   # Create and checkout new branch
git branches       # List branches by date
git current        # Show current branch name
git cleanup        # Delete merged branches
```

### Committing
```bash
git ci             # Commit
git cm "message"   # Commit with message
git ac             # Add all and commit (prompts for message)
git amend          # Amend last commit
git undo           # Undo last commit (keep changes)
```

### Advanced
```bash
git rb             # Interactive rebase
git pr             # Pull with rebase
git preview        # Show what would be pushed
git discard        # Discard all local changes
```

### Custom Aliases

Add your own:

```bash
# Quick push
git config --global alias.pushf "push --force-with-lease"

# Sync with remote
git config --global alias.sync "!git fetch && git rebase origin/main"

# Show changed files
git config --global alias.changed "diff --name-only"
```

## URL Shortcuts

The config includes shortcuts for common Git hosting services:

```bash
# Instead of typing full URLs
git clone git@github.com:username/repo.git

# Use shortcuts
git clone gh:username/repo
git clone gl:username/repo  # GitLab
git clone bb:username/repo  # Bitbucket
```

**Configuration:**
```ini
[url "git@github.com:"]
    insteadOf = gh:
[url "git@gitlab.com:"]
    insteadOf = gl:
[url "git@bitbucket.org:"]
    insteadOf = bb:
```

## Conditional Configuration

Use different configs for different directories:

### Work vs Personal

**Edit `.gitconfig`:**
```ini
[includeIf "gitdir:~/work/"]
    path = ~/.config/git/config-work

[includeIf "gitdir:~/personal/"]
    path = ~/.config/git/config-personal
```

**Create `~/.config/git/config-work`:**
```ini
[user]
    email = you@company.com

[core]
    sshCommand = ssh -i ~/.ssh/id_work_rsa
```

**Create `~/.config/git/config-personal`:**
```ini
[user]
    email = you@personal.com

[core]
    sshCommand = ssh -i ~/.ssh/id_personal_ed25519
```

Now Git automatically uses the right email and SSH key based on repository location!

## GPG Signing (Optional)

Sign commits with GPG keys for verification:

### Setup

```bash
# Generate GPG key (or import existing)
gpg --full-generate-key

# List keys
gpg --list-secret-keys --keyid-format LONG

# Get key ID (looks like: 3AA5C34371567BD2)
# Configure Git to use it
git config --global user.signingkey 3AA5C34371567BD2

# Sign commits by default
git config --global commit.gpgsign true

# Configure GPG program
git config --global gpg.program gpg
```

### Export Public Key

```bash
# Export for GitHub/GitLab
gpg --armor --export 3AA5C34371567BD2

# Copy the output and add to GitHub:
# Settings → SSH and GPG keys → New GPG key
```

## Git LFS (Large File Storage)

For repositories with large files (images, videos, datasets):

### Install Git LFS

```bash
# Git LFS should be included with Git in MSYS2
git lfs version

# Initialize LFS in repository
cd my-repo
git lfs install
```

### Track Large Files

```bash
# Track all PNG files
git lfs track "*.png"

# Track specific directory
git lfs track "assets/**"

# Commit .gitattributes
git add .gitattributes
git commit -m "Configure Git LFS"
```

The portable `.gitconfig` already includes LFS filter configuration.

## Integration with SSH

Git automatically uses your portable SSH keys from `home/.ssh/`:

```bash
# SSH keys from portable location work automatically
git clone git@github.com:username/repo.git

# Git uses: ~/.ssh/id_ed25519 (or configured key)
```

See [PORTABLE-SSH.md](PORTABLE-SSH.md) for SSH configuration.

## Troubleshooting

### Git Uses Wrong Config

**Check which config is loaded:**
```bash
git config --list --show-origin

# Should show: file:C:/path/to/naner_launcher/home/.gitconfig
```

**If using wrong config:**
```bash
# Verify environment variable
echo $GIT_CONFIG_GLOBAL   # Bash
$env:GIT_CONFIG_GLOBAL    # PowerShell

# Should be: C:\path\to\naner_launcher\home\.gitconfig
```

### Commits Showing Wrong Author

**Check user config:**
```bash
git config user.name
git config user.email

# If wrong, set correct values
git config --global user.name "Your Name"
git config --global user.email "your@email.com"
```

### Credential Helper Not Working

**Check credential helper:**
```bash
git config credential.helper

# Should be: manager-core  (or store --file ~/.git-credentials)
```

**Test credential storage:**
```bash
# This should prompt once, then remember
git clone https://github.com/private-repo/example.git
```

### Line Ending Issues (CRLF vs LF)

**Check current setting:**
```bash
git config core.autocrlf

# Should be: true (for Windows)
```

**If you see warnings about line endings:**
```bash
# Normalize repository
git add --renormalize .
git commit -m "Normalize line endings"
```

### GPG Signing Fails

**Check GPG configuration:**
```bash
gpg --version
git config user.signingkey
git config gpg.program

# Test GPG
echo "test" | gpg --clearsign
```

## Portability Considerations

### What's Portable ✅

- User identity (name, email)
- Git aliases
- Default behaviors (push, pull, merge strategies)
- Global gitignore patterns
- URL shortcuts
- GPG signing key (if you export/import it)
- Conditional includes (directory-based configs)

### What's NOT Portable ❌

- Windows Credential Manager credentials (machine-specific)
- GPG keys (unless manually exported/imported)
- Git LFS objects (stored in repo, but binary files are large)

### Making Credentials Portable

**Option 1: Use Personal Access Tokens**
```bash
# Use credential store
git config --global credential.helper "store --file ~/.git-credentials"

# First push prompts for token
git push
# Enter: username and personal access token

# Token is stored in ~/.git-credentials (gitignored)
```

**Option 2: Use SSH for Everything**
```bash
# Convert HTTPS remotes to SSH
git remote set-url origin git@github.com:username/repo.git

# Now uses SSH keys from ~/.ssh/ (portable)
```

## Best Practices

### 1. Use Personal Access Tokens

Never store actual passwords. Use PATs from GitHub/GitLab/Bitbucket.

**Generate PAT:**
- GitHub: Settings → Developer settings → Personal access tokens
- GitLab: Preferences → Access Tokens
- Bitbucket: Personal settings → App passwords

### 2. Separate Work and Personal

Use conditional includes to avoid mixing work/personal email:

```ini
[includeIf "gitdir:~/work/"]
    path = ~/.config/git/config-work
```

### 3. Sign Important Commits

Use GPG signing for release tags and critical commits:

```bash
git commit -S -m "Important commit"
git tag -s v1.0.0 -m "Release 1.0.0"
```

### 4. Review Before Committing

Use aliases to check before pushing:

```bash
git s          # Check status
git preview    # See what will be pushed
git push
```

### 5. Keep Config in Sync

When moving to a new machine:
1. Copy entire `naner_launcher` folder
2. Verify: `git config --list`
3. Test: `git commit --allow-empty -m "Test"`

## Testing Your Setup

Run these commands to verify portable Git configuration:

```bash
# 1. Check config location
git config --list --show-origin | head -5

# 2. Check identity
git config user.name
git config user.email

# 3. Test alias
git s

# 4. Check global ignore
git config core.excludesfile

# 5. Test credential helper
git config credential.helper

# 6. Make test commit (empty)
git init /tmp/test-repo
cd /tmp/test-repo
git commit --allow-empty -m "Test portable config"
# Should show your portable name/email
```

## Related Documentation

- [PORTABLE-SSH.md](PORTABLE-SSH.md) - Portable SSH keys and config
- [PORTABLE-POWERSHELL.md](PORTABLE-POWERSHELL.md) - PowerShell portability
- [home/.gitconfig](../home/.gitconfig) - Full Git configuration file

## Resources

- [Git Documentation](https://git-scm.com/doc) - Official Git docs
- [GitHub Git Guides](https://github.com/git-guides) - Git tutorials
- [Pro Git Book](https://git-scm.com/book/en/v2) - Free comprehensive guide
- [Git Aliases](https://www.git-scm.com/book/en/v2/Git-Basics-Git-Aliases) - Creating custom aliases
