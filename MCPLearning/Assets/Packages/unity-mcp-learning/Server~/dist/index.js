"use strict";
/**
 * @fileoverview Unity MCP Server - Main Entry Point
 * @description MCPプロトコル対応のUnity情報取得・制御サーバー
 *
 * このモジュールはMCP (Model Context Protocol) サーバーのメインエントリーポイントです。
 * Unity Editorとの双方向通信を提供し、リアルタイムな情報取得とコマンド実行を可能にします。
 *
 * @author Unity MCP Learning Project
 * @version 1.0.0
 * @since 1.0.0
 *
 * @example
 * ```bash
 * # サーバーを起動
 * npm start
 *
 * # 環境変数で言語設定
 * MCP_LANGUAGE=ja npm start
 *
 * # 設定ファイル指定
 * MCP_CONFIG_PATH=/path/to/config.json npm start
 * ```
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
const readline_1 = require("readline");
const crypto_1 = require("crypto");
const path = __importStar(require("path"));
const errors_js_1 = require("./errors.js");
const config_validator_js_1 = require("./config-validator.js");
const data_monitor_js_1 = require("./data-monitor.js");
const i18n_js_1 = require("./i18n.js");
const json_rpc_js_1 = require("./json-rpc.js");
const mcp_tools_js_1 = require("./mcp-tools.js");
/**
 * グローバル設定オブジェクト
 * @internal
 */
let globalConfig;
/**
 * Unityデータディレクトリのパス
 * @internal
 */
let dataPath;
/**
 * ログ出力関数（デバッグ用）
 * @param args - ログに出力する引数
 * @internal
 */
function log(...args) {
    console.error('[MCP Server]', ...args);
}
/**
 * サーバー終了時のリソースクリーンアップ
 *
 * このメソッドは以下の処理を行います：
 * - ファイル監視のdebounceタイマーをクリア
 * - その他のリソースを適切に解放
 *
 * @remarks プロセス終了時（SIGINT, SIGTERM）に自動的に呼び出されます
 * @internal
 */
function cleanup() {
    log((0, i18n_js_1.getMessage)('server.cleanup'));
    try {
        (0, data_monitor_js_1.clearDebounceTimers)();
        log((0, i18n_js_1.getMessage)('server.cleanup.completed'));
    }
    catch (error) {
        log((0, i18n_js_1.getMessage)('server.cleanup.error', { error: error }));
    }
}
// 設定初期化関数
function initializeConfig() {
    try {
        // 環境変数から言語設定を読み込み
        const language = process.env.MCP_LANGUAGE;
        if (language) {
            (0, i18n_js_1.setLanguage)(language);
        }
        // 設定ファイルパスを決定
        const configPath = process.env.MCP_CONFIG_PATH ||
            path.join(__dirname, '..', 'mcp-config.json');
        log((0, i18n_js_1.getMessage)('server.config.loading', { configPath }));
        // 設定ファイル検証と読み込み
        globalConfig = (0, config_validator_js_1.loadAndValidateConfig)(configPath);
        // データパスを設定（設定ファイルのディレクトリを基準とする）
        const configDir = path.dirname(configPath);
        dataPath = path.resolve(configDir, globalConfig.unityDataPath);
        log((0, i18n_js_1.getMessage)('server.config.loaded'));
        log(`- Unity Data Path: ${dataPath}`);
        log(`- Log Level: ${globalConfig.logLevel}`);
        log(`- Unity Command Timeout: ${globalConfig.timeout?.unityCommandTimeout}ms`);
        log(`- Language: ${(0, i18n_js_1.getCurrentLanguage)()}`);
    }
    catch (error) {
        if (error instanceof errors_js_1.MCPError) {
            log((0, i18n_js_1.getMessage)('server.config.error', { code: error.code, message: error.message }));
            if (error.context) {
                log('Error context:', error.context);
            }
        }
        else {
            log('Unexpected configuration error:', error);
        }
        // 設定読み込み失敗時は起動を停止
        process.exit(1);
    }
}
// メソッドハンドラー
async function handleMethod(method, params) {
    switch (method) {
        case 'ping':
            return {
                message: 'pong',
                timestamp: new Date().toISOString(),
                id: (0, crypto_1.randomUUID)()
            };
        case 'initialize':
            return {
                protocolVersion: '2024-11-05',
                capabilities: {
                    tools: {},
                    prompts: {},
                    resources: {}
                },
                serverInfo: {
                    name: globalConfig.server?.name || 'unity-mcp-server',
                    version: globalConfig.server?.version || '1.0.0'
                }
            };
        case 'notifications/initialized':
            // 初期化完了通知 - 特に処理は不要
            return {};
        case 'tools/list':
            return {
                tools: mcp_tools_js_1.MCP_TOOLS
            };
        case 'tools/call':
            const toolName = params?.name;
            if (!toolName) {
                throw new errors_js_1.MCPError(errors_js_1.ErrorCode.INVALID_PARAMETER, 'error.tool_name_required');
            }
            return await (0, mcp_tools_js_1.handleToolCall)(toolName, params?.arguments, dataPath, globalConfig, log);
        default:
            throw new errors_js_1.MCPError(errors_js_1.ErrorCode.UNKNOWN_ERROR, 'error.unknown_method', { method });
    }
}
// メイン処理
async function main() {
    log((0, i18n_js_1.getMessage)('server.starting'));
    log('Process ID:', process.pid);
    log('Working directory:', process.cwd());
    // 設定ファイル検証と初期化
    initializeConfig();
    // ファイル監視開始
    (0, data_monitor_js_1.startFileWatching)(dataPath, log);
    // 標準入力からJSON-RPCメッセージを読み取る
    const rl = (0, readline_1.createInterface)({
        input: process.stdin,
        output: process.stdout,
        terminal: false
    });
    rl.on('line', async (line) => {
        try {
            const message = JSON.parse(line);
            const isNotification = !('id' in message);
            if (isNotification) {
                // 通知の場合
                const notification = message;
                log('Received notification:', notification);
                try {
                    await handleMethod(notification.method, notification.params);
                    // 通知にはレスポンスを返さない
                }
                catch (error) {
                    log('Error handling notification:', error);
                    // 通知のエラーもレスポンスしない
                }
            }
            else {
                // リクエストの場合
                const request = message;
                log('Received request:', request);
                try {
                    // リクエストの基本検証
                    (0, json_rpc_js_1.validateJsonRpcRequest)(request);
                    // メソッド実行
                    const result = await handleMethod(request.method, request.params);
                    (0, json_rpc_js_1.sendSuccessResponse)(request.id, result);
                }
                catch (error) {
                    log('Error handling request:', error);
                    (0, json_rpc_js_1.sendErrorResponse)(request.id, error);
                }
            }
        }
        catch (error) {
            log('Failed to parse JSON:', error);
            // パースエラーの場合（idが不明なので0を使用）
            (0, json_rpc_js_1.sendResponse)({
                jsonrpc: '2.0',
                id: 0,
                error: {
                    code: json_rpc_js_1.JSON_RPC_ERRORS.PARSE_ERROR,
                    message: 'Parse error'
                }
            });
        }
    });
    // 標準入力が閉じられたとき
    rl.on('close', () => {
        log('Standard input closed');
        cleanup();
        process.exit(0);
    });
    // プロセス終了時の処理
    process.on('SIGINT', () => {
        log('Received SIGINT');
        cleanup();
        process.exit(0);
    });
    process.on('SIGTERM', () => {
        log('Received SIGTERM');
        cleanup();
        process.exit(0);
    });
    // Keep the process alive
    process.stdin.resume();
}
// サーバーを起動
main().catch((error) => {
    log('Fatal error:', error);
    process.exit(1);
});
//# sourceMappingURL=index.js.map