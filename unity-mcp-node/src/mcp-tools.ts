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