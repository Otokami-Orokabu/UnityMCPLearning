/**
 * プロセス実行セキュリティ管理
 * 
 * Unity MCP Learningにおけるプロセス実行の安全性を確保し、
 * 不正なコマンド実行や権限昇格を防止します。
 */

import * as path from 'path';
import * as fs from 'fs';
import { promisify } from 'util';
import { exec } from 'child_process';

const execAsync = promisify(exec);

/**
 * 許可されたコマンドの種類
 */
export enum AllowedCommandType {
    NODE = 'node',
    NPM = 'npm',
    UNITY = 'unity',
    GIT = 'git',
    SYSTEM_INFO = 'system-info'
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
    maxExecutionTime: number;          // 最大実行時間（ms）
    allowedDirectories: string[];      // 実行許可ディレクトリ
    blockedPatterns: string[];         // 禁止パターン
    logExecutions: boolean;            // 実行ログの記録
    dryRun: boolean;                   // ドライランモード
}

/**
 * プロセスセキュリティマネージャー
 */
export class ProcessSecurityManager {
    private static readonly DEFAULT_CONFIG: SecurityConfig = {
        maxExecutionTime: 30000,  // 30秒
        allowedDirectories: [
            'unity-mcp-node',
            'MCPLearning',
            '.git'
        ],
        blockedPatterns: [
            'rm -rf',
            'sudo',
            'chmod +x',
            'curl',
            'wget',
            'nc ',
            'netcat',
            'ssh',
            'scp',
            'ftp',
            'python -c',
            'eval',
            'exec',
            '$()',
            '`',
            '&&',
            '||',
            ';',
            '|',
            '>',
            '<',
            '&'
        ],
        logExecutions: true,
        dryRun: false
    };

    private static readonly ALLOWED_COMMANDS: Map<AllowedCommandType, string[]> = new Map([
        [AllowedCommandType.NODE, ['node']],
        [AllowedCommandType.NPM, ['npm', 'npx']],
        [AllowedCommandType.UNITY, ['unity', 'Unity', 'Unity.exe']],
        [AllowedCommandType.GIT, ['git']],
        [AllowedCommandType.SYSTEM_INFO, ['ps', 'ls', 'pwd', 'which', 'where']]
    ]);

    private config: SecurityConfig;

    constructor(config?: Partial<SecurityConfig>) {
        this.config = { ...ProcessSecurityManager.DEFAULT_CONFIG, ...config };
    }

    /**
     * 安全なコマンド実行
     */
    async executeSecureCommand(
        command: string,
        workingDirectory?: string,
        commandType?: AllowedCommandType
    ): Promise<CommandExecutionResult> {
        const startTime = Date.now();
        
        try {
            // 1. コマンドの基本検証
            const validationResult = this.validateCommand(command);
            if (!validationResult.isValid) {
                return {
                    success: false,
                    error: `Command validation failed: ${validationResult.reason}`,
                    executionTime: Date.now() - startTime,
                    commandType: commandType || AllowedCommandType.SYSTEM_INFO,
                    sanitizedCommand: this.sanitizeCommand(command)
                };
            }

            // 2. コマンドタイプの判定
            const detectedType = commandType || this.detectCommandType(command);
            if (!detectedType) {
                return {
                    success: false,
                    error: 'Command type not allowed',
                    executionTime: Date.now() - startTime,
                    commandType: AllowedCommandType.SYSTEM_INFO,
                    sanitizedCommand: this.sanitizeCommand(command)
                };
            }

            // 3. 作業ディレクトリの検証
            const safeWorkingDir = await this.validateWorkingDirectory(workingDirectory);
            if (!safeWorkingDir) {
                return {
                    success: false,
                    error: 'Working directory not allowed',
                    executionTime: Date.now() - startTime,
                    commandType: detectedType,
                    sanitizedCommand: this.sanitizeCommand(command)
                };
            }

            // 4. コマンドのサニタイズ
            const sanitizedCommand = this.sanitizeCommand(command);

            // 5. ドライランモードチェック
            if (this.config.dryRun) {
                this.logExecution(sanitizedCommand, safeWorkingDir, 'DRY_RUN');
                return {
                    success: true,
                    output: `[DRY RUN] Would execute: ${sanitizedCommand}`,
                    executionTime: Date.now() - startTime,
                    commandType: detectedType,
                    sanitizedCommand
                };
            }

            // 6. 実際のコマンド実行
            this.logExecution(sanitizedCommand, safeWorkingDir, 'EXECUTING');
            
            const { stdout, stderr } = await Promise.race([
                execAsync(sanitizedCommand, { 
                    cwd: safeWorkingDir,
                    timeout: this.config.maxExecutionTime,
                    maxBuffer: 1024 * 1024 // 1MB
                }),
                new Promise<never>((_, reject) => 
                    setTimeout(() => reject(new Error('Execution timeout')), this.config.maxExecutionTime)
                )
            ]);

            const result: CommandExecutionResult = {
                success: true,
                output: stdout,
                error: stderr || undefined,
                executionTime: Date.now() - startTime,
                commandType: detectedType,
                sanitizedCommand
            };

            this.logExecution(sanitizedCommand, safeWorkingDir, 'SUCCESS', result);
            return result;

        } catch (error) {
            const result: CommandExecutionResult = {
                success: false,
                error: error instanceof Error ? error.message : 'Unknown error',
                executionTime: Date.now() - startTime,
                commandType: commandType || AllowedCommandType.SYSTEM_INFO,
                sanitizedCommand: this.sanitizeCommand(command)
            };

            this.logExecution(command, workingDirectory || process.cwd(), 'ERROR', result);
            return result;
        }
    }

    /**
     * コマンドの基本検証
     */
    private validateCommand(command: string): { isValid: boolean; reason?: string } {
        if (!command || command.trim().length === 0) {
            return { isValid: false, reason: 'Empty command' };
        }

        // 危険なパターンのチェック
        const lowerCommand = command.toLowerCase();
        for (const pattern of this.config.blockedPatterns) {
            if (lowerCommand.includes(pattern.toLowerCase())) {
                return { isValid: false, reason: `Blocked pattern detected: ${pattern}` };
            }
        }

        // コマンド長制限
        if (command.length > 1000) {
            return { isValid: false, reason: 'Command too long' };
        }

        // 基本的な文字セット検証
        const allowedChars = /^[a-zA-Z0-9\s\-_./\\:='"@#]+$/;
        if (!allowedChars.test(command)) {
            return { isValid: false, reason: 'Invalid characters in command' };
        }

        return { isValid: true };
    }

    /**
     * コマンドタイプの検出
     */
    private detectCommandType(command: string): AllowedCommandType | null {
        const firstWord = command.trim().split(' ')[0].toLowerCase();
        
        for (const [type, commands] of ProcessSecurityManager.ALLOWED_COMMANDS) {
            if (commands.some(cmd => firstWord === cmd.toLowerCase() || firstWord.endsWith(cmd.toLowerCase()))) {
                return type;
            }
        }

        return null;
    }

    /**
     * 作業ディレクトリの検証
     */
    private async validateWorkingDirectory(workingDirectory?: string): Promise<string | null> {
        const baseDir = workingDirectory || process.cwd();
        
        try {
            const resolvedDir = path.resolve(baseDir);
            
            // ディレクトリの存在確認
            const stat = await fs.promises.stat(resolvedDir);
            if (!stat.isDirectory()) {
                return null;
            }

            // 許可ディレクトリのチェック
            const projectRoot = process.cwd();
            if (!resolvedDir.startsWith(projectRoot)) {
                return null;
            }

            // 許可リストとの照合
            const relativePath = path.relative(projectRoot, resolvedDir);
            const isAllowed = this.config.allowedDirectories.some(allowedDir => 
                relativePath === allowedDir || 
                relativePath.startsWith(allowedDir + path.sep)
            ) || relativePath === '' || relativePath === '.'; // プロジェクトルート

            return isAllowed ? resolvedDir : null;

        } catch (error) {
            return null;
        }
    }

    /**
     * コマンドのサニタイズ
     */
    private sanitizeCommand(command: string): string {
        if (!command) return '';
        return command
            .trim()
            .replace(/\s+/g, ' ')  // 連続スペースを単一スペースに
            .replace(/['"]/g, '"') // クォートの統一
            .substring(0, 500);    // 長さ制限
    }

    /**
     * 実行ログの記録
     */
    private logExecution(
        command: string, 
        workingDir: string, 
        status: string, 
        result?: CommandExecutionResult
    ): void {
        if (!this.config.logExecutions) return;

        const logEntry = {
            timestamp: new Date().toISOString(),
            command: this.sanitizeCommand(command),
            workingDirectory: workingDir,
            status,
            executionTime: result?.executionTime,
            success: result?.success,
            error: result?.error
        };

        console.log(`[ProcessSecurity] ${JSON.stringify(logEntry)}`);
    }

    /**
     * Node.js安全実行（Unity MCP専用）
     */
    async executeNodeScript(scriptPath: string, args: string[] = []): Promise<CommandExecutionResult> {
        const command = `node "${scriptPath}" ${args.map(arg => `"${arg}"`).join(' ')}`;
        return this.executeSecureCommand(command, undefined, AllowedCommandType.NODE);
    }

    /**
     * npm安全実行
     */
    async executeNpmCommand(npmCommand: string, workingDir?: string): Promise<CommandExecutionResult> {
        const command = `npm ${npmCommand}`;
        return this.executeSecureCommand(command, workingDir, AllowedCommandType.NPM);
    }

    /**
     * Git安全実行
     */
    async executeGitCommand(gitCommand: string, workingDir?: string): Promise<CommandExecutionResult> {
        const command = `git ${gitCommand}`;
        return this.executeSecureCommand(command, workingDir, AllowedCommandType.GIT);
    }

    /**
     * システム情報取得
     */
    async getSystemInfo(infoType: 'processes' | 'files' | 'directory'): Promise<CommandExecutionResult> {
        const commands = {
            processes: 'ps aux',
            files: 'ls -la',
            directory: 'pwd'
        };

        const command = commands[infoType];
        return this.executeSecureCommand(command, undefined, AllowedCommandType.SYSTEM_INFO);
    }

    /**
     * 設定の更新
     */
    updateConfig(newConfig: Partial<SecurityConfig>): void {
        this.config = { ...this.config, ...newConfig };
    }

    /**
     * 現在の設定を取得
     */
    getConfig(): SecurityConfig {
        return { ...this.config };
    }

    /**
     * セキュリティ統計の取得
     */
    getSecurityStats(): {
        allowedCommandTypes: AllowedCommandType[];
        blockedPatterns: string[];
        allowedDirectories: string[];
        maxExecutionTime: number;
    } {
        return {
            allowedCommandTypes: Array.from(ProcessSecurityManager.ALLOWED_COMMANDS.keys()),
            blockedPatterns: this.config.blockedPatterns,
            allowedDirectories: this.config.allowedDirectories,
            maxExecutionTime: this.config.maxExecutionTime
        };
    }
}

/**
 * デフォルトのProcessSecurityManagerインスタンス
 */
export const defaultProcessSecurity = new ProcessSecurityManager();

/**
 * セキュリティ設定のファクトリー関数
 */
export function createSecurityConfig(overrides?: Partial<SecurityConfig>): SecurityConfig {
    return { ...ProcessSecurityManager['DEFAULT_CONFIG'], ...overrides };
}

/**
 * 安全なコマンド実行のヘルパー関数
 */
export async function executeSecurely(
    command: string, 
    options?: {
        workingDir?: string;
        commandType?: AllowedCommandType;
        config?: Partial<SecurityConfig>;
    }
): Promise<CommandExecutionResult> {
    const manager = options?.config 
        ? new ProcessSecurityManager(options.config)
        : defaultProcessSecurity;
    
    return manager.executeSecureCommand(
        command, 
        options?.workingDir, 
        options?.commandType
    );
}