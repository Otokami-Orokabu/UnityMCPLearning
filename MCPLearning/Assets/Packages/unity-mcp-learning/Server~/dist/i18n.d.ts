/**
 * @fileoverview Internationalization (i18n) system for error messages and UI text
 * @description エラーメッセージとUIテキストの国際化システム
 *
 * このモジュールは多言語対応のメッセージシステムを提供します。
 * 現在は英語（en）と日本語（ja）をサポートしています。
 *
 * @example
 * ```typescript
 * import { setLanguage, getMessage } from './i18n';
 *
 * // 言語を日本語に設定
 * setLanguage('ja');
 *
 * // メッセージを取得
 * const message = getMessage('server.starting');
 * console.log(message); // "MCPサーバーを起動中..."
 *
 * // パラメーター付きメッセージ
 * const errorMsg = getMessage('error.unknown_method', { method: 'test' });
 * console.log(errorMsg); // "不明なメソッド: test"
 * ```
 *
 * @author Unity MCP Learning Project
 * @version 1.0.0
 * @since 1.0.0
 */
/**
 * サポートされている言語コード
 * @public
 */
export type Language = 'en' | 'ja';
/**
 * メッセージキーの型定義
 * @public
 */
export type MessageKey = keyof typeof messages.en;
declare const messages: {
    en: {
        'server.starting': string;
        'server.config.loading': string;
        'server.config.loaded': string;
        'server.config.error': string;
        'server.cleanup': string;
        'server.cleanup.completed': string;
        'server.cleanup.error': string;
        'error.unknown_method': string;
        'error.tool_name_required': string;
        'error.invalid_parameter': string;
        'error.missing_parameter': string;
        'error.validation_failed': string;
        'error.config_validation': string;
        'error.file_not_found': string;
        'error.unity_command_failed': string;
        'error.connection_failed': string;
        'error.timeout': string;
        'error.parse_error': string;
        'unity.command.received': string;
        'unity.command.executing': string;
        'unity.command.completed': string;
        'unity.command.failed': string;
        'unity.object.created': string;
        'data.watching': string;
        'data.file_changed': string;
        'data.loaded': string;
        'data.load_error': string;
        'data.directory_not_found': string;
        'validation.name_empty': string;
        'validation.name_too_long': string;
        'validation.name_invalid_chars': string;
        'validation.position_invalid': string;
        'validation.position_out_of_range': string;
        'validation.color_invalid': string;
        'validation.vector_invalid': string;
        'validation.command_type_invalid': string;
    };
    ja: {
        'server.starting': string;
        'server.config.loading': string;
        'server.config.loaded': string;
        'server.config.error': string;
        'server.cleanup': string;
        'server.cleanup.completed': string;
        'server.cleanup.error': string;
        'error.unknown_method': string;
        'error.tool_name_required': string;
        'error.invalid_parameter': string;
        'error.missing_parameter': string;
        'error.validation_failed': string;
        'error.config_validation': string;
        'error.file_not_found': string;
        'error.unity_command_failed': string;
        'error.connection_failed': string;
        'error.timeout': string;
        'error.parse_error': string;
        'unity.command.received': string;
        'unity.command.executing': string;
        'unity.command.completed': string;
        'unity.command.failed': string;
        'unity.object.created': string;
        'data.watching': string;
        'data.file_changed': string;
        'data.loaded': string;
        'data.load_error': string;
        'data.directory_not_found': string;
        'validation.name_empty': string;
        'validation.name_too_long': string;
        'validation.name_invalid_chars': string;
        'validation.position_invalid': string;
        'validation.position_out_of_range': string;
        'validation.color_invalid': string;
        'validation.vector_invalid': string;
        'validation.command_type_invalid': string;
    };
};
/**
 * 言語を設定
 * @param language 設定する言語
 */
export declare function setLanguage(language: Language): void;
/**
 * 現在の言語を取得
 * @returns 現在の言語
 */
export declare function getCurrentLanguage(): Language;
/**
 * メッセージを取得（プレースホルダー置換あり）
 * @param key メッセージキー
 * @param params プレースホルダー用のパラメーター
 * @returns ローカライズされたメッセージ
 */
export declare function getMessage(key: MessageKey, params?: Record<string, any>): string;
/**
 * エラーメッセージを取得（i18n対応）
 * @param errorKey エラーメッセージキー
 * @param params パラメーター
 * @returns ローカライズされたエラーメッセージ
 */
export declare function getErrorMessage(errorKey: MessageKey, params?: Record<string, any>): string;
/**
 * 利用可能な言語一覧を取得
 * @returns 言語コード配列
 */
export declare function getAvailableLanguages(): Language[];
/**
 * メッセージテンプレートが存在するかチェック
 * @param key メッセージキー
 * @returns 存在する場合true
 */
export declare function hasMessage(key: string): boolean;
/**
 * 言語設定をファイルから読み込み（将来的な拡張用）
 * @param configPath 設定ファイルパス
 */
export declare function loadLanguageFromConfig(configPath?: string): void;
export {};
//# sourceMappingURL=i18n.d.ts.map