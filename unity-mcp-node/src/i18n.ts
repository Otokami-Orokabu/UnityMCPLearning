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

// メッセージ定義
const messages = {
  en: {
    // Server startup messages
    'server.starting': 'Starting MCP Server...',
    'server.config.loading': 'Loading configuration from: {configPath}',
    'server.config.loaded': 'Configuration loaded successfully',
    'server.config.error': 'Configuration error [{code}]: {message}',
    'server.cleanup': 'Cleaning up resources...',
    'server.cleanup.completed': 'Cleanup completed',
    'server.cleanup.error': 'Error during cleanup: {error}',
    
    // Error messages
    'error.unknown_method': 'Unknown method: {method}',
    'error.tool_name_required': 'Tool name is required',
    'error.invalid_parameter': 'Invalid parameter: {parameter}',
    'error.missing_parameter': 'Missing required parameter: {parameter}',
    'error.validation_failed': 'Validation failed: {details}',
    'error.config_validation': 'Configuration validation failed: {errors}',
    'error.file_not_found': 'File not found: {filePath}',
    'error.unity_command_failed': 'Unity command failed: {command}',
    'error.connection_failed': 'Connection failed: {details}',
    'error.timeout': 'Operation timed out after {timeout}ms',
    'error.parse_error': 'Failed to parse JSON',
    
    // Unity command messages
    'unity.command.received': 'Command received: {commandType} (ID: {commandId})',
    'unity.command.executing': 'Executing command: {commandType} (ID: {commandId})',
    'unity.command.completed': 'Command completed: {commandType} ({duration}ms)',
    'unity.command.failed': 'Command failed: {commandType} - {error}',
    'unity.object.created': '{objectType} \'{name}\' created at {position}',
    
    // Data monitoring messages
    'data.watching': 'Watching Unity data directory: {path} (debounce: {delay}ms)',
    'data.file_changed': 'Unity data file changed: {filename} (debounced)',
    'data.loaded': 'Data loaded: {filename} -> {key}',
    'data.load_error': 'Error loading {filename}: {error}',
    'data.directory_not_found': 'Unity data directory not found: {path}',
    
    // Validation messages
    'validation.name_empty': 'Name cannot be empty',
    'validation.name_too_long': 'Name must be 50 characters or less',
    'validation.name_invalid_chars': 'Name contains invalid characters',
    'validation.position_invalid': '{axis} must be a finite number',
    'validation.position_out_of_range': '{axis} must be between {min} and {max}',
    'validation.color_invalid': 'Invalid color. Valid colors: {colors}',
    'validation.vector_invalid': '{parameter} must be an object with x, y, z properties',
    'validation.command_type_invalid': 'Unsupported command type: {commandType}',
  },
  
  ja: {
    // サーバー起動メッセージ
    'server.starting': 'MCPサーバーを起動中...',
    'server.config.loading': '設定ファイルを読み込み中: {configPath}',
    'server.config.loaded': '設定ファイルの読み込みが完了しました',
    'server.config.error': '設定エラー [{code}]: {message}',
    'server.cleanup': 'リソースをクリーンアップ中...',
    'server.cleanup.completed': 'クリーンアップが完了しました',
    'server.cleanup.error': 'クリーンアップ中にエラーが発生: {error}',
    
    // エラーメッセージ
    'error.unknown_method': '不明なメソッド: {method}',
    'error.tool_name_required': 'ツール名が必要です',
    'error.invalid_parameter': '無効なパラメーター: {parameter}',
    'error.missing_parameter': '必須パラメーターが不足: {parameter}',
    'error.validation_failed': '検証に失敗: {details}',
    'error.config_validation': '設定ファイルの検証に失敗: {errors}',
    'error.file_not_found': 'ファイルが見つかりません: {filePath}',
    'error.unity_command_failed': 'Unityコマンドが失敗: {command}',
    'error.connection_failed': '接続に失敗: {details}',
    'error.timeout': 'タイムアウト: {timeout}ms経過',
    'error.parse_error': 'JSONの解析に失敗しました',
    
    // Unityコマンドメッセージ
    'unity.command.received': 'コマンド受信: {commandType} (ID: {commandId})',
    'unity.command.executing': 'コマンド実行開始: {commandType} (ID: {commandId})',
    'unity.command.completed': 'コマンド実行完了: {commandType} ({duration}ms)',
    'unity.command.failed': 'コマンド実行失敗: {commandType} - {error}',
    'unity.object.created': '{objectType} \'{name}\' を {position} に作成しました',
    
    // データ監視メッセージ
    'data.watching': 'Unityデータディレクトリを監視中: {path} (debounce: {delay}ms)',
    'data.file_changed': 'Unityデータファイルが変更されました: {filename} (debounced)',
    'data.loaded': 'データを読み込み: {filename} -> {key}',
    'data.load_error': '{filename} の読み込みエラー: {error}',
    'data.directory_not_found': 'Unityデータディレクトリが見つかりません: {path}',
    
    // 検証メッセージ
    'validation.name_empty': '名前は空にできません',
    'validation.name_too_long': '名前は50文字以内で入力してください',
    'validation.name_invalid_chars': '名前に無効な文字が含まれています',
    'validation.position_invalid': '{axis} は有限の数値である必要があります',
    'validation.position_out_of_range': '{axis} は {min} から {max} の間である必要があります',
    'validation.color_invalid': '無効な色です。有効な色: {colors}',
    'validation.vector_invalid': '{parameter} は x, y, z プロパティを持つオブジェクトである必要があります',
    'validation.command_type_invalid': 'サポートされていないコマンドタイプ: {commandType}',
  }
};

// 現在の言語設定（環境変数またはデフォルト）
let currentLanguage: Language = (process.env.MCP_LANGUAGE as Language) || 'en';

/**
 * 言語を設定
 * @param language 設定する言語
 */
export function setLanguage(language: Language): void {
  currentLanguage = language;
}

/**
 * 現在の言語を取得
 * @returns 現在の言語
 */
export function getCurrentLanguage(): Language {
  return currentLanguage;
}

/**
 * メッセージを取得（プレースホルダー置換あり）
 * @param key メッセージキー
 * @param params プレースホルダー用のパラメーター
 * @returns ローカライズされたメッセージ
 */
export function getMessage(key: MessageKey, params?: Record<string, any>): string {
  const messageTemplate = messages[currentLanguage][key] || messages.en[key] || key;
  
  if (!params) {
    return messageTemplate;
  }
  
  // プレースホルダー {key} を実際の値に置換
  return messageTemplate.replace(/\{(\w+)\}/g, (match, paramKey) => {
    const value = params[paramKey];
    return value !== undefined && value !== null ? value.toString() : match;
  });
}

/**
 * エラーメッセージを取得（i18n対応）
 * @param errorKey エラーメッセージキー
 * @param params パラメーター
 * @returns ローカライズされたエラーメッセージ
 */
export function getErrorMessage(errorKey: MessageKey, params?: Record<string, any>): string {
  return getMessage(errorKey, params);
}

/**
 * 利用可能な言語一覧を取得
 * @returns 言語コード配列
 */
export function getAvailableLanguages(): Language[] {
  return Object.keys(messages) as Language[];
}

/**
 * メッセージテンプレートが存在するかチェック
 * @param key メッセージキー
 * @returns 存在する場合true
 */
export function hasMessage(key: string): boolean {
  return key in messages[currentLanguage] || key in messages.en;
}

/**
 * 言語設定をファイルから読み込み（将来的な拡張用）
 * @param configPath 設定ファイルパス
 */
export function loadLanguageFromConfig(configPath?: string): void {
  // 将来的にconfig.jsonから言語設定を読み込む場合の拡張ポイント
  if (configPath) {
    // 設定ファイルから言語を読み込む実装（現在はスキップ）
  }
}