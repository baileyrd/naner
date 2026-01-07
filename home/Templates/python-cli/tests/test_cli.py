"""Tests for CLI commands."""

import pytest
from click.testing import CliRunner
from {{PROJECT_NAME}}.cli import main


@pytest.fixture
def runner():
    """Create a CLI runner."""
    return CliRunner()


def test_hello_default(runner):
    """Test hello command with default parameters."""
    result = runner.invoke(main, ["hello"])
    assert result.exit_code == 0
    assert "Hello, World!" in result.output


def test_hello_with_name(runner):
    """Test hello command with custom name."""
    result = runner.invoke(main, ["hello", "--name", "Python"])
    assert result.exit_code == 0
    assert "Hello, Python!" in result.output


def test_hello_with_count(runner):
    """Test hello command with count."""
    result = runner.invoke(main, ["hello", "--count", "3"])
    assert result.exit_code == 0
    assert result.output.count("Hello, World!") == 3


def test_info(runner):
    """Test info command."""
    result = runner.invoke(main, ["info"])
    assert result.exit_code == 0
    assert "{{PROJECT_NAME}}" in result.output


def test_list_items(runner):
    """Test list-items command."""
    result = runner.invoke(main, ["list-items", "apple", "banana", "cherry"])
    assert result.exit_code == 0
    assert "apple" in result.output
    assert "banana" in result.output
    assert "cherry" in result.output
