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

// Unity コマンド実行関連
const getUnityCommandPath = () => {
  // Unityコマンドディレクトリのパス取得
  const basePath = getUnityDataPath();
  return path.resolve(path.dirname(basePath), 'Commands');
};

const COMMAND_FILE = 'command-queue.json';
const RESULT_FILE = 'command-result.json';

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
            name: 'create_cube',
            description: 'Create a cube in Unity scene',
            inputSchema: {
              type: 'object',
              properties: {
                name: {
                  type: 'string',
                  description: 'Name of the cube',
                  default: 'Cube'
                },
                position: {
                  type: 'object',
                  properties: {
                    x: { type: 'number', default: 0 },
                    y: { type: 'number', default: 0 },
                    z: { type: 'number', default: 0 }
                  },
                  description: 'Position in 3D space'
                },
                scale: {
                  type: 'object',
                  properties: {
                    x: { type: 'number', default: 1 },
                    y: { type: 'number', default: 1 },
                    z: { type: 'number', default: 1 }
                  },
                  description: 'Scale in 3D space'
                },
                color: {
                  type: 'string',
                  enum: ['red', 'green', 'blue', 'yellow', 'magenta', 'cyan', 'white', 'black'],
                  description: 'Color of the cube',
                  default: 'white'
                }
              },
              required: []
            }
          },
          {
            name: 'create_sphere',
            description: 'Create a sphere in Unity scene',
            inputSchema: {
              type: 'object',
              properties: {
                name: {
                  type: 'string',
                  description: 'Name of the sphere',
                  default: 'Sphere'
                },
                position: {
                  type: 'object',
                  properties: {
                    x: { type: 'number', default: 0 },
                    y: { type: 'number', default: 0 },
                    z: { type: 'number', default: 0 }
                  },
                  description: 'Position in 3D space'
                },
                scale: {
                  type: 'object',
                  properties: {
                    x: { type: 'number', default: 1 },
                    y: { type: 'number', default: 1 },
                    z: { type: 'number', default: 1 }
                  },
                  description: 'Scale in 3D space'
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
        case 'create_cube':
          return await executeUnityCommand('create_cube', params?.arguments);
        
        case 'create_sphere':
          return await executeUnityCommand('create_sphere', params?.arguments);

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

// Unity コマンド実行
async function executeUnityCommand(commandType: string, args: any): Promise<any> {
  const commandId = randomUUID();
  
  try {
    // 入力検証
    if (!commandType || typeof commandType !== 'string') {
      throw new Error('Invalid command type provided');
    }
    
    // サポートされているコマンドタイプの検証
    const supportedCommands = ['create_cube', 'create_sphere', 'create_plane', 'create_gameobject'];
    if (!supportedCommands.includes(commandType)) {
      throw new Error(`Unsupported command type: ${commandType}`);
    }
    
    const commandPath = getUnityCommandPath();
    
    // コマンドディレクトリを作成
    if (!fs.existsSync(commandPath)) {
      try {
        fs.mkdirSync(commandPath, { recursive: true });
        log(`Created command directory: ${commandPath}`);
      } catch (dirError: any) {
        throw new Error(`Failed to create command directory: ${dirError.message}`);
      }
    }
    
    // パラメータの検証と正規化
    const validatedParams = validateCommandParameters(commandType, args);
    
    // コマンドオブジェクトを作成
    const command = {
      commandId: commandId,
      commandType: commandType,
      parameters: validatedParams,
      timestamp: new Date().toISOString(),
      status: 'Pending',
      retryCount: 0,
      maxRetries: 3
    };
    
    // コマンドファイルに書き込み
    const commandFilePath = path.join(commandPath, COMMAND_FILE);
    try {
      fs.writeFileSync(commandFilePath, JSON.stringify(command, null, 2));
      log(`Command sent to Unity: ${commandType} (ID: ${command.commandId})`);
    } catch (writeError: any) {
      throw new Error(`Failed to write command file: ${writeError.message}`);
    }
    
    // 結果ファイルを監視して待機（タイムアウト拡張）
    const result = await waitForCommandResult(commandPath, command.commandId, 15000); // 15秒タイムアウト
    
    // 結果の検証
    if (result.status === 'Failed') {
      throw new Error(`Unity command failed: ${result.errorMessage || 'Unknown error'}`);
    }
    
    return {
      content: [{
        type: 'text',
        text: `✅ Unity Command executed successfully: ${result.result || 'Command completed'}\n` +
              `Command ID: ${commandId}\n` +
              `Duration: ${calculateDuration(command.timestamp, new Date().toISOString())}`
      }],
      isError: false
    };
    
  } catch (error: any) {
    log(`❌ Unity command execution error [${commandId}]:`, error.message);
    
    // エラーの分類
    let errorCategory = 'Unknown';
    let userMessage = error.message;
    
    if (error.message.includes('timeout')) {
      errorCategory = 'Timeout';
      userMessage = 'Unity command timed out. Unity Editor may be busy or not responding.';
    } else if (error.message.includes('directory')) {
      errorCategory = 'FileSystem';
      userMessage = 'Failed to access command directory. Check Unity project permissions.';
    } else if (error.message.includes('Unsupported command')) {
      errorCategory = 'InvalidCommand';
    } else if (error.message.includes('Invalid')) {
      errorCategory = 'ValidationError';
    }
    
    return {
      content: [{
        type: 'text',
        text: `❌ Unity Command Failed\n` +
              `Error Category: ${errorCategory}\n` +
              `Command ID: ${commandId}\n` +
              `Message: ${userMessage}\n` +
              `\nTroubleshooting:\n` +
              `- Ensure Unity Editor is running\n` +
              `- Check Unity project is open\n` +
              `- Verify UnityMCP scripts are installed`
      }],
      isError: true
    };
  }
}

// コマンドパラメーターの検証
function validateCommandParameters(commandType: string, args: any): any {
  const params = args || {};
  
  switch (commandType) {
    case 'create_cube':
    case 'create_sphere':
    case 'create_plane':
      // 名前の検証
      if (params.name && typeof params.name !== 'string') {
        throw new Error('Name parameter must be a string');
      }
      
      // 位置の検証
      if (params.position) {
        validateVector3Parameter('position', params.position);
      }
      
      // スケールの検証
      if (params.scale) {
        validateVector3Parameter('scale', params.scale);
      }
      
      // 色の検証（create_cubeのみ）
      if (commandType === 'create_cube' && params.color) {
        const validColors = ['red', 'green', 'blue', 'yellow', 'magenta', 'cyan', 'black', 'white'];
        if (typeof params.color !== 'string' || !validColors.includes(params.color.toLowerCase())) {
          throw new Error(`Invalid color. Supported colors: ${validColors.join(', ')}`);
        }
        params.color = params.color.toLowerCase();
      }
      break;
      
    case 'create_gameobject':
      if (params.name && typeof params.name !== 'string') {
        throw new Error('Name parameter must be a string');
      }
      if (params.position) {
        validateVector3Parameter('position', params.position);
      }
      break;
      
    default:
      throw new Error(`Unknown command type for validation: ${commandType}`);
  }
  
  return params;
}

// Vector3パラメーターの検証
function validateVector3Parameter(paramName: string, vector: any): void {
  if (typeof vector !== 'object' || vector === null) {
    throw new Error(`${paramName} must be an object with x, y, z properties`);
  }
  
  ['x', 'y', 'z'].forEach(axis => {
    if (vector[axis] !== undefined && typeof vector[axis] !== 'number') {
      throw new Error(`${paramName}.${axis} must be a number`);
    }
    // 範囲チェック（過度に大きな値を防ぐ）
    if (vector[axis] !== undefined && (vector[axis] < -1000 || vector[axis] > 1000)) {
      throw new Error(`${paramName}.${axis} must be between -1000 and 1000`);
    }
  });
}

// 実行時間計算
function calculateDuration(startTime: string, endTime: string): string {
  const start = new Date(startTime).getTime();
  const end = new Date(endTime).getTime();
  const duration = end - start;
  return `${duration}ms`;
}

// コマンド結果の待機
async function waitForCommandResult(commandPath: string, commandId: string, timeoutMs: number): Promise<any> {
  const resultFilePath = path.join(commandPath, RESULT_FILE);
  const startTime = Date.now();
  
  return new Promise((resolve, reject) => {
    const checkResult = () => {
      // タイムアウトチェック
      if (Date.now() - startTime > timeoutMs) {
        reject(new Error('Command execution timeout'));
        return;
      }
      
      // 結果ファイルの存在確認
      if (fs.existsSync(resultFilePath)) {
        try {
          const resultJson = fs.readFileSync(resultFilePath, 'utf-8');
          const result = JSON.parse(resultJson);
          
          // コマンドIDが一致するかチェック
          if (result.commandId === commandId) {
            // 結果ファイルを削除
            fs.unlinkSync(resultFilePath);
            
            if (result.status === 'Completed') {
              resolve(result);
            } else {
              reject(new Error(result.errorMessage || 'Command execution failed'));
            }
            return;
          }
        } catch (error) {
          // ファイル読み込みエラーは無視して再試行
        }
      }
      
      // 100ms後に再チェック
      setTimeout(checkResult, 100);
    };
    
    checkResult();
  });
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