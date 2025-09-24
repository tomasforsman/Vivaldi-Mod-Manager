# Vivaldi Mod Manager

[![CI](https://github.com/tomasforsman/Vivaldi-Mod-Manager/actions/workflows/ci.yml/badge.svg)](https://github.com/tomasforsman/Vivaldi-Mod-Manager/actions/workflows/ci.yml)
[![CodeQL](https://github.com/tomasforsman/Vivaldi-Mod-Manager/actions/workflows/codeql.yml/badge.svg)](https://github.com/tomasforsman/Vivaldi-Mod-Manager/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Vivaldi Mod Manager is a Windows application and service that lets users install, enable, and manage custom JavaScript modifications for the Vivaldi browser. It ensures mods persist across browser updates through automatic injection, monitoring, and healing, with backup, restore, Safe Mode, and an intuitive user interface.

## ⚠️ Disclaimer

This project is not affiliated with or endorsed by Vivaldi Technologies. Use at your own risk; browser modifications may break after browser updates or cause unexpected behavior.

## ✨ Features

- **Centralized Mod Management**: Organize all your Vivaldi mods in one location
- **Automatic Update Handling**: Mods persist across Vivaldi browser updates
- **Safe Mode**: Quickly disable all mods and restore clean Vivaldi state
- **Backup & Recovery**: Automatic backups of modified files with rollback capability
- **Real-time Monitoring**: File system monitoring for changes and healing
- **User-friendly UI**: WPF desktop application with system tray integration
- **Command Line Interface**: Headless operations for power users
- **Mod Ordering**: Control the load order of your modifications
- **Compatibility Tracking**: Track which Vivaldi versions work with your mods

## 🚀 Quick Start

### Prerequisites

- Windows 10 (1903+) or Windows 11
- .NET 8 Runtime
- Vivaldi Browser
- Administrator privileges (for initial setup and injection)

### Installation

1. Download the latest release from [Releases](https://github.com/tomasforsman/Vivaldi-Mod-Manager/releases)
2. Extract the archive to your preferred location
3. Run `VivaldiModManager.exe` as Administrator (first time only)
4. Follow the setup wizard to configure your mods directory
5. Start adding your JavaScript mods!

## 📁 Project Structure

```
Vivaldi-Mod-Manager/
├── .github/                    # GitHub configuration and workflows
│   ├── workflows/             # CI/CD workflows
│   ├── ISSUE_TEMPLATE/        # Issue templates
│   ├── CODEOWNERS            # Code ownership definitions
│   └── pull_request_template.md
├── src/                       # Source code (to be created)
│   ├── VivaldiModManager.Core/    # Core business logic
│   ├── VivaldiModManager.Service/ # Background service
│   ├── VivaldiModManager.UI/      # WPF user interface
│   └── VivaldiModManager.CLI/     # Command line interface
├── tests/                     # Test projects (to be created)
├── docs/                      # Additional documentation
├── .editorconfig             # Code style configuration
├── .gitignore               # Git ignore rules
├── LICENSE                  # MIT License
├── README.md               # This file
├── CONTRIBUTING.md         # Contribution guidelines
├── SECURITY.md            # Security policy
└── Software-Requirements-Specification.md
```

## 🏗️ Architecture Overview

The application consists of three main components:

### Service Component
- **Background Process**: Runs continuously to monitor file changes
- **Auto-healing**: Detects and fixes broken mod injections
- **Update Detection**: Monitors for new Vivaldi versions
- **File System Monitoring**: Watches mods directory and Vivaldi installation

### Application Component  
- **WPF Desktop UI**: User-friendly interface for mod management
- **System Tray Integration**: Quick access and status monitoring
- **Mod Management**: Enable, disable, reorder, and configure mods
- **Log Viewer**: Review system logs and troubleshoot issues

### Data Layer
- **Manifest Management**: JSON-based mod metadata storage
- **Backup System**: Automatic backups of modified Vivaldi files
- **Configuration**: User preferences and system settings
- **Logging**: Structured logging for debugging and monitoring

## 🔧 Development

### Building from Source

```bash
# Clone the repository
git clone https://github.com/tomasforsman/Vivaldi-Mod-Manager.git
cd Vivaldi-Mod-Manager

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run tests
dotnet test
```

### Development Environment

- **IDE**: Visual Studio 2022 or VS Code with C# extension
- **Framework**: .NET 8
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Testing**: xUnit, Moq, FluentAssertions
- **CI/CD**: GitHub Actions

## 📖 Documentation

- [Software Requirements Specification](Software-Requirements-Specification.md) - Detailed technical requirements
- [Contributing Guidelines](CONTRIBUTING.md) - How to contribute to the project
- [Security Policy](SECURITY.md) - Security considerations and reporting
- [Code of Conduct](CODE_OF_CONDUCT.md) - Community guidelines

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details on:

- Setting up the development environment
- Code style and standards
- Testing requirements
- Pull request process

## 🔒 Security

Security is important to us. Please review our [Security Policy](SECURITY.md) for:

- Supported versions
- Reporting vulnerabilities
- Security best practices

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Vivaldi Technologies for creating an amazing browser
- The open-source community for inspiration and tools
- Contributors who help make this project better

## 📞 Support

- 📋 [Report Issues](https://github.com/tomasforsman/Vivaldi-Mod-Manager/issues)
- 💬 [Discussions](https://github.com/tomasforsman/Vivaldi-Mod-Manager/discussions)
- 📚 [Documentation](https://github.com/tomasforsman/Vivaldi-Mod-Manager/blob/main/README.md)
