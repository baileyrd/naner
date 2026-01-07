# Naner Project Templates

Pre-built project templates for rapid development with Naner.

## Available Templates

### 1. React + Vite + TypeScript (`react-vite-ts`)

Modern React application with TypeScript and Vite for fast development.

**Features:**
- React 18 with TypeScript
- Vite for blazing-fast HMR
- ESLint for code quality
- Modern CSS with CSS variables
- Production-ready build system

**Usage:**
```powershell
New-NanerProject -Type react-vite-ts -Name my-app
cd my-app
npm run dev
```

**Tech Stack:**
- React 18
- TypeScript 5
- Vite 5
- ESLint

---

### 2. Node.js Express API (`nodejs-express-api`)

REST API backend with Express.js and modern JavaScript (ES modules).

**Features:**
- Express.js web framework
- CORS support
- Environment variable management
- Sample CRUD endpoints
- Error handling middleware

**Usage:**
```powershell
New-NanerProject -Type nodejs-express-api -Name my-api
cd my-api
npm run dev
```

**Tech Stack:**
- Express.js 4
- CORS
- dotenv
- ES Modules

---

### 3. Python CLI Tool (`python-cli`)

Command-line application with Click framework and Rich terminal UI.

**Features:**
- Click for CLI framework
- Rich for beautiful terminal output
- pytest for testing
- Black for formatting
- Modern pyproject.toml configuration

**Usage:**
```powershell
New-NanerProject -Type python-cli -Name mytool
cd mytool
pip install -e ".[dev]"
mytool --help
```

**Tech Stack:**
- Click (CLI framework)
- Rich (Terminal UI)
- pytest (Testing)
- Black (Formatting)

---

### 4. Static Website (`static-website`)

Clean and responsive static website with HTML, CSS, and JavaScript.

**Features:**
- Responsive design
- Modern CSS (Grid, Flexbox, CSS Variables)
- Smooth scrolling navigation
- Contact form
- Mobile-friendly

**Usage:**
```powershell
New-NanerProject -Type static-website -Name my-site
cd my-site
python -m http.server 8000
# Open http://localhost:8000
```

**Tech Stack:**
- HTML5
- CSS3
- Vanilla JavaScript

---

## Using `New-NanerProject`

### Basic Syntax

```powershell
New-NanerProject -Type <template-type> -Name <project-name>
```

### Parameters

**-Type** (Required)
- Template type to use
- Options: `react-vite-ts`, `nodejs-express-api`, `python-cli`, `static-website`

**-Name** (Required)
- Name of the project
- Used for directory name and placeholder substitution

**-Path** (Optional)
- Directory where project will be created
- Defaults to current directory

**-NoInstall** (Optional)
- Skip automatic dependency installation
- Useful for offline work or manual dependency management

### Examples

**Create React app in current directory:**
```powershell
New-NanerProject -Type react-vite-ts -Name my-app
```

**Create Express API in specific location:**
```powershell
New-NanerProject -Type nodejs-express-api -Name my-api -Path C:\Projects
```

**Create Python CLI without installing dependencies:**
```powershell
New-NanerProject -Type python-cli -Name mytool -NoInstall
```

**Create static website:**
```powershell
New-NanerProject -Type static-website -Name my-site
```

---

## Template Structure

Each template includes:

- **README.md** - Project documentation
- **.gitignore** - Pre-configured gitignore
- **Configuration files** - Language/framework specific configs
- **Source files** - Starter code with best practices
- **Sample code** - Working examples to build upon

Templates use `{{PROJECT_NAME}}` as a placeholder, which is automatically replaced with your project name.

---

## Customizing Templates

Templates are located in `%NANER_ROOT%\home\Templates\`.

### Modify Existing Templates

1. Navigate to template directory:
   ```powershell
   cd $env:NANER_ROOT\home\Templates\react-vite-ts
   ```

2. Edit template files as needed

3. Use `{{PROJECT_NAME}}` for dynamic substitution

### Create New Templates

1. Create a new directory in `Templates/`:
   ```powershell
   mkdir $env:NANER_ROOT\home\Templates\my-template
   ```

2. Add your template files

3. Use `{{PROJECT_NAME}}` wherever the project name should appear

4. Update `New-NanerProject.ps1` to include your template:
   ```powershell
   # Add to ValidateSet in param block
   [ValidateSet('react-vite-ts', 'nodejs-express-api', 'python-cli', 'static-website', 'my-template')]

   # Add to Get-TemplateInfo function
   'my-template' = @{
       Name = 'My Template'
       Description = 'Description of my template'
       InstallCmd = 'installation command'
       RunCmd = 'run command'
   }
   ```

---

## Template Features

### Placeholder Substitution

The `{{PROJECT_NAME}}` placeholder is automatically replaced in:
- File contents
- File names
- Directory names

**Example:**
```json
{
  "name": "{{PROJECT_NAME}}",
  "version": "1.0.0"
}
```

Becomes:
```json
{
  "name": "my-app",
  "version": "1.0.0"
}
```

### Automatic Dependency Installation

Templates with package managers automatically run installation:
- **npm** templates: `npm install`
- **Python** templates: `pip install -e ".[dev]"`

Skip with `-NoInstall` flag.

---

## Best Practices

### When Creating Projects

1. **Use descriptive names:** Clear project names help organization
2. **Check directory first:** Ensure target directory doesn't exist
3. **Review README:** Each template has specific instructions
4. **Initialize git:** Don't forget to `git init` after creation

### When Modifying Templates

1. **Test changes:** Create a test project after modifying templates
2. **Keep placeholders:** Always use `{{PROJECT_NAME}}` for project name
3. **Update README:** Document template changes
4. **Maintain .gitignore:** Keep exclusion patterns up-to-date

---

## Troubleshooting

### Template Not Found

**Error:** `Template not found: ...`

**Solution:** Ensure you're using a valid template type:
```powershell
# List available templates
ls $env:NANER_ROOT\home\Templates
```

### Directory Already Exists

**Error:** `Directory already exists: ...`

**Solution:** Choose a different name or remove existing directory:
```powershell
Remove-Item -Path ./my-app -Recurse -Force
```

### Dependency Installation Fails

If automatic installation fails:

1. Navigate to project directory
2. Run installation command manually
3. Check network connection
4. Verify package manager is in PATH

**For Node.js:**
```powershell
npm install
```

**For Python:**
```powershell
pip install -e ".[dev]"
```

---

## Related Documentation

- [PORTABLE-NODE.md](../naner_launcher/docs/PORTABLE-NODE.md) - Node.js runtime
- [PORTABLE-PYTHON.md](../naner_launcher/docs/PORTABLE-PYTHON.md) - Python/Conda runtime
- [PORTABLE-GIT.md](../naner_launcher/docs/PORTABLE-GIT.md) - Git configuration

---

## Contributing Templates

Have a useful project template? Consider adding it to Naner!

1. Create template in `Templates/` directory
2. Follow existing template structure
3. Include comprehensive README
4. Test with `New-NanerProject`
5. Document in this file
6. Commit and share

---

## Template Roadmap

**Planned Templates:**
- Go CLI Application
- Rust CLI Application
- PowerShell Module
- Jekyll Static Site
- Hugo Static Site
- FastAPI Python Web API
- Django REST Framework
- Next.js Application
- Vue.js Application

Want a specific template? Open an issue or create it yourself!
