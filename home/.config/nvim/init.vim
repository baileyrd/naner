" Naner Portable Neovim Configuration
" For Neovim-specific features and modern defaults

" Load Vim config as base
if filereadable(expand('~/.vimrc'))
  source ~/.vimrc
endif

" Neovim-specific Settings
set clipboard+=unnamedplus   " Use system clipboard
set inccommand=split         " Show substitution results in split window
set termguicolors            " Enable 24-bit RGB colors

" Better defaults for Neovim
set mouse=a                  " Enable mouse support
set undofile                 " Persistent undo
set undodir=~/.config/nvim/undo
set updatetime=300           " Faster completion

" Create undo directory if it doesn't exist
if !isdirectory(expand('~/.config/nvim/undo'))
  call mkdir(expand('~/.config/nvim/undo'), 'p')
endif

" LSP Configuration (if nvim-lspconfig is installed)
lua << EOF
-- Uncomment after installing nvim-lspconfig
-- local lspconfig = require('lspconfig')

-- Python
-- lspconfig.pylsp.setup{}

-- TypeScript/JavaScript
-- lspconfig.ts_ls.setup{}

-- Go
-- lspconfig.gopls.setup{}

-- Rust
-- lspconfig.rust_analyzer.setup{}

-- Ruby
-- lspconfig.solargraph.setup{}
EOF

" Telescope (if installed)
nnoremap <leader>ff <cmd>Telescope find_files<cr>
nnoremap <leader>fg <cmd>Telescope live_grep<cr>
nnoremap <leader>fb <cmd>Telescope buffers<cr>
nnoremap <leader>fh <cmd>Telescope help_tags<cr>

" Terminal mode mappings
tnoremap <Esc> <C-\><C-n>
tnoremap <C-w> <C-\><C-n><C-w>

" Welcome message
autocmd VimEnter * echo "Naner Neovim - Ready!"
