# Unity MCP Learning - UMP パッケージ構造設計

## 🎯 概要

Unity Package Manager (UMP) 配布に最適化されたパッケージ構造の詳細設計。現在のプロジェクト構造から配布可能なパッケージ形式への変換計画。

## 📦 現在の構造 vs 目標構造

### **現在の構造**
```
UnityMCPLearning/
├── MCPLearning/                    # Unity プロジェクト
│   ├── Assets/UnityMCP/           # 開発時のスクリプト
│   └── [その他Unityアセット]
├── unity-mcp-node/                # MCPサーバー
├── docs/                          # ドキュメント
└── [その他開発ファイル]
```

### **目標UMPパッケージ構造**
```
UnityMCPLearning/ (umpブランチ)
├── package.json                    # UMP メタデータ
├── README.md                       # パッケージ説明
├── CHANGELOG.md                    # 変更履歴
├── LICENSE                         # ライセンス
├── Documentation~/                 # ユーザー向けドキュメント
│   ├── installation.md
│   ├── quick-start.md
│   ├── api-reference.md
│   └── troubleshooting.md
├── Editor/                         # Unity Editor 統合
│   ├── UnityMCP.Editor.asmdef
│   ├── Common/                     # 共通機能
│   │   ├── MCPServerManager.cs
│   │   ├── MCPConnectionMonitor.cs
│   │   ├── MCPLogger.cs
│   │   └── [その他共通クラス]
│   ├── Exporters/                  # データエクスポーター
│   │   ├── AssetInfoExporter.cs
│   │   ├── ConsoleLogExporter.cs
│   │   └── [その他エクスポーター]
│   ├── Windows/                    # Editor ウィンドウ
│   │   └── MCPServerManagerWindow.cs
│   └── Setup/                      # セットアップ機能
│       ├── MCPAutoSetup.cs
│       └── MCPSetupWizard.cs
├── Runtime/                        # ランタイム機能（将来用）
│   └── UnityMCP.Runtime.asmdef
├── Tests/                          # テストコード
│   ├── Editor/
│   │   ├── UnityMCP.Tests.Editor.asmdef
│   │   ├── MCPCommandProcessorTests.cs
│   │   └── SecurityTests.cs
│   └── Runtime/
│       └── UnityMCP.Tests.Runtime.asmdef
├── Samples~/                       # サンプル（~で除外）
│   └── BasicUsage/
│       ├── README.md
│       ├── BasicExample.cs
│       └── SampleScene.unity
├── MCPServer~/                     # MCPサーバー（~で除外だがGitには含む）
│   ├── package.json               # Node.js依存関係
│   ├── dist/                      # ビルド済みJavaScript
│   │   └── index.js
│   ├── src/                       # TypeScriptソース
│   └── README.md
└── .github/                        # GitHub Actions（開発時のみ）
    └── workflows/
        └── package-release.yml
```

## 📋 UMP package.json 設計

### **メインpackage.json**
```json
{
  "name": "com.orlab.unity-mcp-learning",
  "version": "1.0.0",
  "displayName": "Unity MCP Learning",
  "description": "AI-driven Unity development with Claude Desktop integration via MCP protocol. Enables automatic code generation, real-time error detection, and intelligent development assistance through Claude Code.",
  "unity": "6000.0",
  "unityRelease": "0f1",
  "documentationUrl": "https://github.com/orlab/UnityMCPLearning/blob/main/Documentation~/README.md",
  "changelogUrl": "https://github.com/orlab/UnityMCPLearning/blob/main/CHANGELOG.md",
  "licensesUrl": "https://github.com/orlab/UnityMCPLearning/blob/main/LICENSE",
  "dependencies": {},
  "keywords": [
    "mcp",
    "ai",
    "claude",
    "automation",
    "editor-extension",
    "development-tools",
    "code-generation",
    "ai-assistant"
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
  "samples": [
    {
      "displayName": "Basic Usage Example",
      "description": "Basic setup and usage example with sample scene",
      "path": "Samples~/BasicUsage"
    }
  ],
  "type": "tool"
}
```

## 🔧 ファイル移行計画

### **Phase 1: ディレクトリ構造作成**
```bash
# ルートレベルディレクトリ作成
mkdir -p Editor/Common
mkdir -p Editor/Exporters  
mkdir -p Editor/Windows
mkdir -p Editor/Setup
mkdir -p Runtime
mkdir -p Tests/Editor
mkdir -p Tests/Runtime
mkdir -p Documentation~
mkdir -p Samples~/BasicUsage
mkdir -p MCPServer~
```

### **Phase 2: ファイル移行**
```bash
# Editor スクリプト移行
MCPLearning/Assets/UnityMCP/Editor/Common/ → Editor/Common/
MCPLearning/Assets/UnityMCP/Editor/Exporters/ → Editor/Exporters/
MCPLearning/Assets/UnityMCP/Editor/Windows/ → Editor/Windows/
MCPLearning/Assets/UnityMCP/Editor/*.cs → Editor/

# テストファイル移行
MCPLearning/Assets/UnityMCP/Tests/Editor/ → Tests/Editor/

# Assembly Definition ファイル
MCPLearning/Assets/UnityMCP/Editor/UnityMCP.Editor.asmdef → Editor/
MCPLearning/Assets/UnityMCP/Tests/Editor/UnityMCP.Tests.Editor.asmdef → Tests/Editor/

# MCPサーバー統合
unity-mcp-node/ → MCPServer~/
```

### **Phase 3: 新規ファイル作成**
```bash
# UMP 必須ファイル
package.json                        # UMP メタデータ
README.md                          # パッケージ説明
CHANGELOG.md                       # 変更履歴

# ドキュメント
Documentation~/installation.md     # インストール手順
Documentation~/quick-start.md      # クイックスタート
Documentation~/api-reference.md    # API リファレンス
Documentation~/troubleshooting.md  # トラブルシューティング

# サンプル
Samples~/BasicUsage/README.md      # サンプル説明
Samples~/BasicUsage/BasicExample.cs # サンプルスクリプト

# Runtime Assembly Definition
Runtime/UnityMCP.Runtime.asmdef     # ランタイム用（将来）
Tests/Runtime/UnityMCP.Tests.Runtime.asmdef # ランタイムテスト用
```

## 🛠️ Assembly Definition 設計

### **Editor Assembly Definition**
```json
{
    "name": "UnityMCP.Editor",
    "rootNamespace": "UnityMCP.Editor",
    "references": [],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### **Runtime Assembly Definition**
```json
{
    "name": "UnityMCP.Runtime", 
    "rootNamespace": "UnityMCP.Runtime",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### **Editor Tests Assembly Definition**
```json
{
    "name": "UnityMCP.Tests.Editor",
    "rootNamespace": "UnityMCP.Tests.Editor", 
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "UnityMCP.Editor"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

## 📚 ドキュメント構造設計

### **README.md (パッケージルート)**
```markdown
# Unity MCP Learning

AI-driven Unity development with Claude Desktop integration via MCP protocol.

## ✨ Features
- Automatic code generation and error detection
- Real-time Unity Editor monitoring  
- Claude Code integration
- Multi-project support
- Secure data handling

## 🚀 Quick Start
1. Install via Package Manager: `https://github.com/orlab/UnityMCPLearning.git#ump`
2. Tools > MCP Server Manager
3. Click "Setup MCP Server"

## 📖 Documentation
- [Installation Guide](Documentation~/installation.md)
- [Quick Start](Documentation~/quick-start.md)
- [API Reference](Documentation~/api-reference.md)
- [Troubleshooting](Documentation~/troubleshooting.md)

## 🔧 Requirements
- Unity 6000.0+
- Node.js 18+
- Claude Desktop

## 📄 License
MIT License - see [LICENSE](LICENSE) for details
```

### **Documentation~/installation.md**
```markdown
# Installation Guide

## System Requirements
- Unity 6000.0 or later
- Node.js 18.0 or later
- macOS 11+, Windows 10+, or Ubuntu 20.04+
- Claude Desktop (latest version)

## Installation Steps

### 1. Install via Unity Package Manager
1. Open Unity Package Manager
2. Click "+" → "Add package from git URL"
3. Enter: `https://github.com/orlab/UnityMCPLearning.git#ump`
4. Click "Add"

### 2. Setup MCP Server
1. Open Tools > MCP Server Manager
2. Click "Setup MCP Server"
3. Wait for automatic setup completion

### 3. Verify Installation
1. Check Console for setup completion message
2. Verify server status in MCP Server Manager
3. Test connection with Claude Desktop

## Troubleshooting
See [Troubleshooting Guide](troubleshooting.md) for common issues.
```

## 🧪 サンプル設計

### **Samples~/BasicUsage/BasicExample.cs**
```csharp
using UnityEngine;
using UnityEditor;
using UnityMCP.Editor;

namespace UnityMCP.Samples
{
    /// <summary>
    /// Basic Unity MCP Learning usage example
    /// </summary>
    public class BasicExample : MonoBehaviour
    {
        [Header("MCP Integration")]
        public bool enableMCPLogging = true;
        public bool autoStartServer = true;
        
        void Start()
        {
            if (enableMCPLogging)
            {
                MCPLogger.Log("BasicExample started - MCP integration active");
            }
            
            #if UNITY_EDITOR
            if (autoStartServer && !MCPServerManager.IsRunning)
            {
                MCPServerManager.StartServer();
            }
            #endif
        }
        
        void Update()
        {
            // Example: Monitor for compilation errors
            #if UNITY_EDITOR
            if (EditorApplication.isCompiling)
            {
                MCPLogger.Log("Compilation in progress...");
            }
            #endif
        }
    }
}
```

## 🔄 移行手順実行計画

### **Step 1: バックアップ作成**
```bash
# 現在の状態をバックアップ
git add -A
git commit -m "backup: UMP構造変更前のバックアップ"
git tag backup-before-ump-restructure
```

### **Step 2: 新しいブランチ作成**
```bash
# 構造変更用ブランチ作成
git checkout -b feature/ump-package-structure
```

### **Step 3: ディレクトリ構造作成**
```bash
# 必要なディレクトリを作成
mkdir -p Editor/Common Editor/Exporters Editor/Windows Editor/Setup
mkdir -p Runtime Tests/Editor Tests/Runtime
mkdir -p Documentation~ Samples~/BasicUsage MCPServer~
```

### **Step 4: ファイル移行実行**
```bash
# Editorスクリプト移行
cp -r MCPLearning/Assets/UnityMCP/Editor/Common/* Editor/Common/
cp -r MCPLearning/Assets/UnityMCP/Editor/Exporters/* Editor/Exporters/
cp -r MCPLearning/Assets/UnityMCP/Editor/Windows/* Editor/Windows/
cp MCPLearning/Assets/UnityMCP/Editor/*.cs Editor/

# テスト移行
cp -r MCPLearning/Assets/UnityMCP/Tests/Editor/* Tests/Editor/

# MCPサーバー統合
cp -r unity-mcp-node/* MCPServer~/
```

### **Step 5: メタファイル作成**
```bash
# .meta ファイルを削除（Unity が自動生成）
find . -name "*.meta" -delete

# Assembly Definition ファイル更新
# Editor/UnityMCP.Editor.asmdef 作成
# Tests/Editor/UnityMCP.Tests.Editor.asmdef 作成
# Runtime/UnityMCP.Runtime.asmdef 作成
```

### **Step 6: UMP package.json 作成**
```bash
# ルートにUMP用package.json作成
# CHANGELOG.md, README.md作成
# Documentation~/ 内ドキュメント作成
```

### **Step 7: 動作確認**
```bash
# 新しい Unity プロジェクトで動作テスト
# Package Manager でローカルパッケージとして追加テスト
```

### **Step 8: 不要ファイル整理**
```bash
# 開発用ファイルの除外設定
# .gitignore 更新
# MCPLearning/ ディレクトリの処理検討
```

## 📋 品質チェックリスト

### **構造チェック**
- [ ] package.json が適切に配置されている
- [ ] Editor/ ディレクトリに全エディター機能が含まれている
- [ ] Assembly Definition ファイルが正しく設定されている
- [ ] Tests/ ディレクトリにテストが適切に配置されている
- [ ] Documentation~/ に必要なドキュメントが揃っている

### **機能チェック**
- [ ] MCPサーバー起動・停止が正常動作する
- [ ] データエクスポート機能が動作する
- [ ] エディターウィンドウが正常表示される
- [ ] テストが全て通る
- [ ] サンプルが正常動作する

### **配布チェック**
- [ ] 別のUnityプロジェクトでパッケージインポートが成功する
- [ ] 自動セットアップが正常動作する
- [ ] Node.js依存関係のインストールが成功する
- [ ] Claude Desktop との連携が動作する
- [ ] マルチプロジェクト対応が機能する

## 🚦 成功基準

### **必須条件**
- UMP形式でのパッケージインポートが成功する
- 既存の全機能が新構造で正常動作する
- テストが全て通る
- サンプルプロジェクトが動作する

### **推奨条件**
- パッケージサイズが10MB以下
- インポート時間が30秒以内
- ドキュメントが完備されている
- GitHub Actions での自動化が機能する

---

**作成日**: 2025年6月8日  
**設計方針**: UMP標準準拠・最大互換性・最小サイズ  
**移行戦略**: 段階的移行・動作確認重視・バックアップ保全

この設計により、**Unity Package Manager 標準に完全準拠した配布可能なパッケージ**が実現されます。