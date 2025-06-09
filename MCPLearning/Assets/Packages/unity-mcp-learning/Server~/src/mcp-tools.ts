/**
 * MCP Tools Definition
 * MCPプロトコルのツール定義と処理
 */

import * as path from 'path';
import { ErrorCode, MCPError } from './errors.js';
import { getCachedData, loadAllData, CATEGORY_MAP } from './data-monitor.js';
import { executeUnityCommand } from './unity-commands.js';
import { MCPConfig } from './config-validator.js';

// MCPツールの定義
export const MCP_TOOLS = [
  {
    name: 'unity_info_realtime',
    description: 'Get real-time Unity project information',
    inputSchema: {
      type: 'object',
      properties: {
        category: {
          type: 'string',
          enum: ['project', 'scene', 'gameobjects', 'assets', 'build', 'editor', 'all'],
          description: 'Category of information to retrieve',
          default: 'all'
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
          description: 'Color of the cube (e.g., "red", "blue", "green")',
          examples: ['red', 'blue', 'green', 'yellow', 'white']
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
    name: 'create_plane',
    description: 'Create a plane in Unity scene',
    inputSchema: {
      type: 'object',
      properties: {
        name: {
          type: 'string',
          description: 'Name of the plane',
          default: 'Plane'
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
    name: 'create_empty_gameobject',
    description: 'Create an empty GameObject in Unity scene',
    inputSchema: {
      type: 'object',
      properties: {
        name: {
          type: 'string',
          description: 'Name of the GameObject',
          default: 'GameObject'
        },
        position: {
          type: 'object',
          properties: {
            x: { type: 'number', default: 0 },
            y: { type: 'number', default: 0 },
            z: { type: 'number', default: 0 }
          },
          description: 'Position in 3D space'
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
  },
  {
    name: 'get_console_logs',
    description: 'Get Unity Console logs (errors, warnings, logs)',
    inputSchema: {
      type: 'object',
      properties: {
        filter: {
          type: 'string',
          enum: ['all', 'errors', 'warnings', 'logs', 'recent'],
          description: 'Type of logs to retrieve',
          default: 'all'
        },
        limit: {
          type: 'number',
          description: 'Maximum number of logs to retrieve',
          default: 50
        }
      },
      required: []
    }
  },
  {
    name: 'wait_for_compilation',
    description: 'Wait for Unity compilation to complete and return results',
    inputSchema: {
      type: 'object',
      properties: {
        timeout: {
          type: 'number',
          description: 'Timeout in seconds (default: 30)',
          default: 30
        }
      },
      required: []
    }
  }
];

// ツール実行処理
export async function handleToolCall(
  toolName: string,
  params: any,
  dataPath: string,
  config: MCPConfig,
  log: (...args: any[]) => void
): Promise<any> {
  switch (toolName) {
    case 'unity_info_realtime':
      return handleUnityInfoRealtime(params, dataPath, log);
    
    case 'create_cube':
    case 'create_sphere':
    case 'create_plane':
    case 'create_gameobject':
      return executeUnityCommand(toolName, params, dataPath, config, log);
    
    case 'ping':
      return handlePing();
    
    case 'get_console_logs':
      return handleGetConsoleLogs(params, dataPath, log);
    
    case 'wait_for_compilation':
      return handleWaitForCompilation(params, dataPath, log);
    
    default:
      throw new MCPError(
        ErrorCode.INVALID_PARAMETER,
        `Unknown tool: ${toolName}`
      );
  }
}

// Unity情報取得処理
async function handleUnityInfoRealtime(
  params: any,
  dataPath: string,
  log: (...args: any[]) => void
): Promise<any> {
  const category = params?.category || 'all';
  
  // データの存在確認
  const cachedData = getCachedData();
  const hasData = Object.keys(cachedData).length > 0;
  log(`Unity data check: hasData=${hasData}, keys=${Object.keys(cachedData)}, dataPath=${dataPath}`);
  
  if (!hasData) {
    // 強制的にデータ再読み込みを試行
    loadAllData(dataPath, log);
    const hasDataAfterReload = Object.keys(getCachedData()).length > 0;
    log(`After reload: hasData=${hasDataAfterReload}, keys=${Object.keys(getCachedData())}`);
    
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
        text: `# Unity Project Information (MCPLearning)\n\n${JSON.stringify(getCachedData(), null, 2)}`
      }],
      isError: false
    };
  } else {
    const dataKey = CATEGORY_MAP[category];
    const data = getCachedData()[dataKey];
    
    if (!data) {
      return {
        content: [{
          type: 'text',
          text: `No data found for category: ${category}. Available categories: ${Object.keys(CATEGORY_MAP).join(', ')}`
        }],
        isError: false
      };
    }
    
    return {
      content: [{
        type: 'text',
        text: `# Unity ${category} Information\n\n${JSON.stringify(data, null, 2)}`
      }],
      isError: false
    };
  }
}

// Ping処理
function handlePing(): any {
  return {
    content: [{
      type: 'text',
      text: '🏓 Pong! MCP Server is running and responsive.'
    }],
    isError: false
  };
}

// Console Logs取得処理
async function handleGetConsoleLogs(
  params: any,
  dataPath: string,
  log: (...args: any[]) => void
): Promise<any> {
  const filter = params?.filter || 'all';
  const limit = params?.limit || 50;
  
  try {
    const consoleLogsPath = path.join(dataPath, 'console-logs.json');
    const fs = await import('fs/promises');
    
    // ファイルの存在確認
    try {
      await fs.access(consoleLogsPath);
    } catch (error) {
      return {
        content: [{
          type: 'text',
          text: `Console logs are not available yet. Please make sure Unity Editor is running and the ConsoleLogExporter is active.\nExpected path: ${consoleLogsPath}`
        }],
        isError: false
      };
    }
    
    // データ読み込み
    const consoleData = JSON.parse(await fs.readFile(consoleLogsPath, 'utf8'));
    let logs = consoleData.logs || [];
    
    // フィルタリング
    switch (filter) {
      case 'errors':
        logs = logs.filter((log: any) => log.type === 'Error' || log.type === 'Exception');
        break;
      case 'warnings':
        logs = logs.filter((log: any) => log.type === 'Warning');
        break;
      case 'logs':
        logs = logs.filter((log: any) => log.type === 'Log');
        break;
      case 'recent':
        logs = logs.slice(-10); // 直近10件
        break;
    }
    
    // 件数制限
    if (logs.length > limit) {
      logs = logs.slice(-limit);
    }
    
    // 結果フォーマット
    const summary = consoleData.summary || {};
    const formattedLogs = logs.map((log: any) => {
      const timestamp = log.timestamp || 'Unknown time';
      const type = log.type || 'Unknown';
      const message = log.message || '';
      const stackTrace = log.stackTrace && log.stackTrace.trim() ? `\n  Stack: ${log.stackTrace}` : '';
      
      return `[${timestamp}] [${type}] ${message}${stackTrace}`;
    }).join('\n\n');
    
    const result = `# Unity Console Logs\n\n` +
      `## Summary\n` +
      `- Total Logs: ${summary.totalLogs || 0}\n` +
      `- Errors: ${summary.errorCount || 0}\n` +
      `- Warnings: ${summary.warningCount || 0}\n` +
      `- Info: ${summary.logCount || 0}\n` +
      `- Last Update: ${consoleData.lastUpdate || 'Unknown'}\n\n` +
      `## Logs (Filter: ${filter}, Showing: ${logs.length})\n\n` +
      (formattedLogs || 'No logs found.');
    
    return {
      content: [{
        type: 'text',
        text: result
      }],
      isError: false
    };
    
  } catch (error: any) {
    log(`Error reading console logs: ${error.message}`);
    throw new MCPError(
      ErrorCode.FILE_READ_ERROR,
      `Failed to read console logs: ${error.message}`
    );
  }
}

// Compilation 監視処理
async function handleWaitForCompilation(
  params: any,
  dataPath: string,
  log: (...args: any[]) => void
): Promise<any> {
  const timeout = (params?.timeout || 30) * 1000; // ミリ秒変換
  const startTime = Date.now();
  const compileStatusPath = path.join(dataPath, 'compile-status.json');
  
  log(`Starting compilation wait - timeout: ${timeout}ms, path: ${compileStatusPath}`);
  
  return new Promise((resolve, reject) => {
    const checkCompileStatus = async () => {
      try {
        const elapsed = Date.now() - startTime;
        
        if (elapsed > timeout) {
          reject(new MCPError(
            ErrorCode.TIMEOUT_ERROR,
            `Compilation timeout after ${timeout/1000}s`
          ));
          return;
        }
        
        const fs = await import('fs/promises');
        
        // ファイルの存在確認
        try {
          await fs.access(compileStatusPath);
        } catch (error) {
          log(`Compile status file not found, retrying in 500ms...`);
          setTimeout(checkCompileStatus, 500);
          return;
        }
        
        // ファイル読み込み
        const compileData = JSON.parse(await fs.readFile(compileStatusPath, 'utf8'));
        log(`Compile status: ${compileData.status}, duration: ${compileData.duration}ms`);
        
        if (compileData.status === "SUCCESS" || compileData.status === "FAILED") {
          // ステータスファイル削除（次回のため）
          try {
            await fs.unlink(compileStatusPath);
            log(`Compile status file deleted for next compilation`);
          } catch (error) {
            log(`Failed to delete compile status file: ${error}`);
          }
          
          const duration = Date.now() - startTime;
          const compileTime = compileData.duration || 0;
          
          let resultText = '';
          if (compileData.status === "SUCCESS") {
            resultText = `✅ **Compilation Successful!**\n\n` +
              `- **Duration**: ${(compileTime / 1000).toFixed(1)}s\n` +
              `- **Warnings**: ${compileData.warningCount || 0}\n` +
              `- **Wait Time**: ${(duration / 1000).toFixed(1)}s\n\n` +
              `Unity compilation completed successfully.`;
            
            if (compileData.warningCount > 0) {
              resultText += `\n\n⚠️  **Warnings Found**: ${compileData.warningCount} warning(s) detected.`;
            }
          } else {
            resultText = `❌ **Compilation Failed!**\n\n` +
              `- **Duration**: ${(compileTime / 1000).toFixed(1)}s\n` +
              `- **Errors**: ${compileData.errorCount || 0}\n` +
              `- **Warnings**: ${compileData.warningCount || 0}\n` +
              `- **Wait Time**: ${(duration / 1000).toFixed(1)}s\n\n`;
            
            if (compileData.messages && compileData.messages.length > 0) {
              resultText += `**Error Details:**\n`;
              compileData.messages.forEach((msg: any) => {
                if (msg.file && msg.file !== "Unknown") {
                  resultText += `- \`${msg.file}(${msg.line},${msg.column})\`: ${msg.type} - ${msg.message}\n`;
                } else {
                  resultText += `- ${msg.type}: ${msg.message}\n`;
                }
              });
            } else {
              resultText += `Check Unity Console for detailed error information.`;
            }
          }
          
          resolve({
            content: [{
              type: 'text',
              text: resultText
            }],
            isError: compileData.status === "FAILED"
          });
          return;
        } else if (compileData.status === "COMPILING") {
          log(`Compilation in progress, retrying in 500ms...`);
          setTimeout(checkCompileStatus, 500);
          return;
        }
        
        // 不明な状態の場合は500ms後に再チェック
        setTimeout(checkCompileStatus, 500);
        
      } catch (error: any) {
        log(`Error checking compile status: ${error.message}`);
        reject(new MCPError(
          ErrorCode.FILE_READ_ERROR,
          `Failed to read compile status: ${error.message}`
        ));
      }
    };
    
    checkCompileStatus();
  });
}