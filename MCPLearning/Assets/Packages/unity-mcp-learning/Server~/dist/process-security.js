"use strict";
/**
 * プロセス実行セキュリティ管理
 *
 * Unity MCP Learningにおけるプロセス実行の安全性を確保し、
 * 不正なコマンド実行や権限昇格を防止します。
 */
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.defaultProcessSecurity = exports.ProcessSecurityManager = exports.AllowedCommandType = void 0;
exports.createSecurityConfig = createSecurityConfig;
exports.executeSecurely = executeSecurely;
const path = __importStar(require("path"));
const fs = __importStar(require("fs"));
const util_1 = require("util");
const child_process_1 = require("child_process");
const execAsync = (0, util_1.promisify)(child_process_1.exec);
/**
 * 許可されたコマンドの種類
 */
var AllowedCommandType;
(function (AllowedCommandType) {
    AllowedCommandType["NODE"] = "node";
    AllowedCommandType["NPM"] = "npm";
    AllowedCommandType["UNITY"] = "unity";
    AllowedCommandType["GIT"] = "git";
    AllowedCommandType["SYSTEM_INFO"] = "system-info";
})(AllowedCommandType || (exports.AllowedCommandType = AllowedCommandType = {}));
/**
 * プロセスセキュリティマネージャー
 */
class ProcessSecurityManager {
    static DEFAULT_CONFIG = {
        maxExecutionTime: 30000, // 30秒
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
    static ALLOWED_COMMANDS = new Map([
        [AllowedCommandType.NODE, ['node']],
        [AllowedCommandType.NPM, ['npm', 'npx']],
        [AllowedCommandType.UNITY, ['unity', 'Unity', 'Unity.exe']],
        [AllowedCommandType.GIT, ['git']],
        [AllowedCommandType.SYSTEM_INFO, ['ps', 'ls', 'pwd', 'which', 'where']]
    ]);
    config;
    constructor(config) {
        this.config = { ...ProcessSecurityManager.DEFAULT_CONFIG, ...config };
    }
    /**
     * 安全なコマンド実行
     */
    async executeSecureCommand(command, workingDirectory, commandType) {
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
                new Promise((_, reject) => setTimeout(() => reject(new Error('Execution timeout')), this.config.maxExecutionTime))
            ]);
            const result = {
                success: true,
                output: stdout,
                error: stderr || undefined,
                executionTime: Date.now() - startTime,
                commandType: detectedType,
                sanitizedCommand
            };
            this.logExecution(sanitizedCommand, safeWorkingDir, 'SUCCESS', result);
            return result;
        }
        catch (error) {
            const result = {
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
    validateCommand(command) {
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
    detectCommandType(command) {
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
    async validateWorkingDirectory(workingDirectory) {
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
            const isAllowed = this.config.allowedDirectories.some(allowedDir => relativePath === allowedDir ||
                relativePath.startsWith(allowedDir + path.sep)) || relativePath === '' || relativePath === '.'; // プロジェクトルート
            return isAllowed ? resolvedDir : null;
        }
        catch (error) {
            return null;
        }
    }
    /**
     * コマンドのサニタイズ
     */
    sanitizeCommand(command) {
        if (!command)
            return '';
        return command
            .trim()
            .replace(/\s+/g, ' ') // 連続スペースを単一スペースに
            .replace(/['"]/g, '"') // クォートの統一
            .substring(0, 500); // 長さ制限
    }
    /**
     * 実行ログの記録
     */
    logExecution(command, workingDir, status, result) {
        if (!this.config.logExecutions)
            return;
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
    async executeNodeScript(scriptPath, args = []) {
        const command = `node "${scriptPath}" ${args.map(arg => `"${arg}"`).join(' ')}`;
        return this.executeSecureCommand(command, undefined, AllowedCommandType.NODE);
    }
    /**
     * npm安全実行
     */
    async executeNpmCommand(npmCommand, workingDir) {
        const command = `npm ${npmCommand}`;
        return this.executeSecureCommand(command, workingDir, AllowedCommandType.NPM);
    }
    /**
     * Git安全実行
     */
    async executeGitCommand(gitCommand, workingDir) {
        const command = `git ${gitCommand}`;
        return this.executeSecureCommand(command, workingDir, AllowedCommandType.GIT);
    }
    /**
     * システム情報取得
     */
    async getSystemInfo(infoType) {
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
    updateConfig(newConfig) {
        this.config = { ...this.config, ...newConfig };
    }
    /**
     * 現在の設定を取得
     */
    getConfig() {
        return { ...this.config };
    }
    /**
     * セキュリティ統計の取得
     */
    getSecurityStats() {
        return {
            allowedCommandTypes: Array.from(ProcessSecurityManager.ALLOWED_COMMANDS.keys()),
            blockedPatterns: this.config.blockedPatterns,
            allowedDirectories: this.config.allowedDirectories,
            maxExecutionTime: this.config.maxExecutionTime
        };
    }
}
exports.ProcessSecurityManager = ProcessSecurityManager;
/**
 * デフォルトのProcessSecurityManagerインスタンス
 */
exports.defaultProcessSecurity = new ProcessSecurityManager();
/**
 * セキュリティ設定のファクトリー関数
 */
function createSecurityConfig(overrides) {
    return { ...ProcessSecurityManager['DEFAULT_CONFIG'], ...overrides };
}
/**
 * 安全なコマンド実行のヘルパー関数
 */
async function executeSecurely(command, options) {
    const manager = options?.config
        ? new ProcessSecurityManager(options.config)
        : exports.defaultProcessSecurity;
    return manager.executeSecureCommand(command, options?.workingDir, options?.commandType);
}
//# sourceMappingURL=process-security.js.map