" Naner Portable Vim Configuration
" Compatible with Vim 8+ and Neovim

" Basic Settings
set nocompatible              " Use Vim settings rather than Vi
filetype plugin indent on     " Enable file type detection
syntax enable                 " Enable syntax highlighting

" UI Settings
set number                    " Show line numbers
set relativenumber            " Show relative line numbers
set cursorline               " Highlight current line
set showmatch                " Highlight matching brackets
set wildmenu                 " Visual autocomplete for command menu
set wildmode=longest:full,full
set laststatus=2             " Always show status line
set showcmd                  " Show command in bottom bar
set ruler                    " Show line and column number

" Editor Settings
set tabstop=4                " Number of spaces per TAB
set softtabstop=4            " Number of spaces in tab when editing
set shiftwidth=4             " Number of spaces for auto-indent
set expandtab                " Convert tabs to spaces
set autoindent               " Auto-indent new lines
set smartindent              " Smart indent
set backspace=indent,eol,start

" Search Settings
set incsearch                " Search as characters are entered
set hlsearch                 " Highlight search results
set ignorecase               " Case insensitive searching
set smartcase                " Case sensitive if uppercase used

" Performance
set lazyredraw               " Redraw only when needed
set ttyfast                  " Faster scrolling

" Backup and Swap
set nobackup                 " No backup files
set nowritebackup            " No backup while editing
set noswapfile               " No swap files

" Split Windows
set splitbelow               " Horizontal splits below
set splitright               " Vertical splits to the right

" File Encoding
set encoding=utf-8
set fileencoding=utf-8

" Line Wrapping
set wrap                     " Wrap lines
set linebreak                " Break lines at word boundaries

" Colors
set background=dark
colorscheme desert           " Default colorscheme

" Status Line
set statusline=%F%m%r%h%w\ [FORMAT=%{&ff}]\ [TYPE=%Y]\ [POS=%l,%v][%p%%]\ %{strftime('%Y-%m-%d\ %H:%M')}

" Key Mappings

" Leader key
let mapleader = ","

" Quick save
nnoremap <leader>w :w<CR>

" Quick quit
nnoremap <leader>q :q<CR>

" Clear search highlighting
nnoremap <leader><space> :nohlsearch<CR>

" Split navigation
nnoremap <C-h> <C-w>h
nnoremap <C-j> <C-w>j
nnoremap <C-k> <C-w>k
nnoremap <C-l> <C-w>l

" Buffer navigation
nnoremap <leader>n :bnext<CR>
nnoremap <leader>p :bprevious<CR>
nnoremap <leader>d :bdelete<CR>

" Tab navigation
nnoremap <leader>tn :tabnew<CR>
nnoremap <leader>tc :tabclose<CR>
nnoremap <leader>to :tabonly<CR>

" Move lines up/down
nnoremap <A-j> :m .+1<CR>==
nnoremap <A-k> :m .-2<CR>==
vnoremap <A-j> :m '>+1<CR>gv=gv
vnoremap <A-k> :m '<-2<CR>gv=gv

" Indent/unindent in visual mode
vnoremap < <gv
vnoremap > >gv

" Paste without yanking
vnoremap p "_dP

" File Type Specific Settings

" Python
autocmd FileType python setlocal tabstop=4 shiftwidth=4 expandtab

" JavaScript/TypeScript
autocmd FileType javascript,typescript,javascriptreact,typescriptreact setlocal tabstop=2 shiftwidth=2 expandtab

" HTML/CSS
autocmd FileType html,css,scss setlocal tabstop=2 shiftwidth=2 expandtab

" Go
autocmd FileType go setlocal tabstop=4 shiftwidth=4 noexpandtab

" Ruby
autocmd FileType ruby setlocal tabstop=2 shiftwidth=2 expandtab

" Markdown
autocmd FileType markdown setlocal wrap linebreak textwidth=80

" PowerShell
autocmd FileType ps1 setlocal tabstop=4 shiftwidth=4 expandtab

" Plugin Management (vim-plug)
" Install vim-plug if not present
if empty(glob('~/.vim/autoload/plug.vim'))
  silent !curl -fLo ~/.vim/autoload/plug.vim --create-dirs
    \ https://raw.githubusercontent.com/junegunn/vim-plug/master/plug.vim
  autocmd VimEnter * PlugInstall --sync | source $MYVIMRC
endif

" Plugins
call plug#begin('~/.vim/plugged')

" File navigation
Plug 'preservim/nerdtree'
Plug 'ctrlpvim/ctrlp.vim'

" Git integration
Plug 'tpope/vim-fugitive'
Plug 'airblade/vim-gitgutter'

" Status line
Plug 'vim-airline/vim-airline'
Plug 'vim-airline/vim-airline-themes'

" Code completion
Plug 'neoclide/coc.nvim', {'branch': 'release'}

" Syntax highlighting
Plug 'sheerun/vim-polyglot'

" Commenting
Plug 'tpope/vim-commentary'

" Surround
Plug 'tpope/vim-surround'

" Auto pairs
Plug 'jiangmiao/auto-pairs'

" Color schemes
Plug 'morhetz/gruvbox'
Plug 'joshdick/onedark.vim'

call plug#end()

" Plugin Configurations

" NERDTree
nnoremap <leader>e :NERDTreeToggle<CR>
let NERDTreeShowHidden=1

" CtrlP
let g:ctrlp_show_hidden=1
let g:ctrlp_custom_ignore = {
  \ 'dir':  '\v[\/](\.(git|hg|svn)|node_modules|dist|build)$',
  \ 'file': '\v\.(exe|so|dll|pyc)$',
  \ }

" Airline
let g:airline_powerline_fonts = 1
let g:airline_theme='onedark'
let g:airline#extensions#tabline#enabled = 1

" Gruvbox
colorscheme gruvbox

" Custom Commands

" Remove trailing whitespace
command! TrimWhitespace :%s/\s\+$//e

" Convert tabs to spaces
command! TabsToSpaces :%s/\t/    /g

" Auto commands

" Remember cursor position
autocmd BufReadPost * if line("'\"") > 1 && line("'\"") <= line("$") | exe "normal! g`\"" | endif

" Highlight TODO, FIXME, NOTE
autocmd Syntax * syntax match Todo /\v<(TODO|FIXME|NOTE|BUG|HACK)/

" Welcome message
autocmd VimEnter * echo "Naner Vim - Ready!"
