# PackageCache環境 トラブルシューティングガイド

**最終更新**: 2025年6月9日  
**対象バージョン**: v0.1.12以降

## 🎯 概要

Unity Package ManagerのGit URL経由でインストールしたパッケージがPackageCache環境で正常動作しない問題の診断・解決方法をまとめたガイドです。

## 🔍 よくある問題と解決方法

### 1. MCPサーバー起動失敗（Exit Code 1）

#### 症状
```
[MCPServerManager] MCP Server process exited immediately with code: 1
```

#### 診断方法
```csharp
// Unity Console または MCPServerManagerWindow ログを確認
// 以下のようなエラーが表示される場合
```

#### 原因と解決策

**原因A: Schemaファイル不足**
```
Failed to load JSON schema: ENOENT: no such file or directory
```
- **解決**: v0.1.10以降を使用（schemaディレクトリが自動含有）
- **確認方法**: `Library/PackageCache/com.orlab.unity-mcp-learning@<hash>/Server~/schema/` 存在確認

**原因B: node_modules不足**
```
Cannot find module 'ajv'
```
- **解決**: v0.1.10以降を使用（node_modules完全同梱）
- **確認方法**: `Library/PackageCache/com.orlab.unity-mcp-learning@<hash>/Server~/node_modules/` 存在確認

**原因C: mcp-config.json不足**
```
Configuration file not found
```
- **解決**: v0.1.8以降を使用（mcp-config.json自動含有）

### 2. UIファイル読み込みエラー

#### 症状
```
[MCP] [MCPServerManagerWindow] UXML not found, creating programmatic UI
[MCP] [MCPServerManagerWindow] USS file not found, using default styles
```

#### 診断方法
1. MCPServerManagerWindowを開く
2. プログラマティックUIが表示される（UXMLベースUIではない）
3. コンソールに警告メッセージが表示

#### 解決策
- **解決**: v0.1.11以降を使用（UIファイル自動配置）
- **確認方法**: 
  ```
  Library/PackageCache/com.orlab.unity-mcp-learning@<hash>/Server~/Scripts/Editor/Windows/
  ├── MCPServerManagerWindow.uxml
  └── MCPServerManagerWindow.uss
  ```

### 3. AssetDatabase検索エラー

#### 症状
```
AssetDatabase.FindAssets: Folder not found: 'Assets/Packages/unity-mcp-learning'
```

#### 診断方法
1. Console → Clear → 再生成
2. UnityMCP/Data/console-logs.jsonで詳細確認

#### 解決策
- **解決**: v0.1.12以降を使用（動的パス解決実装）
- **技術詳細**: AssetInfoExporterがMCPPackageResolver使用に変更

### 4. パッケージバージョン更新されない

#### 症状
- 新しいバージョンをインストールしても古いパッケージハッシュが使用される
- `Library/PackageCache/com.orlab.unity-mcp-learning@7ffe88d60307/` など

#### 解決策
1. **Package Managerでリフレッシュ**
   ```
   Window > Package Manager > In Project > Unity MCP Learning > Remove
   ```

2. **PackageCacheクリア**
   ```bash
   # Unityエディター終了後
   rm -rf Library/PackageCache/com.orlab.unity-mcp-learning*
   ```

3. **Git URLで再インストール**
   ```
   https://github.com/Otokami-Orokabu/UnityMCPLearning.git?path=MCPLearning/Assets/Packages/unity-mcp-learning#v0.1.12
   ```

## 🛠️ 診断ツール

### 1. MCPServerManagerWindow診断機能

MCPServerManagerWindowの「Package Info」セクションで以下を確認：

- **Package Path**: パッケージの検出パス
- **Server Path**: MCPサーバーファイルの配置パス
- **Version**: 現在のパッケージバージョン

### 2. コンソールログ確認

```json
// UnityMCP/Data/console-logs.json
{
  "logs": [
    {
      "message": "[MCPPackageResolver] Package found via dynamic search: Assets/Packages/unity-mcp-learning -> /path/to/package",
      "type": "Warning"
    }
  ]
}
```

### 3. MCP通信テスト

MCPServerManagerWindowの「Test Connection」ボタンで通信テスト実行：

```json
// 正常レスポンス例
{
  "jsonrpc": "2.0",
  "result": {
    "tools": [
      {"name": "unity_info_realtime"},
      {"name": "create_cube"},
      // ...
    ]
  }
}
```

## 📦 パッケージ構造の確認

### 正常なPackageCache構造（v0.1.12）

```
Library/PackageCache/com.orlab.unity-mcp-learning@<hash>/
├── CHANGELOG.md
├── package.json
├── Scripts/
│   └── Editor/
│       ├── Common/
│       │   ├── MCPPackageResolver.cs      # 動的パス解決
│       │   ├── MCPProjectIdentifier.cs    # プロジェクトID
│       │   └── MCPPortManager.cs          # ポート管理
│       ├── Exporters/
│       │   └── AssetInfoExporter.cs       # 修正済み
│       └── Windows/
│           ├── MCPServerManagerWindow.cs  # 強化済み
│           ├── MCPServerManagerWindow.uxml
│           └── MCPServerManagerWindow.uss
└── Server~/                               # v0.1.10以降
    ├── dist/                              # TypeScriptビルド成果物
    ├── node_modules/                      # 完全な依存関係
    ├── schema/                            # JSON Schema
    │   └── mcp-config.schema.json
    ├── package.json
    ├── mcp-config.json
    └── Scripts/                           # v0.1.11以降
        └── Editor/
            └── Windows/
                ├── MCPServerManagerWindow.uxml
                └── MCPServerManagerWindow.uss
```

## 🔧 手動修復方法

### 開発環境でのServer~構築

MCPLearning開発プロジェクトでServer~ディレクトリが不足している場合：

```bash
# プロジェクトルートで実行
mkdir -p MCPLearning/Assets/Packages/unity-mcp-learning/Server~

# unity-mcp-nodeディレクトリに移動
cd unity-mcp-node

# TypeScriptビルド
npm run build

# ファイルコピー
cp -r dist ../MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
cp -r node_modules ../MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
cp -r schema ../MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
cp package.json ../MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
cp mcp-config.json ../MCPLearning/Assets/Packages/unity-mcp-learning/Server~/

# UIファイルコピー
mkdir -p ../MCPLearning/Assets/Packages/unity-mcp-learning/Server~/Scripts/Editor/Windows
cp ../MCPLearning/Assets/Packages/unity-mcp-learning/Scripts/Editor/Windows/MCPServerManagerWindow.uxml ../MCPLearning/Assets/Packages/unity-mcp-learning/Server~/Scripts/Editor/Windows/
cp ../MCPLearning/Assets/Packages/unity-mcp-learning/Scripts/Editor/Windows/MCPServerManagerWindow.uss ../MCPLearning/Assets/Packages/unity-mcp-learning/Server~/Scripts/Editor/Windows/
```

## 🐛 既知の問題と制限事項

### 1. Unity Editor再起動後のポート競合

**症状**: 前回のMCPサーバープロセスが残存してポート競合

**解決策**:
```bash
# macOS/Linux
lsof -ti:3000 | xargs kill -9

# Windows
netstat -ano | findstr :3000
taskkill /PID <PID> /F
```

### 2. Claude Desktop設定の重複

**症状**: 複数のUnityプロジェクトで同じポートが設定される

**解決策**: MCPServerManagerWindowで異なるポートを手動割り当て

### 3. プロジェクト移動時のデータパス問題

**症状**: プロジェクトパスが変更された際にデータが見つからない

**解決策**: 
- プロジェクトIDは不変（パスのハッシュベース）
- UnityMCP/Dataディレクトリを新しい場所にコピー

## 📞 サポート情報

### 報告すべき情報

問題報告時は以下の情報を含めてください：

1. **Unity バージョン**: (例: 6000.1.5f1)
2. **パッケージバージョン**: (例: v0.1.12)
3. **パッケージハッシュ**: `Library/PackageCache/`ディレクトリ名
4. **OS**: (例: macOS 15.5, Windows 11)
5. **エラーメッセージ**: コンソールログまたはconsole-logs.json
6. **Package Manager情報**: Git URLとインストール方法

### 関連ドキュメント

- [Git URL配布対応実装記録](git-url-package-cache-support.md)
- [Unity MCP Server Manager完全ガイド](../tutorial/11-mcp-server-manager-guide.md)
- [GitHub Issues](https://github.com/Otokami-Orokabu/UnityMCPLearning/issues)

### バージョン履歴

- **v0.1.12**: AssetInfoExporter動的パス解決、UI検索強化
- **v0.1.11**: UIファイル配布対応
- **v0.1.10**: Schema/node_modules配布対応
- **v0.1.8**: mcp-config.json配布対応

**最新安定版**: v0.1.12  
**推奨Git URL**: `https://github.com/Otokami-Orokabu/UnityMCPLearning.git?path=MCPLearning/Assets/Packages/unity-mcp-learning#v0.1.12`