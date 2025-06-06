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
  
  server.stdin.write(JSON.stringify(request) + '\n');
}

// サーバーからの応答を処理
server.stdout.on('data', (data) => {
  const lines = data.toString().split('\n').filter(line => line.trim());
  
  lines.forEach(line => {
    try {
      const response = JSON.parse(line);
      if (response.result && response.result.content) {
        const content = response.result.content[0];
        if (content.type === 'text') {
          console.log(content.text);
        }
      }
    } catch (error) {
      // Ignore parsing errors for debug output
    }
  });
});

server.stderr.on('data', (data) => {
  // Suppress stderr output for cleaner results
});

server.on('close', (code) => {
  process.exit(code);
});

// シーケンス実行
console.log('🎮 Unity MCP 情報取得');
console.log('==================');

setTimeout(() => {
  // 1. Initialize
  sendRequest('initialize', {
    protocolVersion: "2024-11-05",
    capabilities: {},
    clientInfo: {
      name: "unity-info-client",
      version: "1.0.0"
    }
  });
}, 100);

setTimeout(() => {
  console.log('\n📋 プロジェクト情報:');
  sendRequest('tools/call', {
    name: 'unity_info_realtime',
    arguments: {
      category: 'project'
    }
  });
}, 500);

setTimeout(() => {
  console.log('\n🎬 シーン情報:');
  sendRequest('tools/call', {
    name: 'unity_info_realtime',
    arguments: {
      category: 'scene'
    }
  });
}, 1000);

setTimeout(() => {
  console.log('\n🎲 ゲームオブジェクト:');
  sendRequest('tools/call', {
    name: 'unity_info_realtime',
    arguments: {
      category: 'gameobjects'
    }
  });
}, 1500);

setTimeout(() => {
  console.log('\n🎯 エディター状態:');
  sendRequest('tools/call', {
    name: 'unity_info_realtime',
    arguments: {
      category: 'editor'
    }
  });
}, 2000);

setTimeout(() => {
  console.log('\n📦 アセット情報:');
  sendRequest('tools/call', {
    name: 'unity_info_realtime',
    arguments: {
      category: 'assets'
    }
  });
}, 2500);

setTimeout(() => {
  console.log('\n🔧 ビルド設定:');
  sendRequest('tools/call', {
    name: 'unity_info_realtime',
    arguments: {
      category: 'build'
    }
  });
}, 3000);

// 4秒後に終了
setTimeout(() => {
  server.kill();
}, 4000);