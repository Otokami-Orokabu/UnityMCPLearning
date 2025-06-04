# Step 2: Unity → JSON出力 → MCPデータ連携の実装

## 目標
UnityエディターからリアルタイムでJSON形式のデータを出力し、MCPサーバーがそれを監視・読み取る仕組みを構築します。

## 前提条件
- Step 1が完了していること（MCPサーバーとClaude Desktopの通信確立済み）
- UnityプロジェクトMCPLearningが作成済みであること

## 概要

### データフロー
```
Unity Editor Script → JSON出力 → ファイル監視 → MCP読み取り → Claude Desktop
```

### 設計決定事項
- **ログシステム**: Unity Logging package使用（Debug.Logではなく）
- **ファイル分割**: インターフェース設計で各エクスポーターを独立実装
- **変更検知**: データ変更時のみ出力（効率化）
- **出力先**: UnityプロジェクトのAssets外（ビルド影響回避）

### ファイル構成
```
MCPLearning/                        # Unityプロジェクト
├── Assets/
│   └── UnityMCP/
│       ├── Editor/
│       │   ├── MCPDataExporter.cs          # メインクラス・メニュー
│       │   ├── Exporters/
│       │   │   ├── ProjectInfoExporter.cs  # プロジェクト情報
│       │   │   ├── SceneInfoExporter.cs    # シーン情報
│       │   │   ├── GameObjectExporter.cs   # GameObject情報
│       │   │   ├── AssetInfoExporter.cs    # アセット情報
│       │   │   ├── BuildInfoExporter.cs    # ビルド情報
│       │   │   └── EditorStateExporter.cs  # エディター状態
│       │   └── Common/
│       │       ├── IDataExporter.cs        # エクスポーターインターフェース
│       │       ├── IChangeDetector.cs      # 変更検知インターフェース
│       │       ├── MCPDataWriter.cs        # JSON書き込み共通処理
│       │       └── MCPLogger.cs            # Unity Logging設定
│       └── Scripts/
│           └── (将来のRuntime Scripts)
└── UnityMCP/Data/                  # 出力先（Assets外）
    ├── project-info.json           # プロジェクト基本情報
    ├── scene-info.json             # シーン情報
    ├── gameobjects.json            # GameObject一覧
    ├── assets-info.json            # アセット情報
    ├── build-info.json             # ビルド設定
    └── editor-state.json           # エディター状態
```

## 実装手順

### Phase 1: ディレクトリとファイル準備

#### 1-1. Unityプロジェクト側ディレクトリ作成
```bash
cd MCPLearning
mkdir -p Assets/UnityMCP/Editor
mkdir -p Assets/UnityMCP/Scripts
mkdir -p UnityMCP/Data
```

#### 1-2. .gitignoreに出力ディレクトリを追加
MCPLearning/.gitignoreに以下を追加：
```gitignore
# MCP Data Output
/UnityMCP/Data/*.json

# Unity Logging
/Logs/
```

### Phase 2: Unity Logging Package追加

#### 2-1. Package導入
`Packages/manifest.json`にUnity Loggingを追加：
```json
{
  "dependencies": {
    "com.unity.logging": "1.0.11"
  }
}
```

### Phase 3: インターフェース設計

#### 3-1. 基本インターフェース作成
`Assets/UnityMCP/Editor/Common/IDataExporter.cs`：
```csharp
public interface IDataExporter
{
    string FileName { get; }
    void Export();
}
```

`Assets/UnityMCP/Editor/Common/IChangeDetector.cs`：
```csharp
public interface IChangeDetector
{
    bool HasChanged();
    void MarkAsUpdated();
}
```

#### 3-2. Unity Logging設定
`Assets/UnityMCP/Editor/Common/MCPLogger.cs`：
```csharp
using Unity.Logging;

namespace UnityMCP.Editor
{
    public static class MCPLogger
    {
        private static Logger _logger;
        
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            _logger = new LoggerConfig()
                .MinimumLevel.Debug()
                .WriteTo.File("Logs/mcp-export.log")
                .CreateLogger();
        }
        
        public static void LogExportStart(string fileName)
        {
            _logger.LogInfo("Export started for {FileName}", fileName);
        }
        
        public static void LogExportSuccess(string fileName, long fileSize, double duration)
        {
            _logger.LogInfo("Export completed for {FileName} - Size: {FileSize}B, Duration: {Duration}ms", 
                fileName, fileSize, duration);
        }
        
        public static void LogExportSkipped(string fileName, string reason)
        {
            _logger.LogDebug("Export skipped for {FileName} - Reason: {Reason}", fileName, reason);
        }
        
        public static void LogError(string fileName, string error)
        {
            _logger.LogError("Export failed for {FileName} - Error: {Error}", fileName, error);
        }
    }
}
```

#### 3-3. 共通データ書き込みクラス
`Assets/UnityMCP/Editor/Common/MCPDataWriter.cs`：
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityMCP.Editor
{
    public static class MCPDataWriter
    {
        private static readonly string DataPath = Path.Combine(Application.dataPath, "../UnityMCP/Data");
        
        public static void WriteJsonFile(string fileName, Dictionary<string, object> data)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            MCPLogger.LogExportStart(fileName);
            
            try
            {
                EnsureDataDirectory();
                
                var json = JsonUtility.ToJson(new SerializableDict(data), true);
                var filePath = Path.Combine(DataPath, fileName);
                
                File.WriteAllText(filePath, json);
                stopwatch.Stop();
                
                var fileInfo = new FileInfo(filePath);
                MCPLogger.LogExportSuccess(fileName, fileInfo.Length, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                MCPLogger.LogError(fileName, ex.Message);
                throw;
            }
        }
        
        private static void EnsureDataDirectory()
        {
            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }
        }
    }
    
    [System.Serializable]
    public class SerializableDict
    {
        public Dictionary<string, object> data;
        
        public SerializableDict(Dictionary<string, object> data)
        {
            this.data = data;
        }
    }
}
```

### Phase 4: エクスポーター実装

#### 4-1. プロジェクト情報エクスポーター
`Assets/UnityMCP/Editor/Exporters/ProjectInfoExporter.cs`：
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace UnityMCP.Editor
{
    public class ProjectInfoExporter : IDataExporter, IChangeDetector
    {
        public string FileName => "project-info.json";
        
        private static Dictionary<string, object> _lastData;
        
        public void Export()
        {
            if (!HasChanged())
            {
                MCPLogger.LogExportSkipped(FileName, "No changes detected");
                return;
            }
            
            var data = GatherData();
            MCPDataWriter.WriteJsonFile(FileName, data);
            MarkAsUpdated();
        }
        
        public bool HasChanged()
        {
            var currentData = GatherData();
            return !DataEquals(_lastData, currentData);
        }
        
        public void MarkAsUpdated()
        {
            _lastData = GatherData();
        }
        
        private Dictionary<string, object> GatherData()
        {
            return new Dictionary<string, object>
            {
                ["projectName"] = Application.productName,
                ["unityVersion"] = Application.unityVersion,
                ["platform"] = Application.platform.ToString(),
                ["lastUpdated"] = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }
        
        private bool DataEquals(Dictionary<string, object> a, Dictionary<string, object> b)
        {
            if (a == null || b == null) return a == b;
            if (a.Count != b.Count) return false;
            
            foreach (var kvp in a)
            {
                if (!b.ContainsKey(kvp.Key) || !Equals(b[kvp.Key], kvp.Value))
                    return false;
            }
            return true;
        }
    }
}
```

### Phase 5: メインエクスポーター実装

#### 5-1. MCPDataExporter.csの作成
`Assets/UnityMCP/Editor/MCPDataExporter.cs`を作成：

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP.Editor
{
    public static class MCPDataExporter
    {
        private static readonly List<IDataExporter> _exporters = new()
        {
            new ProjectInfoExporter(),
            new SceneInfoExporter(),
            new GameObjectExporter(),
            new AssetInfoExporter(),
            new BuildInfoExporter(),
            new EditorStateExporter()
        };
        
        [MenuItem("UnityMCP/Export All Data")]
        public static void ExportAllData()
        {
            ExportChangedData();
        }
        
        [MenuItem("UnityMCP/Force Export All Data")]
        public static void ForceExportAllData()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            foreach (var exporter in _exporters)
            {
                exporter.Export();
                if (exporter is IChangeDetector detector)
                    detector.MarkAsUpdated();
            }
            
            stopwatch.Stop();
            MCPLogger.LogPerformanceMetrics(_exporters.Count, _exporters.Count, stopwatch.Elapsed.TotalMilliseconds);
        }
        
        public static void ExportChangedData()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var changedExporters = new List<IDataExporter>();
            
            foreach (var exporter in _exporters)
            {
                if (exporter is IChangeDetector detector && detector.HasChanged())
                {
                    changedExporters.Add(exporter);
                    exporter.Export();
                    detector.MarkAsUpdated();
                }
                else if (exporter is not IChangeDetector)
                {
                    // 変更検知機能がない場合は常に実行
                    changedExporters.Add(exporter);
                    exporter.Export();
                }
            }
            
            stopwatch.Stop();
            
            if (changedExporters.Count > 0)
            {
                MCPLogger.LogPerformanceMetrics(_exporters.Count, changedExporters.Count, stopwatch.Elapsed.TotalMilliseconds);
            }
        }
        
        // 自動更新機能
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }
        
        private static void OnHierarchyChanged()
        {
            // シーンとGameObject関連のみ更新
            ExportSpecificData(typeof(SceneInfoExporter), typeof(GameObjectExporter), typeof(EditorStateExporter));
        }
        
        private static void OnProjectChanged()
        {
            // アセット関連のみ更新
            ExportSpecificData(typeof(AssetInfoExporter));
        }
        
        private static void OnSelectionChanged()
        {
            // エディター状態のみ更新
            ExportSpecificData(typeof(EditorStateExporter));
        }
        
        private static void ExportSpecificData(params System.Type[] exporterTypes)
        {
            var targetExporters = _exporters.Where(e => exporterTypes.Contains(e.GetType())).ToList();
            
            foreach (var exporter in targetExporters)
            {
                if (exporter is IChangeDetector detector && detector.HasChanged())
                {
                    exporter.Export();
                    detector.MarkAsUpdated();
                }
            }
        }
        
        private static void ExportSceneInfo()
        {
            var activeScene = SceneManager.GetActiveScene();
            var data = new Dictionary<string, object>
            {
                ["currentScene"] = activeScene.name,
                ["scenePath"] = activeScene.path,
                ["isPlaying"] = Application.isPlaying,
                ["sceneObjectCount"] = activeScene.rootCount,
                ["isDirty"] = activeScene.isDirty
            };
            
            WriteJsonFile("scene-info.json", data);
        }
        
        private static void ExportGameObjects()
        {
            var objects = new List<Dictionary<string, object>>();
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            
            foreach (var obj in rootObjects)
            {
                objects.Add(new Dictionary<string, object>
                {
                    ["name"] = obj.name,
                    ["position"] = new Dictionary<string, float>
                    {
                        ["x"] = obj.transform.position.x,
                        ["y"] = obj.transform.position.y,
                        ["z"] = obj.transform.position.z
                    },
                    ["active"] = obj.activeInHierarchy,
                    ["tag"] = obj.tag,
                    ["layer"] = obj.layer,
                    ["componentCount"] = obj.GetComponents<Component>().Length
                });
            }
            
            var data = new Dictionary<string, object>
            {
                ["totalCount"] = objects.Count,
                ["objects"] = objects
            };
            
            WriteJsonFile("gameobjects.json", data);
        }
        
        private static void ExportAssetsInfo()
        {
            var data = new Dictionary<string, object>
            {
                ["assetDatabaseRefresh"] = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["totalAssets"] = "計算中", // 実装時に詳細化
                ["recentlyModified"] = new List<string>() // 実装時に詳細化
            };
            
            WriteJsonFile("assets-info.json", data);
        }
        
        private static void ExportBuildInfo()
        {
            var data = new Dictionary<string, object>
            {
                ["buildTarget"] = EditorUserBuildSettings.activeBuildTarget.ToString(),
                ["developmentBuild"] = EditorUserBuildSettings.development,
                ["scriptingBackend"] = PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup).ToString()
            };
            
            WriteJsonFile("build-info.json", data);
        }
        
        private static void ExportEditorState()
        {
            var data = new Dictionary<string, object>
            {
                ["editorFocused"] = UnityEditorInternal.InternalEditorUtility.isApplicationActive,
                ["compilingAssemblies"] = EditorApplication.isCompiling,
                ["playModeState"] = EditorApplication.isPlaying ? "Playing" : "Stopped",
                ["selectedObjectsCount"] = Selection.objects.Length
            };
            
            WriteJsonFile("editor-state.json", data);
        }
        
        private static void WriteJsonFile(string filename, Dictionary<string, object> data)
        {
            var json = EditorJsonUtility.ToJson(new SerializableDict(data), true);
            var filePath = Path.Combine(DataPath, filename);
            File.WriteAllText(filePath, json);
        }
    }
    
    [System.Serializable]
    public class SerializableDict
    {
        public Dictionary<string, object> data;
        
        public SerializableDict(Dictionary<string, object> data)
        {
            this.data = data;
        }
    }
}
```

#### 2-2. 自動更新機能の追加
EditorApplicationのイベントを使用して自動更新：

```csharp
[InitializeOnLoadMethod]
private static void Initialize()
{
    EditorApplication.hierarchyChanged += OnHierarchyChanged;
    EditorApplication.projectChanged += OnProjectChanged;
    Selection.selectionChanged += OnSelectionChanged;
}

private static void OnHierarchyChanged()
{
    ExportSceneInfo();
    ExportGameObjects();
    ExportEditorState();
}

private static void OnProjectChanged()
{
    ExportAssetsInfo();
}

private static void OnSelectionChanged()
{
    ExportEditorState();
}
```

### Phase 6: MCPサーバー側ファイル監視実装

#### 6-1. unity-mcp-node/src/index.tsの拡張

```typescript
import * as fs from 'fs';
import * as path from 'path';

// ファイル監視の追加
const dataPath = './MCPLearning/UnityMCP/Data';
let cachedData: { [key: string]: any } = {};

// データ監視開始
function startFileWatching() {
  if (fs.existsSync(dataPath)) {
    console.log(`Watching Unity data directory: ${dataPath}`);
    
    fs.watch(dataPath, { recursive: true }, (eventType, filename) => {
      if (filename && filename.endsWith('.json')) {
        console.log(`Unity data file changed: ${filename}`);
        loadDataFile(filename);
      }
    });
    
    // 初期データ読み込み
    loadAllData();
  }
}

// データファイル読み込み
function loadDataFile(filename: string) {
  try {
    const filePath = path.join(dataPath, filename);
    if (fs.existsSync(filePath)) {
      const content = fs.readFileSync(filePath, 'utf-8');
      const data = JSON.parse(content);
      cachedData[filename.replace('.json', '')] = data;
      console.log(`Loaded ${filename}: ${Object.keys(data).length} properties`);
    }
  } catch (error) {
    console.error(`Error loading ${filename}:`, error);
  }
}

// 全データ読み込み
function loadAllData() {
  const files = ['project-info.json', 'scene-info.json', 'gameobjects.json', 
                 'assets-info.json', 'build-info.json', 'editor-state.json'];
  
  files.forEach(loadDataFile);
}

// 新しいツール: unity_info_realtime
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return {
    tools: [
      // 既存のツール...
      {
        name: "unity_info_realtime",
        description: "Unityプロジェクトのリアルタイム情報を取得",
        inputSchema: {
          type: "object",
          properties: {
            category: {
              type: "string",
              enum: ["project", "scene", "gameobjects", "assets", "build", "editor", "all"],
              description: "取得する情報のカテゴリ"
            }
          }
        }
      }
    ]
  };
});

// unity_info_realtimeツールの実装
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  if (request.params.name === "unity_info_realtime") {
    const category = request.params.arguments?.category || "all";
    
    if (category === "all") {
      return {
        content: [{
          type: "text",
          text: `Unity リアルタイム情報:\n${JSON.stringify(cachedData, null, 2)}`
        }]
      };
    } else {
      const data = cachedData[category.replace('-', '_')];
      return {
        content: [{
          type: "text",
          text: data ? JSON.stringify(data, null, 2) : `${category} データが見つかりません`
        }]
      };
    }
  }
  
  // 既存のツール処理...
});

// サーバー起動時にファイル監視開始
startFileWatching();
```

### Phase 7: テストと動作確認

#### 7-1. Unity側テスト
1. Unityエディターでプロジェクトを開く
2. メニューから `UnityMCP > Export All Data` を実行
3. `MCPLearning/UnityMCP/Data/` に6つのJSONファイルが生成されることを確認

#### 7-2. MCPサーバーテスト
```bash
cd unity-mcp-node
npm run dev
```
ログで「Watching Unity data directory」と「Loaded *.json」が表示されることを確認

#### 7-3. Claude Desktop統合テスト
Claude Desktopで以下をテスト：
```
unity_info_realtime を実行してUnityの最新情報を取得してください
```

## 設計上のメリット

### 1. インターフェース設計
- **柔軟性**: 各エクスポーターが独立して実装可能
- **テスト容易性**: モック作成が簡単
- **拡張性**: 新しいデータ種別の追加が容易

### 2. 変更検知による効率化
- **パフォーマンス**: 変更されたデータのみ出力
- **ファイルI/O削減**: 不要な書き込み処理を回避
- **ログ最適化**: 実際の変更のみログ出力

### 3. Unity Logging使用
- **構造化ログ**: パラメーター付きログで分析容易
- **パフォーマンス**: Debug.Logより高速
- **ノイズレス**: エディターコンソールに出力しない

## トラブルシューティング

### よくある問題
1. **JSONファイルが生成されない**
   - Unity Logging packageのインストール確認
   - UnityMCP/Dataディレクトリの存在確認
   - ログファイル（Logs/mcp-export.log）でエラー確認

2. **MCPサーバーがファイル変更を検知しない**
   - ファイルパスの確認
   - ファイル監視ログの確認
   - fs.watch対象ディレクトリの確認

3. **Claude Desktopでデータが取得できない**
   - MCPサーバーのログ確認
   - cachedDataの内容確認
   - ファイル読み取り権限の確認

4. **ログが出力されない**
   - Unity Logging packageのバージョン確認
   - LoggerConfigの設定確認
   - ログファイルの書き込み権限確認

## 次のステップ
Step 3では、Claude Desktop → MCP → Unityの逆方向通信（Cube生成など）を実装します。

## 参考資料
- [Unity EditorApplication API](https://docs.unity3d.com/ScriptReference/EditorApplication.html)
- [Node.js fs.watch API](https://nodejs.org/api/fs.html#fswatchfilename-options-listener)
- [MCP Protocol Specification](https://spec.modelcontextprotocol.io/specification/)