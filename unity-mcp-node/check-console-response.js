#!/usr/bin/env node

const { spawn } = require('child_process');
const path = require('path');

console.log('🔧 Checking Console Response');

const mcpServerPath = path.join(__dirname, 'dist', 'index.js');
const mcpProcess = spawn('node', [mcpServerPath], {
  stdio: ['pipe', 'pipe', 'pipe']
});

// タイムアウト設定
const timeout = setTimeout(() => {
  console.error('❌ Timeout');
  mcpProcess.kill();
  process.exit(1);
}, 10000);

let responseReceived = false;

mcpProcess.stdout.on('data', (data) => {
  const lines = data.toString().split('\n');
  
  lines.forEach(line => {
    if (line.trim()) {
      try {
        const message = JSON.parse(line);
        
        if (message.result && message.id === 2 && !responseReceived) {
          responseReceived = true;
          console.log('\n🔍 Full get_console_logs Response:');
          console.log(JSON.stringify(message, null, 2));
          
          if (message.result.content && message.result.content[0]) {
            console.log('\n📝 Console Logs Content:');
            console.log(message.result.content[0].text);
          }
          
          clearTimeout(timeout);
          mcpProcess.kill();
          process.exit(0);
        } else if (message.result && message.id === 1) {
          console.log('✅ Server initialized successfully');
        }
      } catch (e) {
        // JSON解析エラーは無視
      }
    }
  });
});

// 初期化
setTimeout(() => {
  const initMsg = {
    jsonrpc: "2.0",
    method: "initialize",
    params: {
      protocolVersion: "2024-11-05",
      capabilities: { tools: {} },
      clientInfo: { name: "check-console", version: "1.0.0" }
    },
    id: 1
  };
  mcpProcess.stdin.write(JSON.stringify(initMsg) + '\n');
}, 100);

// get_console_logs実行
setTimeout(() => {
  const toolMsg = {
    jsonrpc: "2.0",
    method: "tools/call",
    params: {
      name: "get_console_logs",
      arguments: { filter: "all", limit: 10 }
    },
    id: 2
  };
  mcpProcess.stdin.write(JSON.stringify(toolMsg) + '\n');
}, 500);

mcpProcess.on('error', (error) => {
  console.error('❌ Process error:', error);
});

mcpProcess.stderr.on('data', (data) => {
  // stderrは表示しない（ログが多いため）
});