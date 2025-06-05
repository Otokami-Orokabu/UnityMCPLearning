# Step 2: Unity連携システムの実装

## 🎯 このステップの目標
- Unity → JSON → MCP → Claude Desktopのリアルタイムデータ流れを確立
- 6種類のUnityデータ自動エクスポートシステム構築
- 変更検知による自動更新機能の実装

## 📋 このステップで学ぶこと
- Unity C#スクリプトによるエディター拡張
- ファイル監視システムの実装
- リアルタイムデータ連携の仕組み

## 🏗 システム構成図

```
Unity Editor → C#エクスポーター → JSON出力 → ファイル監視 → MCPサーバー → Claude Desktop
     ↑                                                    ↑
  変更検知システム                                  リアルタイム取得
```

## 🔧 Unity側の実装

### 1. フォルダ構造の作成

```
Assets/UnityMCP/
├── Editor/
│   ├── Common/              # 共通機能
│   │   ├── IDataExporter.cs        # エクスポーターインターフェース
│   │   ├── IChangeDetector.cs      # 変更検知インターフェース
│   │   ├── MCPDataWriter.cs        # JSONファイル書き込み
│   │   └── MCPLogger.cs            # ログ機能
│   ├── Exporters/           # データエクスポーター
│   │   ├── ProjectInfoExporter.cs  # プロジェクト情報
│   │   ├── SceneInfoExporter.cs    # シーン情報
│   │   ├── GameObjectExporter.cs   # GameObject情報
│   │   ├── AssetInfoExporter.cs    # アセット情報
│   │   ├── BuildInfoExporter.cs    # ビルド設定
│   │   └── EditorStateExporter.cs  # エディター状態
│   └── MCPDataExporter.cs   # メインエクスポーター
└── Scripts/                 # ランタイムスクリプト（将来用）
```

### 2. 基本インターフェースの実装

#### IDataExporter.cs
```csharp
using System.Collections.Generic;

namespace UnityMCP.Editor
{
    public interface IDataExporter
    {
        string GetDataKey();
        Dictionary<string, object> ExportData();
        bool HasChanged();
    }
}
```

#### IChangeDetector.cs
```csharp
namespace UnityMCP.Editor
{
    public interface IChangeDetector
    {
        bool DetectChanges();
        void ResetChangeFlag();
    }
}
```

### 3. データエクスポーターの実装例

#### ProjectInfoExporter.cs
```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UnityMCP.Editor
{
    public class ProjectInfoExporter : IDataExporter, IChangeDetector
    {
        private bool _hasChanged = true;

        public string GetDataKey() => \"project_info\";

        public Dictionary<string, object> ExportData()
        {
            return new Dictionary<string, object>
            {
                [\"projectName\"] = Application.productName,
                [\"unityVersion\"] = Application.unityVersion,
                [\"platform\"] = Application.platform.ToString(),
                [\"companyName\"] = Application.companyName,
                [\"dataPath\"] = Application.dataPath,
                [\"persistentDataPath\"] = Application.persistentDataPath,
                [\"timestamp\"] = System.DateTime.UtcNow.ToString(\"yyyy-MM-ddTHH:mm:ssZ\")
            };
        }

        public bool HasChanged() => _hasChanged;
        public bool DetectChanges() => _hasChanged;
        public void ResetChangeFlag() => _hasChanged = false;
    }
}
```

#### GameObjectExporter.cs（変更検知付き）
```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP.Editor
{
    public class GameObjectExporter : IDataExporter, IChangeDetector
    {
        private bool _hasChanged = true;
        private int _lastObjectCount = -1;

        public string GetDataKey() => \"gameobjects\";

        public Dictionary<string, object> ExportData()
        {
            var rootObjects = UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().GetRootGameObjects();
            
            var objectsList = rootObjects.Select(obj => 
                $\"{obj.name}({obj.transform.position.x},{obj.transform.position.y},{obj.transform.position.z})\");

            return new Dictionary<string, object>
            {
                [\"totalCount\"] = rootObjects.Length.ToString(),
                [\"activeCount\"] = rootObjects.Count(obj => obj.activeInHierarchy).ToString(),
                [\"objectsList\"] = string.Join(\", \", objectsList),
                [\"sceneObjectNames\"] = string.Join(\"|\", rootObjects.Select(obj => obj.name)),
                [\"timestamp\"] = System.DateTime.UtcNow.ToString(\"yyyy-MM-ddTHH:mm:ssZ\")
            };
        }

        public bool HasChanged() => _hasChanged;

        public bool DetectChanges()
        {
            var currentCount = UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().GetRootGameObjects().Length;
            
            if (currentCount != _lastObjectCount)
            {
                _lastObjectCount = currentCount;
                _hasChanged = true;
                return true;
            }
            return false;
        }

        public void ResetChangeFlag() => _hasChanged = false;
    }
}
```

### 4. メインエクスポータークラス

#### MCPDataExporter.cs
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

        private static bool _autoExportEnabled = false;

        [InitializeOnLoadMethod]
        private static void InitializeAutoExport()
        {
            EnableAutoExport();
        }

        [MenuItem(\"UnityMCP/Export All Data\")]
        public static void ExportAllData()
        {
            foreach (var exporter in _exporters)
            {
                ExportData(exporter);
            }
            MCPLogger.Log($\"[MCP] {_exporters.Count}データのエクスポートが完了しました\");
        }

        [MenuItem(\"UnityMCP/Toggle Auto Export\")]
        public static void ToggleAutoExport()
        {
            if (_autoExportEnabled)
                DisableAutoExport();
            else
                EnableAutoExport();
        }

        public static void EnableAutoExport()
        {
            if (_autoExportEnabled) return;

            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            Selection.selectionChanged += OnSelectionChanged;
            
            _autoExportEnabled = true;
            MCPLogger.Log(\"[MCP] 自動エクスポートを有効にしました\");
        }

        private static void OnHierarchyChanged()
        {
            CheckAndExportChanges();
        }

        private static void CheckAndExportChanges()
        {
            foreach (var exporter in _exporters.OfType<IChangeDetector>())
            {
                if (exporter.DetectChanges())
                {
                    ExportData((IDataExporter)exporter);
                    exporter.ResetChangeFlag();
                }
            }
        }

        private static void ExportData(IDataExporter exporter)
        {
            try
            {
                var data = exporter.ExportData();
                var fileName = $\"{exporter.GetDataKey().Replace(\"_\", \"-\")}.json\";
                MCPDataWriter.WriteData(fileName, data);
            }
            catch (System.Exception ex)
            {
                MCPLogger.LogError($\"エクスポート失敗 {exporter.GetDataKey()}: {ex.Message}\");
            }
        }
    }
}
```

## 🔧 MCP Server側の実装

### 1. ファイル監視システム

#### データパス設定の改良
```typescript
// 動的パス解決
const getUnityDataPath = () => {
  // 環境変数から取得
  if (process.env.UNITY_MCP_DATA_PATH) {
    return path.resolve(process.env.UNITY_MCP_DATA_PATH);
  }
  
  // 設定ファイルから取得
  try {
    const configPath = path.join(__dirname, '..', 'mcp-config.json');
    if (fs.existsSync(configPath)) {
      const config = JSON.parse(fs.readFileSync(configPath, 'utf-8'));
      if (config.unityDataPath) {
        const configDir = path.dirname(configPath);
        return path.resolve(configDir, '..', config.unityDataPath);
      }
    }
  } catch (error) {
    log('Config file read error:', error);
  }
  
  // フォールバック
  return path.resolve(process.cwd(), 'MCPLearning/UnityMCP/Data');
};
```

#### ファイル監視の実装
```typescript
function startFileWatching() {
  const fullPath = path.resolve(dataPath);
  if (fs.existsSync(fullPath)) {
    log(`Watching Unity data directory: ${fullPath}`);
    
    fs.watch(fullPath, { recursive: false }, (eventType, filename) => {
      if (filename && filename.endsWith('.json')) {
        log(`Unity data file changed: ${filename}`);
        loadDataFile(filename);
      }
    });
    
    // 初期データ読み込み
    loadAllData();
  } else {
    log(`Unity data directory not found: ${fullPath}`);
  }
}
```

### 2. unity_info_realtimeツールの実装

```typescript
case 'unity_info_realtime':
  const category = params?.arguments?.category || 'all';
  
  // データの存在確認
  const hasData = Object.keys(cachedData).length > 0;
  
  if (!hasData) {
    // 強制的にデータ再読み込みを試行
    loadAllData();
    const hasDataAfterReload = Object.keys(cachedData).length > 0;
    
    if (!hasDataAfterReload) {
      return {
        content: [{
          type: 'text',
          text: 'Unity project data is not available. Please ensure Unity editor has been opened and MCP export scripts are running.'
        }],
        isError: false
      };
    }
  }

  if (category === 'all') {
    return {
      content: [{
        type: 'text',
        text: `# Unity Project Information (MCPLearning)\\n\\n${JSON.stringify(cachedData, null, 2)}`
      }],
      isError: false
    };
  } else {
    // カテゴリ別データ取得
    const categoryMap = {
      'project': 'project_info',
      'scene': 'scene_info',
      'gameobjects': 'gameobjects',
      'assets': 'assets_info',
      'build': 'build_info',
      'editor': 'editor_state'
    };
    const dataKey = categoryMap[category];
    const data = cachedData[dataKey];
    
    return {
      content: [{
        type: 'text',
        text: `# Unity ${category.charAt(0).toUpperCase() + category.slice(1)} Information\\n\\n${JSON.stringify(data, null, 2)}`
      }],
      isError: false
    };
  }
```

## 🧪 動作テスト手順

### Phase 1: Unity側データエクスポート確認

#### 1. 初回エクスポート
1. Unityエディターで`MCPLearning`プロジェクトを開く
2. メニューから`UnityMCP > Export All Data`を実行
3. コンソールに「[MCP] 6データのエクスポートが完了しました」と表示されることを確認

#### 2. データフォルダ確認
1. `UnityMCP > Open Data Folder`を実行
2. 以下の6つのJSONファイルが生成されていることを確認：
   - `project-info.json`
   - `scene-info.json`
   - `gameobjects.json`
   - `assets-info.json`
   - `build-info.json`
   - `editor-state.json`

#### 3. 自動エクスポート確認
1. UnityでGameObjectを追加
2. シーンが自動的に再エクスポートされることを確認
3. `gameobjects.json`の内容が更新されることを確認

### Phase 2: MCPサーバー側ファイル監視確認

#### 1. サーバー起動とログ確認
```bash
cd unity-mcp-node
npm run build
node dist/index.js
```

**期待されるログ**:
```
[MCP Server] Starting MCP Server...
[MCP Server] Watching Unity data directory: /path/to/MCPLearning/UnityMCP/Data
[MCP Server] Loaded project-info.json: 7 properties
[MCP Server] Loaded scene-info.json: 8 properties
[MCP Server] Loaded gameobjects.json: 5 properties
[MCP Server] Loaded assets-info.json: 10 properties
[MCP Server] Loaded build-info.json: 10 properties
[MCP Server] Loaded editor-state.json: 9 properties
```

#### 2. リアルタイム更新確認
1. UnityでGameObjectを追加/削除
2. MCPサーバーのログで変更検知が表示されることを確認：
   ```
   [MCP Server] Unity data file changed: gameobjects.json
   [MCP Server] Loaded gameobjects.json: 5 properties
   ```

### Phase 3: Claude Desktop統合テスト

#### 1. 基本的なデータ取得テスト
```
unity_info_realtimeツールを使って、Unityプロジェクトの全情報を表示してください
```

**期待結果**: 6種類のUnityデータが整形されて表示される

#### 2. カテゴリ別データ取得テスト
```
unity_info_realtimeツールを使って、現在のシーン情報だけを表示してください（category: scene）
```

**期待結果**: シーン情報のみが表示される

#### 3. 動的更新確認テスト
1. UnityでGameObjectを追加
2. 再度`unity_info_realtime`を実行
3. 新しいGameObjectが表示されることを確認

## 📊 実装されるデータカテゴリ

| カテゴリ | ファイル名 | 内容 |
|---------|-----------|------|
| **Project** | `project-info.json` | プロジェクト名、Unityバージョン、会社名など |
| **Scene** | `scene-info.json` | 現在のシーン、オブジェクト数、再生状態など |
| **GameObjects** | `gameobjects.json` | GameObject一覧と位置情報 |
| **Assets** | `assets-info.json` | アセット統計、最近のアセットなど |
| **Build** | `build-info.json` | ビルドターゲット、設定、識別子など |
| **Editor** | `editor-state.json` | エディターフォーカス、コンパイル状況など |

## 🐛 トラブルシューティング

### Unity側の問題

#### 「UnityMCPメニューが表示されない」
**原因**: スクリプトコンパイルエラー
**解決方法**:
1. Unityコンソールでエラーを確認
2. `Assets/UnityMCP/Editor/`フォルダのスクリプトを確認
3. Unity Loggingパッケージがインストールされているか確認

#### 「JSONファイルが生成されない」
**原因**: 書き込み権限またはパスの問題
**解決方法**:
```csharp
// MCPDataWriter.csでデバッグログを追加
Debug.Log($\"[MCP] Writing to: {filePath}\");
```

#### 「自動エクスポートが動作しない」
**原因**: イベント登録の失敗
**解決方法**:
1. `UnityMCP > Toggle Auto Export`で手動で有効化
2. Unityエディターを再起動

### MCPサーバー側の問題

#### 「Unity data directory not found」
**原因**: パス設定の問題
**デバッグ方法**:
```typescript
console.error('[DEBUG] Data path:', dataPath);
console.error('[DEBUG] Path exists:', fs.existsSync(dataPath));
console.error('[DEBUG] Working directory:', process.cwd());
```

#### 「ファイル変更が検知されない」
**原因**: ファイル監視の失敗
**確認事項**:
1. `fs.watch`が正常に動作しているか
2. ファイルが実際に更新されているか（タイムスタンプ確認）
3. 監視対象ディレクトリの権限

### Claude Desktop連携の問題

#### 「データが取得できない」
**原因**: `cachedData`が空
**デバッグ方法**:
```typescript
log(`Cached data keys: ${Object.keys(cachedData)}`);
log(`Data path resolved: ${dataPath}`);
```

## ✅ Step 2完了チェックリスト

- [ ] Unity側で6種類のエクスポーターが正常に動作する
- [ ] JSONファイルが`UnityMCP/Data/`に生成される
- [ ] 自動変更検知システムが動作する
- [ ] MCPサーバーがファイル変更を検知する
- [ ] `unity_info_realtime`ツールが全データを取得できる
- [ ] カテゴリ別データ取得ができる
- [ ] Unityでの変更がリアルタイムに反映される
- [ ] エラーハンドリングが適切に動作する

## 🚀 次のステップ

Step 2が完了したら、`04-troubleshooting.md`で詳細なトラブルシューティング方法を学び、`05-advanced-configuration.md`でより高度な設定とカスタマイズ方法を習得しましょう。

### Step 3の予告
- Claude Desktop → Unity制御の実装
- GameObjectの動的生成・操作
- Unityエディターの自動化機能

Step 2でリアルタイムデータ取得が実現できれば、Step 3でUnityを完全に制御できるようになります！