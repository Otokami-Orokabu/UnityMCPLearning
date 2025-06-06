#!/usr/bin/env node

/**
 * Unity Console Logs取得テストスクリプト
 * 
 * 使用方法:
 * node test-console-logs.js
 */

const { spawn } = require('child_process');
const path = require('path');

// MCPサーバーのパス解決
const projectRoot = __dirname;
const mcpServerPath = path.join(projectRoot, 'dist', 'index.js');

console.log('🔧 Unity Console Logs Test Script');
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
}, 30000); // 30秒

// MCPサーバーからの応答を処理
let buffer = '';
mcpProcess.stdout.on('data', (data) => {
  buffer += data.toString();
  
  // 改行で分割して処理
  const lines = buffer.split('\n');
  buffer = lines.pop() || ''; // 最後の不完全な行を保持
  
  lines.forEach(line => {
    if (line.trim()) {
      try {
        const message = JSON.parse(line);
        
        if (message.result) {
          console.log('\n✅ Response received from MCP server');
          
          // Console Logsのフォーマット済みテキストを表示
          if (message.result.content && message.result.content[0]) {
            console.log('\n' + '='.repeat(50));
            console.log(message.result.content[0].text);
            console.log('='.repeat(50));
          }
          
          clearTimeout(timeout);
          mcpProcess.kill();
          process.exit(0);
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
console.log('\n📤 Sending get_console_logs request...');

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
      name: "test-console-logs",
      version: "1.0.0"
    }
  },
  id: 1
};

mcpProcess.stdin.write(JSON.stringify(initializeMessage) + '\n');

// 少し待ってからツール実行リクエストを送信
setTimeout(() => {
  // 異なるフィルターオプションでテスト
  const testRequests = [
    {
      jsonrpc: "2.0",
      method: "tools/call",
      params: {
        name: "get_console_logs",
        arguments: {
          filter: "all",
          limit: 20
        }
      },
      id: 2
    }
  ];
  
  testRequests.forEach((request, index) => {
    setTimeout(() => {
      console.log(`\n📤 Testing with filter: ${request.params.arguments.filter}`);
      mcpProcess.stdin.write(JSON.stringify(request) + '\n');
    }, index * 100);
  });
}, 1000);

// エラーハンドリング
process.on('uncaughtException', (error) => {
  console.error('❌ Uncaught exception:', error);
  mcpProcess.kill();
  process.exit(1);
});