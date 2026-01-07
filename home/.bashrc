# Naner Portable Bash Configuration
# This file is automatically loaded when Bash starts in Naner
# Location: %NANER_ROOT%\home\.bashrc

# If not running interactively, don't do anything
[[ $- != *i* ]] && return

# ============================================================================
# Naner Branding
# ============================================================================

echo -e "\033[0;36mNaner Bash Environment\033[0m"
echo -e "\033[0;36m======================\033[0m"
echo ""

if [ -n "$NANER_ROOT" ]; then
    echo -e "\033[0;90mNaner Root: \033[0;32m$NANER_ROOT\033[0m"
fi

if [ -n "$HOME" ]; then
    echo -e "\033[0;90mHome:       \033[0;32m$HOME\033[0m"
fi

echo ""

# ============================================================================
# Shell Options
# ============================================================================

# Append to history file, don't overwrite
shopt -s histappend

# Check window size after each command
shopt -s checkwinsize

# Case-insensitive globbing
shopt -s nocaseglob

# Autocorrect typos in path names when using cd
shopt -s cdspell

# Enable extended pattern matching
shopt -s extglob

# Enable recursive globbing with **
shopt -s globstar 2>/dev/null

# ============================================================================
# History Configuration
# ============================================================================

# Don't put duplicate lines or lines starting with space in history
HISTCONTROL=ignoreboth:erasedups

# History size
HISTSIZE=10000
HISTFILESIZE=20000

# History file location (portable)
HISTFILE="$HOME/.bash_history"

# Ignore common commands
HISTIGNORE="ls:ll:la:cd:pwd:exit:clear:history"

# Add timestamp to history
HISTTIMEFORMAT="%F %T "

# ============================================================================
# Environment Variables
# ============================================================================

# Set default editor (prefers nano from MSYS2, falls back to vim)
export EDITOR="nano"
export VISUAL="nano"

# Less options for better paging
export LESS="-R -F -X"

# Enable colored output for ls (if available)
if [ -x /usr/bin/dircolors ]; then
    test -r ~/.dircolors && eval "$(dircolors -b ~/.dircolors)" || eval "$(dircolors -b)"
fi

# ============================================================================
# Prompt Configuration
# ============================================================================

# Color definitions
COLOR_RESET='\[\033[0m\]'
COLOR_CYAN='\[\033[0;36m\]'
COLOR_GREEN='\[\033[0;32m\]'
COLOR_YELLOW='\[\033[0;33m\]'
COLOR_RED='\[\033[0;31m\]'
COLOR_GRAY='\[\033[0;90m\]'

# Function to get current Git branch
git_branch() {
    if git rev-parse --git-dir >/dev/null 2>&1; then
        local branch=$(git symbolic-ref --short HEAD 2>/dev/null || git rev-parse --short HEAD 2>/dev/null)
        if [ -n "$branch" ]; then
            # Check if there are uncommitted changes
            if ! git diff --quiet 2>/dev/null; then
                echo " (git:$branch*)"
            else
                echo " (git:$branch)"
            fi
        fi
    fi
}

# Set prompt
# Format: [Naner] current_directory (git:branch) >
PS1="${COLOR_CYAN}[Naner]${COLOR_RESET} ${COLOR_GREEN}\W${COLOR_RESET}${COLOR_YELLOW}\$(git_branch)${COLOR_RESET} > "

# ============================================================================
# Aliases
# ============================================================================

# Load additional aliases from .bash_aliases if it exists
if [ -f "$HOME/.bash_aliases" ]; then
    source "$HOME/.bash_aliases"
fi

# Directory listing
alias ls='ls --color=auto'
alias ll='ls -alF'
alias la='ls -A'
alias l='ls -CF'

# Directory navigation
alias ..='cd ..'
alias ...='cd ../..'
alias ....='cd ../../..'
alias ~='cd ~'

# Safety nets
alias rm='rm -i'
alias cp='cp -i'
alias mv='mv -i'

# Shortcuts
alias h='history'
alias c='clear'
alias e='$EDITOR'

# Grep with color
alias grep='grep --color=auto'
alias fgrep='fgrep --color=auto'
alias egrep='egrep --color=auto'

# Process management
alias psg='ps aux | grep -v grep | grep -i -e VSZ -e'

# Disk usage
alias du='du -h'
alias df='df -h'

# Make directories with parents
alias mkdir='mkdir -pv'

# Path
alias path='echo -e ${PATH//:/\\n}'

# Get external IP
alias myip='curl -s https://api.ipify.org'

# ============================================================================
# Git Shortcuts (matching PowerShell profile)
# ============================================================================

if command -v git >/dev/null 2>&1; then
    alias gs='git status -sb'
    alias ga='git add'
    alias gc='git commit'
    alias gp='git push'
    alias gl='git pull'
    alias gd='git diff'
    alias gco='git checkout'
    alias gb='git branch'
    alias glog='git log --oneline --graph --decorate --all'
    alias glg='git log --graph --pretty=format:"%Cred%h%Creset -%C(yellow)%d%Creset %s %Cgreen(%cr) %C(bold blue)<%an>%Creset" --abbrev-commit'
fi

# ============================================================================
# Docker Shortcuts (if Docker available)
# ============================================================================

if command -v docker >/dev/null 2>&1; then
    alias dps='docker ps'
    alias dpa='docker ps -a'
    alias di='docker images'
    alias dex='docker exec -it'
    alias dlog='docker logs -f'
    alias dstop='docker stop $(docker ps -q)'
fi

# ============================================================================
# Functions
# ============================================================================

# Create directory and cd into it
mkcd() {
    mkdir -p "$1" && cd "$1"
}

# Extract archives (auto-detect format)
extract() {
    if [ -f "$1" ]; then
        case "$1" in
            *.tar.bz2)   tar xjf "$1"     ;;
            *.tar.gz)    tar xzf "$1"     ;;
            *.tar.xz)    tar xJf "$1"     ;;
            *.bz2)       bunzip2 "$1"     ;;
            *.rar)       unrar x "$1"     ;;
            *.gz)        gunzip "$1"      ;;
            *.tar)       tar xf "$1"      ;;
            *.tbz2)      tar xjf "$1"     ;;
            *.tgz)       tar xzf "$1"     ;;
            *.zip)       unzip "$1"       ;;
            *.Z)         uncompress "$1"  ;;
            *.7z)        7z x "$1"        ;;
            *)           echo "'$1' cannot be extracted via extract()" ;;
        esac
    else
        echo "'$1' is not a valid file"
    fi
}

# Find file by name
ff() {
    find . -type f -iname "*$1*"
}

# Find directory by name
fd() {
    find . -type d -iname "*$1*"
}

# Search file contents
search() {
    grep -rnw . -e "$1"
}

# Naner utility functions
naner-info() {
    echo ""
    echo -e "\033[0;36mNaner Environment Information\033[0m"
    echo -e "\033[0;36m=============================\033[0m"
    echo ""
    echo "Naner Root:   $NANER_ROOT"
    echo "Home:         $HOME"
    echo "SSH Home:     $SSH_HOME"
    echo "Git Config:   $GIT_CONFIG_GLOBAL"
    echo "Shell:        $SHELL"
    echo "Editor:       $EDITOR"
    echo ""
}

naner-aliases() {
    echo ""
    echo -e "\033[0;36mNaner Bash Aliases\033[0m"
    echo -e "\033[0;36m==================\033[0m"
    echo ""
    echo "Git:     gs ga gc gp gl gd gco gb glog"
    echo "Docker:  dps dpa di dex dlog dstop"
    echo "Files:   ll la l .. ... ...."
    echo "Utils:   mkcd extract ff fd search"
    echo "Naner:   naner-info naner-aliases"
    echo ""
}

# Quick access to Naner scripts
naner-setup() {
    "$NANER_ROOT/src/powershell/Setup-NanerVendor.ps1" "$@"
}

naner-test() {
    "$NANER_ROOT/src/powershell/Test-NanerInstallation.ps1" "$@"
}

# ============================================================================
# Completion
# ============================================================================

# Enable programmable completion features
if ! shopt -oq posix; then
    if [ -f /usr/share/bash-completion/bash_completion ]; then
        . /usr/share/bash-completion/bash_completion
    elif [ -f /etc/bash_completion ]; then
        . /etc/bash_completion
    fi
fi

# Git completion (if available)
if [ -f /usr/share/bash-completion/completions/git ]; then
    . /usr/share/bash-completion/completions/git
fi

# ============================================================================
# Welcome Message
# ============================================================================

echo -e "\033[0;36mQuick Commands:\033[0m"
echo -e "\033[0;90m  naner-info         - Show Naner environment info\033[0m"
echo -e "\033[0;90m  naner-aliases      - Show all aliases\033[0m"
echo -e "\033[0;90m  naner-setup        - Run vendor setup\033[0m"
echo -e "\033[0;90m  naner-test         - Test Naner installation\033[0m"
echo ""
echo -e "\033[0;36mGit Shortcuts:\033[0m \033[0;90mgs, ga, gc, gp, gl, gd, glog\033[0m"
echo ""

# ============================================================================
# User Customizations
# ============================================================================

# Add your custom configurations below this line
# This section is preserved when updating .bashrc

# User aliases
# alias myalias='command'

# User functions
# myfunction() {
#     # Your code here
# }

# User PATH additions
# export PATH="$PATH:/custom/path"
