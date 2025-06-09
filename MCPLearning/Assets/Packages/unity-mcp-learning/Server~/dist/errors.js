"use strict";
/**
 * @fileoverview MCP Unity Server Error Handling System
 * @description エラーコードの統一管理とエラークラスの実装
 *
 * このモジュールは統一されたエラーハンドリングシステムを提供します。
 * エラーコードによる体系的な分類と、国際化対応のエラーメッセージをサポートします。
 *
 * @example
 * ```typescript
 * import { ErrorCode, MCPError } from './errors';
 *
 * // 基本的なエラー作成
 * const error = new MCPError(
 *   ErrorCode.INVALID_PARAMETER,
 *   'Invalid parameter provided'
 * );
 *
 * // 国際化対応エラー
 * const i18nError = new MCPError(
 *   ErrorCode.VALIDATION_ERROR,
 *   'validation.name_empty',
 *   { fieldName: 'userName' },
 *   { fieldName: 'userName' }
 * );
 *
 * // 異なる言語でメッセージを取得
 * console.log(i18nError.getLocalizedMessage('en')); // English
 * console.log(i18nError.getLocalizedMessage('ja')); // 日本語
 * ```
 *
 * @author Unity MCP Learning Project
 * @version 1.0.0
 * @since 1.0.0
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.MCPError = exports.ErrorCode = void 0;
exports.isValidationError = isValidationError;
exports.isExecutionError = isExecutionError;
exports.isSystemError = isSystemError;
exports.createErrorResponse = createErrorResponse;
const i18n_js_1 = require("./i18n.js");
/**
 * エラーコード定義
 * 体系的なエラー分類と識別
 */
var ErrorCode;
(function (ErrorCode) {
    // 通信エラー (1xxx)
    ErrorCode[ErrorCode["CONNECTION_ERROR"] = 1000] = "CONNECTION_ERROR";
    ErrorCode[ErrorCode["TIMEOUT_ERROR"] = 1001] = "TIMEOUT_ERROR";
    // 検証エラー (2xxx)
    ErrorCode[ErrorCode["VALIDATION_ERROR"] = 2000] = "VALIDATION_ERROR";
    ErrorCode[ErrorCode["INVALID_PARAMETER"] = 2001] = "INVALID_PARAMETER";
    ErrorCode[ErrorCode["MISSING_PARAMETER"] = 2002] = "MISSING_PARAMETER";
    ErrorCode[ErrorCode["TYPE_MISMATCH"] = 2003] = "TYPE_MISMATCH";
    // 実行エラー (3xxx)
    ErrorCode[ErrorCode["EXECUTION_ERROR"] = 3000] = "EXECUTION_ERROR";
    ErrorCode[ErrorCode["UNITY_COMMAND_FAILED"] = 3001] = "UNITY_COMMAND_FAILED";
    ErrorCode[ErrorCode["FILE_NOT_FOUND"] = 3002] = "FILE_NOT_FOUND";
    ErrorCode[ErrorCode["FILE_READ_ERROR"] = 3003] = "FILE_READ_ERROR";
    ErrorCode[ErrorCode["FILE_WRITE_ERROR"] = 3004] = "FILE_WRITE_ERROR";
    // 設定エラー (4xxx)
    ErrorCode[ErrorCode["CONFIG_ERROR"] = 4000] = "CONFIG_ERROR";
    ErrorCode[ErrorCode["INVALID_CONFIG"] = 4001] = "INVALID_CONFIG";
    ErrorCode[ErrorCode["MISSING_CONFIG"] = 4002] = "MISSING_CONFIG";
    // システムエラー (5xxx)
    ErrorCode[ErrorCode["SYSTEM_ERROR"] = 5000] = "SYSTEM_ERROR";
    ErrorCode[ErrorCode["UNKNOWN_ERROR"] = 5999] = "UNKNOWN_ERROR";
})(ErrorCode || (exports.ErrorCode = ErrorCode = {}));
/**
 * MCPエラークラス
 * 統一されたエラー情報の管理（国際化対応）
 */
class MCPError extends Error {
    code;
    timestamp;
    context;
    messageKey;
    messageParams;
    constructor(code, message, context, messageParams) {
        // messageParams が提供された場合はメッセージキーとして扱う
        const isMessageKey = messageParams !== undefined;
        const finalMessage = isMessageKey
            ? (0, i18n_js_1.getMessage)(message, messageParams)
            : message;
        super(finalMessage);
        this.name = 'MCPError';
        this.code = code;
        this.timestamp = new Date();
        this.context = context;
        this.messageKey = isMessageKey ? message : undefined;
        this.messageParams = messageParams;
        // Ensure prototype chain is properly set up for instanceof checks
        Object.setPrototypeOf(this, MCPError.prototype);
    }
    /**
     * エラー情報をJSON形式で取得
     */
    toJSON() {
        return {
            name: this.name,
            code: this.code,
            message: this.message,
            messageKey: this.messageKey,
            messageParams: this.messageParams,
            timestamp: this.timestamp.toISOString(),
            context: this.context
        };
    }
    /**
     * 異なる言語でエラーメッセージを取得
     * @param language 言語コード
     * @returns 指定された言語のエラーメッセージ
     */
    getLocalizedMessage(language) {
        if (!this.messageKey) {
            return this.message;
        }
        // 一時的に言語を変更してメッセージを取得
        const { setLanguage, getCurrentLanguage, getMessage: getMsg } = require('./i18n.js');
        const currentLang = getCurrentLanguage();
        if (language && language !== currentLang) {
            setLanguage(language);
            const localizedMessage = getMsg(this.messageKey, this.messageParams);
            setLanguage(currentLang); // 元の言語に戻す
            return localizedMessage;
        }
        return getMsg(this.messageKey, this.messageParams);
    }
    /**
     * エラーコードから人間が読みやすい説明を取得
     */
    static getErrorDescription(code) {
        const descriptions = {
            [ErrorCode.CONNECTION_ERROR]: 'Connection to Unity failed',
            [ErrorCode.TIMEOUT_ERROR]: 'Operation timed out',
            [ErrorCode.VALIDATION_ERROR]: 'Validation failed',
            [ErrorCode.INVALID_PARAMETER]: 'Invalid parameter provided',
            [ErrorCode.MISSING_PARAMETER]: 'Required parameter is missing',
            [ErrorCode.TYPE_MISMATCH]: 'Parameter type mismatch',
            [ErrorCode.EXECUTION_ERROR]: 'Execution failed',
            [ErrorCode.UNITY_COMMAND_FAILED]: 'Unity command execution failed',
            [ErrorCode.FILE_NOT_FOUND]: 'File not found',
            [ErrorCode.FILE_READ_ERROR]: 'Failed to read file',
            [ErrorCode.FILE_WRITE_ERROR]: 'Failed to write file',
            [ErrorCode.CONFIG_ERROR]: 'Configuration error',
            [ErrorCode.INVALID_CONFIG]: 'Invalid configuration',
            [ErrorCode.MISSING_CONFIG]: 'Configuration file missing',
            [ErrorCode.SYSTEM_ERROR]: 'System error occurred',
            [ErrorCode.UNKNOWN_ERROR]: 'Unknown error occurred'
        };
        return descriptions[code] || 'Unknown error';
    }
    /**
     * エラーレベルを取得（ログレベル判定用）
     */
    getLevel() {
        if (this.code >= 5000)
            return 'error'; // システムエラー
        if (this.code >= 3000)
            return 'error'; // 実行エラー
        if (this.code >= 2000)
            return 'warn'; // 検証エラー
        return 'info'; // その他
    }
}
exports.MCPError = MCPError;
/**
 * エラーハンドリングヘルパー関数
 */
function isValidationError(error) {
    return error instanceof MCPError && error.code >= 2000 && error.code < 3000;
}
function isExecutionError(error) {
    return error instanceof MCPError && error.code >= 3000 && error.code < 4000;
}
function isSystemError(error) {
    return error instanceof MCPError && error.code >= 5000;
}
/**
 * エラーレスポンス生成ヘルパー
 */
function createErrorResponse(error) {
    if (error instanceof MCPError) {
        return {
            content: [{
                    type: 'text',
                    text: `Error [${error.code}]: ${error.message}`
                }],
            isError: true,
            errorDetails: error.toJSON()
        };
    }
    else {
        // 通常のErrorの場合
        return {
            content: [{
                    type: 'text',
                    text: `Error: ${error.message}`
                }],
            isError: true
        };
    }
}
//# sourceMappingURL=errors.js.map