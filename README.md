# 🔗 Revit MCP Plugin

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Revit 2019-2024](https://img.shields.io/badge/Revit-2019--2024-blue.svg)](https://www.autodesk.com/products/revit/overview)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)

> A powerful Revit plugin that enables AI assistants to interact with Autodesk Revit through the Model Context Protocol (MCP)

## 📋 Table of Contents

- [✨ Features](#-features)
- [📋 Prerequisites](#-prerequisites)
- [🚀 Installation](#-installation)
- [⚙️ Configuration](#️-configuration)
- [🎯 Usage](#-usage)
- [🔧 Custom Commands](#-custom-commands)
- [🏗️ Project Structure](#️-project-structure)
- [🐛 Troubleshooting](#-troubleshooting)
- [🤝 Contributing](#-contributing)
- [📄 License](#-license)

## ✨ Features

- **🤖 AI Integration**: Seamless integration with AI assistants via MCP protocol
- **⚡ Command Management**: Dynamic loading and management of Revit command sets
- **🔌 Socket Communication**: Real-time communication with external MCP services
- **🎛️ Settings Interface**: User-friendly configuration through Revit add-in interface
- **📦 Version Compatibility**: Support for Revit 2019-2024
- **🔧 Extensible Architecture**: Easy development of custom commands

## 📋 Prerequisites

### System Requirements
- **Operating System**: Windows 10/11 (64-bit)
- **Revit Version**: 2019, 2020, 2021, 2022, 2023, or 2024
- **.NET Framework**: 4.8 or higher
- **Memory**: 8GB RAM minimum, 16GB recommended
- **Disk Space**: 500MB free space

### Dependencies
This plugin works in conjunction with:
- **[revit-mcp](https://github.com/revit-mcp/revit-mcp)**: Core MCP service provider
- **[revit-mcp-commandset](https://github.com/revit-mcp/revit-mcp-commandset)**: Command implementations

## 🚀 Installation

### Step 1: Build the Plugin

1. **Clone the repository**:
   ```bash
   git clone https://github.com/neelshah-starline/revit-mcp-plugin.git
   cd revit-mcp-plugin
   ```

2. **Build the solution**:
   - Open `revit-mcp-plugin.sln` in Visual Studio
   - Set build configuration to `Release`
   - Build the solution (`Ctrl+Shift+B`)

3. **Locate the compiled DLL**:
   - Navigate to `revit-mcp-plugin\bin\Release\`
   - Copy `revit-mcp-plugin.dll` to your desired location

### Step 2: Register the Plugin

1. **Create the add-in file** (`revit-mcp-plugin.addin`):
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <RevitAddIns>
     <AddIn Type="Application">
       <Name>revit-mcp</Name>
       <Assembly>%your_path%\revit-mcp-plugin.dll</Assembly>
       <FullClassName>revit_mcp_plugin.Core.Application</FullClassName>
       <ClientId>090A4C8C-61DC-426D-87DF-E4BAE0F80EC1</ClientId>
       <VendorId>revit-mcp</VendorId>
       <VendorDescription></VendorDescription>
     </AddIn>
   </RevitAddIns>
   ```

2. **Install the add-in**:
   - Copy the `.addin` file to one of these locations:
     - **Current User**: `%APPDATA%\Autodesk\Revit\Addins\2024\` (or your Revit version)
     - **All Users**: `%ALLUSERSPROFILE%\Autodesk\Revit\Addins\2024\` (or your Revit version)
   - Replace `%your_path%` with the actual path to your compiled DLL

3. **Restart Revit** to load the plugin

## ⚙️ Configuration

### Initial Setup

1. **Access Settings**:
   - Launch Revit
   - Navigate to **Add-in Modules → Revit MCP Plugin → Settings**

2. **Configure Command Sets**:
   - Click **OpenCommandSetFolder** to open the command sets directory
   - Place your command set folders in this location

### Command Set Structure

A typical command set should follow this structure:

```
MyCommandSet/
├── 2019/                          # Revit 2019 compatible DLL
├── 2020/                          # Revit 2020 compatible DLL
├── 2021/                          # Revit 2021 compatible DLL
├── 2022/                          # Revit 2022 compatible DLL
├── 2023/                          # Revit 2023 compatible DLL
├── 2024/                          # Revit 2024 compatible DLL
└── command.json                   # Command configuration file
```

3. **Enable Commands**:
   - Check the boxes for commands you want to load
   - Click **Save** to apply changes

## 🎯 Usage

### Enable MCP Service

1. **Start the Service**:
   - Go to **Add-in → Revit MCP Plugin → Revit MCP Switch**
   - Toggle the service **ON**

2. **AI Integration**:
   - Your AI assistant can now discover and control your Revit instance
   - The plugin will appear as an available MCP server

### Important Notes

- **⚠️ Configuration Changes**: If you modify command configurations after enabling the service, restart Revit for changes to take effect
- **🔒 Service State**: The service state persists between Revit sessions
- **📊 Real-time Updates**: Command availability updates dynamically when service is active

## 🔧 Custom Commands

### Development Overview

Custom commands extend the plugin's functionality. Refer to the [revit-mcp-commandset](https://github.com/revit-mcp/revit-mcp-commandset) project for detailed development guidelines.

### Quick Start

1. **Create Command Class**:
   ```csharp
   public class MyCustomCommand : RevitCommandBase
   {
       public override void Execute(ExternalCommandData commandData)
       {
           // Your command implementation
       }
   }
   ```

2. **Register Command**:
   - Implement `IExternalCommand` interface
   - Add command configuration to `command.json`
   - Build for target Revit versions

## 🏗️ Project Structure

```
revit-mcp-plugin/
├── Configuration/                 # Configuration management
│   ├── CommandConfig.cs          # Command configuration models
│   ├── ConfigurationManager.cs   # Configuration loading/saving
│   ├── DeveloperInfo.cs          # Developer information
│   ├── FrameworkConfig.cs        # Framework settings
│   └── ServiceSettings.cs        # Service configuration
│
├── Core/                         # Core functionality
│   ├── Application.cs            # Plugin entry point
│   ├── CommandExecutor.cs        # Command execution engine
│   ├── CommandManager.cs         # Command lifecycle management
│   ├── ExternalEventManager.cs   # Revit external events
│   ├── MCPServiceConnection.cs   # MCP protocol handling
│   ├── RevitCommandRegistry.cs   # Command registration
│   ├── Settings.cs               # Settings interface trigger
│   └── SocketService.cs          # Network communication
│
├── UI/                           # User interface components
│   ├── SettingsWindow.xaml       # Main settings window
│   ├── SettingsWindow.xaml.cs   # Settings window logic
│   └── CommandSetSettingsPage.xaml # Command configuration UI
│
└── Utils/                        # Utility classes
    ├── Logger.cs                 # Logging functionality
    └── PathManager.cs            # Path management utilities
```

## 🐛 Troubleshooting

### Common Issues

**❌ Plugin doesn't load**
- Verify the `.addin` file is in the correct location
- Check that the DLL path in the `.addin` file is correct
- Ensure Revit version compatibility
- Check Windows Event Viewer for error details

**❌ Commands don't appear**
- Verify command set folder structure
- Check `command.json` syntax
- Ensure compatible DLLs are present for your Revit version
- Restart Revit after configuration changes

**❌ MCP service not discovered**
- Confirm the service is enabled in Revit
- Check firewall settings for network communication
- Verify MCP client configuration

**❌ Command execution fails**
- Check Revit API compatibility
- Verify command permissions
- Review Revit Journal files for errors
- Enable logging for detailed error information

### Getting Help

1. **Enable Logging**:
   - Check **Enable Logging** in settings
   - Review log files in the plugin directory

2. **Check Revit Journal**:
   - Located in `%APPDATA%\Autodesk\Revit\Journals\`
   - Look for errors related to the plugin

3. **Community Support**:
   - [GitHub Issues](https://github.com/neelshah-starline/revit-mcp-plugin/issues)
   - [Discussions](https://github.com/neelshah-starline/revit-mcp-plugin/discussions)

## 🤝 Contributing

We welcome contributions! Here's how you can help:

### Development Setup

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Make your changes**
4. **Add tests** for new functionality
5. **Ensure all tests pass**
6. **Commit your changes**: `git commit -m 'Add amazing feature'`
7. **Push to branch**: `git push origin feature/amazing-feature`
8. **Open a Pull Request**

### Contribution Guidelines

- **📝 Code Style**: Follow existing code patterns and conventions
- **🧪 Testing**: Add tests for new features
- **📚 Documentation**: Update documentation for API changes
- **🔄 Compatibility**: Maintain backward compatibility when possible
- **🐛 Bug Reports**: Use GitHub Issues with detailed information

### Areas for Contribution

- 🔧 Bug fixes and performance improvements
- ✨ New features and command implementations
- 📚 Documentation and usage examples
- 🧪 Test coverage expansion
- 🌐 Localization support

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

**Built with ❤️ for the Revit and AI community**

[⭐ Star this project](https://github.com/neelshah-starline/revit-mcp-plugin) |
[🐛 Report issues](https://github.com/neelshah-starline/revit-mcp-plugin/issues) |
[💬 Start discussion](https://github.com/neelshah-starline/revit-mcp-plugin/discussions)

</div>
