/**
 * MCPツール定義・処理のテスト
 */

import { MCP_TOOLS, handleToolCall } from '../src/mcp-tools';
import { ErrorCode, MCPError } from '../src/errors';
import { MCPConfig } from '../src/config-validator';
import * as dataMonitor from '../src/data-monitor';

// data-monitorモジュールのモック
jest.mock('../src/data-monitor');
const mockDataMonitor = dataMonitor as jest.Mocked<typeof dataMonitor>;

// unity-commandsモジュールのモック
jest.mock('../src/unity-commands', () => ({
  executeUnityCommand: jest.fn()
}));

describe('MCP_TOOLS definition', () => {
  it('should have correct number of tools', () => {
    expect(MCP_TOOLS).toHaveLength(6);
  });

  it('should have unity_info_realtime tool', () => {
    const tool = MCP_TOOLS.find(t => t.name === 'unity_info_realtime');
    
    expect(tool).toBeDefined();
    expect(tool?.description).toBe('Get real-time Unity project information');
    expect(tool?.inputSchema.properties.category).toBeDefined();
    expect(tool?.inputSchema.properties.category?.enum).toEqual([
      'project', 'scene', 'gameobjects', 'assets', 'build', 'editor', 'all'
    ]);
  });

  it('should have create_cube tool', () => {
    const tool = MCP_TOOLS.find(t => t.name === 'create_cube');
    
    expect(tool).toBeDefined();
    expect(tool?.description).toBe('Create a cube in Unity scene');
    expect(tool?.inputSchema.properties.name).toBeDefined();
    expect(tool?.inputSchema.properties.position).toBeDefined();
    expect(tool?.inputSchema.properties.scale).toBeDefined();
    expect(tool?.inputSchema.properties.color).toBeDefined();
  });

  it('should have create_sphere tool', () => {
    const tool = MCP_TOOLS.find(t => t.name === 'create_sphere');
    
    expect(tool).toBeDefined();
    expect(tool?.description).toBe('Create a sphere in Unity scene');
    expect(tool?.inputSchema.properties.name).toBeDefined();
    expect(tool?.inputSchema.properties.position).toBeDefined();
    expect(tool?.inputSchema.properties.scale).toBeDefined();
    expect(tool?.inputSchema.properties.color).toBeUndefined(); // sphereには色指定なし
  });

  it('should have create_plane tool', () => {
    const tool = MCP_TOOLS.find(t => t.name === 'create_plane');
    
    expect(tool).toBeDefined();
    expect(tool?.description).toBe('Create a plane in Unity scene');
  });

  it('should have create_empty_gameobject tool', () => {
    const tool = MCP_TOOLS.find(t => t.name === 'create_empty_gameobject');
    
    expect(tool).toBeDefined();
    expect(tool?.description).toBe('Create an empty GameObject in Unity scene');
  });

  it('should have ping tool', () => {
    const tool = MCP_TOOLS.find(t => t.name === 'ping');
    
    expect(tool).toBeDefined();
    expect(tool?.description).toBe('Test server connection');
    expect(tool?.inputSchema.properties).toEqual({});
  });

  it('should have consistent schema structure', () => {
    MCP_TOOLS.forEach(tool => {
      expect(tool.name).toBeTruthy();
      expect(tool.description).toBeTruthy();
      expect(tool.inputSchema).toBeDefined();
      expect(tool.inputSchema.type).toBe('object');
      expect(tool.inputSchema.properties).toBeDefined();
      expect(tool.inputSchema.required).toBeDefined();
      expect(Array.isArray(tool.inputSchema.required)).toBe(true);
    });
  });
});

describe('handleToolCall', () => {
  let mockConfig: MCPConfig;
  let mockLog: jest.Mock;

  beforeEach(() => {
    mockConfig = {
      mcpServers: {},
      unityDataPath: './test/data',
      timeout: {
        unityCommandTimeout: 30000,
        dataWaitTimeout: 5000
      }
    };
    
    mockLog = jest.fn();
    
    // モックのリセット
    jest.clearAllMocks();
  });

  describe('ping tool', () => {
    it('should return pong response', async () => {
      const result = await handleToolCall('ping', {}, '/test/data', mockConfig, mockLog);
      
      expect(result.content).toHaveLength(1);
      expect(result.content[0].type).toBe('text');
      expect(result.content[0].text).toBe('🏓 Pong! MCP Server is running and responsive.');
      expect(result.isError).toBe(false);
    });
  });

  describe('unity_info_realtime tool', () => {
    beforeEach(() => {
      // data-monitorのモック設定
      mockDataMonitor.getCachedData.mockReturnValue({
        project_info: { name: 'Test Project', version: '1.0.0' },
        scene_info: { name: 'Test Scene', gameObjectCount: 5 }
      });
      
      // CATEGORY_MAPは読み取り専用なので、モック設定を調整
      Object.defineProperty(mockDataMonitor, 'CATEGORY_MAP', {
        value: {
          'project': 'project_info',
          'scene': 'scene_info',
          'gameobjects': 'gameobjects',
          'assets': 'assets_info',
          'build': 'build_info',
          'editor': 'editor_state'
        },
        writable: false
      });
    });

    it('should return all data when category is "all"', async () => {
      const result = await handleToolCall('unity_info_realtime', { category: 'all' }, '/test/data', mockConfig, mockLog);
      
      expect(result.content[0].text).toContain('Unity Project Information (MCPLearning)');
      expect(result.content[0].text).toContain('Test Project');
      expect(result.isError).toBe(false);
    });

    it('should return specific category data', async () => {
      const result = await handleToolCall('unity_info_realtime', { category: 'project' }, '/test/data', mockConfig, mockLog);
      
      expect(result.content[0].text).toContain('Unity project Information');
      expect(result.content[0].text).toContain('Test Project');
      expect(result.isError).toBe(false);
    });

    it('should handle missing category data', async () => {
      const result = await handleToolCall('unity_info_realtime', { category: 'missing' }, '/test/data', mockConfig, mockLog);
      
      expect(result.content[0].text).toContain('No data found for category: missing');
      expect(result.isError).toBe(false);
    });

    it('should handle no cached data', async () => {
      mockDataMonitor.getCachedData.mockReturnValue({});
      
      const result = await handleToolCall('unity_info_realtime', { category: 'all' }, '/test/data', mockConfig, mockLog);
      
      expect(result.content[0].text).toContain('Unity project data is not available');
      expect(result.isError).toBe(false);
    });

    it('should use default category "all"', async () => {
      const result = await handleToolCall('unity_info_realtime', {}, '/test/data', mockConfig, mockLog);
      
      expect(result.content[0].text).toContain('Unity Project Information (MCPLearning)');
      expect(result.isError).toBe(false);
    });
  });

  describe('Unity command tools', () => {
    const unityCommandTools = ['create_cube', 'create_sphere', 'create_plane', 'create_gameobject'];

    unityCommandTools.forEach(toolName => {
      it(`should call executeUnityCommand for ${toolName}`, async () => {
        const mockExecuteUnityCommand = require('../src/unity-commands').executeUnityCommand;
        mockExecuteUnityCommand.mockResolvedValue({ success: true });

        const params = { name: 'TestObject' };
        
        await handleToolCall(toolName, params, '/test/data', mockConfig, mockLog);
        
        expect(mockExecuteUnityCommand).toHaveBeenCalledWith(
          toolName,
          params,
          '/test/data',
          mockConfig,
          mockLog
        );
      });
    });

    it('should propagate executeUnityCommand errors', async () => {
      const mockExecuteUnityCommand = require('../src/unity-commands').executeUnityCommand;
      const testError = new MCPError(ErrorCode.UNITY_COMMAND_FAILED, 'Command failed');
      mockExecuteUnityCommand.mockRejectedValue(testError);

      await expect(
        handleToolCall('create_cube', {}, '/test/data', mockConfig, mockLog)
      ).rejects.toThrow(testError);
    });
  });

  describe('unknown tool', () => {
    it('should throw error for unknown tool', async () => {
      await expect(
        handleToolCall('unknown_tool', {}, '/test/data', mockConfig, mockLog)
      ).rejects.toThrow(MCPError);
      
      await expect(
        handleToolCall('unknown_tool', {}, '/test/data', mockConfig, mockLog)
      ).rejects.toThrow(/Unknown tool: unknown_tool/);
    });
  });

  describe('error handling', () => {
    it('should handle empty tool name', async () => {
      await expect(
        handleToolCall('', {}, '/test/data', mockConfig, mockLog)
      ).rejects.toThrow(MCPError);
    });

    it('should handle null tool name', async () => {
      await expect(
        handleToolCall(null as any, {}, '/test/data', mockConfig, mockLog)
      ).rejects.toThrow(MCPError);
    });

    it('should handle undefined tool name', async () => {
      await expect(
        handleToolCall(undefined as any, {}, '/test/data', mockConfig, mockLog)
      ).rejects.toThrow(MCPError);
    });
  });
});