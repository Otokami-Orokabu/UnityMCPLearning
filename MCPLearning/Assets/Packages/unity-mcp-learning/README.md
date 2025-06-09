# Unity MCP Learning

AI-driven Unity development environment using Model Context Protocol (MCP). Enables natural language control of Unity Editor through Claude Desktop/Code integration.

## Features

- **Natural Language Unity Control**: Create GameObjects using simple commands like "create a cube"
- **Real-time Unity Information**: Get project, scene, and GameObject data instantly
- **Unity Console Integration**: Access and filter Unity Console logs via AI
- **Compilation Monitoring**: Real-time compilation status and error detection
- **Multi-project Support**: Handle multiple Unity projects simultaneously
- **Auto-Accept Integration**: Streamlined development workflow

## Quick Start

1. **Install Package**
   ```
   Window > Package Manager > + > Add package from git URL
   https://github.com/orlab/UnityMCPLearning.git?path=MCPLearning/Assets/Packages/unity-mcp-learning
   ```

2. **Setup MCP Server**
   - Open Unity MCP Server Manager: `Window > Unity MCP > Server Manager`
   - Click "Auto Setup" to configure everything automatically
   - Click "Start Server" to begin

3. **Configure Claude Desktop**
   - The Auto Setup will generate the configuration for you
   - Or manually add to `claude_desktop_config.json`:
   ```json
   {
     "mcpServers": {
       "unity-mcp": {
         "command": "node",
         "args": ["path/to/Server~/unity-mcp-node/dist/index.js"]
       }
     }
   }
   ```

4. **Start Development**
   - Open Claude Desktop or Claude Code
   - Try: `ping` to test connection
   - Try: `create a cube` to create a GameObject
   - Try: `unity_info_realtime` to get project information

## Available MCP Tools

### Unity Control
- `create_cube` - Create cube GameObject
- `create_sphere` - Create sphere GameObject  
- `create_plane` - Create plane GameObject
- `create_gameobject` - Create empty GameObject

### Unity Information
- `unity_info_realtime` - Get real-time Unity project information
- `get_console_logs` - Get Unity Console logs with filtering
- `wait_for_compilation` - Wait for Unity compilation to complete

### System
- `ping` - Test MCP server connection

## Requirements

- Unity 6000.0 or later
- Node.js 18.0 or later
- Claude Desktop (with MCP support) or Claude Code CLI

## Documentation

- [Installation Guide](Documentation~/Installation.md)
- [API Reference](Documentation~/API.md)
- [Troubleshooting](Documentation~/Troubleshooting.md)

## License

MIT License - see [LICENSE](https://github.com/orlab/UnityMCPLearning/blob/main/LICENSE)

## Support

For issues and feature requests, please visit the [GitHub repository](https://github.com/orlab/UnityMCPLearning).