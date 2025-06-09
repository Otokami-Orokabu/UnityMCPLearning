/**
 * プロセス実行セキュリティ管理
 *
 * Unity MCP Learningにおけるプロセス実行の安全性を確保し、
 * 不正なコマンド実行や権限昇格を防止します。
 */
/**
 * 許可されたコマンドの種類
 */
export declare enum AllowedCommandType {
    NODE = "node",
    NPM = "npm",
    UNITY = "unity",
    GIT = "git",
    SYSTEM_INFO = "system-info"
}
/**
 * コマンド実行結果
 */
export interface CommandExecutionResult {
    success: boolean;
    output?: string;
    error?: string;
    executionTime: number;
    commandType: AllowedCommandType;
    sanitizedCommand: string;
}
/**
 * セキュリティ設定
 */
export interface SecurityConfig {
    maxExecutionTime: number;
    allowedDirectories: string[];
    blockedPatterns: string[];
    logExecutions: boolean;
    dryRun: boolean;
}
/**
 * プロセスセキュリティマネージャー
 */
export declare class ProcessSecurityManager {
    private static readonly DEFAULT_CONFIG;
    private static readonly ALLOWED_COMMANDS;
    private config;
    constructor(config?: Partial<SecurityConfig>);
    /**
     * 安全なコマンド実行
     */
    executeSecureCommand(command: string, workingDirectory?: string, commandType?: AllowedCommandType): Promise<CommandExecutionResult>;
    /**
     * コマンドの基本検証
     */
    private validateCommand;
    /**
     * コマンドタイプの検出
     */
    private detectCommandType;
    /**
     * 作業ディレクトリの検証
     */
    private validateWorkingDirectory;
    /**
     * コマンドのサニタイズ
     */
    private sanitizeCommand;
    /**
     * 実行ログの記録
     */
    private logExecution;
    /**
     * Node.js安全実行（Unity MCP専用）
     */
    executeNodeScript(scriptPath: string, args?: string[]): Promise<CommandExecutionResult>;
    /**
     * npm安全実行
     */
    executeNpmCommand(npmCommand: string, workingDir?: string): Promise<CommandExecutionResult>;
    /**
     * Git安全実行
     */
    executeGitCommand(gitCommand: string, workingDir?: string): Promise<CommandExecutionResult>;
    /**
     * システム情報取得
     */
    getSystemInfo(infoType: 'processes' | 'files' | 'directory'): Promise<CommandExecutionResult>;
    /**
     * 設定の更新
     */
    updateConfig(newConfig: Partial<SecurityConfig>): void;
    /**
     * 現在の設定を取得
     */
    getConfig(): SecurityConfig;
    /**
     * セキュリティ統計の取得
     */
    getSecurityStats(): {
        allowedCommandTypes: AllowedCommandType[];
        blockedPatterns: string[];
        allowedDirectories: string[];
        maxExecutionTime: number;
    };
}
/**
 * デフォルトのProcessSecurityManagerインスタンス
 */
export declare const defaultProcessSecurity: ProcessSecurityManager;
/**
 * セキュリティ設定のファクトリー関数
 */
export declare function createSecurityConfig(overrides?: Partial<SecurityConfig>): SecurityConfig;
/**
 * 安全なコマンド実行のヘルパー関数
 */
export declare function executeSecurely(command: string, options?: {
    workingDir?: string;
    commandType?: AllowedCommandType;
    config?: Partial<SecurityConfig>;
}): Promise<CommandExecutionResult>;
//# sourceMappingURL=process-security.d.ts.map