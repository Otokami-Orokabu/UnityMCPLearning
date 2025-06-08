# Unity MCP Learning - Git URL インストールガイド

## 📋 システム要件

### 必須環境
- **Unity**: 6000.0以降
- **Node.js**: 18.0以降（**必須**）
- **Claude Desktop**: 最新版
- **OS**: macOS、Windows、Linux

> ⚠️ **重要**: Node.js 18.0以降が事前にインストールされている必要があります。
> [Node.js公式サイト](https://nodejs.org/)からダウンロードしてインストールしてください。

### 推奨環境
- **Unity**: 6000.1.5f1以降
- **Node.js**: 20.0以降
- **メモリ**: 8GB以上
- **ストレージ**: 500MB以上の空き容量

## 🚀 インストール手順

### Step 1: Unity Package Managerでのインストール

1. **Unityプロジェクトを開く**
   - Unity 6以降のプロジェクトを開いてください
   - 新規プロジェクトでも既存プロジェクトでも対応可能です

2. **Package Managerを開く**
   ```
   Window > Package Manager
   ```

3. **Git URLからパッケージを追加**
   - Package Managerの左上「+」ボタンをクリック
   - 「Add package from git URL...」を選択

4. **Git URLを入力**
   ```
   https://github.com/orlab/UnityMCPLearning.git
   ```
   - URLを入力し「Add」をクリック

5. **インストール完了を待機**
   - パッケージのダウンロードとインストールが自動実行されます
   - `com.orlab.unity-mcp-learning` がPackage Managerに表示されたら完了

### Step 2: 初回セットアップ

1. **MCP Server Managerを開く**
   ```
   Tools > MCP Server Manager
   ```

2. **Node.js環境の確認**
   - まず「Check Node.js」ボタンをクリック
   - Node.js 18.0以降がインストールされていることを確認
   - ❌ エラーが表示された場合は、Node.jsをインストールしてください

3. **自動セットアップの実行**
   - 「Setup MCP Server」ボタンをクリック
   - 進行状況が詳細に表示されます：
     
     ```
     [1/5] Node.js依存関係をインストール中...
     [2/5] プロジェクト固有ID（a1b2c3d4）を生成中...
     [3/5] データディレクトリを作成中...
     [4/5] 利用可能ポート（3001）を検索中...
     [5/5] 設定ファイルを保存中...
     ✅ セットアップ完了！
     ```

4. **Claude Desktop設定の生成**（詳細設計は後日）
   - 「Generate Claude Desktop Config」ボタンをクリック
   - プロジェクト専用の設定が生成されます
   - 進行状況の表示：
     ```
     📝 設定ファイルを生成中...
     📁 Claude Desktop設定パスを検出中...
     💾 設定を適用中...
     ✅ Claude Desktop設定完了！
     ```

## 🔧 Claude Desktopの設定

### 自動設定（推奨）

1. **設定ファイルの自動追加**
   - MCP Server Managerの「Apply to Claude Desktop」をクリック
   - 以下のパスに設定が自動追加されます：
     - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
     - **Windows**: `%APPDATA%/Claude/claude_desktop_config.json`

2. **設定内容の確認**
   ```json
   {
     "mcpServers": {
       "unity-mcp-project-a1b2c3d4": {
         "command": "node",
         "args": ["/path/to/package/MCPServer~/dist/server.js"],
         "env": {
           "MCP_DATA_PATH": "/path/to/project/UnityMCPData/project_a1b2c3d4/Data",
           "MCP_PROJECT_ID": "a1b2c3d4",
           "MCP_PORT": "3001"
         }
       }
     }
   }
   ```

### 手動設定（上級者向け）

1. **Claude Desktop設定ファイルを開く**
   - 上記パスのファイルをテキストエディタで開く

2. **MCP Server Manager画面の設定をコピー**
   - 「Copy Configuration」ボタンで設定をクリップボードにコピー
   - 設定ファイルの`mcpServers`セクションに貼り付け

## 🎯 動作確認

### Step 1: MCPサーバーの起動確認

1. **サーバー起動**
   - MCP Server Managerで「Start Server」をクリック
   - ステータスが「Running」になることを確認

2. **接続テスト**
   - 「Test Connection」ボタンをクリック
   - ✅「Connection successful」が表示されることを確認

### Step 2: Claude Desktopでの動作確認

1. **Claude Desktopを再起動**
   - 設定変更を反映するため、Claude Desktopを完全に再起動

2. **MCPツールの確認**
   - Claude Desktopで新しいチャットを開始
   - 以下のコマンドを試行：
   ```
   ping
   ```
   - 「pong」が返ってくれば正常動作

3. **Unity制御テスト**
   ```
   create_cube
   ```
   - Unityのシーンにキューブが作成されることを確認

## 📁 インストール後のディレクトリ構造

```
YourUnityProject/
├── Assets/
│   └── (あなたのアセット)
├── Library/
│   └── PackageCache/
│       └── com.orlab.unity-mcp-learning@1.0.0/
│           ├── Editor/              # Unity統合システム
│           ├── Runtime/
│           ├── MCPServer~/          # MCPサーバー
│           └── Documentation~/
├── ProjectSettings/
├── UserSettings/
│   └── MCPServerConfig.json        # サーバー設定
└── UnityMCPData/                   # MCPデータ（自動作成）
    └── project_a1b2c3d4/           # プロジェクト固有ID
        ├── Data/
        │   ├── assets-info.json
        │   ├── console-logs.json
        │   └── gameobjects.json
        ├── Commands/
        └── Logs/
```

## 🔄 複数プロジェクトでの使用

### 自動対応
- **別のUnityプロジェクトでも同じ手順でインストール**
- プロジェクトごとに自動的に：
  - 異なるプロジェクトID（例：e5f6g7h8）が生成
  - 異なるポート（例：3002）が割り当て
  - 独立したデータディレクトリが作成

### 同時実行
- 複数のUnityエディタを同時に開いても競合しません
- それぞれが独立したMCPサーバーで動作します

## 🛠️ 利用可能なMCPツール

### Unity制御
```bash
create_cube          # キューブを作成
create_sphere        # スフィアを作成
create_plane         # プレーンを作成
create_gameobject    # 空のGameObjectを作成
```

### Unity情報取得
```bash
unity_info_realtime  # Unity状態をリアルタイム取得
get_console_logs     # コンソールログを取得
wait_for_compilation # コンパイル完了を待機
```

### システム
```bash
ping                 # 接続確認
```

## 🔧 トラブルシューティング

### Node.jsが見つからない・バージョンが古い
**症状**: 
- "node command not found"エラー
- "Node.js version 16.x detected. Minimum required: 18.0"エラー

**解決策**: 
1. [Node.js公式サイト](https://nodejs.org/)から**LTS版（推奨）**をダウンロード
2. インストール後、ターミナル/コマンドプロンプトで確認：
   ```bash
   node --version
   # v20.x.x のように18.0以降が表示されることを確認
   ```
3. 古いバージョンが表示される場合：
   - **Windows**: Node.jsを再インストール
   - **macOS**: Homebrew使用時は `brew upgrade node`
   - **Linux**: パッケージマネージャーで更新

### ポートが使用中
**症状**: "Port 3000 is already in use"エラー
**解決策**: 
- 自動的に別のポート（3001-3100）が使用されます
- MCP Server Managerで使用ポートを確認可能

### Claude Desktop接続エラー
**症状**: Claude Desktopで応答がない
**解決策**: 
1. Claude Desktopを完全再起動
2. MCP Server Managerで「Test Connection」実行
3. 設定ファイルのパスが正しいことを確認

### Unity Package Manager エラー
**症状**: Git URLが解決できない
**解決策**: 
1. インターネット接続を確認
2. Git URLが正しいことを確認
3. Unityを最新版に更新

## 📚 関連ドキュメント

- [MCP Server Manager完全ガイド](11-mcp-server-manager-guide.md)
- [Unity Console統合ガイド](10-unity-console-integration-guide.md)
- [Claude Code MCP統合ガイド](09-claude-code-mcp-integration.md)
- [トラブルシューティング](04-troubleshooting.md)

## 💡 おすすめワークフロー

### AI駆動開発サイクル
1. **Claude Desktopでアイデアを相談**
   ```
   「Unity でプレイヤーキャラクターの移動システムを作りたい」
   ```

2. **自動でGameObjectを作成**
   ```
   create_cube  # プレイヤーキャラクター用
   ```

3. **スクリプト生成と確認**
   - Claude DesktopがC#スクリプトを生成
   - Unity Consoleでコンパイル状況を自動監視

4. **エラー検知と修正**
   ```
   get_console_logs  # エラーを自動取得
   wait_for_compilation  # 修正後のコンパイル完了を待機
   ```

### チーム開発
- チームメンバーがそれぞれ異なるプロジェクトで同時作業可能
- プロジェクトごとの独立したAI支援環境
- 設定やデータの競合なし

---

**作成日**: 2025年6月8日  
**対象バージョン**: Unity MCP Learning v1.0.0  
**前提**: Git URL配布・マルチプロジェクト対応版