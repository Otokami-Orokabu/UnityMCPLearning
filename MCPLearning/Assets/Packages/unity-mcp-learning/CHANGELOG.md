# Changelog

All notable changes to Unity MCP Learning package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-06-08

### Added
- Initial Unity Package Manager (UPM) distribution support
- Git URL installation capability
- Multi-project support with dynamic port management
- Auto-Accept integration for streamlined development
- Unity MCP Server Manager with UI Toolkit interface
- 8 MCP tools for Unity control and information gathering
- Real-time Unity Console integration
- Compilation monitoring and error detection
- Comprehensive test suite (125 Jest tests + Unity Test Runner)
- Package-based architecture following Unity standards

### Changed
- Migrated from standalone project to Unity Package structure
- Adopted `Server~/` directory pattern following mcp-unity conventions
- Implemented project ID-based data separation for multi-project support
- Enhanced settings system with persistent configuration

### Technical Details
- Unity 6000.0+ requirement
- Node.js 18.0+ requirement
- TypeScript 5.0+ implementation
- JSON-RPC 2.0 over stdio communication
- Modular architecture with 8 specialized modules
- Assembly Definition setup for proper Unity integration

### Package Structure
```
unity-mcp-learning/
├── package.json
├── Scripts/Runtime/
├── Scripts/Editor/  
├── Server~/unity-mcp-node/
├── Tests/Editor/
└── Documentation~/
```

### MCP Tools Included
- Unity Control: create_cube, create_sphere, create_plane, create_gameobject
- Unity Information: unity_info_realtime, get_console_logs, wait_for_compilation
- System: ping

### Documentation
- Complete installation and setup guide
- API reference documentation
- Troubleshooting guide
- Integration examples