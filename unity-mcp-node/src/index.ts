/**
 * @fileoverview Unity MCP Server - Main Entry Point
 * @description MCPプロトコル対応のUnity情報取得・制御サーバー
 * 
 * このモジュールはMCP (Model Context Protocol) サーバーのメインエントリーポイントです。
 * Unity Editorとの双方向通信を提供し、リアルタイムな情報取得とコマンド実行を可能にします。
 * 
 * @author Unity MCP Learning Project
 * @version 1.0.0
 * @since 1.0.0
 * 
 * @example
 * ```bash
 * # サーバーを起動
 * npm start
 * 
 * # 環境変数で言語設定
 * MCP_LANGUAGE=ja npm start
 * 
 * # 設定ファイル指定
 * MCP_CONFIG_PATH=/path/to/config.json npm start
 * ```
 */

import { createInterface } from 'readline';
import { randomUUID } from 'crypto';
import * as path from 'path';
import { ErrorCode, MCPError } from './errors.js';
import { loadAndValidateConfig, MCPConfig } from './config-validator.js';
import { startFileWatching, clearDebounceTimers } from './data-monitor.js';
import { getMessage, setLanguage, getCurrentLanguage } from './i18n.js';
import { 
  JsonRpcRequest, 
  JsonRpcNotification,
  JSON_RPC_ERRORS,
  sendResponse,
  sendErrorResponse,
  sendSuccessResponse,
  validateJsonRpcRequest 
} from './json-rpc.js';
import { MCP_TOOLS, handleToolCall } from './mcp-tools.js';

/**
 * グローバル設定オブジェクト
 * @internal
 */
let globalConfig: MCPConfig;

/**
 * Unityデータディレクトリのパス
 * @internal
 */
let dataPath: string;

/**
 * ログ出力関数（デバッグ用）
 * @param args - ログに出力する引数
 * @internal
 */
function log(...args: any[]) {
  console.error('[MCP Server]', ...args);
}

/**
 * サーバー終了時のリソースクリーンアップ
 * 
 * このメソッドは以下の処理を行います：
 * - ファイル監視のdebounceタイマーをクリア
 * - その他のリソースを適切に解放
 * 
 * @remarks プロセス終了時（SIGINT, SIGTERM）に自動的に呼び出されます
 * @internal
 */
function cleanup(): void {
  log(getMessage('server.cleanup'));
  try {
    clearDebounceTimers();
    log(getMessage('server.cleanup.completed'));
  } catch (error) {
    log(getMessage('server.cleanup.error', { error: error }));
  }
}

// 設定初期化関数
function initializeConfig(): void {
  try {
    // 環境変数から言語設定を読み込み
    const language = process.env.MCP_LANGUAGE as 'en' | 'ja';
    if (language) {
      setLanguage(language);
    }
    
    // 設定ファイルパスを決定
    const configPath = process.env.MCP_CONFIG_PATH || 
                      path.join(__dirname, '..', 'mcp-config.json');
    
    log(getMessage('server.config.loading', { configPath }));
    
    // 設定ファイル検証と読み込み
    globalConfig = loadAndValidateConfig(configPath);
    
    // データパスを設定（設定ファイルのディレクトリを基準とする）
    const configDir = path.dirname(configPath);
    dataPath = path.resolve(configDir, globalConfig.unityDataPath);
    
    log(getMessage('server.config.loaded'));
    log(`- Unity Data Path: ${dataPath}`);
    log(`- Log Level: ${globalConfig.logLevel}`);
    log(`- Unity Command Timeout: ${globalConfig.timeout?.unityCommandTimeout}ms`);
    log(`- Language: ${getCurrentLanguage()}`);
    
  } catch (error: any) {
    if (error instanceof MCPError) {
      log(getMessage('server.config.error', { code: error.code, message: error.message }));
      if (error.context) {
        log('Error context:', error.context);
      }
    } else {
      log('Unexpected configuration error:', error);
    }
    
    // 設定読み込み失敗時は起動を停止
    process.exit(1);
  }
}

// メソッドハンドラー
async function handleMethod(method: string, params: any): Promise<any> {
  switch (method) {
    case 'ping':
      return {
        message: 'pong',
        timestamp: new Date().toISOString(),
        id: randomUUID()
      };

    case 'initialize':
      return {
        protocolVersion: '2024-11-05',
        capabilities: {
          tools: {},
          prompts: {},
          resources: {}
        },
        serverInfo: {
          name: globalConfig.server?.name || 'unity-mcp-server',
          version: globalConfig.server?.version || '1.0.0'
        }
      };

    case 'notifications/initialized':
      // 初期化完了通知 - 特に処理は不要
      return {};

    case 'tools/list':
      return {
        tools: MCP_TOOLS
      };

    case 'tools/call':
      const toolName = params?.name;
      if (!toolName) {
        throw new MCPError(
          ErrorCode.INVALID_PARAMETER,
          'error.tool_name_required'
        );
      }
      
      return await handleToolCall(toolName, params?.arguments, dataPath, globalConfig, log);

    default:
      throw new MCPError(
        ErrorCode.UNKNOWN_ERROR,
        'error.unknown_method',
        { method }
      );
  }
}

// メイン処理
async function main() {
  log(getMessage('server.starting'));
  log('Process ID:', process.pid);
  log('Working directory:', process.cwd());
  
  // 設定ファイル検証と初期化
  initializeConfig();
  
  // ファイル監視開始
  startFileWatching(dataPath, log);
  
  // 標準入力からJSON-RPCメッセージを読み取る
  const rl = createInterface({
    input: process.stdin,
    output: process.stdout,
    terminal: false
  });
  
  rl.on('line', async (line) => {
    try {
      const message = JSON.parse(line);
      const isNotification = !('id' in message);
      
      if (isNotification) {
        // 通知の場合
        const notification = message as JsonRpcNotification;
        log('Received notification:', notification);
        
        try {
          await handleMethod(notification.method, notification.params);
          // 通知にはレスポンスを返さない
        } catch (error: any) {
          log('Error handling notification:', error);
          // 通知のエラーもレスポンスしない
        }
      } else {
        // リクエストの場合
        const request = message as JsonRpcRequest;
        log('Received request:', request);
        
        try {
          // リクエストの基本検証
          validateJsonRpcRequest(request);
          
          // メソッド実行
          const result = await handleMethod(request.method, request.params);
          sendSuccessResponse(request.id, result);
          
        } catch (error: any) {
          log('Error handling request:', error);
          sendErrorResponse(request.id, error);
        }
      }
    } catch (error) {
      log('Failed to parse JSON:', error);
      // パースエラーの場合（idが不明なので0を使用）
      sendResponse({
        jsonrpc: '2.0',
        id: 0,
        error: {
          code: JSON_RPC_ERRORS.PARSE_ERROR,
          message: 'Parse error'
        }
      });
    }
  });
  
  // 標準入力が閉じられたとき
  rl.on('close', () => {
    log('Standard input closed');
    cleanup();
    process.exit(0);
  });
  
  // プロセス終了時の処理
  process.on('SIGINT', () => {
    log('Received SIGINT');
    cleanup();
    process.exit(0);
  });
  
  process.on('SIGTERM', () => {
    log('Received SIGTERM');
    cleanup();
    process.exit(0);
  });
  
  // Keep the process alive
  process.stdin.resume();
}

// サーバーを起動
main().catch((error) => {
  log('Fatal error:', error);
  process.exit(1);
});