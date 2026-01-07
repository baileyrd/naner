# Portable Conda Environments

This directory contains portable conda packages and environments for Miniconda.

## Usage

```bash
# Create environment
conda create -n myenv python=3.11

# Activate environment
conda activate myenv

# Install packages
conda install numpy pandas matplotlib

# List environments
conda env list

# Deactivate
conda deactivate
```

## Popular Packages

```bash
# Data Science
conda install numpy pandas matplotlib seaborn scikit-learn

# Jupyter
conda install jupyter notebook jupyterlab

# Web Development
conda install flask fastapi requests beautifulsoup4

# Machine Learning
conda install tensorflow pytorch torchvision

# Database
conda install sqlalchemy psycopg2
```

## Locations

- Package cache: `%NANER_ROOT%\home\.conda\pkgs`
- Environments: `%NANER_ROOT%\home\.conda\envs`
- User packages: `%NANER_ROOT%\home\.local`

All conda environments are portable and travel with Naner.
