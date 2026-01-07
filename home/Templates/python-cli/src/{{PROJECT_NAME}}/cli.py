"""Main CLI interface for {{PROJECT_NAME}}."""

import click
from rich.console import Console
from rich.table import Table

console = Console()


@click.group()
@click.version_option(version="0.1.0")
def main():
    """{{PROJECT_NAME}} - A powerful CLI tool."""
    pass


@main.command()
@click.option("--name", default="World", help="Name to greet")
@click.option("--count", default=1, help="Number of greetings")
def hello(name, count):
    """Greet someone."""
    for _ in range(count):
        console.print(f"[bold green]Hello, {name}![/bold green]")


@main.command()
def info():
    """Display project information."""
    table = Table(title="{{PROJECT_NAME}} Info")

    table.add_column("Property", style="cyan")
    table.add_column("Value", style="magenta")

    table.add_row("Name", "{{PROJECT_NAME}}")
    table.add_row("Version", "0.1.0")
    table.add_row("Description", "A CLI tool built with Python")

    console.print(table)


@main.command()
@click.argument("items", nargs=-1)
def list_items(items):
    """List items provided as arguments."""
    if not items:
        console.print("[yellow]No items provided![/yellow]")
        return

    console.print("[bold]Items:[/bold]")
    for i, item in enumerate(items, 1):
        console.print(f"  {i}. {item}")


if __name__ == "__main__":
    main()
