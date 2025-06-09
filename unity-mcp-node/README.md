# Unity MCP Server

Unity統合のためのMCP (Model Context Protocol) サーバー実装。
Claude DesktopからUnity Editorを自然言語で操作できる革新的なMCPサーバー。

**🔄 Server~自動同期対応**: このディレクトリの変更は自動的にServer~に同期されます。  
**🧪 GitHub Actions テスト**: node_modules生成・同期確認中...

## ✨ 特徴

- 🎮 **自然言語でUnity操作**: Claude Desktopから直接Unity Editor操作
- ⚡ **リアルタイム連携**: Unity状態・Console・コンパイル結果の自動監視  
- 🛡️ **エンタープライズセキュリティ**: パストラバーサル攻撃防止・機密データ保護
- 📊 **包括的テスト**: 159件のJestテスト・セキュリティテスト完備
- 🔧 **TypeScript**: 型安全な実装・自動APIドキュメント生成

## 📁 プロジェクト構成

```
unity-mcp-node/
├── src/                    # TypeScriptソースコード
│   ├── index.ts           # メインエントリーポイント
│   ├── mcp-tools.ts       # MCPツール定義・実行
│   ├── unity-commands.ts  # Unityコマンド処理
│   ├── errors.ts          # エラーハンドリング統一化
│   ├── config-validator.ts # JSON Schema設定検証
│   ├── data-monitor.ts    # データ監視・debounce機能
│   ├── json-rpc.ts        # JSON-RPCプロトコル処理
│   ├── i18n.ts            # 多言語対応
│   └── process-security.ts # プロセス実行セキュリティ
├── tests/                  # Jestテストコード
├── dist/                   # ビルド済みファイル
├── docs/api/              # TypeDoc自動生成APIドキュメント
├── scripts/               # ユーティリティスクリプト
├── schema/                # JSON Schema定義
├── mcp-config.json        # 設定ファイル
└── package.json           # Node.js依存関係
```

## 🚀 クイックスタート

### 必要環境
- Node.js 18.0以降
- Unity 6.0以降（NamedBuildTarget API使用）
- Claude Desktop MCP対応版

### セットアップ

```bash
# 1. 依存関係インストール・ビルド
npm run setup

# 2. サーバー起動
npm start

# 3. Claude Desktopでテスト
# "ping" → "create a cube" → "get scene info"
```

## 📊 利用可能なスクリプト

### 🔧 開発・ビルド
```bash
npm run dev              # 開発モード（自動リロード）
npm run build            # TypeScriptビルド
npm run start            # プロダクションサーバー起動
```

### 🧪 テスト・品質保証  
```bash
npm test                 # Jestテスト実行
npm run test:coverage    # カバレッジ付きテスト
npm run test:ci          # CI/CD用テスト
npm run lint             # ESLintコード検査
npm run lint:security    # セキュリティルール検査
```

### 📚 ドキュメント
```bash
npm run docs             # TypeDoc APIドキュメント生成
npm run docs:serve       # ドキュメントサーバー起動（:8080）
npm run docs:clean       # ドキュメントクリーンアップ
```

### 🛠️ ユーティリティ
```bash
npm run utils:test-connection  # MCP接続テスト
npm run clean                  # dist・coverageクリーン
npm run clean:all             # 完全クリーンアップ
```

## ⚙️ 設定

### mcp-config.json
```json
{
  "unityDataPath": "../MCPLearning/UnityMCP/Data",
  "serverName": "unity-mcp",
  "logLevel": "info",
  "dataWaitTimeout": 5000,
  "unityCommandTimeout": 30000,
  "language": "en"
}
```

### 環境変数（.env）
```bash
# Unityプロジェクトデータパス
UNITY_MCP_DATA_PATH=./MCPLearning/UnityMCP/Data

# 言語設定
MCP_LANGUAGE=en
```

## 🛡️ セキュリティ機能

### 実装済みセキュリティ
- ✅ **パストラバーサル攻撃防止**
- ✅ **機密データ自動検出・除外**
- ✅ **危険コマンド実行防止**
- ✅ **プロセス実行サンドボックス**
- ✅ **入力値検証・サニタイズ**

### セキュリティテスト
```bash
# セキュリティルール適用
npm run lint:security

# 全セキュリティテスト実行
npm test -- --testNamePattern="security|Security"
```

## 📈 テスト統計

- **総テスト数**: 159件
- **テストカバレッジ**: 95%+
- **セキュリティテスト**: 32件
- **パフォーマンステスト**: 実行時間・メモリ使用量測定

## 🔗 関連リソース

- **[Unity MCP Learning](../README.md)** - プロジェクト全体概要
- **[チュートリアル](../docs/tutorial/README.md)** - 段階的学習ガイド
- **[セキュリティガイド](../docs/tutorial/12-security-implementation-guide.md)** - セキュリティ詳細

## 📞 サポート

- **GitHub Issues**: バグ報告・機能要求
- **Documentation**: TypeDoc APIドキュメント
- **Security**: 責任ある脆弱性報告体制