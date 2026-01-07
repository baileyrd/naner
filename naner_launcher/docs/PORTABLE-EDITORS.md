# Portable Editor Configurations

Naner provides portable configurations for popular code editors, ensuring consistent editing experience across machines.

## Overview

Portable editor configurations travel with your Naner installation:
- **VS Code**: Settings, keybindings, and extension recommendations
- **Vim/Neovim**: Complete configuration with plugin support
- **Nano**: Customized settings and syntax highlighting

All configurations use Naner's portable paths for tools and runtimes.

---

## VS Code Portable Configuration

### Location

```
%NANER_ROOT%\home\.vscode\
├── settings.json      # Editor settings
├── keybindings.json   # Keyboard shortcuts
└── extensions.json    # Recommended extensions
```

### Features

**Editor Settings:**
- Font: Cascadia Code with ligatures
- Format on save enabled
- Auto-import organization
- Bracket pair colorization
- 80/120 character rulers

**Language Support:**
- JavaScript/TypeScript with Prettier
- Python with Black formatter and Flake8
- Go with automatic formatting
- Rust with rust-analyzer
- Ruby with ruby-lsp
- PowerShell with proper encoding

**Integrated Terminal:**
- PowerShell (Naner portable)
- Bash (MSYS2 from Naner)
- CMD

**Git Integration:**
- Uses portable Git from Naner
- Auto-fetch enabled
- Smart commit enabled

### Using VS Code with Naner

#### Option 1: Use Workspace Settings

Create a `.vscode` folder in your project and symlink Naner settings:

**PowerShell:**
```powershell
# In your project directory
New-Item -ItemType SymbolicLink -Path ".\.vscode\settings.json" -Target "$env:NANER_ROOT\home\.vscode\settings.json"
```

**Bash:**
```bash
# In your project directory
ln -s "$NANER_ROOT/home/.vscode/settings.json" .vscode/settings.json
```

#### Option 2: VS Code Portable Mode

Download VS Code Portable and configure it to use Naner:

1. Download VS Code ZIP from https://code.visualstudio.com/
2. Extract to `%NANER_ROOT%\opt\vscode\`
3. Create `data` folder inside VS Code directory
4. Copy Naner settings:
   ```powershell
   Copy-Item "$env:NANER_ROOT\home\.vscode\*" "$env:NANER_ROOT\opt\vscode\data\user-data\User\" -Recurse
   ```

#### Option 3: Use Settings Sync

1. Open VS Code
2. Enable Settings Sync (Ctrl+Shift+P → "Settings Sync: Turn On")
3. Manually import settings from `%NANER_ROOT%\home\.vscode\`

### Recommended Extensions

The [extensions.json](../home/.vscode/extensions.json) includes:

**Language Support:**
- Python (Pylance, Black formatter)
- Go
- Rust (rust-analyzer)
- Ruby (Ruby LSP)
- PowerShell

**Web Development:**
- ESLint
- Prettier
- Tailwind CSS IntelliSense
- Auto Rename Tag

**Git:**
- GitLens
- Git Graph

**Productivity:**
- Live Server
- Path IntelliSense
- Better Comments
- Error Lens
- Code Spell Checker

### Customization

Edit `%NANER_ROOT%\home\.vscode\settings.json`:

```json
{
  "editor.fontSize": 16,
  "workbench.colorTheme": "GitHub Dark",
  "terminal.integrated.fontSize": 14
}
```

---

## Vim/Neovim Configuration

### Location

**Vim:**
```
%NANER_ROOT%\home\.vimrc
%NANER_ROOT%\home\.vim\     # Plugin directory
```

**Neovim:**
```
%NANER_ROOT%\home\.config\nvim\init.vim
%NANER_ROOT%\home\.config\nvim\undo\    # Undo history
```

### Features

**UI Enhancements:**
- Line numbers (absolute + relative)
- Syntax highlighting
- Cursor line highlight
- Status line with file info

**Editor Behavior:**
- 4-space tabs (language-specific overrides)
- Smart indenting
- Incremental search with highlighting
- Split windows (below/right)

**Key Bindings:**
- Leader key: `,`
- `,w` - Save
- `,q` - Quit
- `,<space>` - Clear search highlighting
- `Ctrl+h/j/k/l` - Navigate splits
- `,n/p` - Next/previous buffer
- `Alt+j/k` - Move lines up/down

**Plugins (via vim-plug):**
- NERDTree - File explorer (`,e`)
- CtrlP - Fuzzy file finder
- vim-fugitive - Git integration
- vim-gitgutter - Git diff in gutter
- vim-airline - Status line
- coc.nvim - Code completion
- vim-polyglot - Syntax for many languages
- vim-commentary - Easy commenting
- vim-surround - Surround text objects
- auto-pairs - Auto-close brackets
- gruvbox - Color scheme

### Language-Specific Settings

```vim
" Python - 4 spaces
autocmd FileType python setlocal tabstop=4 shiftwidth=4

" JavaScript/TypeScript - 2 spaces
autocmd FileType javascript setlocal tabstop=2 shiftwidth=2

" Go - tabs (no spaces)
autocmd FileType go setlocal noexpandtab

" Ruby - 2 spaces
autocmd FileType ruby setlocal tabstop=2 shiftwidth=2
```

### Installing Plugins

Plugins are managed with vim-plug.

**First time setup:**
```bash
# vim-plug will auto-install on first run
vim
# Inside Vim:
:PlugInstall
```

**Update plugins:**
```vim
:PlugUpdate
```

**Remove unused plugins:**
```vim
:PlugClean
```

### Neovim-Specific Features

Neovim configuration extends Vim config with:
- System clipboard integration
- 24-bit RGB colors
- Live substitution preview
- Mouse support
- Persistent undo history

### Using Vim/Neovim

**Launch Vim:**
```bash
vim myfile.txt
```

**Launch Neovim:**
```bash
nvim myfile.txt
```

**Quick Reference:**
- `i` - Insert mode
- `Esc` - Normal mode
- `:w` - Save
- `:q` - Quit
- `:wq` - Save and quit
- `/pattern` - Search
- `dd` - Delete line
- `yy` - Copy line
- `p` - Paste
- `u` - Undo
- `Ctrl+r` - Redo

### Customization

Edit `%NANER_ROOT%\home\.vimrc`:

```vim
" Change colorscheme
colorscheme onedark

" Change leader key
let mapleader = " "

" Add custom mapping
nnoremap <leader>x :!python %<CR>
```

---

## Nano Configuration

### Location

```
%NANER_ROOT%\home\.nanorc
```

### Features

**Editor Behavior:**
- Auto-indent enabled
- 4-space tabs converted to spaces
- Smooth scrolling
- Line numbers
- Mouse support
- Soft-wrap long lines

**Syntax Highlighting:**
- Python, JavaScript, TypeScript
- HTML, CSS, JSON
- Markdown, YAML, XML
- Shell scripts
- Ruby, Go, Rust
- C/C++
- Dockerfiles
- .env files
- .gitignore files

**Key Bindings (Emacs-style):**
- `Ctrl+S` - Save
- `Ctrl+Q` - Quit
- `Ctrl+F` - Find
- `Ctrl+R` - Replace
- `Ctrl+G` - Help
- `Ctrl+X` - Cut
- `Ctrl+C` - Copy
- `Ctrl+V` - Paste
- `Ctrl+Z` - Undo
- `Ctrl+Y` - Redo

### Using Nano

**Open file:**
```bash
nano myfile.txt
```

**Create new file:**
```bash
nano
```

**Quick reference:**
- All commands shown at bottom
- `^` means Ctrl key
- `M-` means Alt key
- `^G` - Show help with all keybindings

### Customization

Edit `%NANER_ROOT%\home\.nanorc`:

```nanorc
## Change tab size
set tabsize 2

## Enable backup files
set backup
set backupdir "~/.nano/backups"

## Change colors
set titlecolor bold,white,red
```

---

## Editor Comparison

| Feature | VS Code | Vim/Neovim | Nano |
|---------|---------|------------|------|
| **GUI** | ✅ Full | ❌ Terminal | ❌ Terminal |
| **Mouse Support** | ✅ | ⚠️ Limited | ✅ |
| **Learning Curve** | Low | High | Low |
| **Performance** | Medium | Fast | Fast |
| **Extensibility** | High | High | Low |
| **Remote Editing** | ✅ | ✅ | ✅ |
| **Integrated Terminal** | ✅ | ✅ | ❌ |
| **Git Integration** | ✅ Full | ✅ Plugins | ❌ |
| **LSP Support** | ✅ Built-in | ✅ Plugins | ❌ |
| **Best For** | General dev | Power users | Quick edits |

---

## Troubleshooting

### VS Code

**Settings not applying:**
1. Check file location: `%NANER_ROOT%\home\.vscode\settings.json`
2. Verify JSON syntax (use VS Code to open and validate)
3. Restart VS Code

**Terminal not finding Naner tools:**
1. Ensure `NANER_ROOT` environment variable is set
2. Check terminal profile paths in settings.json
3. Restart VS Code to pick up environment changes

### Vim/Neovim

**Plugins not installing:**
```bash
# Manually install vim-plug
curl -fLo ~/.vim/autoload/plug.vim --create-dirs \
  https://raw.githubusercontent.com/junegunn/vim-plug/master/plug.vim

# Then in Vim:
:PlugInstall
```

**Colors not showing:**
```bash
# Check terminal supports 256 colors
echo $TERM
# Should show: xterm-256color or similar

# Set in .bashrc if needed:
export TERM=xterm-256color
```

**Config not loading:**
```bash
# Verify file exists
ls ~/.vimrc

# Check for syntax errors
vim -u ~/.vimrc -c "quit"
```

### Nano

**Syntax highlighting not working:**
```bash
# Check if syntax files exist
ls /usr/share/nano/

# Manually specify in .nanorc:
include "/usr/share/nano/python.nanorc"
```

**Config not loading:**
```bash
# Verify file exists
ls ~/.nanorc

# Check for syntax errors
nano -C ~/.nanorc
```

---

## Best Practices

### For All Editors

1. **Version Control**: Keep editor configs in git
2. **Backup**: Periodically back up customizations
3. **Documentation**: Document custom keybindings
4. **Share**: Share configs with team for consistency
5. **Test**: Test configs after modifications

### VS Code

1. Use workspace settings for project-specific configs
2. Enable Settings Sync for multi-machine use
3. Review extension updates before installing
4. Use keybindings.json for custom shortcuts
5. Leverage snippets for repetitive code

### Vim/Neovim

1. Start with basic config, add plugins gradually
2. Learn core Vim motions before adding plugins
3. Use `:help` command to learn features
4. Create language-specific mappings
5. Keep .vimrc organized with sections and comments

### Nano

1. Keep config simple and readable
2. Memorize core keybindings (^S, ^Q, ^F, ^R)
3. Use nano for quick edits, not large projects
4. Enable mouse for easier navigation
5. Customize syntax highlighting as needed

---

## Related Documentation

- [PORTABLE-GIT.md](PORTABLE-GIT.md) - Git configuration
- [PORTABLE-POWERSHELL.md](PORTABLE-POWERSHELL.md) - PowerShell configuration
- [PORTABLE-BASH.md](PORTABLE-BASH.md) - Bash configuration

---

## Resources

### VS Code
- [Official Docs](https://code.visualstudio.com/docs)
- [Keyboard Shortcuts](https://code.visualstudio.com/shortcuts/keyboard-shortcuts-windows.pdf)
- [Extension Marketplace](https://marketplace.visualstudio.com/)

### Vim
- [Vim Documentation](https://www.vim.org/docs.php)
- [Learn Vim](https://www.openvim.com/)
- [Vimcasts](http://vimcasts.org/)
- [vim-plug](https://github.com/junegunn/vim-plug)

### Neovim
- [Neovim Docs](https://neovim.io/doc/)
- [Awesome Neovim](https://github.com/rockerBOO/awesome-neovim)

### Nano
- [GNU Nano](https://www.nano-editor.org/)
- [Nano Documentation](https://www.nano-editor.org/dist/latest/nano.html)
