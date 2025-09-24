# Contributing to Vivaldi Mod Manager

Thank you for your interest in contributing to Vivaldi Mod Manager! This document provides guidelines and information for contributors.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Contributing Process](#contributing-process)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Documentation](#documentation)
- [Issue Guidelines](#issue-guidelines)
- [Pull Request Guidelines](#pull-request-guidelines)

## Code of Conduct

By participating in this project, you agree to abide by our Code of Conduct. Please be respectful and constructive in all interactions.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally
3. **Create a branch** for your contribution
4. **Make your changes**
5. **Test your changes**
6. **Submit a pull request**

## Development Setup

### Prerequisites

- Windows 10 (1903+) or Windows 11
- .NET 8 SDK or later
- Visual Studio 2022 or Visual Studio Code with C# extension
- Git for version control

### Setting up the Development Environment

1. Clone the repository:
   ```bash
   git clone https://github.com/tomasforsman/Vivaldi-Mod-Manager.git
   cd Vivaldi-Mod-Manager
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the project:
   ```bash
   dotnet build
   ```

4. Run tests:
   ```bash
   dotnet test
   ```

## Contributing Process

### Before You Start

- Check existing issues and pull requests to avoid duplicates
- For large changes, consider opening an issue first to discuss the approach
- Ensure you understand the project's architecture (see Software-Requirements-Specification.md)

### Making Changes

1. **Create a feature branch** from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes** following the coding standards
3. **Write or update tests** for your changes
4. **Update documentation** if necessary
5. **Commit your changes** with clear, descriptive messages

### Commit Message Format

Use clear, descriptive commit messages:

```
type(scope): brief description

Longer description if necessary

Fixes #123
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

## Coding Standards

### C# Code Style

- Follow the .editorconfig settings in the repository
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Follow SOLID principles
- Handle exceptions appropriately
- Use async/await patterns for I/O operations

### File Organization

- Place classes in appropriate namespaces
- One class per file (with exceptions for small helper classes)
- Use consistent file and folder naming conventions

### Error Handling

- Use structured logging
- Provide meaningful error messages
- Handle edge cases gracefully
- Don't swallow exceptions without logging

## Testing

### Test Requirements

- Write unit tests for new functionality
- Update existing tests when modifying code
- Ensure all tests pass before submitting PR
- Aim for good test coverage of critical paths

### Test Categories

1. **Unit Tests**: Test individual components in isolation
2. **Integration Tests**: Test component interactions
3. **UI Tests**: Test user interface functionality (when applicable)

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test path/to/test/project
```

## Documentation

### Code Documentation

- Add XML documentation comments for public APIs
- Include code examples in documentation when helpful
- Update README.md for user-facing changes

### Architecture Documentation

- Update Software-Requirements-Specification.md for architectural changes
- Document design decisions and rationale
- Include diagrams for complex interactions

## Issue Guidelines

### Before Creating an Issue

- Search existing issues to avoid duplicates
- Check if the issue is already fixed in the latest version
- Gather all relevant information

### Creating a Good Issue

- Use the provided issue templates
- Include system information (Windows version, Vivaldi version, etc.)
- Provide steps to reproduce for bugs
- Include logs or error messages when relevant
- Be clear and concise in your description

## Pull Request Guidelines

### Before Submitting

- Ensure your code follows the coding standards
- Run tests and ensure they pass
- Update documentation if necessary
- Rebase your branch on the latest main branch

### Pull Request Description

- Use the provided PR template
- Clearly describe what changes you made and why
- Reference related issues
- Include testing information
- Add screenshots for UI changes

### Review Process

- Maintainers will review your PR
- Address feedback promptly and professionally
- Be prepared to make changes based on review comments
- Once approved, a maintainer will merge your PR

## Areas for Contribution

We welcome contributions in these areas:

### High Priority
- Bug fixes
- Performance improvements
- Test coverage improvements
- Documentation improvements

### Medium Priority
- New features (discuss first in issues)
- UI/UX improvements
- Code refactoring
- Additional platform support

### Future Enhancements
- Mod marketplace integration
- Advanced debugging tools
- Multi-profile support
- Cross-platform support

## Getting Help

If you need help:

1. Check the documentation and existing issues
2. Open a discussion on GitHub
3. Ask questions in your PR or issue
4. Contact the maintainers directly for sensitive matters

## Recognition

Contributors will be recognized in:
- Release notes for significant contributions
- Contributors section of the README
- GitHub contributors page

Thank you for contributing to Vivaldi Mod Manager!