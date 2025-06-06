# Step 4: コード品質改善とファイル分割

> **対象読者**: TypeScript初学者・コード品質改善を学びたい方  
> **前提条件**: Step 3完了（Unity制御システム構築済み）  
> **所要時間**: 約60分

## 📚 このチュートリアルで学ぶこと

- **エラーハンドリングの統一化**
- **設定ファイルの検証機能**
- **大きなファイルの分割方法**
- **TypeScriptでの型安全性向上**
- **保守しやすいコード構造の作り方**

## 🎯 学習目標

このステップでは、Step 3で完成した基本機能に対して以下の改善を行います：

1. **エラーハンドリングの統一化** - 分散していたエラー処理を体系化
2. **設定ファイルの検証** - JSON Schemaを使った厳密な設定チェック
3. **ファイル分割** - 943行の巨大ファイルを複数の専門ファイルに分割
4. **型安全性の向上** - TypeScriptの機能を活用した堅牢な実装

## 📖 背景：なぜコード品質改善が必要？

### 問題：Step 3完了時点でのコードの状況

```bash
# ファイルサイズの問題
src/index.ts: 943行 (1000行間近の巨大ファイル)

# エラーハンドリングの問題
- 散在するtry-catch文
- 異なるエラー形式
- 不十分なエラー情報

# 設定ファイルの問題  
- 検証なしでJSONを読み込み
- 不正な設定値でもサーバーが起動
- エラー時の原因が不明
```

### 解決：体系的なコード品質改善

```bash
# ファイル構成の改善
src/
├── index.ts           (211行) ← 943行から大幅削減
├── errors.ts          (新規) エラーハンドリング統一
├── config-validator.ts (新規) 設定ファイル検証
├── json-rpc.ts        (新規) JSON-RPCプロトコル処理
├── mcp-tools.ts       (新規) MCPツール定義・処理
├── unity-commands.ts  (新規) Unityコマンド実行
└── data-monitor.ts    (新規) データ監視・読み込み
```

## 🔧 Step 4-1: エラーハンドリングの統一化

### 🎯 目標
分散していたエラー処理を統一された体系に整理する

### 📝 理解のポイント

**Before（改善前）:**
```typescript
// 各所でバラバラなエラー処理
throw new Error('Invalid parameter');
throw { code: 2001, message: 'Validation failed' };
return { isError: true, message: 'Something went wrong' };
```

**After（改善後）:**
```typescript
// 統一されたエラーシステム
throw new MCPError(ErrorCode.INVALID_PARAMETER, 'Invalid parameter');
```

### 🏗️ 実装内容

#### 1. エラーコード体系の設計

```typescript
// src/errors.ts
export enum ErrorCode {
  // 通信エラー (1xxx)
  CONNECTION_ERROR = 1000,
  TIMEOUT_ERROR = 1001,
  
  // 検証エラー (2xxx) 
  VALIDATION_ERROR = 2000,
  INVALID_PARAMETER = 2001,
  MISSING_PARAMETER = 2002,
  
  // 実行エラー (3xxx)
  EXECUTION_ERROR = 3000,
  UNITY_COMMAND_FAILED = 3001,
  FILE_NOT_FOUND = 3002,
  
  // 設定エラー (4xxx)
  CONFIG_ERROR = 4000,
  INVALID_CONFIG = 4001,
  
  // システムエラー (5xxx)
  SYSTEM_ERROR = 5000,
  UNKNOWN_ERROR = 5999
}
```

**💡 初学者向け解説:**
- エラーコードを数値で分類することで、エラーの種類を体系的に管理
- 1000番台、2000番台のように分けることで、エラーの大分類が一目でわかる
- 将来的にエラーコードを追加する際も、適切な番号範囲に配置できる

#### 2. MCPErrorクラスの実装

```typescript
export class MCPError extends Error {
  public readonly code: ErrorCode;
  public readonly timestamp: Date;
  public readonly context?: any;

  constructor(code: ErrorCode, message: string, context?: any) {
    super(message);
    this.name = 'MCPError';
    this.code = code;
    this.timestamp = new Date();
    this.context = context;
  }

  // エラー情報をJSON形式で取得
  toJSON() {
    return {
      name: this.name,
      code: this.code,
      message: this.message,
      timestamp: this.timestamp.toISOString(),
      context: this.context
    };
  }
}
```

**💡 初学者向け解説:**
- `extends Error` でJavaScriptの標準Errorクラスを継承
- `readonly` で不変プロパティを定義（エラー情報の改ざんを防止）
- `context` でエラー発生時の追加情報（変数の値など）を保存
- `toJSON()` でエラー情報をログ出力しやすい形式に変換

### 🎮 使用例

```typescript
// 以前のエラー処理
if (!commandType) {
  throw new Error('Invalid command type provided');
}

// 改善後のエラー処理
if (!commandType) {
  throw new MCPError(
    ErrorCode.INVALID_PARAMETER,
    'Invalid command type provided',
    { providedValue: commandType, expectedType: 'string' }
  );
}
```

**🌟 改善点:**
1. **エラーコードによる分類** - ログ解析が容易
2. **追加情報の保存** - デバッグ情報が豊富
3. **統一されたフォーマット** - エラー処理が一貫性を持つ

## 🔧 Step 4-2: 設定ファイル検証機能

### 🎯 目標
JSON Schemaを使用した厳密な設定ファイル検証を実装

### 📝 理解のポイント

**問題:**
```json
// 不正な設定でもサーバーが起動してしまう
{
  "unityDataPath": "",           // 空文字
  "timeout": {
    "unityCommandTimeout": -1    // 負の値
  }
}
```

**解決:**
```typescript
// JSON Schemaによる厳密な検証
const schema = {
  "properties": {
    "unityDataPath": {
      "type": "string",
      "minLength": 1,              // 空文字を禁止
      "pattern": "^(\\.|\\.\\/|\\.\\\\\|[a-zA-Z]:|\\/).*"  // パス形式をチェック
    },
    "timeout": {
      "properties": {
        "unityCommandTimeout": {
          "type": "integer",
          "minimum": 1000,         // 最小1秒
          "maximum": 300000        // 最大5分
        }
      }
    }
  },
  "required": ["unityDataPath"]    // 必須項目
}
```

### 🏗️ 実装内容

#### 1. JSON Schemaの定義

```typescript
// schema/mcp-config.schema.json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "Unity MCP Server Configuration",
  "type": "object",
  "properties": {
    "mcpServers": {
      "type": "object",
      "patternProperties": {
        "^[a-zA-Z][a-zA-Z0-9_-]*$": {  // サーバー名の正規表現
          "type": "object",
          "properties": {
            "command": { "type": "string", "minLength": 1 },
            "args": { "type": "array", "items": { "type": "string" } }
          },
          "required": ["command", "args"]
        }
      }
    },
    "unityDataPath": {
      "type": "string",
      "minLength": 1,
      "pattern": "^(\\.|\\.\\/|\\.\\\\|[a-zA-Z]:|\\/).*"
    }
  },
  "required": ["mcpServers", "unityDataPath"]
}
```

**💡 初学者向け解説:**
- `patternProperties` で動的なキー名（サーバー名）を検証
- `minLength` で空文字を防ぐ
- `pattern` で正規表現によるフォーマットチェック
- `required` で必須項目を定義

#### 2. 検証システムの実装

```typescript
// src/config-validator.ts
import Ajv from 'ajv';

export class ConfigValidator {
  private ajv: Ajv;
  private schema: object;

  constructor() {
    this.ajv = new Ajv({ allErrors: true, verbose: true });
    this.loadSchema();
  }

  validateConfig(config: any): MCPConfig {
    const validate = this.ajv.compile(this.schema);
    const isValid = validate(config);

    if (!isValid) {
      const errors = validate.errors || [];
      const errorMessages = errors.map(err => {
        const path = err.instancePath || 'root';
        return `${path}: ${err.message}`;
      });

      throw new MCPError(
        ErrorCode.INVALID_CONFIG,
        `Configuration validation failed: ${errorMessages.join(', ')}`,
        { validationErrors: errors, config: config }
      );
    }

    return this.mergeDefaults(config);
  }
}
```

**💡 初学者向け解説:**
- `Ajv`（Another JSON Schema Validator）ライブラリを使用
- `allErrors: true` で全ての検証エラーを取得
- エラー時は詳細な情報を含むMCPErrorを投げる
- 検証成功時はデフォルト値をマージ

### 🎮 使用例

```typescript
// 設定ファイル読み込み時
function initializeConfig(): void {
  try {
    const config = loadAndValidateConfig(configPath);
    log('Configuration loaded successfully');
  } catch (error) {
    if (error instanceof MCPError) {
      log(`Configuration error [${error.code}]: ${error.message}`);
      // 設定エラーの場合は起動を停止
      process.exit(1);
    }
  }
}
```

## 🔧 Step 4-3: ファイル分割とモジュール化

### 🎯 目標
943行の巨大ファイルを専門領域ごとに分割して保守性を向上

### 📝 理解のポイント

**問題:**
```typescript
// src/index.ts (943行)
// JSON-RPC処理、Unity制御、データ監視、エラー処理、設定管理...
// 全てが1つのファイルに混在
```

**解決:**
```typescript
// 専門領域ごとのファイル分割
src/
├── index.ts           (211行) メインエントリーポイント
├── json-rpc.ts        JSON-RPCプロトコル処理
├── mcp-tools.ts       MCPツール定義・実行
├── unity-commands.ts  Unityコマンド処理
├── data-monitor.ts    データ監視・読み込み
├── config-validator.ts 設定ファイル検証
└── errors.ts          エラーハンドリング
```

### 🏗️ ファイル分割の詳細

#### 1. JSON-RPC処理の分離 (`json-rpc.ts`)

```typescript
// JSON-RPC 2.0の型定義
export interface JsonRpcRequest {
  jsonrpc: '2.0';
  id: string | number;
  method: string;
  params?: any;
}

// レスポンス送信のユーティリティ
export function sendSuccessResponse(id: string | number, result: any): void {
  sendResponse({ jsonrpc: '2.0', id, result });
}

export function sendErrorResponse(id: string | number, error: Error): void {
  sendResponse({ jsonrpc: '2.0', id, error: mapError(error) });
}
```

**💡 分割の利点:**
- JSON-RPCプロトコルの詳細がまとまっている
- 型定義とユーティリティが同じ場所にある
- テストしやすい独立したモジュール

#### 2. MCPツール定義の分離 (`mcp-tools.ts`)

```typescript
// MCPツールの定義配列
export const MCP_TOOLS = [
  {
    name: 'unity_info_realtime',
    description: 'Get real-time Unity project information',
    inputSchema: { /* スキーマ定義 */ }
  },
  {
    name: 'create_cube',
    description: 'Create a cube in Unity scene',
    inputSchema: { /* スキーマ定義 */ }
  }
  // ... 他のツール
];

// ツール実行の統一インターフェース
export async function handleToolCall(
  toolName: string,
  params: any,
  dataPath: string,
  config: MCPConfig,
  log: (...args: any[]) => void
): Promise<any> {
  switch (toolName) {
    case 'unity_info_realtime':
      return handleUnityInfoRealtime(params, dataPath, log);
    // ... 他のツール処理
  }
}
```

**💡 分割の利点:**
- ツール定義が一箇所にまとまっている
- 新しいツールの追加が簡単
- ツール間の処理が統一されている

#### 3. Unityコマンド処理の分離 (`unity-commands.ts`)

```typescript
// Unityコマンド実行の専門モジュール
export async function executeUnityCommand(
  commandType: string,
  args: any,
  dataPath: string,
  config: MCPConfig,
  log: (...args: any[]) => void
): Promise<any> {
  // パラメーター検証
  const validatedParams = validateCommandParameters(commandType, args);
  
  // コマンドファイル作成
  const commandPath = getUnityCommandPath(dataPath);
  // ... 実行処理
}

// パラメーター検証も同じファイル内
export function validateCommandParameters(commandType: string, args: any): any {
  // 検証ロジック
}
```

**💡 分割の利点:**
- Unity関連の処理がまとまっている
- コマンド実行の詳細が隠蔽されている
- エラーハンドリングが統一されている

#### 4. データ監視機能の分離 (`data-monitor.ts`)

```typescript
// データキャッシュとファイル監視
let cachedData: { [key: string]: any } = {};

// Debounce機能付きファイル監視
const debounceTimers: { [filename: string]: NodeJS.Timeout } = {};
const DEBOUNCE_DELAY = 300; // 300ms

export function startFileWatching(dataPath: string, log: Function): void {
  fs.watch(dataPath, { recursive: false }, (_eventType, filename) => {
    if (filename && filename.endsWith('.json')) {
      log(`Unity data file changed: ${filename} (debounced)`);
      debouncedLoadDataFile(filename, dataPath, log);
    }
  });
}

// Debounced ファイル読み込み
function debouncedLoadDataFile(filename: string, dataPath: string, log: Function): void {
  // 既存のタイマーをクリア
  if (debounceTimers[filename]) {
    clearTimeout(debounceTimers[filename]);
  }
  
  // 新しいタイマーを設定
  debounceTimers[filename] = setTimeout(() => {
    loadDataFile(filename, dataPath, log);
    delete debounceTimers[filename];
  }, DEBOUNCE_DELAY);
}

export function getCachedData(): { [key: string]: any } {
  return cachedData;
}

// クリーンアップ機能
export function clearDebounceTimers(): void {
  Object.values(debounceTimers).forEach(timer => clearTimeout(timer));
  Object.keys(debounceTimers).forEach(filename => delete debounceTimers[filename]);
}
```

**💡 分割の利点:**
- データ関連の処理が独立している
- キャッシュの管理が明確
- ファイル監視ロジックが分離

**🚀 パフォーマンス最適化:**
- **Debounce機能**: 頻繁なファイル変更時に読み込み処理を遅延実行
- **タイマー管理**: 重複する読み込み処理を防止
- **クリーンアップ**: プロセス終了時のリソース解放

### 🎮 新しいindex.tsの構造

```typescript
// src/index.ts (211行に削減)
import { /* 必要な関数のみインポート */ } from './各モジュール.js';

// 設定初期化
function initializeConfig(): void { /* 設定処理 */ }

// メソッドハンドラー（簡潔に）
async function handleMethod(method: string, params: any): Promise<any> {
  switch (method) {
    case 'tools/call':
      return await handleToolCall(/* パラメーター */);
    // ... 他のメソッド
  }
}

// メイン処理（見通しが良い）
async function main() {
  initializeConfig();
  startFileWatching(dataPath, log);
  
  // JSON-RPC処理のみに集中
  const rl = createInterface({ /* 設定 */ });
  rl.on('line', async (line) => { /* JSON-RPC処理 */ });
}
```

## 🔧 Step 4-4: TypeScript型安全性の向上

### 🎯 目標
TypeScriptの型システムを活用してバグを予防

### 📝 改善内容

#### 1. 設定ファイルの型定義

```typescript
// 以前：any型で型安全性なし
const config: any = JSON.parse(configContent);

// 改善後：厳密な型定義
export interface MCPConfig {
  mcpServers: {
    [serverName: string]: {
      command: string;
      args: string[];
      cwd?: string;
      env?: { [key: string]: string };
    };
  };
  unityDataPath: string;
  logLevel?: 'error' | 'warn' | 'info' | 'debug';  // リテラル型
  timeout?: {
    unityCommandTimeout?: number;
    dataWaitTimeout?: number;
  };
}
```

**💡 型安全性の利点:**
- 存在しないプロパティアクセスをコンパイル時に発見
- 値の型ミスマッチを事前に検出
- IDE（エディタ）の自動補完が効く

#### 2. JSON-RPC型定義の厳密化

```typescript
// JSON-RPC 2.0の厳密な型定義
export interface JsonRpcRequest {
  jsonrpc: '2.0';  // リテラル型で'2.0'のみ許可
  id: string | number;
  method: string;
  params?: any;
}

// エラーレスポンスの型
export interface JsonRpcError {
  code: number;
  message: string;
  data?: any;
}
```

## 📊 改善結果のまとめ

### ファイルサイズの変化

```bash
# Before（改善前）
src/index.ts: 943行

# After（改善後）
src/index.ts:           211行 (-77%)
src/errors.ts:          130行 (新規)
src/config-validator.ts: 200行 (新規)  
src/json-rpc.ts:        120行 (新規)
src/mcp-tools.ts:       150行 (新規)
src/unity-commands.ts:  250行 (新規)
src/data-monitor.ts:     80行 (新規)

合計: 1,141行（元943行から+198行、機能向上分）
```

### コード品質の向上

| 項目 | Before | After | 改善点 |
|------|--------|-------|--------|
| **エラーハンドリング** | 散在・不統一 | 統一システム | デバッグ効率向上 |
| **設定検証** | なし | JSON Schema | 不正設定の事前発見 |
| **型安全性** | 部分的 | 包括的 | バグの早期発見 |
| **保守性** | 低（巨大ファイル） | 高（モジュール分割） | 機能追加が容易 |
| **テスト容易性** | 困難 | 良好 | 各モジュール独立テスト可能 |

### 開発体験の向上

```typescript
// 以前：エラーの原因が不明
[MCP Server] Error: Invalid parameter

// 改善後：詳細なエラー情報
[MCP Server] Configuration error [4001]: Configuration validation failed: 
  unityDataPath: must be a non-empty string, 
  timeout.unityCommandTimeout: must be >= 1000
Context: {
  "configPath": "/path/to/config.json",
  "validationErrors": [/* 詳細なエラー情報 */]
}
```

## 🚀 次のステップ

Step 4完了時点で、以下の基盤が整いました：

1. **堅牢なエラーハンドリング** - 体系的なエラー分類と処理
2. **厳密な設定検証** - JSON Schemaによる事前チェック
3. **保守しやすいコード構造** - モジュール分割による責任の明確化
4. **型安全性** - TypeScriptの恩恵を最大限活用

**次に学ぶべき内容：**
- **Step 5: テスト実装** - Jest単体テスト、Unity Test Runner
- **Step 6: CI/CD構築** - GitHub Actions自動化
- **Step 7: パフォーマンス最適化** - ファイル監視の改善、キャッシュ戦略

## 💡 初学者向けアドバイス

### コード品質改善のベストプラクティス

1. **小さく始める** - 一度に全てを改善しようとせず、段階的に進める
2. **テスト駆動** - 改善前にテストを書いて、動作を保証する
3. **文書化** - なぜその改善を行ったかを記録する
4. **レビュー** - 改善後のコードを第三者の視点で確認する

### 学習リソース

- **TypeScript型安全性**: [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- **JSON Schema**: [JSON Schema公式サイト](https://json-schema.org/)
- **エラーハンドリング**: [Node.js Error Handling Best Practices](https://nodejs.org/en/docs/guides/error-handling/)
- **コード分割**: [Clean Code by Robert Martin](https://www.amazon.com/Clean-Code-Handbook-Software-Craftsmanship/dp/0132350882)

このStep 4により、プロジェクトのコード品質は大幅に向上し、将来の機能拡張やメンテナンスが格段に容易になりました。