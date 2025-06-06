# Unity MCP Server

MCP (Model Context Protocol) server for Unity Editor integration with Claude Desktop.

## Overview

This server provides bidirectional communication between Unity Editor and Claude Desktop, enabling real-time information retrieval and command execution.

## Features

- **Real-time Unity Data Monitoring**: Live access to project, scene, and asset information
- **Unity Object Creation**: Create GameObjects, primitives, and other Unity objects
- **Multi-language Support**: English and Japanese error messages and UI
- **Robust Error Handling**: Comprehensive error classification and reporting
- **Configuration Validation**: JSON Schema-based configuration validation
- **Performance Optimization**: Debounced file monitoring for efficiency

## Installation

```bash
npm install
npm run build
```

## Usage

```bash
# Start the server
npm start

# Start with Japanese language
MCP_LANGUAGE=ja npm start

# Start with custom config
MCP_CONFIG_PATH=/path/to/config.json npm start
```

## Development

```bash
# Run tests
npm test

# Generate documentation
npm run docs

# Run with coverage
npm run test:coverage
```

## API Documentation

Generated API documentation is available in the `docs/api` directory after running `npm run docs`.

## License

MIT License - see LICENSE file for details.