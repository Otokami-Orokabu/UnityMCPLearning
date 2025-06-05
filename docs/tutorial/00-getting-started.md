# Unity MCP Server プロジェクト入門

## プロジェクト概要
このプロジェクトは、UnityエディターとClaude Desktopをリアルタイムで連携させるMCP（Model Context Protocol）サーバーを構築するものです。

## 🎯 このプロジェクトで実現できること

### Step 1: 基本通信の確立
- Claude Desktop ↔ MCPサーバー間の基本的な通信
- JSON-RPC 2.0プロトコルによる安定した接続
- `ping`、`unity_info`などの基本ツール

### Step 2: Unity連携システム
- Unity → JSON → MCP → Claude Desktopのリアルタイムデータ流れ
- 6種類のUnityデータエクスポート（プロジェクト、シーン、オブジェクト、アセット、ビルド、エディター）
- 自動変更検知とリアルタイム更新

### Step 3: Unity制御（今後予定）
- Claude Desktop → MCP → Unityの逆方向制御
- GameObjectの生成・操作
- Unityエディターの自動化

## 📋 前提知識

### 必須
- **Unity基本操作**: プロジェクト作成、エディター操作
- **基本的なファイル操作**: コピー、移動、編集

### あると良い
- **Node.js基礎**: パッケージ管理、npm基本コマンド
- **TypeScript基礎**: 基本的な構文理解
- **JSON理解**: データ構造の読み書き

## 🛠 必要な環境

### 必須ツール
- **Unity 6以上** (6000.1.5f1で検証済み)
- **Node.js** (v18以上推奨)
- **Claude Desktop** (最新版)
- **VS Code または任意のテキストエディター**

### 対応プラットフォーム
- **macOS** (メイン検証環境)
- **Windows** (理論上対応、要検証)
- **Linux** (理論上対応、要検証)

## 📚 学習の流れ

### 1. 環境設定 → `01-environment-setup.md`
- 必要なツールのインストール
- Claude Desktop設定
- プロジェクトのセットアップ

### 2. Step 1: 基本通信 → `02-step1-basic-communication.md`
- MCPサーバーの実装
- Claude Desktopとの接続確認
- 基本ツールの動作テスト

### 3. Step 2: Unity連携 → `03-step2-unity-integration.md`
- Unityエクスポートシステム実装
- リアルタイムデータ取得
- 完全なデータ流れの確立

### 4. トラブルシューティング → `04-troubleshooting.md`
- よくある問題と解決方法
- デバッグ手順
- 設定の確認方法

### 5. 高度な設定 → `05-advanced-configuration.md`
- カスタマイズ方法
- 異なる環境での設定
- 性能最適化

## 🏗 プロジェクト構成

```
UnityMCPLearning/
├── unity-mcp-node/          # Node.js MCPサーバー
│   ├── src/                 # TypeScriptソースコード
│   ├── dist/                # コンパイル済みJavaScript
│   └── mcp-config.json      # 設定ファイル
├── MCPLearning/             # Unityプロジェクト
│   ├── Assets/UnityMCP/     # Unity側エクスポートシステム
│   └── UnityMCP/Data/       # JSON出力先
└── docs/                    # ドキュメント
    └── tutorial/            # このチュートリアル
```

## 🚀 クイックスタート

### 完全初心者向け（推奨）
1. `01-environment-setup.md` から順番に進む
2. 各ステップで動作確認をしっかり行う
3. 問題が発生したら `04-troubleshooting.md` を参照

### 経験者向け
1. 環境が整っている場合は `02-step1-basic-communication.md` から開始
2. Unity経験がある場合は `03-step2-unity-integration.md` に重点を置く

## 📝 注意事項

### セキュリティ
- このプロジェクトはローカル開発環境での使用を想定
- 個人情報（ユーザー名等）が含まれないよう設定済み
- 本番環境での使用前にセキュリティレビューを実施してください

### 互換性
- Unity 6で検証済み、それ以前のバージョンは要検証
- Node.js v18以上を推奨
- Claude Desktop最新版での動作を確認

## 🤝 サポート

### 問題が発生した場合
1. まず `04-troubleshooting.md` を確認
2. エラーメッセージとともに状況を記録
3. 設定ファイルの内容を確認

### フィードバック
- 改善提案やバグ報告は歓迎
- 初学者からの視点での意見は特に貴重

## 次のステップ
準備ができたら `01-environment-setup.md` に進んでください。