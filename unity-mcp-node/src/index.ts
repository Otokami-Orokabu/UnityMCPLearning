import { createInterface } from 'readline';
import { randomUUID } from 'crypto';
import {
  generateMockUnityData,
  generateDynamicMockData
} from './unity-mock-data.js';

// JSON-RPC 2.0のメッセージタイプ
interface JsonRpcRequest {
  jsonrpc: '2.0';
  id: string | number;
  method: string;
  params?: any;
}

interface JsonRpcNotification {
  jsonrpc: '2.0';
  method: string;
  params?: any;
}

interface JsonRpcResponse {
  jsonrpc: '2.0';
  id: string | number;
  result?: any;
  error?: {
    code: number;
    message: string;
    data?: any;
  };
}

// JSON-RPC エラーコード
const JSON_RPC_ERRORS = {
  PARSE_ERROR: -32700,
  INVALID_REQUEST: -32600,
  METHOD_NOT_FOUND: -32601,
  INVALID_PARAMS: -32602,
  INTERNAL_ERROR: -32603
} as const;

// 標準エラー出力にログを出力（デバッグ用）
function log(...args: any[]) {
  console.error('[MCP Server]', ...args);
}

// JSON-RPCレスポンスを標準出力に送信
function sendResponse(response: JsonRpcResponse) {
  const message = JSON.stringify(response);
  console.log(message);
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
    
    case 'server/info':
      return {
        name: 'unity-mcp-server',
        version: '1.0.0',
        status: 'running',
        uptime: process.uptime(),
        pid: process.pid,
        workingDirectory: process.cwd(),
        nodeVersion: process.version
      };
    
    case 'unity/info':
      // Unityモックデータを取得
      return generateMockUnityData();
    
    case 'unity/info/dynamic':
      // 動的なUnityモックデータを取得（テスト用）
      return generateDynamicMockData();
    
    case 'initialize':
      // プロトコルバージョンの検証
      const clientVersion = params?.protocolVersion;
      if (!clientVersion || clientVersion !== '2024-11-05') {
        throw {
          code: JSON_RPC_ERRORS.INVALID_PARAMS,
          message: 'Unsupported protocol version',
          data: { 
            supported: '2024-11-05', 
            received: clientVersion 
          }
        };
      }
      
      return {
        protocolVersion: '2024-11-05',
        capabilities: {
          tools: {
            listChanged: false
          },
          resources: {},
          prompts: {}
        },
        serverInfo: {
          name: 'unity-mcp-server',
          version: '1.0.0'
        },
        instructions: 'Unity MCP Server for Unity Editor integration'
      };
    
    case 'tools/list':
      return {
        tools: [
          {
            name: 'unity_info',
            description: 'Get Unity project information including scene, ' +
              'gameobjects, and editor details',
            inputSchema: {
              type: 'object',
              properties: {},
              required: []
            }
          },
          {
            name: 'unity_info_dynamic',
            description: 'Get dynamic Unity project information with ' +
              'randomized data for testing',
            inputSchema: {
              type: 'object',
              properties: {},
              required: []
            }
          },
          {
            name: 'ping',
            description: 'Test server connection',
            inputSchema: {
              type: 'object',
              properties: {},
              required: []
            }
          }
        ]
      };
    
    case 'tools/call':
      const toolName = params?.name;
      switch (toolName) {
        case 'unity_info':
          return {
            content: [{
              type: 'text',
              text: JSON.stringify(generateMockUnityData(), null, 2)
            }],
            isError: false
          };
        case 'unity_info_dynamic':
          return {
            content: [{
              type: 'text',
              text: JSON.stringify(generateDynamicMockData(), null, 2)
            }],
            isError: false
          };
        case 'ping':
          return {
            content: [{
              type: 'text',
              text: `Pong! Server is running. Timestamp: ` +
                `${new Date().toISOString()}`
            }],
            isError: false
          };
        default:
          throw {
            code: JSON_RPC_ERRORS.INVALID_PARAMS,
            message: `Unknown tool: ${toolName}`
          };
      }
    
    case 'notifications/initialized':
      // 初期化完了通知（レスポンス不要）
      log('Client initialization completed');
      return null;
    
    default:
      throw {
        code: JSON_RPC_ERRORS.METHOD_NOT_FOUND,
        message: `Method '${method}' not found`
      };
  }
}

// メイン処理
async function main() {
  log('Starting MCP Server...');
  log('Process ID:', process.pid);
  log('Working directory:', process.cwd());
  
  // 標準入力からJSON-RPCメッセージを読み取る
  const rl = createInterface({
    input: process.stdin,
    terminal: false
  });
  
  rl.on('line', async (line) => {
    try {
      if (!line.trim()) {
        return; // 空行は無視
      }
      
      // JSON-RPCメッセージをパース
      const message = JSON.parse(line);
      
      // 通知かリクエストかを判定
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
        
        // リクエストの基本検証
        if (!request.jsonrpc || request.jsonrpc !== '2.0' ||
            !request.method) {
          // idが存在しない場合は0を使用
          const responseId = request.id !== undefined ? request.id : 0;
          sendResponse({
            jsonrpc: '2.0',
            id: responseId,
            error: {
              code: JSON_RPC_ERRORS.INVALID_REQUEST,
              message: 'Invalid request'
            }
          });
          return;
        }
        
        // idが必須（リクエストには必ずidが必要）
        if (request.id === undefined || request.id === null) {
          sendResponse({
            jsonrpc: '2.0',
            id: 0,
            error: {
              code: JSON_RPC_ERRORS.INVALID_REQUEST,
              message: 'Request id is required'
            }
          });
          return;
        }
        
        try {
          const result = await handleMethod(
            request.method,
            request.params
          );

          // 成功レスポンスを送信
          const response = {
            jsonrpc: '2.0' as const,
            id: request.id,
            result
          };
          sendResponse(response);
          log('Sent response for method:', request.method);
        } catch (error: any) {
          // エラーレスポンスを送信
          const errorResponse = {
            jsonrpc: '2.0' as const,
            id: request.id,
            error: error.code ? error : {
              code: JSON_RPC_ERRORS.INTERNAL_ERROR,
              message: 'Internal error',
              data: error.message
            }
          };
          sendResponse(errorResponse);
          log('Sent error response for method:', request.method, error);
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
    process.exit(0);
  });
  
  // プロセス終了時の処理
  process.on('SIGINT', () => {
    log('Received SIGINT');
    process.exit(0);
  });
  
  process.on('SIGTERM', () => {
    log('Received SIGTERM');
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