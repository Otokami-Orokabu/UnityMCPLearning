#!/usr/bin/env node

/**
 * Unity Compilation監視テストスクリプト
 * 
 * 使用方法:
 * node test-compilation.js
 */

const { spawn } = require('child_process');
const path = require('path');

// MCPサーバーのパス解決
const projectRoot = __dirname;
const mcpServerPath = path.join(projectRoot, 'dist', 'index.js');

console.log('🔧 Unity Compilation Test Script');
console.log(`📂 MCP Server Path: ${mcpServerPath}`);

// MCPサーバーを起動
const mcpProcess = spawn('node', [mcpServerPath], {
  stdio: ['pipe', 'pipe', 'pipe']
});

// エラー出力をキャプチャ
let stderrData = '';
mcpProcess.stderr.on('data', (data) => {
  stderrData += data.toString();
  console.error(`[STDERR] ${data}`);
});

// タイムアウト設定
const timeout = setTimeout(() => {
  console.error('❌ Timeout: No response from MCP server');
  mcpProcess.kill();
  process.exit(1);
}, 60000); // 60秒（コンパイル待機のため）

// MCPサーバーからの応答を処理
let buffer = '';
let responseReceived = false;

mcpProcess.stdout.on('data', (data) => {
  buffer += data.toString();
  
  // 改行で分割して処理
  const lines = buffer.split('\n');
  buffer = lines.pop() || ''; // 最後の不完全な行を保持
  
  lines.forEach(line => {
    if (line.trim()) {
      try {
        const message = JSON.parse(line);
        
        if (message.result && message.id === 1) {
          console.log('✅ Server initialized successfully');
        } else if (message.result && message.id === 2 && !responseReceived) {
          responseReceived = true;
          console.log('\n🔍 Compilation Result:');
          
          if (message.result.content && message.result.content[0]) {
            console.log('\n' + '='.repeat(50));
            console.log(message.result.content[0].text);
            console.log('='.repeat(50));
          }
          
          console.log('\n✅ wait_for_compilation test completed successfully!');
          clearTimeout(timeout);
          mcpProcess.kill();
          process.exit(0);
        } else if (message.error && message.id === 2) {
          console.log('\n❌ Error from wait_for_compilation:');
          console.log(JSON.stringify(message.error, null, 2));
          clearTimeout(timeout);
          mcpProcess.kill();
          process.exit(1);
        }
      } catch (e) {
        // JSON解析エラーは無視（部分的なデータの可能性）
      }
    }
  });
});

// MCPプロセスが終了した場合
mcpProcess.on('close', (code) => {
  clearTimeout(timeout);
  if (code !== 0) {
    console.error(`\n❌ MCP server exited with code ${code}`);
    if (stderrData) {
      console.error('Error output:', stderrData);
    }
    process.exit(1);
  }
});

// テストリクエストを送信
console.log('\n📤 Sending wait_for_compilation request...');

// まず初期化メッセージを送信
const initializeMessage = {
  jsonrpc: "2.0",
  method: "initialize",
  params: {
    protocolVersion: "2024-11-05",
    capabilities: {
      tools: {}
    },
    clientInfo: {
      name: "test-compilation",
      version: "1.0.0"
    }
  },
  id: 1
};

mcpProcess.stdin.write(JSON.stringify(initializeMessage) + '\n');

// 少し待ってからツール実行リクエストを送信
setTimeout(() => {
  const toolRequest = {
    jsonrpc: "2.0",
    method: "tools/call",
    params: {
      name: "wait_for_compilation",
      arguments: {
        timeout: 45  // 45秒でタイムアウト
      }
    },
    id: 2
  };
  
  console.log('\n📤 Waiting for compilation to complete...');
  console.log('💡 In Unity, generate a compilation error or success to test this feature.');
  mcpProcess.stdin.write(JSON.stringify(toolRequest) + '\n');
}, 1000);

// エラーハンドリング
process.on('uncaughtException', (error) => {
  console.error('❌ Uncaught exception:', error);
  mcpProcess.kill();
  process.exit(1);
});