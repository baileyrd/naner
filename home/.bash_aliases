# Naner Portable Bash Aliases
# This file contains additional aliases loaded by .bashrc
# Location: %NANER_ROOT%\home\.bash_aliases

# ============================================================================
# System Aliases
# ============================================================================

# Quick navigation
alias home='cd ~'
alias desktop='cd ~/Desktop'
alias downloads='cd ~/Downloads'
alias documents='cd ~/Documents'

# System information
alias sysinfo='uname -a'
alias meminfo='free -h'
alias cpuinfo='lscpu'
alias diskinfo='df -h | grep -v loop'

# Process management
alias topcpu='ps aux --sort=-%cpu | head -10'
alias topmem='ps aux --sort=-%mem | head -10'
alias killall='killall'

# ============================================================================
# Development Aliases
# ============================================================================

# Node.js / npm (if installed)
alias npmlist='npm list -g --depth=0'
alias npmout='npm outdated -g'
alias npmu='npm update -g'

# Python (if installed)
alias py='python'
alias pip='python -m pip'
alias piplist='pip list'
alias pipout='pip list --outdated'

# Serve current directory via HTTP
alias serve='python -m http.server 8000'

# ============================================================================
# Git Advanced Aliases
# ============================================================================

# Git status with full details
alias gst='git status'

# Git add all and commit
alias gac='git add -A && git commit'

# Git commit with message
alias gcm='git commit -m'

# Git amend last commit
alias gca='git commit --amend --no-edit'

# Git undo last commit (keep changes)
alias gundo='git reset HEAD~1'

# Git reset hard
alias greset='git reset --hard HEAD'

# Git show branches
alias gbr='git branch -avv'

# Git delete merged branches
alias gclean='git branch --merged | grep -v "\\*\\|main\\|master\\|develop" | xargs -n 1 git branch -d'

# Git log with patches
alias glp='git log -p'

# Git log with stat
alias gls='git log --stat'

# Git show last commit
alias glast='git log -1 HEAD --stat'

# Git contributors
alias gcontrib='git shortlog -sn'

# ============================================================================
# Docker Advanced Aliases
# ============================================================================

# Docker remove all stopped containers
alias drm='docker rm $(docker ps -aq)'

# Docker remove all dangling images
alias drmi='docker rmi $(docker images -qf dangling=true)'

# Docker stop all running containers
alias dstopall='docker stop $(docker ps -q)'

# Docker compose shortcuts
alias dc='docker-compose'
alias dcu='docker-compose up'
alias dcd='docker-compose down'
alias dcl='docker-compose logs -f'

# ============================================================================
# File Operations
# ============================================================================

# Better ls output
alias lh='ls -lh'           # Human-readable sizes
alias lt='ls -lt'           # Sort by time
alias ltr='ls -ltr'         # Sort by time (reverse)
alias lsize='ls -lhS'       # Sort by size

# Count files
alias count='find . -type f | wc -l'

# Find largest files
alias largest='find . -type f -exec ls -lh {} \; | sort -k5 -hr | head -20'

# Find largest directories
alias duhh='du -h --max-depth=1 | sort -hr'

# ============================================================================
# Network Aliases
# ============================================================================

# Network information
alias ports='netstat -tulanp'
alias listening='netstat -tulanp | grep LISTEN'

# Ping with count
alias ping5='ping -c 5'

# Fast ping
alias fping='ping -c 100 -i 0.2'

# Download with wget resumable
alias wget='wget -c'

# Get headers
alias header='curl -I'

# Test download speed
alias speedtest='curl -s https://raw.githubusercontent.com/sivel/speedtest-cli/master/speedtest.py | python -'

# ============================================================================
# Text Processing
# ============================================================================

# Count lines in file
alias wcl='wc -l'

# Sort and unique
alias srt='sort'
alias srtu='sort -u'

# Tail with follow
alias tf='tail -f'

# ============================================================================
# Archive Operations
# ============================================================================

# Create tar.gz
alias targz='tar -czf'

# Extract tar.gz
alias untargz='tar -xzf'

# Create zip
alias mkzip='zip -r'

# List contents of zip
alias lszip='unzip -l'

# ============================================================================
# Utility Aliases
# ============================================================================

# Date shortcuts
alias now='date +"%Y-%m-%d %H:%M:%S"'
alias today='date +"%Y-%m-%d"'
alias timestamp='date +%s'

# Calculator
alias calc='bc -l'

# Copy to clipboard (if xclip available)
if command -v clip.exe >/dev/null 2>&1; then
    alias clip='clip.exe'
    alias pbcopy='clip.exe'
fi

# Generate random password
alias genpass='openssl rand -base64 32'

# ============================================================================
# Fun Aliases
# ============================================================================

# Matrix effect
alias matrix='cmatrix -b'

# Fortune cookie (if installed)
if command -v fortune >/dev/null 2>&1; then
    alias fortune='fortune'
fi

# ============================================================================
# Custom Project Aliases
# ============================================================================

# Add your project-specific aliases here

# Example: Jump to project directories
# alias proj1='cd ~/projects/project1'
# alias proj2='cd ~/projects/project2'

# Example: Start development servers
# alias devstart='npm run dev'
# alias devbuild='npm run build'

# ============================================================================
# Platform-Specific Aliases
# ============================================================================

# Windows-specific (when in MSYS2/Git Bash)
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    # Open Windows Explorer in current directory
    alias explorer='explorer.exe .'
    alias open='explorer.exe'

    # Open file with default program
    alias start='cmd.exe /c start'

    # Open Windows Terminal
    alias wt='wt.exe'
fi

# Linux-specific
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    # Package management
    alias update='sudo apt update && sudo apt upgrade'
    alias install='sudo apt install'

    # Service management
    alias services='systemctl list-units --type=service'
fi
