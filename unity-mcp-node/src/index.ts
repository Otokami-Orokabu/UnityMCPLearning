import { createInterface } from 'readline';
import { randomUUID } from 'crypto';
import * as fs from 'fs';
import * as path from 'path';

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

// ファイル監視用の変数 - 環境変数または設定から取得
const getUnityDataPath = () => {
  // 環境変数から取得を試行
  if (process.env.UNITY_MCP_DATA_PATH) {
    return path.resolve(process.env.UNITY_MCP_DATA_PATH);
  }
  
  // 設定ファイルから取得を試行
  try {
    const configPath = path.join(__dirname, '..', 'mcp-config.json');
    if (fs.existsSync(configPath)) {
      const config = JSON.parse(fs.readFileSync(configPath, 'utf-8'));
      if (config.unityDataPath) {
        // 設定ファイルの場所を基準にして相対パスを解決
        const configDir = path.dirname(configPath);
        const resolvedPath = path.resolve(configDir, '..', config.unityDataPath);
        log(`Config path resolved: ${config.unityDataPath} -> ${resolvedPath}`);
        return resolvedPath;
      }
    }
  } catch (error) {
    log('Config file read error:', error);
  }
  
  // フォールバック: 相対パスから推測
  const fallbackPath = path.resolve(process.cwd(), 'MCPLearning/UnityMCP/Data');
  log(`Using fallback path: ${fallbackPath}`);
  return fallbackPath;
};

const dataPath = getUnityDataPath();
let cachedData: { [key: string]: any } = {};

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
            name: 'unity_info_realtime',
            description: 'Get real-time Unity project information from JSON files',
            inputSchema: {
              type: 'object',
              properties: {
                category: {
                  type: 'string',
                  enum: ['project', 'scene', 'gameobjects', 'assets', 'build', 'editor', 'all'],
                  description: 'Information category to retrieve'
                }
              },
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
        case 'unity_info_realtime':
          const category = params?.arguments?.category || 'all';
          
          // データの存在確認
          const hasData = Object.keys(cachedData).length > 0;
          log(`Unity data check: hasData=${hasData}, keys=${Object.keys(cachedData)}, dataPath=${dataPath}`);
          
          if (!hasData) {
            // 強制的にデータ再読み込みを試行
            loadAllData();
            const hasDataAfterReload = Object.keys(cachedData).length > 0;
            log(`After reload: hasData=${hasDataAfterReload}, keys=${Object.keys(cachedData)}`);
            
            if (!hasDataAfterReload) {
              return {
                content: [{
                  type: 'text',
                  text: `Unity project data is not available. Data path: ${path.resolve(dataPath)}. Please ensure Unity editor has been opened and MCP export scripts are running.`
                }],
                isError: false
              };
            }
          }

          if (category === 'all') {
            return {
              content: [{
                type: 'text',
                text: `# Unity Project Information (MCPLearning)\n\n${JSON.stringify(cachedData, null, 2)}`
              }],
              isError: false
            };
          } else {
            const categoryMap: { [key: string]: string } = {
              'project': 'project_info',
              'scene': 'scene_info',
              'gameobjects': 'gameobjects',
              'assets': 'assets_info',
              'build': 'build_info',
              'editor': 'editor_state'
            };
            const dataKey = categoryMap[category];
            const data = cachedData[dataKey];
            
            if (!data) {
              return {
                content: [{
                  type: 'text',
                  text: `No data found for category: ${category}. Available categories: ${Object.keys(categoryMap).join(', ')}`
                }],
                isError: false
              };
            }
            
            return {
              content: [{
                type: 'text',
                text: `# Unity ${category.charAt(0).toUpperCase() + category.slice(1)} Information\n\n${JSON.stringify(data, null, 2)}`
              }],
              isError: false
            };
          }
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

// データ監視開始
function startFileWatching() {
  const fullPath = path.resolve(dataPath);
  if (fs.existsSync(fullPath)) {
    log(`Watching Unity data directory: ${fullPath}`);
    
    fs.watch(fullPath, { recursive: false }, (eventType, filename) => {
      if (filename && filename.endsWith('.json')) {
        log(`Unity data file changed: ${filename}`);
        loadDataFile(filename);
      }
    });
    
    // 初期データ読み込み
    loadAllData();
  } else {
    log(`Unity data directory not found: ${fullPath}`);
  }
}

// データファイル読み込み
function loadDataFile(filename: string) {
  try {
    const filePath = path.join(path.resolve(dataPath), filename);
    if (fs.existsSync(filePath)) {
      const content = fs.readFileSync(filePath, 'utf-8');
      const rawData = JSON.parse(content);
      
      // Unity側のSerializableDictフォーマット（items配列）を通常のオブジェクトに変換
      let data = rawData;
      if (rawData.items && Array.isArray(rawData.items)) {
        data = {};
        rawData.items.forEach((item: any) => {
          if (item.key && item.value !== undefined) {
            data[item.key] = item.value;
          }
        });
      }
      
      const key = filename.replace('.json', '').replace('-', '_');
      cachedData[key] = data;
      log(`Loaded ${filename}: ${Object.keys(data).length} properties`);
    }
  } catch (error) {
    log(`Error loading ${filename}:`, error);
  }
}

// 全データ読み込み
function loadAllData() {
  const files = ['project-info.json', 'scene-info.json', 'gameobjects.json', 
                 'assets-info.json', 'build-info.json', 'editor-state.json'];
  
  files.forEach(loadDataFile);
}

// メイン処理
async function main() {
  log('Starting MCP Server...');
  log('Process ID:', process.pid);
  log('Working directory:', process.cwd());
  
  // ファイル監視開始
  startFileWatching();
  
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