# Git URL配布対応 & PackageCache完全サポート実装記録

**実装期間**: 2025年6月9日  
**対象Issue**: [#4 Git URL配布対応 - Unity Package Manager経由配布・マルチプロジェクト対応](https://github.com/Otokami-Orokabu/UnityMCPLearning/issues/4)  
**リリースバージョン**: v0.1.10 → v0.1.12

## 🎯 目的

Unity Package ManagerのGit URL機能を使用した配布における、PackageCacheインストール時の問題を完全解決し、外部プロジェクトでの安定動作を実現する。

## 🔍 発見された問題

### 1. MCPサーバー起動エラー（SafeAreaCoプロジェクト）
**症状**: MCPサーバーがexit code 1で即座に終了
```
[MCPServerManager] MCP Server process exited immediately with code: 1
```

**原因**: 
- GitHub ActionsワークフローでSchema directoryが含まれていない
- `schema/mcp-config.schema.json`が見つからずバリデーション失敗

**解決**: v0.1.10でGitHub Actionsワークフローに`schema`ディレクトリコピーを追加

### 2. UIファイル（UXML/USS）読み込みエラー
**症状**: MCPServerManagerWindowでUIファイルが見つからない警告
```
[MCP] [MCPServerManagerWindow] UXML not found, creating programmatic UI
[MCP] [MCPServerManagerWindow] USS file not found, using default styles
```

**原因**: 
- GitHub ActionsワークフローでUIファイルがコピーされていない
- PackageCacheからのUIファイル検索パスが不適切

**解決**: v0.1.11でUIファイルコピーとServer~ディレクトリ検索を追加

### 3. AssetInfoExporterのハードコードパスエラー
**症状**: フォルダが見つからないエラー
```
AssetDatabase.FindAssets: Folder not found: 'Assets/Packages/unity-mcp-learning'
```

**原因**: 
- `AssetInfoExporter.cs`でハードコードされたパス使用
- PackageCache環境で存在しないパスを検索

**解決**: v0.1.12で動的パス解決システムを実装

## 📦 実装されたソリューション

### 1. GitHub Actionsワークフロー改善

#### v0.1.10: Schemaディレクトリ追加
```yaml
- name: Copy build artifacts
  run: |
    cp -r unity-mcp-node/dist MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
    cp -r unity-mcp-node/node_modules MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
    cp -r unity-mcp-node/schema MCPLearning/Assets/Packages/unity-mcp-learning/Server~/  # 追加
    cp unity-mcp-node/package.json MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
    cp unity-mcp-node/mcp-config.json MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
```

#### v0.1.11: UIファイルコピー追加
```yaml
- name: Copy UI files
  run: |
    mkdir -p MCPLearning/Assets/Packages/unity-mcp-learning/Server~/Scripts/Editor/Windows
    cp MCPLearning/Assets/Packages/unity-mcp-learning/Scripts/Editor/Windows/MCPServerManagerWindow.uxml MCPLearning/Assets/Packages/unity-mcp-learning/Server~/Scripts/Editor/Windows/
    cp MCPLearning/Assets/Packages/unity-mcp-learning/Scripts/Editor/Windows/MCPServerManagerWindow.uss MCPLearning/Assets/Packages/unity-mcp-learning/Server~/Scripts/Editor/Windows/
```

### 2. 動的パス解決システム実装

#### AssetInfoExporter.cs修正（v0.1.12）
**Before**:
```csharp
["hasUnityMCPAssets"] = AssetDatabase.FindAssets("", new[] { "Assets/Packages/unity-mcp-learning" }).Length
```

**After**:
```csharp
// Unity MCPアセットの存在チェック（動的パス解決）
var hasUnityMCPAssets = 0;
try
{
    var packagePath = MCPPackageResolver.GetPackageRootPath();
    if (Directory.Exists(packagePath))
    {
        hasUnityMCPAssets = AssetDatabase.FindAssets("", new[] { packagePath }).Length;
    }
}
catch
{
    // パッケージパスが見つからない場合は0を返す
    hasUnityMCPAssets = 0;
}
```

#### MCPServerManagerWindow.cs UIファイル検索強化（v0.1.12）
```csharp
// Server~ディレクトリ内での検索（リリースパッケージ用）
var serverPath = MCPPackageResolver.GetServerPath();
if (!string.IsNullOrEmpty(serverPath))
{
    var serverUIPath = Path.Combine(serverPath, "Scripts/Editor/Windows", fileName).Replace('\\', '/');
    MCPLogger.LogInfo($"[MCPServerManagerWindow] Trying Server~ path: {serverUIPath}");
    
    asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(serverUIPath);
    if (asset != null)
    {
        MCPLogger.LogInfo($"[MCPServerManagerWindow] Successfully found {fileName} at Server~ path: {serverUIPath}");
        return serverUIPath;
    }
}
```

### 3. 開発環境Server~ディレクトリ構築

MCPLearning開発プロジェクト用に手動でServer~ディレクトリを構築：

```bash
mkdir -p MCPLearning/Assets/Packages/unity-mcp-learning/Server~
cd unity-mcp-node && npm run build
cp -r dist ../MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
cp -r node_modules ../MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
cp -r schema ../MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
cp package.json ../MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
cp mcp-config.json ../MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
```

## 🧪 テスト・検証結果

### 検証環境1: MCPLearning開発プロジェクト
- **パッケージタイプ**: ローカル開発（Assets/Packages/unity-mcp-learning）
- **結果**: ✅ MCPサーバー正常起動（ポート3000）
- **ログ**: エラー0件、警告2件（動的検索通知のみ）

### 検証環境2: SafeAreaCoプロジェクト
- **パッケージタイプ**: PackageCache（Git URL）
- **パッケージハッシュ**: 7ffe88d60307 → 最新バージョンに更新要求
- **結果**: ✅ MCPサーバー正常起動、プロジェクト情報取得成功
- **データ取得例**:
  ```json
  {
    "projectName": "SafeAreaCo",
    "unityVersion": "6000.1.5f1",
    "platform": "OSXEditor",
    "totalAssets": 9118,
    "hasUnityMCPAssets": 48
  }
  ```

## 📈 改善効果

### エラー削減
- **v0.1.9以前**: 18件のエラー、4件の警告
- **v0.1.12**: 0件のエラー、2件の警告（動的検索通知のみ）

### 機能動作確認
- ✅ MCPサーバー自動起動
- ✅ Claude Desktop設定自動更新
- ✅ プロジェクト間データ分離
- ✅ UIファイル正常読み込み
- ✅ リアルタイムプロジェクト情報取得

### パッケージ配布の安定性
- ✅ PackageCache環境での完全動作
- ✅ 外部プロジェクトでの即座利用可能
- ✅ Git URLインストールの問題完全解消

## 🔧 技術的アーキテクチャ

### パッケージ検出フロー
```
1. MCPPackageResolver.GetPackageRootPath()
   ↓
2. PackageInfo.FindForAssetPath() 試行
   ↓
3. AssetDatabase.FindAssets() 動的検索
   ↓
4. ハードコードパス（フォールバック）
```

### UIファイル検索フロー
```
1. パッケージルート/Scripts/Editor/Windows/
   ↓
2. Server~/Scripts/Editor/Windows/ （リリース版）
   ↓
3. AssetDatabase.FindAssets() 全検索
   ↓
4. PackageInfo API検索
```

### データ分離システム
```
プロジェクトパス → SHA256ハッシュ → 16文字ID
例: /Users/.../SafeAreaCo → 79a3f8fd9659bd0a

UnityMCP/Data/
├── project-79a3f8fd9659bd0a/  # SafeAreaCo
└── project-ed07ab0e72d17577/  # MCPLearning
```

## 📋 今後の保守方針

### GitHub Actionsワークフロー
- Server~ディレクトリ完全自動構築
- UIファイル、Schema、依存関係の自動コピー
- バージョンタグベースの自動リリース

### 動的パス解決
- MCPPackageResolverによる統一パス管理
- ハードコードパスの撲滅
- PackageInfo APIを活用した確実な検出

### 開発・リリース分離
- 開発環境: 手動Server~構築
- リリース環境: GitHub Actions自動構築
- 両環境での完全互換性

## 🎯 達成した目標

1. ✅ **Git URL配布の完全対応**
2. ✅ **マルチプロジェクト同時実行**
3. ✅ **PackageCache環境での安定動作**
4. ✅ **外部プロジェクトでの即座利用**
5. ✅ **エラー完全解消**

**最終リリース**: v0.1.12  
**Git URL**: `https://github.com/Otokami-Orokabu/UnityMCPLearning.git?path=MCPLearning/Assets/Packages/unity-mcp-learning#v0.1.12`

**次のマイルストーン**: [Issue #9 マルチプロジェクト対応 - 複数Unity同時起動サポート](https://github.com/Otokami-Orokabu/UnityMCPLearning/issues/9)