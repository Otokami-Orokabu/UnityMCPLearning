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
import { MessageKey } from './i18n.js';
/**
 * エラーコード定義
 * 体系的なエラー分類と識別
 */
export declare enum ErrorCode {
    CONNECTION_ERROR = 1000,
    TIMEOUT_ERROR = 1001,
    VALIDATION_ERROR = 2000,
    INVALID_PARAMETER = 2001,
    MISSING_PARAMETER = 2002,
    TYPE_MISMATCH = 2003,
    EXECUTION_ERROR = 3000,
    UNITY_COMMAND_FAILED = 3001,
    FILE_NOT_FOUND = 3002,
    FILE_READ_ERROR = 3003,
    FILE_WRITE_ERROR = 3004,
    CONFIG_ERROR = 4000,
    INVALID_CONFIG = 4001,
    MISSING_CONFIG = 4002,
    SYSTEM_ERROR = 5000,
    UNKNOWN_ERROR = 5999
}
/**
 * MCPエラークラス
 * 統一されたエラー情報の管理（国際化対応）
 */
export declare class MCPError extends Error {
    readonly code: ErrorCode;
    readonly timestamp: Date;
    readonly context?: any;
    readonly messageKey?: MessageKey;
    readonly messageParams?: Record<string, any>;
    constructor(code: ErrorCode, message: string | MessageKey, context?: any, messageParams?: Record<string, any>);
    /**
     * エラー情報をJSON形式で取得
     */
    toJSON(): {
        name: string;
        code: ErrorCode;
        message: string;
        messageKey: "server.starting" | "server.config.loading" | "server.config.loaded" | "server.config.error" | "server.cleanup" | "server.cleanup.completed" | "server.cleanup.error" | "error.unknown_method" | "error.tool_name_required" | "error.invalid_parameter" | "error.missing_parameter" | "error.validation_failed" | "error.config_validation" | "error.file_not_found" | "error.unity_command_failed" | "error.connection_failed" | "error.timeout" | "error.parse_error" | "unity.command.received" | "unity.command.executing" | "unity.command.completed" | "unity.command.failed" | "unity.object.created" | "data.watching" | "data.file_changed" | "data.loaded" | "data.load_error" | "data.directory_not_found" | "validation.name_empty" | "validation.name_too_long" | "validation.name_invalid_chars" | "validation.position_invalid" | "validation.position_out_of_range" | "validation.color_invalid" | "validation.vector_invalid" | "validation.command_type_invalid" | undefined;
        messageParams: Record<string, any> | undefined;
        timestamp: string;
        context: any;
    };
    /**
     * 異なる言語でエラーメッセージを取得
     * @param language 言語コード
     * @returns 指定された言語のエラーメッセージ
     */
    getLocalizedMessage(language?: 'en' | 'ja'): string;
    /**
     * エラーコードから人間が読みやすい説明を取得
     */
    static getErrorDescription(code: ErrorCode): string;
    /**
     * エラーレベルを取得（ログレベル判定用）
     */
    getLevel(): 'error' | 'warn' | 'info';
}
/**
 * エラーハンドリングヘルパー関数
 */
export declare function isValidationError(error: any): error is MCPError;
export declare function isExecutionError(error: any): error is MCPError;
export declare function isSystemError(error: any): error is MCPError;
/**
 * エラーレスポンス生成ヘルパー
 */
export declare function createErrorResponse(error: MCPError | Error): {
    content: {
        type: string;
        text: string;
    }[];
    isError: boolean;
    errorDetails: {
        name: string;
        code: ErrorCode;
        message: string;
        messageKey: "server.starting" | "server.config.loading" | "server.config.loaded" | "server.config.error" | "server.cleanup" | "server.cleanup.completed" | "server.cleanup.error" | "error.unknown_method" | "error.tool_name_required" | "error.invalid_parameter" | "error.missing_parameter" | "error.validation_failed" | "error.config_validation" | "error.file_not_found" | "error.unity_command_failed" | "error.connection_failed" | "error.timeout" | "error.parse_error" | "unity.command.received" | "unity.command.executing" | "unity.command.completed" | "unity.command.failed" | "unity.object.created" | "data.watching" | "data.file_changed" | "data.loaded" | "data.load_error" | "data.directory_not_found" | "validation.name_empty" | "validation.name_too_long" | "validation.name_invalid_chars" | "validation.position_invalid" | "validation.position_out_of_range" | "validation.color_invalid" | "validation.vector_invalid" | "validation.command_type_invalid" | undefined;
        messageParams: Record<string, any> | undefined;
        timestamp: string;
        context: any;
    };
} | {
    content: {
        type: string;
        text: string;
    }[];
    isError: boolean;
    errorDetails?: undefined;
};
//# sourceMappingURL=errors.d.ts.map