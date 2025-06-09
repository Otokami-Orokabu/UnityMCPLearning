/**
 * MCP Tools Definition
 * MCPプロトコルのツール定義と処理
 */
import { MCPConfig } from './config-validator.js';
export declare const MCP_TOOLS: ({
    name: string;
    description: string;
    inputSchema: {
        type: string;
        properties: {
            category: {
                type: string;
                enum: string[];
                description: string;
                default: string;
            };
            name?: undefined;
            position?: undefined;
            scale?: undefined;
            color?: undefined;
            filter?: undefined;
            limit?: undefined;
            timeout?: undefined;
        };
        required: never[];
    };
} | {
    name: string;
    description: string;
    inputSchema: {
        type: string;
        properties: {
            name: {
                type: string;
                description: string;
                default: string;
            };
            position: {
                type: string;
                properties: {
                    x: {
                        type: string;
                        default: number;
                    };
                    y: {
                        type: string;
                        default: number;
                    };
                    z: {
                        type: string;
                        default: number;
                    };
                };
                description: string;
            };
            scale: {
                type: string;
                properties: {
                    x: {
                        type: string;
                        default: number;
                    };
                    y: {
                        type: string;
                        default: number;
                    };
                    z: {
                        type: string;
                        default: number;
                    };
                };
                description: string;
            };
            color: {
                type: string;
                description: string;
                examples: string[];
            };
            category?: undefined;
            filter?: undefined;
            limit?: undefined;
            timeout?: undefined;
        };
        required: never[];
    };
} | {
    name: string;
    description: string;
    inputSchema: {
        type: string;
        properties: {
            name: {
                type: string;
                description: string;
                default: string;
            };
            position: {
                type: string;
                properties: {
                    x: {
                        type: string;
                        default: number;
                    };
                    y: {
                        type: string;
                        default: number;
                    };
                    z: {
                        type: string;
                        default: number;
                    };
                };
                description: string;
            };
            scale: {
                type: string;
                properties: {
                    x: {
                        type: string;
                        default: number;
                    };
                    y: {
                        type: string;
                        default: number;
                    };
                    z: {
                        type: string;
                        default: number;
                    };
                };
                description: string;
            };
            category?: undefined;
            color?: undefined;
            filter?: undefined;
            limit?: undefined;
            timeout?: undefined;
        };
        required: never[];
    };
} | {
    name: string;
    description: string;
    inputSchema: {
        type: string;
        properties: {
            name: {
                type: string;
                description: string;
                default: string;
            };
            position: {
                type: string;
                properties: {
                    x: {
                        type: string;
                        default: number;
                    };
                    y: {
                        type: string;
                        default: number;
                    };
                    z: {
                        type: string;
                        default: number;
                    };
                };
                description: string;
            };
            category?: undefined;
            scale?: undefined;
            color?: undefined;
            filter?: undefined;
            limit?: undefined;
            timeout?: undefined;
        };
        required: never[];
    };
} | {
    name: string;
    description: string;
    inputSchema: {
        type: string;
        properties: {
            category?: undefined;
            name?: undefined;
            position?: undefined;
            scale?: undefined;
            color?: undefined;
            filter?: undefined;
            limit?: undefined;
            timeout?: undefined;
        };
        required: never[];
    };
} | {
    name: string;
    description: string;
    inputSchema: {
        type: string;
        properties: {
            filter: {
                type: string;
                enum: string[];
                description: string;
                default: string;
            };
            limit: {
                type: string;
                description: string;
                default: number;
            };
            category?: undefined;
            name?: undefined;
            position?: undefined;
            scale?: undefined;
            color?: undefined;
            timeout?: undefined;
        };
        required: never[];
    };
} | {
    name: string;
    description: string;
    inputSchema: {
        type: string;
        properties: {
            timeout: {
                type: string;
                description: string;
                default: number;
            };
            category?: undefined;
            name?: undefined;
            position?: undefined;
            scale?: undefined;
            color?: undefined;
            filter?: undefined;
            limit?: undefined;
        };
        required: never[];
    };
})[];
export declare function handleToolCall(toolName: string, params: any, dataPath: string, config: MCPConfig, log: (...args: any[]) => void): Promise<any>;
//# sourceMappingURL=mcp-tools.d.ts.map