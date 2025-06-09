/**
 * Configuration Validation System
 * JSON Schemaを使用したmcp-config.jsonの検証
 */

import Ajv from 'ajv';
import addFormats from 'ajv-formats';
import * as fs from 'fs';
import * as path from 'path';
import { ErrorCode, MCPError } from './errors.js';

// 設定ファイルのTypeScript型定義
export interface MCPConfig {
  mcpServers: {
    [serverName: string]: {
      command: string;
      args: string[];
      cwd?: string;
      env?: { [key: string]: string };
    };
  };
  unityDataPath: string;
  logLevel?: 'error' | 'warn' | 'info' | 'debug';
  timeout?: {
    unityCommandTimeout?: number;
    dataWaitTimeout?: number;
  };
  server?: {
    name?: string;
    version?: string;
    protocol?: string;
  };
  unity?: {
    projectPath?: string;
    autoDetectChanges?: boolean;
    watchFilePattern?: string;
  };
}

// デフォルト値
export const DEFAULT_CONFIG: Partial<MCPConfig> = {
  logLevel: 'info',
  timeout: {
    unityCommandTimeout: 30000,
    dataWaitTimeout: 5000
  },
  server: {
    name: 'unity-mcp-server',
    version: '1.0.0',
    protocol: '2024-11-05'
  },
  unity: {
    autoDetectChanges: true,
    watchFilePattern: '*.json'
  }
};

/**
 * 設定ファイル検証クラス
 */
export class ConfigValidator {
  private ajv: Ajv;
  private schema!: object;

  constructor() {
    this.ajv = new Ajv({
      allErrors: true,
      verbose: true,
      strict: false
    });
    
    // フォーマット検証を追加
    addFormats(this.ajv);
    
    // スキーマを読み込み
    this.loadSchema();
  }

  /**
   * JSON Schemaを読み込み
   */
  private loadSchema(): void {
    try {
      const schemaPath = path.join(__dirname, '../schema/mcp-config.schema.json');
      const schemaContent = fs.readFileSync(schemaPath, 'utf-8');
      this.schema = JSON.parse(schemaContent);
    } catch (error: any) {
      throw new MCPError(
        ErrorCode.CONFIG_ERROR,
        `Failed to load JSON schema: ${error.message}`,
        { schemaPath: path.join(__dirname, '../schema/mcp-config.schema.json') }
      );
    }
  }

  /**
   * 設定ファイルを検証
   */
  validateConfig(config: any): MCPConfig {
    const validate = this.ajv.compile(this.schema);
    const isValid = validate(config);

    if (!isValid) {
      const errors = validate.errors || [];
      const errorMessages = errors.map(err => {
        const path = err.instancePath || 'root';
        return `${path}: ${err.message}`;
      });

      throw new MCPError(
        ErrorCode.INVALID_CONFIG,
        `Configuration validation failed: ${errorMessages.join(', ')}`,
        { 
          validationErrors: errors,
          config: config 
        }
      );
    }

    // デフォルト値をマージ
    const validatedConfig = this.mergeDefaults(config);
    
    // 追加検証（スキーマでは表現しにくいもの）
    this.validateBusinessRules(validatedConfig);

    return validatedConfig;
  }

  /**
   * デフォルト値をマージ
   */
  private mergeDefaults(config: any): MCPConfig {
    return {
      ...DEFAULT_CONFIG,
      ...config,
      timeout: {
        ...DEFAULT_CONFIG.timeout,
        ...config.timeout
      },
      server: {
        ...DEFAULT_CONFIG.server,
        ...config.server
      },
      unity: {
        ...DEFAULT_CONFIG.unity,
        ...config.unity
      }
    } as MCPConfig;
  }

  /**
   * ビジネスルール検証
   * JSON Schemaでは表現が困難な複雑な検証
   */
  private validateBusinessRules(config: MCPConfig): void {
    // Unity データパスの存在確認
    this.validateUnityDataPath(config.unityDataPath);
    
    // MCPサーバー設定の検証
    this.validateMCPServers(config.mcpServers);
    
    // タイムアウト値の妥当性確認
    this.validateTimeouts(config.timeout);
  }

  /**
   * Unity データパスの検証
   */
  private validateUnityDataPath(dataPath: string): void {
    const resolvedPath = path.resolve(dataPath);
    
    // パスの存在確認（ディレクトリが存在するかは実行時にチェック）
    if (!dataPath || dataPath.trim().length === 0) {
      throw new MCPError(
        ErrorCode.INVALID_CONFIG,
        'Unity data path cannot be empty',
        { dataPath }
      );
    }

    // 相対パスか絶対パスかの確認
    if (!path.isAbsolute(dataPath) && !dataPath.startsWith('.')) {
      throw new MCPError(
        ErrorCode.INVALID_CONFIG,
        'Unity data path must be absolute or start with "./"',
        { dataPath, resolvedPath }
      );
    }

    // ディレクトリの存在確認（警告のみ、エラーにはしない）
    if (!fs.existsSync(resolvedPath)) {
      console.warn(`[MCP Server] Warning: Unity data directory not found: ${resolvedPath}`);
      console.warn(`[MCP Server] The server will start but may not function correctly until the directory is created.`);
    }
  }

  /**
   * MCPサーバー設定の検証
   */
  private validateMCPServers(servers: MCPConfig['mcpServers']): void {
    Object.entries(servers).forEach(([serverName, serverConfig]) => {
      // サーバー名の検証
      if (!/^[a-zA-Z][a-zA-Z0-9_-]*$/.test(serverName)) {
        throw new MCPError(
          ErrorCode.INVALID_CONFIG,
          `Invalid server name: ${serverName}. Must start with letter and contain only alphanumeric, underscore, or hyphen`,
          { serverName, serverConfig }
        );
      }

      // コマンドの存在確認
      if (!serverConfig.args || serverConfig.args.length === 0) {
        throw new MCPError(
          ErrorCode.INVALID_CONFIG,
          `Server ${serverName} must have at least one argument`,
          { serverName, serverConfig }
        );
      }
    });
  }

  /**
   * タイムアウト値の検証
   */
  private validateTimeouts(timeout?: MCPConfig['timeout']): void {
    if (!timeout) return;

    const { unityCommandTimeout, dataWaitTimeout } = timeout;

    if (unityCommandTimeout && dataWaitTimeout) {
      if (unityCommandTimeout < dataWaitTimeout) {
        throw new MCPError(
          ErrorCode.INVALID_CONFIG,
          'Unity command timeout should be greater than or equal to data wait timeout',
          { unityCommandTimeout, dataWaitTimeout }
        );
      }
    }
  }
}

/**
 * 設定ファイルを読み込んで検証
 */
export function loadAndValidateConfig(configPath: string): MCPConfig {
  const validator = new ConfigValidator();

  try {
    // 設定ファイルの存在確認
    if (!fs.existsSync(configPath)) {
      throw new MCPError(
        ErrorCode.MISSING_CONFIG,
        `Configuration file not found: ${configPath}`,
        { configPath }
      );
    }

    // 設定ファイル読み込み
    const configContent = fs.readFileSync(configPath, 'utf-8');
    let config: any;

    try {
      config = JSON.parse(configContent);
    } catch (parseError: any) {
      throw new MCPError(
        ErrorCode.INVALID_CONFIG,
        `Failed to parse configuration file: ${parseError.message}`,
        { configPath, parseError: parseError.message }
      );
    }

    // 環境変数で設定を上書き
    if (process.env.UNITY_DATA_PATH) {
      config.unityDataPath = process.env.UNITY_DATA_PATH;
    }

    // 検証実行
    return validator.validateConfig(config);

  } catch (error: any) {
    if (error instanceof MCPError) {
      throw error;
    }
    
    throw new MCPError(
      ErrorCode.CONFIG_ERROR,
      `Configuration loading failed: ${error.message}`,
      { configPath, originalError: error.message }
    );
  }
}