#!/usr/bin/env tsx

import { spawn, ChildProcess } from 'child_process';
import * as path from 'path';

interface JsonRpcRequest {
  jsonrpc: string;
  id: number;
  method: string;
  params?: any;
}

interface JsonRpcResponse {
  jsonrpc: string;
  id: number;
  result?: any;
  error?: any;
}

interface UnityInfoClient {
  server: ChildProcess;
  requestId: number;
}

class UnityInfoFetcher {
  private client: UnityInfoClient;

  constructor() {
    const serverPath = path.join(__dirname, 'dist', 'index.js');
    const server = spawn('node', [serverPath], {
      stdio: ['pipe', 'pipe', 'pipe'],
      cwd: path.dirname(__dirname)
    });

    this.client = {
      server,
      requestId: 1
    };

    this.setupEventHandlers();
  }

  private setupEventHandlers(): void {
    this.client.server.stdout.on('data', (data: Buffer) => {
      const lines = data.toString().split('\n').filter(line => line.trim());
      
      lines.forEach(line => {
        try {
          const response: JsonRpcResponse = JSON.parse(line);
          if (response.result?.content) {
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

    this.client.server.stderr.on('data', (data: Buffer) => {
      // Suppress stderr output for cleaner results
    });

    this.client.server.on('close', (code: number | null) => {
      process.exit(code || 0);
    });
  }

  private sendRequest(method: string, params: any = {}): void {
    const request: JsonRpcRequest = {
      jsonrpc: "2.0",
      id: this.client.requestId++,
      method,
      params
    };
    
    this.client.server.stdin?.write(JSON.stringify(request) + '\n');
  }

  async initialize(): Promise<void> {
    return new Promise((resolve) => {
      setTimeout(() => {
        this.sendRequest('initialize', {
          protocolVersion: "2024-11-05",
          capabilities: {},
          clientInfo: {
            name: "unity-info-typescript-client",
            version: "1.0.0"
          }
        });
        resolve();
      }, 100);
    });
  }

  async getUnityInfo(category: string, title: string, delay: number): Promise<void> {
    return new Promise((resolve) => {
      setTimeout(() => {
        console.log(`\n${title}:`);
        this.sendRequest('tools/call', {
          name: 'unity_info_realtime',
          arguments: { category }
        });
        resolve();
      }, delay);
    });
  }

  async run(): Promise<void> {
    console.log('🎮 Unity MCP 情報取得 (TypeScript版)');
    console.log('================================');

    await this.initialize();

    const infoRequests = [
      { category: 'project', title: '📋 プロジェクト情報', delay: 500 },
      { category: 'scene', title: '🎬 シーン情報', delay: 1000 },
      { category: 'gameobjects', title: '🎲 ゲームオブジェクト', delay: 1500 },
      { category: 'editor', title: '🎯 エディター状態', delay: 2000 },
      { category: 'assets', title: '📦 アセット情報', delay: 2500 },
      { category: 'build', title: '🔧 ビルド設定', delay: 3000 }
    ];

    for (const request of infoRequests) {
      await this.getUnityInfo(request.category, request.title, request.delay);
    }

    // 4秒後に終了
    setTimeout(() => {
      this.client.server.kill();
    }, 4000);
  }
}

// 実行
async function main(): Promise<void> {
  const fetcher = new UnityInfoFetcher();
  await fetcher.run();
}

main().catch(console.error);