#!/usr/bin/env node

const { spawn } = require('child_process');
const path = require('path');

// MCPサーバープロセスを起動
const serverPath = path.join(__dirname, 'dist', 'index.js');
const server = spawn('node', [serverPath], {
  stdio: ['pipe', 'pipe', 'pipe'],
  cwd: path.dirname(__dirname)
});

let requestId = 1;

// JSON-RPC 2.0リクエストを送信する関数
function sendRequest(method, params = {}) {
  const request = {
    jsonrpc: "2.0",
    id: requestId++,
    method: method,
    params: params
  };
  
  console.log(`📤 Sending: ${method}`);
  server.stdin.write(JSON.stringify(request) + '\n');
}

// サーバーからの応答を処理
server.stdout.on('data', (data) => {
  const lines = data.toString().split('\n').filter(line => line.trim());
  
  lines.forEach(line => {
    try {
      const response = JSON.parse(line);
      console.log(`📨 Response:`, JSON.stringify(response, null, 2));
    } catch (error) {
      console.log(`📄 Output: ${line}`);
    }
  });
});

server.stderr.on('data', (data) => {
  console.log(`❌ Error: ${data.toString()}`);
});

server.on('close', (code) => {
  console.log(`🔚 Server closed with code ${code}`);
  process.exit(code);
});

// シーケンス実行
console.log('🚀 Unity MCP Server Connection Test');
console.log('=====================================');

setTimeout(() => {
  // 1. Initialize
  sendRequest('initialize', {
    protocolVersion: "2024-11-05",
    capabilities: {},
    clientInfo: {
      name: "claude-code-test",
      version: "1.0.0"
    }
  });
}, 100);

setTimeout(() => {
  // 2. Ping test
  sendRequest('tools/call', {
    name: 'ping',
    arguments: {}
  });
}, 500);

setTimeout(() => {
  // 3. Unity info
  sendRequest('tools/call', {
    name: 'unity_info_realtime',
    arguments: {
      category: 'project'
    }
  });
}, 1000);

setTimeout(() => {
  // 4. Create cube test
  sendRequest('tools/call', {
    name: 'create_cube',
    arguments: {
      name: 'ClaudeCodeTestCube',
      position: { x: 0, y: 1, z: 0 },
      color: 'red'
    }
  });
}, 1500);

// 5秒後に終了
setTimeout(() => {
  console.log('\n✅ Test completed');
  server.kill();
}, 5000);