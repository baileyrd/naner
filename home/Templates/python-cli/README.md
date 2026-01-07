# {{PROJECT_NAME}}

A CLI tool built with Python, created with Naner.

## Getting Started

```bash
# Install in development mode
pip install -e .

# Or install with dev dependencies
pip install -e ".[dev]"

# Run the CLI
{{PROJECT_NAME}} --help
```

## Tech Stack

- **Click** - CLI framework
- **Rich** - Beautiful terminal output
- **pytest** - Testing framework

## Usage

### Hello Command
```bash
# Basic greeting
{{PROJECT_NAME}} hello

# Greet someone specific
{{PROJECT_NAME}} hello --name Python

# Multiple greetings
{{PROJECT_NAME}} hello --name World --count 3
```

### Info Command
```bash
{{PROJECT_NAME}} info
```

### List Items Command
```bash
{{PROJECT_NAME}} list-items apple banana cherry
```

## Project Structure

```
{{PROJECT_NAME}}/
├── src/
│   └── {{PROJECT_NAME}}/
│       ├── __init__.py      # Package initialization
│       └── cli.py           # CLI commands
├── tests/
│   └── test_cli.py          # Tests
├── pyproject.toml           # Project configuration
├── .gitignore
└── README.md
```

## Development

### Running Tests
```bash
pytest
```

### Code Formatting
```bash
black src/ tests/
```

### Linting
```bash
flake8 src/ tests/
```

## Adding Commands

Edit `src/{{PROJECT_NAME}}/cli.py` and add new commands using the `@main.command()` decorator:

```python
@main.command()
@click.option("--option", help="An option")
def mycommand(option):
    """Description of my command."""
    console.print(f"[bold]Running mycommand with {option}[/bold]")
```

## Learn More

- [Click Documentation](https://click.palletsprojects.com/)
- [Rich Documentation](https://rich.readthedocs.io/)
- [pytest Documentation](https://docs.pytest.org/)
