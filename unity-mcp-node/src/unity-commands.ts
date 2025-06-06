/**
 * Unity Command Execution System
 * Unityコマンドの実行と検証を管理
 */

import * as fs from 'fs';
import * as path from 'path';
import { randomUUID } from 'crypto';
import { ErrorCode, MCPError } from './errors.js';
import { MCPConfig } from './config-validator.js';
import { getMessage } from './i18n.js';

// Unity コマンド実行関連
export function getUnityCommandPath(dataPath: string): string {
  return path.resolve(path.dirname(dataPath), 'Commands');
}

// Unity コマンド実行
export async function executeUnityCommand(
  commandType: string, 
  args: any, 
  dataPath: string,
  config: MCPConfig,
  log: (...args: any[]) => void
): Promise<any> {
  const commandId = randomUUID();
  
  try {
    // 入力検証
    if (!commandType || typeof commandType !== 'string') {
      throw new MCPError(
        ErrorCode.INVALID_PARAMETER,
        'error.invalid_parameter',
        { parameter: 'commandType' }
      );
    }
    
    // サポートされているコマンドタイプの検証
    const supportedCommands = ['create_cube', 'create_sphere', 'create_plane', 'create_gameobject'];
    if (!supportedCommands.includes(commandType)) {
      throw new MCPError(
        ErrorCode.INVALID_PARAMETER,
        'validation.command_type_invalid',
        { commandType }
      );
    }
    
    const commandPath = getUnityCommandPath(dataPath);
    
    // コマンドディレクトリを作成
    if (!fs.existsSync(commandPath)) {
      try {
        fs.mkdirSync(commandPath, { recursive: true });
        log(`Created command directory: ${commandPath}`);
      } catch (dirError: any) {
        throw new MCPError(
          ErrorCode.FILE_WRITE_ERROR,
          `Failed to create command directory: ${dirError.message}`
        );
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
      result: null,
      error: null
    };
    
    // コマンドファイルに書き込み
    const commandFilePath = path.join(commandPath, `${commandId}.json`);
    
    try {
      fs.writeFileSync(commandFilePath, JSON.stringify(command, null, 2));
      log(`Unity command written: ${commandFilePath}`);
    } catch (writeError: any) {
      throw new MCPError(
        ErrorCode.FILE_WRITE_ERROR,
        `Failed to write command file: ${writeError.message}`
      );
    }
    
    // Unityからの結果を待機
    const timeout = config.timeout?.unityCommandTimeout || 30000;
    const result = await waitForCommandResult(commandFilePath, timeout, log);
    
    log(`Unity command completed: ${commandId}`);
    return result;
    
  } catch (error: any) {
    log(`Unity command error: ${commandId}`, error);
    
    if (error instanceof MCPError) {
      throw error;
    }
    
    const errorCategory = getErrorCategory(error);
    const userMessage = getUserFriendlyMessage(error);
    
    throw new MCPError(
      ErrorCode.UNITY_COMMAND_FAILED,
      userMessage,
      { commandId, commandType, originalError: error.message, category: errorCategory }
    );
  }
}

function getErrorCategory(error: any): string {
  let errorCategory = 'UnknownError';
  
  if (error.code === 'ENOENT') {
    errorCategory = 'FileNotFound';
  } else if (error.code === 'EACCES') {
    errorCategory = 'PermissionDenied';
  } else if (error.code === 'TIMEOUT') {
    errorCategory = 'Timeout';
  } else if (error.message && error.message.includes('Unsupported command')) {
    errorCategory = 'InvalidCommand';
  } else if (error.message && error.message.includes('Invalid')) {
    errorCategory = 'ValidationError';
  }
  
  return errorCategory;
}

function getUserFriendlyMessage(error: any): string {
  if (error.code === 'ENOENT') {
    return 'Unity command directory not found. Please ensure Unity project is set up correctly.';
  } else if (error.code === 'EACCES') {
    return 'Permission denied writing Unity command. Check file permissions.';
  } else if (error.code === 'TIMEOUT') {
    return 'Unity command timed out. Ensure Unity Editor is running and responsive.';
  } else if (error.message && error.message.includes('Unsupported command')) {
    return `Unsupported Unity command. Available commands: create_cube, create_sphere, create_plane, create_gameobject`;
  } else if (error.message && error.message.includes('Invalid')) {
    return `Invalid command parameters: ${error.message}`;
  } else {
    return `Unity command execution failed: ${error.message}`;
  }
}

// コマンドパラメーターの検証
export function validateCommandParameters(commandType: string, args: any): any {
  const params = args || {};
  
  switch (commandType) {
    case 'create_cube':
    case 'create_sphere':
    case 'create_plane':
      // 名前の検証
      if (params.name && typeof params.name !== 'string') {
        throw new MCPError(
          ErrorCode.INVALID_PARAMETER,
          'Name parameter must be a string'
        );
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
        if (typeof params.color !== 'string') {
          throw new MCPError(
            ErrorCode.INVALID_PARAMETER,
            'Color parameter must be a string'
          );
        }
      }
      
      return {
        name: params.name || getDefaultName(commandType),
        position: params.position || { x: 0, y: 0, z: 0 },
        scale: params.scale || { x: 1, y: 1, z: 1 },
        ...(commandType === 'create_cube' && params.color ? { color: params.color } : {})
      };
    
    case 'create_gameobject':
      // 名前の検証
      if (params.name && typeof params.name !== 'string') {
        throw new MCPError(
          ErrorCode.INVALID_PARAMETER,
          'Name parameter must be a string'
        );
      }
      
      // 位置の検証
      if (params.position) {
        validateVector3Parameter('position', params.position);
      }
      
      return {
        name: params.name || 'GameObject',
        position: params.position || { x: 0, y: 0, z: 0 }
      };
    
    default:
      throw new MCPError(
        ErrorCode.INVALID_PARAMETER,
        `Unknown command type: ${commandType}`
      );
  }
}

function getDefaultName(commandType: string): string {
  switch (commandType) {
    case 'create_cube': return 'Cube';
    case 'create_sphere': return 'Sphere';
    case 'create_plane': return 'Plane';
    default: return 'GameObject';
  }
}

// Vector3パラメータの検証
function validateVector3Parameter(paramName: string, vector: any): void {
  if (!vector || typeof vector !== 'object') {
    throw new MCPError(
      ErrorCode.INVALID_PARAMETER,
      `${paramName} must be an object with x, y, z properties`
    );
  }
  
  for (const axis of ['x', 'y', 'z']) {
    if (vector[axis] !== undefined) {
      if (typeof vector[axis] !== 'number' || !isFinite(vector[axis])) {
        throw new MCPError(
          ErrorCode.INVALID_PARAMETER,
          `${paramName}.${axis} must be a finite number`
        );
      }
      
      // 妥当な範囲をチェック（-10000から10000）
      if (vector[axis] < -10000 || vector[axis] > 10000) {
        throw new MCPError(
          ErrorCode.INVALID_PARAMETER,
          `${paramName}.${axis} must be between -10000 and 10000`
        );
      }
    }
  }
}

// Unityコマンドの結果を待機
async function waitForCommandResult(
  commandFilePath: string, 
  timeoutMs: number,
  log: (...args: any[]) => void
): Promise<any> {
  return new Promise((resolve, reject) => {
    const startTime = Date.now();
    const timeout = setTimeout(() => {
      reject(new MCPError(
        ErrorCode.TIMEOUT_ERROR,
        `Unity command timed out after ${timeoutMs}ms`
      ));
    }, timeoutMs);
    
    const checkResult = () => {
      try {
        if (fs.existsSync(commandFilePath)) {
          const commandData = JSON.parse(fs.readFileSync(commandFilePath, 'utf-8'));
          
          if (commandData.status === 'Completed') {
            clearTimeout(timeout);
            
            // 実行時間を計算
            const executionTime = Date.now() - startTime;
            log(`Command completed in ${executionTime}ms`);
            
            if (commandData.error) {
              reject(new MCPError(
                ErrorCode.UNITY_COMMAND_FAILED,
                `Unity command failed: ${commandData.error}`
              ));
            } else {
              resolve({
                content: [{
                  type: 'text',
                  text: `✅ Unity Command Successful\n` +
                        `Command: ${commandData.commandType}\n` +
                        `Parameters: ${JSON.stringify(commandData.parameters, null, 2)}\n` +
                        `Result: ${commandData.result || 'GameObject created successfully'}\n` +
                        `Execution Time: ${executionTime}ms`
                }],
                isError: false
              });
            }
            return;
          } else if (commandData.status === 'Failed') {
            clearTimeout(timeout);
            reject(new MCPError(
              ErrorCode.UNITY_COMMAND_FAILED,
              `Unity command failed: ${commandData.error || 'Unknown error'}`
            ));
            return;
          }
        }
        
        // まだ完了していない場合は、100ms後に再チェック
        setTimeout(checkResult, 100);
      } catch (error) {
        clearTimeout(timeout);
        reject(new MCPError(
          ErrorCode.FILE_READ_ERROR,
          `Failed to read command result: ${error}`
        ));
      }
    };
    
    // 初回チェック
    checkResult();
  });
}