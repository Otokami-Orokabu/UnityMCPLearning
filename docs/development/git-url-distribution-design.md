# Git URL配布設計とマルチプロジェクト対応

## 概要

Unity Package Manager (UPM) のGit URL機能を使用した配布方法と、複数のUnityエディタ同時起動に対応するための設計ドキュメント。

## 背景と課題

### 現状の問題点

1. **パス依存性**: 現在の実装は`UnityMCPLearning`プロジェクト構造に依存
2. **単一インスタンス前提**: MCPサーバーは1つのUnityプロジェクトのみ対応
3. **データ競合**: 複数のUnityエディタが同じデータディレクトリを使用すると競合発生
4. **ポート競合**: 固定ポート（3000）使用により複数サーバー起動不可

### Git URL配布時の課題

- パッケージは`Library/PackageCache/`に配置される
- 相対パスでのMCPサーバー参照が不可能
- ユーザープロジェクトごとの設定管理が必要

## 設計方針

### 1. Unity 6以降限定

- Unity 6000.0以降のみサポート
- 最新機能の活用とシンプルな実装
- フォールバック処理不要

### 2. 自己完結型パッケージ

- MCPサーバー（ビルド済み）を同梱
- 外部依存を最小限に
- ワンクリックセットアップ

### 3. マルチプロジェクト対応

- プロジェクトごとの独立したデータ管理
- 動的ポート割り当て
- 競合回避機構

## 実装設計

### パッケージ構造

```
com.orlab.unity-mcp-learning/
├── package.json                 # UPM用メタデータ
├── package.json.meta
├── README.md
├── README.md.meta
├── CHANGELOG.md
├── CHANGELOG.md.meta
├── LICENSE
├── LICENSE.meta
├── Editor/                      # Unity Editor統合
│   ├── UnityMCP.Editor.asmdef
│   ├── Common/
│   │   ├── MCPPackageResolver.cs    # パッケージパス解決
│   │   ├── MCPProjectIdentifier.cs  # プロジェクトID生成
│   │   └── MCPPortManager.cs        # ポート管理
│   ├── Windows/
│   │   └── MCPServerManagerWindow.cs
│   └── Settings/
│       └── MCPSettingsProvider.cs
├── Runtime/                     # ランタイム機能（将来用）
│   └── UnityMCP.Runtime.asmdef
├── MCPServer~/                  # MCPサーバー（~で除外されるがGitには含む）
│   ├── dist/
│   │   └── server.js           # ビルド済み単一ファイル
│   ├── package.json            # 最小限の依存関係
│   └── README.md               # サーバー仕様書
├── Documentation~/              # ドキュメント
│   ├── GettingStarted.md
│   └── API.md
└── Samples~/                    # サンプル
    └── BasicUsage/
```

### package.json (UPM用)

```json
{
  "name": "com.orlab.unity-mcp-learning",
  "version": "1.0.0",
  "displayName": "Unity MCP Learning",
  "description": "AI-driven Unity development with Claude Desktop integration",
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
    "editor-extension"
  ],
  "author": {
    "name": "orlab",
    "email": "contact@orlab.dev",
    "url": "https://github.com/orlab"
  },
  "repository": {
    "type": "git",
    "url": "https://github.com/orlab/UnityMCPLearning.git"
  }
}
```

### プロジェクト識別とパス解決

```csharp
// Editor/Common/MCPPackageResolver.cs
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using System.IO;

namespace UnityMCP.Editor
{
    public static class MCPPackageResolver
    {
        private const string PACKAGE_NAME = "com.orlab.unity-mcp-learning";
        
        /// <summary>
        /// インストールされたパッケージのパスを取得
        /// </summary>
        public static string GetPackagePath()
        {
            var packageInfo = PackageInfo.FindForAssembly(typeof(MCPPackageResolver).Assembly);
            return packageInfo?.resolvedPath ?? string.Empty;
        }
        
        /// <summary>
        /// MCPサーバーのパスを取得
        /// </summary>
        public static string GetMCPServerPath()
        {
            return Path.Combine(GetPackagePath(), "MCPServer~");
        }
        
        /// <summary>
        /// ユーザープロジェクトのデータディレクトリパスを取得
        /// </summary>
        public static string GetUserDataPath()
        {
            // プロジェクトルートの UnityMCPData ディレクトリ
            return Path.Combine(Application.dataPath, "..", "UnityMCPData");
        }
    }
}
```

```csharp
// Editor/Common/MCPProjectIdentifier.cs
using UnityEngine;
using System.Security.Cryptography;
using System.Text;

namespace UnityMCP.Editor
{
    public static class MCPProjectIdentifier
    {
        private static string _projectId;
        
        /// <summary>
        /// プロジェクト固有のIDを生成（プロジェクトパスのハッシュ値）
        /// </summary>
        public static string GetProjectId()
        {
            if (string.IsNullOrEmpty(_projectId))
            {
                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
                    _projectId = System.BitConverter.ToString(hash)
                        .Replace("-", "")
                        .Substring(0, 8)
                        .ToLower();
                }
            }
            return _projectId;
        }
        
        /// <summary>
        /// プロジェクト固有のデータパスを取得
        /// </summary>
        public static string GetProjectDataPath()
        {
            var basePath = MCPPackageResolver.GetUserDataPath();
            return Path.Combine(basePath, $"project_{GetProjectId()}", "Data");
        }
    }
}
```

### 動的ポート管理

```csharp
// Editor/Common/MCPPortManager.cs
using System.Net;
using System.Net.Sockets;

namespace UnityMCP.Editor
{
    public static class MCPPortManager
    {
        private const int BASE_PORT = 3000;
        private const int MAX_PORT = 3100;
        
        /// <summary>
        /// 利用可能なポートを検索
        /// </summary>
        public static int FindAvailablePort()
        {
            for (int port = BASE_PORT; port <= MAX_PORT; port++)
            {
                if (IsPortAvailable(port))
                {
                    return port;
                }
            }
            throw new System.Exception($"No available port found between {BASE_PORT} and {MAX_PORT}");
        }
        
        private static bool IsPortAvailable(int port)
        {
            try
            {
                using (var listener = new TcpListener(IPAddress.Any, port))
                {
                    listener.Start();
                    listener.Stop();
                    return true;
                }
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
```

### MCPサーバー起動処理

```csharp
// Editor/MCPServerLauncher.cs
using UnityEngine;
using System.Diagnostics;
using System.IO;

namespace UnityMCP.Editor
{
    public static class MCPServerLauncher
    {
        public static Process StartServer()
        {
            // パスの解決
            var serverPath = MCPPackageResolver.GetMCPServerPath();
            var dataPath = MCPProjectIdentifier.GetProjectDataPath();
            var port = MCPPortManager.FindAvailablePort();
            
            // データディレクトリの作成
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(Path.Combine(dataPath, "..", "Commands"));
            Directory.CreateDirectory(Path.Combine(dataPath, "..", "Logs"));
            
            // 環境変数の設定
            var startInfo = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = $"\"{Path.Combine(serverPath, "dist", "server.js")}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            // 環境変数でプロジェクト情報を渡す
            startInfo.EnvironmentVariables["MCP_DATA_PATH"] = dataPath;
            startInfo.EnvironmentVariables["MCP_PROJECT_ID"] = MCPProjectIdentifier.GetProjectId();
            startInfo.EnvironmentVariables["MCP_PORT"] = port.ToString();
            
            // 設定を保存
            SaveServerConfig(port, dataPath);
            
            return Process.Start(startInfo);
        }
        
        private static void SaveServerConfig(int port, string dataPath)
        {
            var config = new ServerConfig
            {
                projectId = MCPProjectIdentifier.GetProjectId(),
                port = port,
                dataPath = dataPath,
                lastStarted = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            var settingsPath = Path.Combine(Application.dataPath, "..", "UserSettings", "MCPServerConfig.json");
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            File.WriteAllText(settingsPath, JsonUtility.ToJson(config, true));
        }
        
        [System.Serializable]
        private class ServerConfig
        {
            public string projectId;
            public int port;
            public string dataPath;
            public string lastStarted;
        }
    }
}
```

### MCPサーバー側の対応

```typescript
// MCPServer~/src/server.ts
import * as path from 'path';

// 環境変数から設定を取得
const config = {
    dataPath: process.env.MCP_DATA_PATH || path.join(__dirname, '..', 'data'),
    projectId: process.env.MCP_PROJECT_ID || 'default',
    port: parseInt(process.env.MCP_PORT || '3000', 10)
};

// プロジェクト固有のパスを構築
const paths = {
    data: config.dataPath,
    commands: path.join(path.dirname(config.dataPath), 'Commands'),
    logs: path.join(path.dirname(config.dataPath), 'Logs')
};

console.log(`Starting MCP Server for project ${config.projectId} on port ${config.port}`);
console.log(`Data path: ${paths.data}`);

// サーバー起動処理...
```

## インストール後のディレクトリ構造

```
YourUnityProject/
├── Assets/
│   └── (ユーザーのアセット)
├── Library/
│   └── PackageCache/
│       └── com.orlab.unity-mcp-learning@1.0.0/
│           ├── Editor/
│           ├── Runtime/
│           └── MCPServer~/
├── ProjectSettings/
│   └── (プロジェクト設定)
├── UserSettings/
│   └── MCPServerConfig.json    # サーバー設定（Git無視）
└── UnityMCPData/               # MCPデータディレクトリ
    └── project_a1b2c3d4/       # プロジェクトID別
        ├── Data/
        │   ├── assets-info.json
        │   ├── compile-status.json
        │   ├── console-logs.json
        │   └── gameobjects.json
        ├── Commands/
        │   └── (コマンドファイル)
        └── Logs/
            └── (ログファイル)
```

## 利点

1. **完全な独立性**: 各プロジェクトが独自のデータ領域を持つ
2. **競合回避**: ポートとファイルの競合を自動的に回避
3. **簡単なインストール**: Unity Package ManagerでGit URLを指定するだけ
4. **透過的な動作**: ユーザーは複数プロジェクトを意識する必要なし

## セキュリティ考慮事項

1. **プロジェクトID**: ハッシュ値使用によりプロジェクトパスを隠蔽
2. **ポート範囲制限**: 3000-3100の範囲内でのみポート使用
3. **ローカル接続のみ**: localhost以外からの接続を拒否

## 今後の拡張

1. **プロジェクト間通信**: 将来的に複数プロジェクト間でのデータ共有
2. **中央管理UI**: すべての起動中のMCPサーバーを管理するツール
3. **クラウド同期**: プロジェクト設定のクラウドバックアップ

---

作成日: 2025年6月8日  
更新日: 2025年6月8日