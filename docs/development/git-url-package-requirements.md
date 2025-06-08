# Unity MCP Learning - Git URL配布パッケージ要件定義

## 📋 概要

Unity Package Manager (UPM) を使用したGit URL配布の詳細要件定義書。Unity 6以降を対象とし、MCPサーバーを同梱した完全パッケージを提供する。

## 🎯 基本要件

### **対象環境**
- **Unity**: 6000.0以降（必須）
- **Node.js**: 18.0以降（ユーザー環境に必須）
- **OS**: macOS, Windows, Linux
- **Claude Desktop**: 最新版

### **配布方式**
- **方法**: Unity Package Manager経由のGit URL
- **URL形式**: `https://github.com/[organization]/UnityMCPLearning.git#[version]`
- **ブランチ戦略**: `main`（開発）、`upm`（配布用）、タグによるバージョン管理

## 📦 パッケージ構造

### **ルートディレクトリ構成**
```
UnityMCPLearning/
├── package.json                    # UPM用メタデータ
├── package.json.meta
├── README.md                       # パッケージ説明
├── README.md.meta
├── CHANGELOG.md                    # 変更履歴
├── CHANGELOG.md.meta
├── LICENSE                         # ライセンス情報
├── LICENSE.meta
├── Documentation~/                 # ドキュメント（~で除外）
│   ├── Installation.md
│   ├── QuickStart.md
│   ├── API.md
│   └── Troubleshooting.md
├── Editor/                         # Unity Editor統合
│   ├── UnityMCP.Editor.asmdef
│   ├── UnityMCP.Editor.asmdef.meta
│   └── [各種Editorスクリプト]
├── Runtime/                        # ランタイム機能（将来用）
│   ├── UnityMCP.Runtime.asmdef
│   └── UnityMCP.Runtime.asmdef.meta
├── MCPServer~/                     # MCPサーバー（~で除外されるがGitには含む）
│   ├── dist/                       # ビルド済みJavaScript
│   ├── package.json               # Node.js依存関係
│   ├── README.md
│   └── setup.js                   # セットアップスクリプト
├── Samples~/                       # サンプルコード
│   └── BasicUsage/
│       ├── BasicExample.cs
│       └── SampleScene.unity
└── Tests/                          # テストコード
    ├── Editor/
    └── Runtime/
```

### **package.json (UPM用)**
```json
{
  "name": "com.orlab.unity-mcp-learning",
  "version": "1.0.0",
  "displayName": "Unity MCP Learning",
  "description": "AI-driven Unity development with Claude Desktop integration via MCP protocol",
  "unity": "6000.0",
  "unityRelease": "0f1",
  "documentationUrl": "https://github.com/orlab/UnityMCPLearning/blob/main/README.md",
  "changelogUrl": "https://github.com/orlab/UnityMCPLearning/blob/main/CHANGELOG.md",
  "licensesUrl": "https://github.com/orlab/UnityMCPLearning/blob/main/LICENSE",
  "dependencies": {},
  "keywords": [
    "mcp",
    "ai",
    "claude",
    "automation",
    "editor-extension",
    "development-tools"
  ],
  "author": {
    "name": "orlab",
    "email": "contact@orlab.dev",
    "url": "https://github.com/orlab"
  },
  "repository": {
    "type": "git",
    "url": "https://github.com/orlab/UnityMCPLearning.git"
  },
  "publishConfig": {
    "registry": "https://npm.pkg.github.com"
  }
}
```

## 🛠️ MCPサーバー同梱要件

### **同梱方式**
- **ビルド済みJavaScript**: `MCPServer~/dist/` に配置
- **依存関係**: 最小限のproduction依存のみ
- **サイズ**: 圧縮後10MB以下目標

### **ビルドプロセス**
```bash
# MCPサーバーのビルド手順
cd unity-mcp-node
npm run build:production  # webpackで単一ファイルにバンドル
cp dist/server.min.js ../MCPServer~/dist/
cp package-minimal.json ../MCPServer~/package.json
```

### **MCPServer~/package.json (最小構成)**
```json
{
  "name": "unity-mcp-server",
  "version": "1.0.0",
  "main": "dist/server.min.js",
  "engines": {
    "node": ">=18.0.0"
  },
  "scripts": {
    "postinstall": "node setup.js"
  },
  "dependencies": {
    "ws": "^8.0.0"
  }
}
```

## 🚀 インストールプロセス

### **ユーザー側の手順**
1. Unity Package Manager を開く
2. "+" → "Add package from git URL"
3. `https://github.com/orlab/UnityMCPLearning.git` を入力
4. 自動インストール完了を待つ
5. Tools > MCP Server Manager を開く
6. "Setup MCP Server" をクリック

### **自動セットアップ機能**
```csharp
// Editor/Setup/MCPAutoSetup.cs
[InitializeOnLoad]
public static class MCPAutoSetup
{
    static MCPAutoSetup()
    {
        EditorApplication.delayCall += CheckAndSetup;
    }
    
    static void CheckAndSetup()
    {
        if (!MCPSetupValidator.IsSetupComplete())
        {
            if (EditorUtility.DisplayDialog(
                "Unity MCP Setup",
                "Unity MCP Learning needs to set up the MCP server. Continue?",
                "Yes", "Later"))
            {
                MCPSetupWizard.StartSetup();
            }
        }
    }
}
```

### **セットアップ検証項目**
```csharp
public static class MCPSetupValidator
{
    public static SetupStatus ValidateSetup()
    {
        return new SetupStatus
        {
            NodeJsInstalled = CheckNodeJs(),
            NodeJsVersion = GetNodeJsVersion(),
            MCPServerInstalled = CheckMCPServer(),
            DataDirectoryExists = CheckDataDirectory(),
            ClaudeDesktopConfigured = CheckClaudeConfig(),
            ProjectIdGenerated = CheckProjectId()
        };
    }
}
```

## 🔧 マルチプロジェクト対応

### **プロジェクト識別**
```csharp
// Editor/Core/MCPProjectIdentifier.cs
public static class MCPProjectIdentifier
{
    private const string PROJECT_ID_KEY = "UnityMCP.ProjectId";
    
    public static string GetOrCreateProjectId()
    {
        var projectId = EditorPrefs.GetString(PROJECT_ID_KEY);
        
        if (string.IsNullOrEmpty(projectId))
        {
            projectId = GenerateProjectId();
            EditorPrefs.SetString(PROJECT_ID_KEY, projectId);
        }
        
        return projectId;
    }
    
    private static string GenerateProjectId()
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(
                $"{Application.dataPath}_{DateTime.Now.Ticks}_{Guid.NewGuid()}"
            );
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash)
                .Replace("-", "")
                .Substring(0, 8)
                .ToLower();
        }
    }
}
```

### **データディレクトリ構造**
```
{ProjectRoot}/
└── UnityMCPData/
    └── project_{projectId}/
        ├── Data/
        │   ├── assets-info.json
        │   ├── compile-status.json
        │   ├── console-logs.json
        │   └── gameobjects.json
        ├── Commands/
        ├── Logs/
        └── Settings/
            └── server-config.json
```

### **ポート管理**
```csharp
// Editor/Core/MCPPortManager.cs
public static class MCPPortManager
{
    private const int BASE_PORT = 3000;
    private const int MAX_PORT_RANGE = 100;
    
    public static int AllocatePort(string projectId)
    {
        // プロジェクトIDベースでポートを決定
        var hash = projectId.GetHashCode();
        var offset = Math.Abs(hash) % MAX_PORT_RANGE;
        var port = BASE_PORT + offset;
        
        // ポートが使用中の場合は次を探す
        while (!IsPortAvailable(port) && port < BASE_PORT + MAX_PORT_RANGE)
        {
            port++;
        }
        
        if (port >= BASE_PORT + MAX_PORT_RANGE)
        {
            throw new Exception("No available port found");
        }
        
        return port;
    }
}
```

## 📋 品質要件

### **パフォーマンス**
- パッケージインポート: 30秒以内
- MCPサーバー起動: 5秒以内
- メモリ使用量: 100MB以下
- CPU使用率: アイドル時1%以下

### **互換性**
- Unity 6000.0以降のすべてのバージョン
- macOS 11以降、Windows 10以降、Ubuntu 20.04以降
- Node.js 18/20/22 LTS版

### **セキュリティ**
- ローカル接続のみ（localhost）
- プロジェクト間のデータ分離
- 最小権限原則の適用

## 🧪 テスト要件

### **自動テスト**
```csharp
// Tests/Editor/PackageTests.cs
public class PackageInstallationTests
{
    [Test]
    public void ValidatePackageStructure()
    {
        Assert.IsTrue(File.Exists("package.json"));
        Assert.IsTrue(Directory.Exists("Editor"));
        Assert.IsTrue(Directory.Exists("MCPServer~"));
    }
    
    [Test]
    public void ValidateNodeJsCompatibility()
    {
        var version = MCPSetupValidator.GetNodeJsVersion();
        Assert.IsTrue(version.Major >= 18);
    }
}
```

### **手動テスト項目**
- [ ] クリーンなUnityプロジェクトでのインストール
- [ ] 既存プロジェクトへの追加
- [ ] 複数プロジェクト同時起動
- [ ] 各OS環境での動作確認
- [ ] Claude Desktop連携テスト

## 📝 ドキュメント要件

### **必須ドキュメント**
- **README.md**: 概要、特徴、クイックスタート
- **Installation.md**: 詳細なインストール手順
- **Configuration.md**: 設定方法
- **Troubleshooting.md**: トラブルシューティング
- **API.md**: 利用可能なMCPツール一覧

### **動画チュートリアル**
- インストール手順（3分）
- 基本的な使い方（5分）
- 高度な機能（10分）

## 🚦 リリース基準

### **必須条件**
- [ ] すべての自動テストがパス
- [ ] 3つ以上の異なる環境での動作確認
- [ ] ドキュメント完備
- [ ] サンプルプロジェクト動作確認
- [ ] セキュリティ監査完了

### **推奨条件**
- [ ] パフォーマンステスト合格
- [ ] ユーザビリティテスト実施
- [ ] ベータテスター5名以上のフィードバック

---

**作成日**: 2025年6月8日  
**更新日**: 2025年6月8日  
**バージョン**: 1.0.0-draft