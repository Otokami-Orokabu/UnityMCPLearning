# Unity MCP Learning - シンプル化されたパッケージ戦略

## 🎯 基本方針

MCPサーバーのビルド済み版を同梱するのではなく、**unity-mcp-nodeをnpm経由で直接インストール**する方式を採用。これにより、メンテナンスが容易でシンプルな構成を実現。

## 📦 簡略化されたパッケージ構造

```
UnityMCPLearning/
├── package.json                    # UPM用
├── README.md
├── LICENSE
├── Editor/                         # Unity Editor統合
│   ├── UnityMCP.Editor.asmdef
│   └── [各種Editorスクリプト]
├── Runtime/                        # 将来用
│   └── UnityMCP.Runtime.asmdef
├── unity-mcp-node/                # MCPサーバー（Git Submoduleまたは直接含む）
│   ├── package.json               # Node.js依存関係
│   ├── src/                       # TypeScriptソース
│   ├── dist/                      # ビルド済みJS
│   └── README.md
└── .github/
    └── workflows/
        └── package-release.yml     # 自動パッケージ作成
```

## 🚀 インストールフロー

### **ユーザー操作**
1. Unity Package Manager → Git URL入力
2. Tools > MCP Server Manager 開く
3. "Setup MCP Server" クリック

### **自動実行される処理**
```csharp
// Editor/Setup/MCPSimpleSetup.cs
public static class MCPSimpleSetup
{
    public static async Task SetupMCPServer()
    {
        var steps = new[]
        {
            "Node.js確認中...",
            "npm依存関係インストール中...",
            "プロジェクト設定中...",
            "完了！"
        };
        
        // Step 1: Node.js確認
        if (!await CheckNodeJs())
        {
            EditorUtility.DisplayDialog(
                "Node.js Required",
                "Node.js 18以降をインストールしてください。\n" +
                "https://nodejs.org/",
                "OK");
            return;
        }
        
        // Step 2: npm install実行
        var packagePath = GetPackagePath();
        var mcpPath = Path.Combine(packagePath, "unity-mcp-node");
        
        await ExecuteNpmInstall(mcpPath);
        
        // Step 3: プロジェクト設定
        SetupProjectConfiguration();
        
        EditorUtility.DisplayDialog(
            "Setup Complete",
            "Unity MCP Serverのセットアップが完了しました！",
            "OK");
    }
    
    static async Task ExecuteNpmInstall(string path)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "npm",
            Arguments = "install",
            WorkingDirectory = path,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using (var process = Process.Start(startInfo))
        {
            await process.WaitForExitAsync();
        }
    }
}
```

## 🔧 プロジェクト設定の自動化

### **設定ファイル生成**
```csharp
public static class MCPConfigGenerator
{
    public static void GenerateProjectConfig()
    {
        var projectId = MCPProjectIdentifier.GetOrCreateProjectId();
        var dataPath = GetProjectDataPath(projectId);
        var port = MCPPortManager.AllocatePort(projectId);
        
        // mcp-config.json生成
        var mcpConfig = new
        {
            mcpServers = new
            {
                unityMcp = new
                {
                    command = "node",
                    args = new[] { GetMCPServerPath() },
                    env = new
                    {
                        UNITY_PROJECT_ID = projectId,
                        UNITY_DATA_PATH = dataPath,
                        MCP_PORT = port.ToString()
                    }
                }
            }
        };
        
        SaveMcpConfig(mcpConfig);
    }
    
    static string GetMCPServerPath()
    {
        var packagePath = GetPackagePath();
        return Path.Combine(packagePath, "unity-mcp-node", "dist", "index.js");
    }
}
```

## 🎬 GitHub Actions自動化

### **パッケージリリースワークフロー**
```yaml
# .github/workflows/package-release.yml
name: Package Release

on:
  push:
    tags:
      - 'v*'
  workflow_dispatch:

jobs:
  create-upm-package:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
      with:
        submodules: recursive
    
    # Node.js環境セットアップ
    - uses: actions/setup-node@v3
      with:
        node-version: '20'
    
    # unity-mcp-nodeのビルド
    - name: Build MCP Server
      run: |
        cd unity-mcp-node
        npm ci
        npm run build
    
    # UPMブランチの準備
    - name: Prepare UPM Branch
      run: |
        # 不要なファイルを削除
        rm -rf .git
        rm -rf unity-mcp-node/node_modules
        rm -rf unity-mcp-node/.git
        rm -rf docs
        rm -rf .github
        
        # package.jsonのバージョン更新
        VERSION=${GITHUB_REF#refs/tags/v}
        jq ".version = \"$VERSION\"" package.json > tmp.json
        mv tmp.json package.json
    
    # UPMブランチへプッシュ
    - name: Push to UPM Branch
      run: |
        git config --global user.name "GitHub Actions"
        git config --global user.email "actions@github.com"
        
        git checkout -b upm
        git add -A
        git commit -m "UPM Release ${GITHUB_REF#refs/tags/}"
        
        git push origin upm --force
    
    # リリースノート作成
    - name: Create Release
      uses: actions/create-release@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        tag_name: ${{ github.ref }}
        release_name: Release ${{ github.ref }}
        body: |
          ## Installation
          
          1. Open Unity Package Manager
          2. Click "+" → "Add package from git URL"
          3. Enter: `https://github.com/${{ github.repository }}.git#upm`
          
          ## Requirements
          - Unity 6000.0+
          - Node.js 18+
        draft: false
        prerelease: false
```

## 🏗️ ローカル開発フロー

### **開発時の構成**
```bash
# 開発環境セットアップ
git clone https://github.com/orlab/UnityMCPLearning.git
cd UnityMCPLearning

# unity-mcp-nodeの開発
cd unity-mcp-node
npm install
npm run dev

# Unityでのテスト
# MCPLearning/プロジェクトを開く
# Package Managerでローカルパッケージとして追加
```

### **パッケージテスト**
```bash
# ローカルでUPMパッケージ作成テスト
./scripts/create-upm-package.sh

# 別のUnityプロジェクトでテスト
# file:///path/to/UnityMCPLearning をPackage Managerで追加
```

## 📋 シンプル化の利点

### **開発者視点**
- unity-mcp-nodeの更新が直接反映
- ビルドプロセスが不要
- デバッグが容易

### **ユーザー視点**
- インストール手順がシンプル
- Node.jsとnpm installのみで完結
- 更新が簡単

### **メンテナンス視点**
- 単一リポジトリ管理
- GitHub Actions自動化
- バージョン管理の一元化

## 🔄 更新フロー

### **パッチリリース**
```bash
# バグ修正
git commit -m "fix: ..."
git tag v1.0.1
git push origin v1.0.1
# → GitHub Actionsが自動でUPMパッケージ作成
```

### **機能追加**
```bash
# 新機能開発
git commit -m "feat: ..."
git tag v1.1.0
git push origin v1.1.0
# → 自動リリース
```

## 📝 ユーザー向け手順

### **初回インストール**
```
1. Unity Package Manager開く
2. Git URL: https://github.com/orlab/UnityMCPLearning.git#upm
3. インストール完了後、Tools > MCP Server Manager
4. "Setup MCP Server" → 自動でnpm install実行
5. 完了！
```

### **更新方法**
```
1. Package Managerで"Update"クリック
2. MCP Server Managerで"Update Dependencies"
3. 完了！
```

---

**作成日**: 2025年6月8日  
**方式**: unity-mcp-node直接インストール方式  
**利点**: シンプル・メンテナンス容易・透明性高