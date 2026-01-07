# Portable Python with Naner

Complete guide to using portable Python (Miniconda) within the Naner environment.

## Overview

Naner provides a fully portable Python runtime via Miniconda, including conda and pip package managers, allowing you to develop Python applications without installing Python system-wide.

**Benefits:**
- No system-wide Python installation required
- Full Miniconda distribution with conda and pip
- Portable package directories (`home/.conda`)
- Virtual environments in portable location
- Version-locked for team consistency
- Works across multiple machines

## Quick Start

### Enable Python Vendor

1. **Edit vendor configuration:**
   ```powershell
   # Open config/vendors.json
   code config/vendors.json
   ```

2. **Set Miniconda enabled to true:**
   ```json
   {
     "Miniconda": {
       "enabled": true,
       ...
     }
   }
   ```

3. **Install Python vendor:**
   ```powershell
   .\src\powershell\Setup-NanerVendor.ps1 -VendorId Miniconda
   ```

4. **Verify installation:**
   ```powershell
   python --version
   conda --version
   pip --version
   ```

## Configuration

### Vendor Configuration

Located in `config/vendors.json` (lines 117-132):

```json
{
  "Miniconda": {
    "enabled": false,
    "name": "Miniconda (Python)",
    "extractDir": "miniconda",
    "releaseSource": {
      "type": "static",
      "url": "https://repo.anaconda.com/miniconda/Miniconda3-latest-Windows-x86_64.exe"
    },
    "paths": [
      "miniconda",
      "miniconda\\Scripts",
      "miniconda\\Library\\bin"
    ],
    "postInstall": "Initialize-Miniconda"
  }
}
```

### PostInstall Configuration

The `Initialize-Miniconda` function (Naner.Vendors.psm1:706-780) configures:

1. **Silent installation** with portable configuration
2. **Creates portable directories**:
   - `home/.conda/pkgs` - Package cache
   - `home/.conda/envs` - Virtual environments
3. **Disables auto-activation** of base environment
4. **Configures conda settings**:
   ```
   conda config --set pkgs_dirs "%NANER_ROOT%\home\.conda\pkgs"
   conda config --set envs_dirs "%NANER_ROOT%\home\.conda\envs"
   conda config --set auto_activate_base false
   ```
5. **Verifies installation** - Displays Python and conda versions

## Usage

### Basic Python Commands

```powershell
# Check Python version
python --version

# Run Python script
python script.py

# Start Python REPL
python

# Run Python module
python -m http.server 8000
```

### Package Management

#### Using pip

```powershell
# Install package
pip install requests

# Install from requirements.txt
pip install -r requirements.txt

# Install in development mode
pip install -e .

# List installed packages
pip list

# Show package info
pip show requests

# Uninstall package
pip uninstall requests
```

#### Using conda

```powershell
# Install package
conda install numpy

# Install specific version
conda install pandas=2.0.0

# Install from conda-forge
conda install -c conda-forge scikit-learn

# List installed packages
conda list

# Update package
conda update numpy

# Remove package
conda remove numpy
```

## Virtual Environments

### Using conda environments

```powershell
# Create new environment
conda create -n myenv python=3.11

# Activate environment
conda activate myenv

# Deactivate environment
conda deactivate

# List environments
conda env list

# Remove environment
conda env remove -n myenv

# Export environment
conda env export > environment.yml

# Create from environment file
conda env create -f environment.yml
```

### Using venv (Python standard library)

```powershell
# Create virtual environment
python -m venv myenv

# Activate (PowerShell)
.\myenv\Scripts\Activate.ps1

# Activate (CMD)
.\myenv\Scripts\activate.bat

# Activate (Bash)
source myenv/Scripts/activate

# Deactivate
deactivate
```

## Directory Structure

```
naner/
├── vendor/
│   └── miniconda/           # Miniconda installation
│       ├── python.exe
│       ├── Scripts/
│       │   ├── conda.exe
│       │   └── pip.exe
│       └── Library/
├── home/
│   └── .conda/
│       ├── pkgs/           # Package cache
│       └── envs/           # Virtual environments
│           ├── myenv/
│           └── projectenv/
└── projects/
    └── my-project/
        ├── venv/            # Project venv (optional)
        └── requirements.txt
```

## Common Tasks

### Create Python CLI

```powershell
# Using Naner template (recommended)
New-NanerProject -Type python-cli -Name mytool
cd mytool
pip install -e ".[dev]"
mytool --help
```

### Data Science Project

```powershell
# Create conda environment for data science
conda create -n datascience python=3.11
conda activate datascience

# Install data science packages
conda install numpy pandas matplotlib scikit-learn jupyter

# Start Jupyter
jupyter notebook
```

### Web Development with Flask

```powershell
# Create project
mkdir my-flask-app
cd my-flask-app

# Create environment
conda create -n flaskenv python=3.11
conda activate flaskenv

# Install Flask
pip install flask

# Create app.py
@"
from flask import Flask
app = Flask(__name__)

@app.route('/')
def hello():
    return 'Hello, World!'

if __name__ == '__main__':
    app.run(debug=True)
"@ | Out-File app.py

# Run app
python app.py
```

### Django Project

```powershell
# Create environment
conda create -n djangoenv python=3.11
conda activate djangoenv

# Install Django
pip install django

# Create project
django-admin startproject mysite
cd mysite

# Run development server
python manage.py runserver
```

### Working with requirements.txt

```powershell
# Generate requirements.txt
pip freeze > requirements.txt

# Install from requirements.txt
pip install -r requirements.txt

# Install with versions
pip install 'requests>=2.28.0,<3.0.0'
```

## Environment Variables

Naner sets these environment variables:

```powershell
# Python is in PATH
$env:PATH  # Contains vendor/miniconda, miniconda/Scripts, miniconda/Library/bin

# Conda configuration (set by PostInstall)
conda config --show pkgs_dirs    # Returns home/.conda/pkgs
conda config --show envs_dirs    # Returns home/.conda/envs
```

## Version Management

### Check Current Version

```powershell
python --version
# Example: Python 3.11.7

conda --version
# Example: conda 23.11.0
```

### Update Miniconda

```powershell
# Update conda itself
conda update conda

# Update all packages in base
conda update --all
```

### Update Python in Environment

```powershell
# Update Python in specific environment
conda activate myenv
conda update python
```

## Troubleshooting

### Python Not Found

**Error:** `python: command not found` or `'python' is not recognized`

**Causes:**
- Miniconda vendor not installed
- Naner environment not loaded

**Solutions:**
```powershell
# Check if vendor is installed
Test-Path vendor/miniconda/python.exe

# If not installed
.\src\powershell\Setup-NanerVendor.ps1 -VendorId Miniconda

# Reload environment
pwsh  # Start new PowerShell session with Naner profile
```

### conda activate Not Working

**Error:** `CommandNotFoundError: Your shell has not been properly configured`

**Cause:** Shell not initialized for conda

**Solution:**
```powershell
# Initialize conda for PowerShell
conda init powershell

# Restart PowerShell
pwsh

# Now activate should work
conda activate myenv
```

### Package Installation Fails

**Error:** `CondaHTTPError` or pip network errors

**Solutions:**
```powershell
# Clear conda cache
conda clean --all

# Update conda
conda update conda

# Try pip instead
pip install package-name

# Or use conda-forge channel
conda install -c conda-forge package-name
```

### ImportError After Installation

**Error:** `ModuleNotFoundError: No module named 'package'`

**Causes:**
- Package not installed in active environment
- Using wrong Python interpreter

**Solutions:**
```powershell
# Check which Python
Get-Command python
# Should show: vendor\miniconda\python.exe

# Check if package is installed
pip list | Select-String package-name

# Reinstall if needed
pip install package-name
```

### Environment Activation Issues

**Error:** Environment doesn't activate properly

**Solutions:**
```powershell
# List environments
conda env list

# Verify environment exists
Test-Path $env:NANER_ROOT\home\.conda\envs\myenv

# Recreate if corrupted
conda env remove -n myenv
conda create -n myenv python=3.11
```

## Best Practices

### ✅ DO

- **Use virtual environments** for each project:
  ```powershell
  conda create -n myproject python=3.11
  conda activate myproject
  ```

- **Pin package versions** in requirements.txt:
  ```
  requests==2.31.0
  flask==3.0.0
  ```

- **Use Naner Python CLI template**:
  ```powershell
  New-NanerProject -Type python-cli -Name mytool
  ```

- **Document dependencies**:
  ```powershell
  pip freeze > requirements.txt
  conda env export > environment.yml
  ```

- **Use editable installs for development**:
  ```powershell
  pip install -e ".[dev]"
  ```

### ❌ DON'T

- **Don't install packages in base environment** - Always use project-specific environments
- **Don't mix conda and pip carelessly** - Prefer conda for conda packages, pip for PyPI packages
- **Don't commit virtual environments** - Always in .gitignore
- **Don't use sudo/admin** - Not needed with Naner

## Integration with Other Tools

### VS Code Integration

Naner's VS Code settings (home/.vscode/settings.json) automatically detect Python:

```json
{
  "python.defaultInterpreterPath": "${env:NANER_ROOT}\\vendor\\miniconda\\python.exe",
  "python.condaPath": "${env:NANER_ROOT}\\vendor\\miniconda\\Scripts\\conda.exe",
  "python.terminal.activateEnvironment": true,
  "[python]": {
    "editor.defaultFormatter": "ms-python.black-formatter",
    "editor.formatOnSave": true
  }
}
```

VS Code will auto-detect conda environments in `home/.conda/envs/`.

### Black Formatter

```powershell
# Install Black
pip install black

# Format file
black script.py

# Format directory
black src/

# Check without modifying
black --check src/
```

### Flake8 Linter

```powershell
# Install Flake8
pip install flake8

# Lint file
flake8 script.py

# Lint directory
flake8 src/

# Configure in setup.cfg
[flake8]
max-line-length = 88
extend-ignore = E203
```

### pytest Testing

```powershell
# Install pytest
pip install pytest

# Run tests
pytest

# Run with coverage
pip install pytest-cov
pytest --cov=src tests/

# Run specific test
pytest tests/test_module.py::test_function
```

## Example Workflows

### Machine Learning Project

```powershell
# Create environment
conda create -n mlproject python=3.11
conda activate mlproject

# Install ML packages
conda install numpy pandas scikit-learn matplotlib
pip install tensorflow

# Create project structure
mkdir src tests data notebooks
```

### Web Scraping Project

```powershell
# Create environment
conda create -n scraper python=3.11
conda activate scraper

# Install packages
pip install requests beautifulsoup4 selenium

# Create requirements.txt
pip freeze > requirements.txt
```

### API Development

```powershell
# Create environment
conda create -n api python=3.11
conda activate api

# Install FastAPI
pip install fastapi uvicorn

# Create main.py
python
# Save as main.py
from fastapi import FastAPI
app = FastAPI()

@app.get("/")
def read_root():
    return {"Hello": "World"}


# Run server
uvicorn main:app --reload
```

## Performance Tips

### Speed Up Package Installation

```powershell
# Use conda-libmamba-solver (faster)
conda install -n base conda-libmamba-solver
conda config --set solver libmamba

# Use pip with no cache (when space is limited)
pip install --no-cache-dir package-name

# Install multiple packages at once
pip install requests flask pandas numpy
```

### Reduce Environment Size

```powershell
# Install only needed packages
conda install --no-deps package-name

# Clean unused packages
conda clean --all

# Use pip instead of conda when possible (smaller)
pip install package-name
```

## Migration from System Python

If you have Python installed system-wide:

1. **Identify installed packages:**
   ```powershell
   pip list > installed-packages.txt
   ```

2. **Enable Naner Python:**
   ```powershell
   # Enable in vendors.json
   .\src\powershell\Setup-NanerVendor.ps1 -VendorId Miniconda
   ```

3. **Create environment and install packages:**
   ```powershell
   conda create -n myenv python=3.11
   conda activate myenv
   pip install -r installed-packages.txt
   ```

4. **Update project configurations** to use Naner's Python

## Related Documentation

- [Project Templates](../home/Templates/README.md) - Python CLI template
- [VS Code Settings](../home/.vscode/settings.json) - Python editor integration
- [Vendor Lock Files](VENDOR-LOCK-FILE.md) - Version control
- [Error Codes](ERROR-CODES.md) - Troubleshooting reference

## References

- [Python Official Documentation](https://docs.python.org/)
- [Miniconda Documentation](https://docs.conda.io/projects/miniconda/)
- [Conda Documentation](https://docs.conda.io/)
- [pip Documentation](https://pip.pypa.io/)

---

**Version:** 1.0
**Last Updated:** 2026-01-07
**Python Version Tested:** 3.11.7 (Miniconda3-latest)
