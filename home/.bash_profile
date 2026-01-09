# Naner Portable Bash Login Profile
# This file is loaded for login shells
# Location: %NANER_ROOT%\home\.bash_profile

# ============================================================================
# Environment Setup for Login Shells
# ============================================================================

# Source .bashrc if it exists (for interactive shells)
if [ -f "$HOME/.bashrc" ]; then
    source "$HOME/.bashrc"
fi

# ============================================================================
# PATH Configuration
# ============================================================================

# Naner paths are already configured by naner.json and Invoke-Naner.ps1
# Additional paths can be added here

# Add user's private bin if it exists
if [ -d "$HOME/bin" ]; then
    export PATH="$HOME/bin:$PATH"
fi

# Add local bin if it exists
if [ -d "$HOME/.local/bin" ]; then
    export PATH="$HOME/.local/bin:$PATH"
fi

# ============================================================================
# Login-Specific Configuration
# ============================================================================

# Set umask for file creation permissions
# umask 022

# Initialize SSH agent (optional - uncomment if needed)
# if [ -z "$SSH_AUTH_SOCK" ]; then
#     eval $(ssh-agent -s)
#     ssh-add ~/.ssh/id_ed25519 2>/dev/null
# fi

# ============================================================================
# User Customizations for Login Shells
# ============================================================================

# Add your login-specific configurations below
# This section is for things that should only run once per session

# Example: Set up cloud CLI tools
# if command -v aws >/dev/null 2>&1; then
#     complete -C '/usr/local/bin/aws_completer' aws
# fi
