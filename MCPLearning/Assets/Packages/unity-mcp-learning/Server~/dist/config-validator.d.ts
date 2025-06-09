/**
 * Configuration Validation System
 * JSON Schemaを使用したmcp-config.jsonの検証
 */
export interface MCPConfig {
    mcpServers: {
        [serverName: string]: {
            command: string;
            args: string[];
            cwd?: string;
            env?: {
                [key: string]: string;
            };
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
export declare const DEFAULT_CONFIG: Partial<MCPConfig>;
/**
 * 設定ファイル検証クラス
 */
export declare class ConfigValidator {
    private ajv;
    private schema;
    constructor();
    /**
     * JSON Schemaを読み込み
     */
    private loadSchema;
    /**
     * 設定ファイルを検証
     */
    validateConfig(config: any): MCPConfig;
    /**
     * デフォルト値をマージ
     */
    private mergeDefaults;
    /**
     * ビジネスルール検証
     * JSON Schemaでは表現が困難な複雑な検証
     */
    private validateBusinessRules;
    /**
     * Unity データパスの検証
     */
    private validateUnityDataPath;
    /**
     * MCPサーバー設定の検証
     */
    private validateMCPServers;
    /**
     * タイムアウト値の検証
     */
    private validateTimeouts;
}
/**
 * 設定ファイルを読み込んで検証
 */
export declare function loadAndValidateConfig(configPath: string): MCPConfig;
//# sourceMappingURL=config-validator.d.ts.map