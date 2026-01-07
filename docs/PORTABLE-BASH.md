# Portable Bash Configuration Guide

Naner provides portable Bash configuration files that ensure consistent shell experience across machines when using Git Bash or MSYS2.

## Overview

When you launch Bash in Naner, the following portable configurations are automatically loaded:
- **Bash RC**: `%NANER_ROOT%\home\.bashrc` - Interactive shell configuration
- **Bash Profile**: `%NANER_ROOT%\home\.bash_profile` - Login shell configuration
- **Bash Aliases**: `%NANER_ROOT%\home\.bash_aliases` - Additional aliases
- **Bash History**: `%NANER_ROOT%\home\.bash_history` - Command history (gitignored)

This means your Bash prompt, aliases, functions, and preferences travel with your Naner installation.

## Quick Start

### 1. Launch Bash in Naner

```cmd
naner.bat
# Then select Bash profile, or:
```

```powershell
.\src\powershell\Invoke-Naner.ps1 -Profile Bash
```

### 2. Verify Portable Configuration

```bash
# Check that HOME points to portable location
echo $HOME
# Should show: C:\path\to\naner_launcher\home

# Verify .bashrc was loaded
naner-info
```

### 3. Use Built-in Aliases and Functions

```bash
# Git shortcuts
gs              # git status
ga .            # git add .
gc -m "msg"     # git commit
gp              # git push

# Navigation
..              # cd ..
...             # cd ../..
ll              # ls -alF

# Utilities
naner-info      # Show Naner environment
naner-aliases   # List all aliases
mkcd mydir      # Create and cd into directory
extract file.tar.gz  # Auto-extract archives
```

## Directory Structure

```
home/
├── .bashrc              # Interactive shell config (tracked)
├── .bash_profile        # Login shell config (tracked)
├── .bash_aliases        # Additional aliases (tracked)
└── .bash_history        # Command history (gitignored)
```

## Configuration Files

### .bashrc - Interactive Shell Configuration

Loaded for every interactive Bash session. Contains:

**Features:**
- Naner branding and welcome message
- Custom prompt with Git branch display
- Shell options (history, completion, globbing)
- Environment variables (EDITOR, LESS)
- Common aliases (ls, cd, grep)
- Git shortcuts matching PowerShell profile
- Docker shortcuts
- Utility functions

**Custom Prompt:**
```bash
[Naner] directory_name (git:branch) >
```

The prompt shows:
- Current directory (basename only)
- Git branch (if in a Git repository)
- Git status indicator (* if uncommitted changes)

### .bash_profile - Login Shell Configuration

Loaded for login shells. Contains:
- Sources .bashrc for interactive features
- PATH additions for ~/bin and ~/.local/bin
- Optional SSH agent initialization (commented out)
- Login-specific customizations

### .bash_aliases - Extended Aliases

Additional aliases loaded by .bashrc. Organized by category:
- System (navigation, info, processes)
- Development (npm, python, serve)
- Git advanced (gac, gcm, gclean, gcontrib)
- Docker advanced (drm, drmi, dc shortcuts)
- File operations (largest, count, duhh)
- Network (ports, listening, speedtest)
- Text processing (wcl, srt, tf)
- Archive operations (targz, untargz, mkzip)
- Utilities (now, calc, genpass)

## Built-in Aliases

### Git Shortcuts

```bash
gs              # git status -sb
ga              # git add
gc              # git commit
gp              # git push
gl              # git pull
gd              # git diff
gco             # git checkout
gb              # git branch
glog            # git log --oneline --graph
glg             # git log (pretty format with colors)

# From .bash_aliases:
gac             # git add -A && git commit
gcm             # git commit -m
gca             # git commit --amend --no-edit
gundo           # git reset HEAD~1
greset          # git reset --hard HEAD
gbr             # git branch -avv
gclean          # delete merged branches
gcontrib        # git shortlog -sn
```

### Docker Shortcuts

```bash
dps             # docker ps
dpa             # docker ps -a
di              # docker images
dex             # docker exec -it
dlog            # docker logs -f
dstop           # docker stop $(docker ps -q)

# From .bash_aliases:
drm             # remove all stopped containers
drmi            # remove dangling images
dstopall        # stop all running containers
dc              # docker-compose
dcu             # docker-compose up
dcd             # docker-compose down
```

### File Operations

```bash
ll              # ls -alF
la              # ls -A
..              # cd ..
...             # cd ../..
....            # cd ../../..
~               # cd ~

# From .bash_aliases:
lh              # ls -lh (human-readable)
lt              # ls -lt (sort by time)
lsize           # ls -lhS (sort by size)
largest         # find largest files
duhh            # du -h sorted by size
count           # count files in directory
```

## Built-in Functions

### Naner Utilities

```bash
naner-info         # Show Naner environment information
naner-aliases      # List all Naner aliases
naner-setup        # Run Setup-NanerVendor.ps1
naner-test         # Run Test-NanerInstallation.ps1
```

### File Utilities

```bash
mkcd dirname       # Create directory and cd into it
extract file.tar.gz # Auto-detect and extract archive
ff pattern         # Find files by name
fd pattern         # Find directories by name
search "text"      # Search file contents (grep -rnw)
```

**Example:**
```bash
# Create project and navigate
mkcd my-project

# Extract downloaded archive
extract ~/Downloads/package.tar.gz

# Find all JavaScript files
ff "*.js"

# Search for TODO comments
search "TODO"
```

## Customization

### Adding Custom Aliases

**Option 1: Edit .bash_aliases**

```bash
nano ~/.bash_aliases

# Add your aliases
alias myproject='cd ~/projects/myproject'
alias serve8080='python -m http.server 8080'
```

**Option 2: Edit .bashrc directly**

```bash
nano ~/.bashrc

# Scroll to "User Customizations" section
# Add custom aliases there
```

### Adding Custom Functions

```bash
nano ~/.bashrc

# Add to User Customizations section
my_deploy() {
    echo "Deploying to production..."
    git push origin main
    ssh user@server 'cd /app && git pull && systemctl restart app'
}
```

### Customizing the Prompt

The prompt is defined in .bashrc around line 85. Customize PS1 variable:

```bash
# Simple prompt
PS1="\u@\h:\w\$ "

# Colorful prompt
PS1="\[\033[01;32m\]\u@\h\[\033[00m\]:\[\033[01;34m\]\w\[\033[00m\]\$ "

# Or edit the existing Naner prompt
# Change colors, add/remove elements, etc.
```

### Changing Default Editor

```bash
# Edit .bashrc
export EDITOR="vim"    # or "code", "emacs", etc.
export VISUAL="vim"
```

## Git Integration

Bash configuration works seamlessly with portable Git config:

```bash
# Git uses portable config from home/.gitconfig
git config user.name

# Uses portable SSH keys from home/.ssh/
git clone git@github.com:username/repo.git

# Git aliases from both .gitconfig and .bashrc work
git s      # From .gitconfig
gs         # From .bashrc (same as git s)
```

## Shell Options

The .bashrc enables several useful Bash features:

```bash
shopt -s histappend      # Append to history, don't overwrite
shopt -s checkwinsize    # Update window size after each command
shopt -s nocaseglob      # Case-insensitive globbing
shopt -s cdspell         # Autocorrect typos in cd
shopt -s extglob         # Extended pattern matching
shopt -s globstar        # Recursive globbing with **
```

**Example usage:**
```bash
# Case-insensitive glob
ls *.PDF              # Matches *.pdf, *.PDF, *.Pdf

# Recursive glob
ls **/*.js            # All .js files in subdirectories
```

## History Configuration

```bash
HISTSIZE=10000         # Commands in memory
HISTFILESIZE=20000     # Commands in file
HISTCONTROL=ignoreboth:erasedups  # No duplicates
HISTIGNORE="ls:cd:exit"  # Don't save these
HISTTIMEFORMAT="%F %T "  # Add timestamp
```

**Search history:**
```bash
history | grep git    # Search history
!123                  # Run command #123
!!                    # Run last command
!$                    # Last argument of previous command
```

## Bash vs PowerShell

Naner provides consistent experience across both:

| Feature | Bash | PowerShell |
|---------|------|------------|
| Git status | `gs` | `gs` |
| Git commit | `gc -m "msg"` | `gc -m "msg"` |
| Docker ps | `dps` | `dps` |
| Naner info | `naner-info` | `Get-NanerInfo` |
| Prompt | Cyan with Git branch | Cyan with Git branch |

Use whichever shell you prefer - aliases are consistent!

## Completion

Bash completion is enabled for:
- Git commands and branches
- File and directory names
- Command names
- Variable names

**Usage:**
```bash
git che<TAB>          # Completes to "git checkout"
git checkout ma<TAB>  # Completes to branch name
cd ~/Doc<TAB>         # Completes to ~/Documents
```

## Troubleshooting

### .bashrc Not Loading

**Check if file exists:**
```bash
ls -la ~/.bashrc
```

**Manually source it:**
```bash
source ~/.bashrc
```

**Check for syntax errors:**
```bash
bash -n ~/.bashrc
```

### Aliases Not Working

**Check if alias is defined:**
```bash
alias gs
# Should show: alias gs='git status -sb'
```

**Reload configuration:**
```bash
source ~/.bashrc
```

### Prompt Not Showing Git Branch

**Verify you're in a Git repository:**
```bash
git status
```

**Check git_branch function:**
```bash
type git_branch
```

### Colors Not Showing

**Check terminal supports colors:**
```bash
tput colors
# Should show: 256 or higher
```

**Verify color codes:**
```bash
echo -e "\033[0;36mCyan text\033[0m"
```

## Platform-Specific Features

### Windows (MSYS2/Git Bash)

Special aliases for Windows:

```bash
explorer           # Open Windows Explorer in current dir
open file.txt      # Open file with default program
wt                 # Launch Windows Terminal
clip               # Copy to clipboard
```

### Integration with Windows

```bash
# Open current directory in Explorer
explorer .

# Open file with default program
start myfile.pdf

# Copy command output to clipboard
ls | clip
```

## Best Practices

### 1. Keep Core Config Unchanged

Edit the "User Customizations" section instead of modifying core aliases.

### 2. Use Functions for Complex Logic

Instead of complex aliases, create functions:

```bash
# Good - function
deploy() {
    git push && ssh server "cd /app && git pull"
}

# Avoid - complex alias
alias deploy='git push && ssh server "cd /app && git pull"'
```

### 3. Document Custom Additions

```bash
# My custom function for project setup
# Usage: newproject projectname
newproject() {
    mkcd "$1"
    git init
    npm init -y
}
```

### 4. Test Before Committing

```bash
# Test .bashrc for syntax errors
bash -n ~/.bashrc

# Start new shell to test
bash --login
```

## Portable Across Machines

When moving Naner to a new machine:

1. Copy entire `naner_launcher` folder
2. Launch Bash profile
3. Verify: `naner-info`
4. All aliases and functions work immediately

**What's portable:**
- ✅ Aliases
- ✅ Functions
- ✅ Prompt configuration
- ✅ Shell options
- ✅ Environment variables
- ✅ Git integration

**What's NOT portable:**
- ❌ Command history (gitignored)
- ❌ Bash sessions (gitignored)

## Related Documentation

- [PORTABLE-GIT.md](PORTABLE-GIT.md) - Git configuration
- [PORTABLE-POWERSHELL.md](PORTABLE-POWERSHELL.md) - PowerShell configuration
- [PORTABLE-SSH.md](PORTABLE-SSH.md) - SSH configuration
- [home/.bashrc](../home/.bashrc) - Full Bash configuration

## Resources

- [Bash Reference Manual](https://www.gnu.org/software/bash/manual/) - Official docs
- [Bash Guide for Beginners](https://tldp.org/LDP/Bash-Beginners-Guide/html/) - Tutorial
- [Advanced Bash-Scripting Guide](https://tldp.org/LDP/abs/html/) - Advanced topics
- [Bash Hackers Wiki](https://wiki.bash-hackers.org/) - Tips and tricks
