# Claude Code + Unity MCP 統合ガイド

Claude Code と Unity MCP サーバーを統合して、Unity エディターをリアルタイムで操作・監視する方法を初学者向けに解説します。

## 📋 目次

1. [概要](#概要)
2. [前提条件](#前提条件)
3. [Claude Code インストール](#claude-code-インストール)
4. [Unity MCP サーバー設定](#unity-mcp-サーバー設定)
5. [Claude Desktop 設定](#claude-desktop-設定)
6. [接続テスト](#接続テスト)
7. [基本的な使用方法](#基本的な使用方法)
8. [トラブルシューティング](#トラブルシューティング)

## 🎯 概要

この統合により以下が可能になります：

- **リアルタイム Unity 情報取得**: プロジェクト、シーン、オブジェクトの状態をリアルタイムで取得
- **Unity オブジェクト操作**: キューブ、スフィア、プレーンなどのオブジェクトを Claude Code から直接作成
- **開発効率向上**: コマンドラインから Unity を制御し、開発作業を自動化

## ✅ 前提条件

### 必要なソフトウェア

1. **Unity Editor 6000.1.5f1 以上**
2. **Node.js 18+ および npm**
3. **Claude Code CLI**
4. **このプロジェクトのセットアップ完了**

### 確認方法

```bash
# Node.js バージョン確認
node --version  # v18.0.0 以上

# npm バージョン確認
npm --version   # 9.0.0 以上

# Unity プロジェクトの確認
# MCPLearning プロジェクトが正常に開けること
```

## 🚀 Claude Code インストール

### 1. Claude Code のインストール

```bash
# npm を使用してグローバルインストール
npm install -g @anthropic-ai/claude-code

# インストール確認
claude-code --version
```

### 2. Claude Code の初期設定

```bash
# 初回設定（API キーの設定）
claude-code auth

# 設定確認
claude-code config
```

## ⚙️ Unity MCP サーバー設定

### 1. プロジェクトのクローンとセットアップ

```bash
# プロジェクトクローン（既に完了している場合はスキップ）
git clone https://github.com/your-repo/UnityMCPLearning.git
cd UnityMCPLearning

# Node.js 依存関係のインストール
cd unity-mcp-node
npm install

# TypeScript ビルド
npm run build
```

### 2. Unity プロジェクトの準備

1. **Unity で MCPLearning プロジェクトを開く**
2. **MCP エクスポートスクリプトが動作していることを確認**
   - `Window` → `General` → `Console` でエラーがないことを確認
   - `Assets/UnityMCP/` フォルダが存在することを確認

### 3. MCP サーバー設定ファイルの確認

`unity-mcp-node/mcp-config.json` を確認：

```json
{
  "mcpServers": {
    "unity-mcp-prod": {
      "command": "node",
      "args": ["./unity-mcp-node/dist/index.js"],
      "cwd": "."
    }
  },
  "unityDataPath": "../MCPLearning/UnityMCP/Data"
}
```

## 🔧 Claude Desktop 設定

### 1. Claude Desktop 設定ファイルの編集

Claude Desktop の設定ファイルを編集します：

**macOS の場合:**
```bash
# 設定ファイルを開く
open "/Users/$(whoami)/Library/Application Support/Claude/claude_desktop_config.json"
```

**Windows の場合:**
```
%APPDATA%\Claude\claude_desktop_config.json
```

### 2. 設定ファイルの内容

以下の内容を追加してください（**パスは自分の環境に合わせて変更**）：

```json
{
  "mcpServers": {
    "unity-mcp-dev": {
      "command": "node",
      "args": [
        "/あなたのパス/UnityMCPLearning/unity-mcp-node/dist/index.js"
      ],
      "cwd": "/あなたのパス/UnityMCPLearning",
      "env": {
        "MCP_LANGUAGE": "ja",
        "MCP_LOG_LEVEL": "debug",
        "MCP_CONFIG_PATH": "/あなたのパス/UnityMCPLearning/unity-mcp-node/mcp-config.json"
      }
    }
  }
}
```

**⚠️ 重要**: `/あなたのパス/` を実際のプロジェクトパスに置き換えてください。

### 3. パスの確認方法

```bash
# プロジェクトの絶対パスを確認
cd UnityMCPLearning
pwd
# 結果例: /Users/username/Projects/UnityMCPLearning
```

## 🧪 接続テスト

### 1. Unity MCP サーバーの動作確認

```bash
# プロジェクトディレクトリに移動
cd UnityMCPLearning/unity-mcp-node

# 単体テストを実行
npm test

# 接続テストを実行
node test-connection.js
```

**期待される出力:**
```
🚀 Unity MCP Server Connection Test
=====================================
📨 Response: {"jsonrpc":"2.0","id":1,"result":{"protocolVersion":"2024-11-05"...}}
📨 Response: {"jsonrpc":"2.0","id":2,"result":{"content":[{"type":"text","text":"🏓 Pong! MCP Server is running and responsive."}]}}
✅ Test completed
```

### 2. Claude Desktop の再起動

1. **Claude Desktop を完全に終了**
2. **Claude Desktop を再起動**
3. **ログを確認** (設定に問題がある場合エラーが表示されます)

### 3. Claude Code からの接続テスト

```bash
# Claude Code を起動
claude-code

# プロジェクトディレクトリで起動
cd UnityMCPLearning
claude-code
```

Claude Code の中で以下をテスト：

```
Unity の情報を教えて
```

## 🎮 基本的な使用方法

### 1. Unity 情報の取得

```
Unity プロジェクトの詳細情報を表示して
```

```
現在のシーンにあるオブジェクトをリストアップして
```

```
Unity エディターの状態を教えて
```

### 2. Unity オブジェクトの作成

```
Unity で赤いキューブを座標(0, 1, 0)に作成して
```

```
青いスフィアをシーンに追加して
```

```
"TestPlane"という名前でプレーンを作成して
```

### 3. リアルタイム監視

Unity エディターで変更を行うと、Claude Code が自動的に変更を検出して情報を更新します。

## 🔍 トラブルシューティング

### よくある問題と解決方法

#### 1. **"Server disconnected" エラー**

**原因**: パス設定が間違っている
**解決方法**:
```bash
# パスが正しいか確認
ls "/あなたのパス/UnityMCPLearning/unity-mcp-node/dist/index.js"

# ファイルが存在しない場合はビルド
cd unity-mcp-node
npm run build
```

#### 2. **"Cannot find module" エラー**

**原因**: 依存関係がインストールされていない
**解決方法**:
```bash
cd unity-mcp-node
npm install
npm run build
```

#### 3. **"Unity data not available" エラー**

**原因**: Unity プロジェクトが開かれていない、またはデータエクスポートが動作していない
**解決方法**:
1. Unity で MCPLearning プロジェクトを開く
2. `MCPLearning/UnityMCP/Data/` フォルダに JSON ファイルが生成されているか確認
3. Unity Console でエラーがないか確認

#### 4. **相対パスエラー**

**原因**: 設定ファイルで相対パスが正しく解決されていない
**解決方法**:
```json
{
  "env": {
    "MCP_CONFIG_PATH": "/絶対パス/UnityMCPLearning/unity-mcp-node/mcp-config.json"
  }
}
```

### デバッグ方法

#### 1. ログの確認

```bash
# Claude Desktop のログを確認（macOS）
tail -f "/Users/$(whoami)/Library/Logs/Claude/claude_desktop.log"
```

#### 2. MCP サーバーの直接テスト

```bash
# TypeScript版テスト
cd unity-mcp-node
npx tsx get-unity-info.ts

# JavaScript版テスト
node get-unity-info.js
```

#### 3. 設定ファイルの検証

```bash
# JSON 構文チェック
cat claude_desktop_config.json | python -m json.tool
```

## 📚 参考資料

- [Claude Code 公式ドキュメント](https://docs.anthropic.com/en/docs/claude-code)
- [MCP (Model Context Protocol) 仕様](https://modelcontextprotocol.io/)
- [Unity MCP Learning プロジェクト README](../../README.md)

## 🎯 次のステップ

統合が完了したら、以下をお試しください：

1. **[Unity Console 統合ガイド](./06-step3-unity-control.md)** - より高度な Unity 制御
2. **カスタムコマンドの作成** - 独自の Unity 操作を追加
3. **CI/CD パイプライン構築** - 自動テストと統合

---

**⚠️ 注意**: このガイドは初学者向けですが、ある程度の技術的な知識（コマンドライン操作、JSON編集など）が必要です。困った場合は [トラブルシューティング](#トラブルシューティング) セクションを参照してください。