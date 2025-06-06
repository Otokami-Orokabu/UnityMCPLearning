/**
 * 設定ファイル検証システムのテスト
 */

import * as fs from 'fs';
import * as path from 'path';
import { ConfigValidator, loadAndValidateConfig, MCPConfig, DEFAULT_CONFIG } from '../src/config-validator';
import { ErrorCode, MCPError } from '../src/errors';

describe('ConfigValidator', () => {
  let validator: ConfigValidator;
  let tempConfigPath: string;

  beforeEach(() => {
    validator = new ConfigValidator();
    tempConfigPath = path.join(__dirname, 'temp-config.json');
  });

  afterEach(() => {
    if (fs.existsSync(tempConfigPath)) {
      fs.unlinkSync(tempConfigPath);
    }
  });

  describe('valid configuration', () => {
    it('should validate minimal valid config', () => {
      const config = {
        mcpServers: {
          'test-server': {
            command: 'node',
            args: ['./dist/index.js']
          }
        },
        unityDataPath: './test/data'
      };

      const result = validator.validateConfig(config);
      
      expect(result.mcpServers).toEqual(config.mcpServers);
      expect(result.unityDataPath).toBe('./test/data');
      expect(result.logLevel).toBe('info'); // デフォルト値
      expect(result.timeout?.unityCommandTimeout).toBe(30000); // デフォルト値
    });

    it('should validate full config with all options', () => {
      const config = {
        mcpServers: {
          'unity-server': {
            command: 'node',
            args: ['./dist/index.js'],
            cwd: '.',
            env: {
              NODE_ENV: 'production'
            }
          }
        },
        unityDataPath: './MCPLearning/UnityMCP/Data',
        logLevel: 'debug',
        timeout: {
          unityCommandTimeout: 60000,
          dataWaitTimeout: 10000
        },
        server: {
          name: 'custom-unity-server',
          version: '2.0.0',
          protocol: '2024-11-05'
        },
        unity: {
          projectPath: './MCPLearning',
          autoDetectChanges: false,
          watchFilePattern: '*.json'
        }
      };

      const result = validator.validateConfig(config);
      
      expect(result).toMatchObject(config);
    });
  });

  describe('invalid configuration', () => {
    it('should reject config without required fields', () => {
      const config = {
        unityDataPath: './test/data'
        // mcpServers missing
      };

      expect(() => validator.validateConfig(config)).toThrow(MCPError);
      expect(() => validator.validateConfig(config)).toThrow(/Configuration validation failed/);
    });

    it('should reject empty unityDataPath', () => {
      const config = {
        mcpServers: {
          'test-server': {
            command: 'node',
            args: ['./dist/index.js']
          }
        },
        unityDataPath: ''
      };

      expect(() => validator.validateConfig(config)).toThrow(MCPError);
    });

    it('should reject invalid server name', () => {
      const config = {
        mcpServers: {
          '123-invalid': { // 数字で開始
            command: 'node',
            args: ['./dist/index.js']
          }
        },
        unityDataPath: './test/data'
      };

      expect(() => validator.validateConfig(config)).toThrow(MCPError);
    });

    it('should reject invalid timeout values', () => {
      const config = {
        mcpServers: {
          'test-server': {
            command: 'node',
            args: ['./dist/index.js']
          }
        },
        unityDataPath: './test/data',
        timeout: {
          unityCommandTimeout: 500 // 最小値1000未満
        }
      };

      expect(() => validator.validateConfig(config)).toThrow(MCPError);
    });

    it('should reject invalid protocol version format', () => {
      const config = {
        mcpServers: {
          'test-server': {
            command: 'node',
            args: ['./dist/index.js']
          }
        },
        unityDataPath: './test/data',
        server: {
          protocol: 'invalid-format'
        }
      };

      expect(() => validator.validateConfig(config)).toThrow(MCPError);
    });
  });

  describe('business rule validation', () => {
    it('should reject when unity command timeout is less than data wait timeout', () => {
      const config = {
        mcpServers: {
          'test-server': {
            command: 'node',
            args: ['./dist/index.js']
          }
        },
        unityDataPath: './test/data',
        timeout: {
          unityCommandTimeout: 5000,
          dataWaitTimeout: 10000 // より大きい値
        }
      };

      expect(() => validator.validateConfig(config)).toThrow(MCPError);
      expect(() => validator.validateConfig(config)).toThrow(/Unity command timeout should be greater than or equal to data wait timeout/);
    });

    it('should reject invalid unity data path format', () => {
      const config = {
        mcpServers: {
          'test-server': {
            command: 'node',
            args: ['./dist/index.js']
          }
        },
        unityDataPath: 'invalid-path' // 相対パスの形式が正しくない
      };

      expect(() => validator.validateConfig(config)).toThrow(MCPError);
    });
  });
});

describe('loadAndValidateConfig function', () => {
  let tempConfigPath: string;

  beforeEach(() => {
    tempConfigPath = path.join(__dirname, 'temp-config.json');
  });

  afterEach(() => {
    if (fs.existsSync(tempConfigPath)) {
      fs.unlinkSync(tempConfigPath);
    }
  });

  it('should load and validate valid config file', () => {
    const config = {
      mcpServers: {
        'test-server': {
          command: 'node',
          args: ['./dist/index.js']
        }
      },
      unityDataPath: './test/data'
    };

    fs.writeFileSync(tempConfigPath, JSON.stringify(config, null, 2));
    
    const result = loadAndValidateConfig(tempConfigPath);
    
    expect(result.mcpServers).toEqual(config.mcpServers);
    expect(result.unityDataPath).toBe('./test/data');
  });

  it('should throw error for non-existent config file', () => {
    const nonExistentPath = path.join(__dirname, 'non-existent.json');
    
    expect(() => loadAndValidateConfig(nonExistentPath)).toThrow(MCPError);
    expect(() => loadAndValidateConfig(nonExistentPath)).toThrow(/Configuration file not found/);
  });

  it('should throw error for invalid JSON', () => {
    fs.writeFileSync(tempConfigPath, '{ invalid json }');
    
    expect(() => loadAndValidateConfig(tempConfigPath)).toThrow(MCPError);
    expect(() => loadAndValidateConfig(tempConfigPath)).toThrow(/Failed to parse configuration file/);
  });

  it('should throw error for invalid config structure', () => {
    const invalidConfig = {
      unityDataPath: './test/data'
      // mcpServers missing
    };

    fs.writeFileSync(tempConfigPath, JSON.stringify(invalidConfig, null, 2));
    
    expect(() => loadAndValidateConfig(tempConfigPath)).toThrow(MCPError);
    expect(() => loadAndValidateConfig(tempConfigPath)).toThrow(/Configuration validation failed/);
  });
});

describe('DEFAULT_CONFIG', () => {
  it('should have correct default values', () => {
    expect(DEFAULT_CONFIG.logLevel).toBe('info');
    expect(DEFAULT_CONFIG.timeout?.unityCommandTimeout).toBe(30000);
    expect(DEFAULT_CONFIG.timeout?.dataWaitTimeout).toBe(5000);
    expect(DEFAULT_CONFIG.server?.name).toBe('unity-mcp-server');
    expect(DEFAULT_CONFIG.server?.version).toBe('1.0.0');
    expect(DEFAULT_CONFIG.server?.protocol).toBe('2024-11-05');
    expect(DEFAULT_CONFIG.unity?.autoDetectChanges).toBe(true);
    expect(DEFAULT_CONFIG.unity?.watchFilePattern).toBe('*.json');
  });
});