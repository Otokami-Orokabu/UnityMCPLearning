# 高度な設定とカスタマイズ

## 🎯 このドキュメントの対象
- 基本的な動作が確認できた開発者
- システムをカスタマイズしたい開発者
- チーム開発や配布を考えている開発者

## 🚀 パフォーマンス最適化

### 1. ファイル監視の最適化

#### デバウンス機能の実装
```typescript
// unity-mcp-node/src/index.ts
class DebouncedFileWatcher {
  private debounceTimers: Map<string, NodeJS.Timeout> = new Map();
  private readonly debounceDelay = 500; // 500ms

  watch(filePath: string, callback: () => void) {
    const existingTimer = this.debounceTimers.get(filePath);
    if (existingTimer) {
      clearTimeout(existingTimer);
    }

    const timer = setTimeout(() => {
      callback();
      this.debounceTimers.delete(filePath);
    }, this.debounceDelay);

    this.debounceTimers.set(filePath, timer);
  }
}

const fileWatcher = new DebouncedFileWatcher();

// ファイル監視での使用
fs.watch(fullPath, { recursive: false }, (eventType, filename) => {
  if (filename && filename.endsWith('.json')) {
    fileWatcher.watch(filename, () => {
      log(`Processing delayed change: ${filename}`);
      loadDataFile(filename);
    });
  }
});
```

#### Unity側の変更検知最適化
```csharp
// Assets/UnityMCP/Editor/Common/ChangeDetectionManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityMCP.Editor
{
    [InitializeOnLoad]
    public class ChangeDetectionManager
    {
        private static readonly Dictionary<string, float> _lastChangeTime = new();
        private static readonly float CHANGE_COOLDOWN = 0.5f; // 500ms

        static ChangeDetectionManager()
        {
            EditorApplication.update += CheckForDelayedChanges;
        }

        public static bool ShouldProcessChange(string changeKey)
        {
            var currentTime = (float)EditorApplication.timeSinceStartup;
            
            if (_lastChangeTime.TryGetValue(changeKey, out var lastTime))
            {
                if (currentTime - lastTime < CHANGE_COOLDOWN)
                {
                    _lastChangeTime[changeKey] = currentTime; // 更新のみ
                    return false;
                }
            }
            
            _lastChangeTime[changeKey] = currentTime;
            return true;
        }

        private static void CheckForDelayedChanges()
        {
            // 定期的な遅延処理チェック（必要に応じて実装）
        }
    }
}
```

### 2. メモリ使用量の最適化

#### MCPサーバー側のキャッシュ管理
```typescript
class DataCache {
  private cache: Map<string, any> = new Map();
  private lastModified: Map<string, number> = new Map();
  private readonly maxCacheSize = 10;

  set(key: string, data: any, modTime: number) {
    // キャッシュサイズ制限
    if (this.cache.size >= this.maxCacheSize) {
      const oldestKey = this.getOldestKey();
      this.cache.delete(oldestKey);
      this.lastModified.delete(oldestKey);
    }

    this.cache.set(key, data);
    this.lastModified.set(key, modTime);
  }

  get(key: string): any {
    return this.cache.get(key);
  }

  private getOldestKey(): string {
    let oldestKey = '';
    let oldestTime = Number.MAX_SAFE_INTEGER;

    for (const [key, time] of this.lastModified) {
      if (time < oldestTime) {
        oldestTime = time;
        oldestKey = key;
      }
    }
    return oldestKey;
  }
}

const dataCache = new DataCache();
```

## 🔧 カスタムエクスポーターの作成

### 1. 新しいデータエクスポーターの実装

#### カスタムエクスポーターの例（パフォーマンス情報）
```csharp
// Assets/UnityMCP/Editor/Exporters/PerformanceExporter.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Profiling;

namespace UnityMCP.Editor
{
    public class PerformanceExporter : IDataExporter, IChangeDetector
    {
        private bool _hasChanged = true;
        private float _lastCheckTime = 0f;
        private readonly float CHECK_INTERVAL = 2.0f; // 2秒間隔

        public string GetDataKey() => \"performance_info\";

        public Dictionary<string, object> ExportData()
        {
            return new Dictionary<string, object>
            {
                [\"fps\"] = GetAverageFPS(),
                [\"memoryUsage\"] = GetMemoryUsage(),
                [\"renderingStats\"] = GetRenderingStats(),
                [\"timestamp\"] = System.DateTime.UtcNow.ToString(\"yyyy-MM-ddTHH:mm:ssZ\")
            };
        }

        public bool HasChanged() => _hasChanged;

        public bool DetectChanges()
        {
            var currentTime = (float)EditorApplication.timeSinceStartup;
            if (currentTime - _lastCheckTime > CHECK_INTERVAL)
            {
                _lastCheckTime = currentTime;
                _hasChanged = true;
                return true;
            }
            return false;
        }

        public void ResetChangeFlag() => _hasChanged = false;

        private float GetAverageFPS()
        {
            return 1.0f / Time.smoothDeltaTime;
        }

        private Dictionary<string, object> GetMemoryUsage()
        {
            return new Dictionary<string, object>
            {
                [\"totalReservedMemory\"] = Profiler.GetTotalReservedMemory(Profiler.GetDefaultProfilerName()),
                [\"totalAllocatedMemory\"] = Profiler.GetTotalAllocatedMemory(Profiler.GetDefaultProfilerName()),
                [\"totalUnusedReservedMemory\"] = Profiler.GetTotalUnusedReservedMemory(Profiler.GetDefaultProfilerName())
            };
        }

        private Dictionary<string, object> GetRenderingStats()
        {
            return new Dictionary<string, object>
            {
                [\"drawCalls\"] = UnityStats.drawCalls,
                [\"triangles\"] = UnityStats.triangles,
                [\"vertices\"] = UnityStats.vertices
            };
        }
    }
}
```

### 2. エクスポーターの動的登録

#### プラグイン方式のエクスポーター管理
```csharp
// Assets/UnityMCP/Editor/Common/ExporterManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityMCP.Editor
{
    public static class ExporterManager
    {
        private static List<IDataExporter> _customExporters;

        public static List<IDataExporter> GetAllExporters()
        {
            if (_customExporters == null)
            {
                LoadExporters();
            }
            return _customExporters;
        }

        private static void LoadExporters()
        {
            _customExporters = new List<IDataExporter>();

            // アセンブリから自動でエクスポーターを検索
            var exporterType = typeof(IDataExporter);
            var types = Assembly.GetAssembly(exporterType).GetTypes()
                .Where(t => exporterType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in types)
            {
                try
                {
                    var exporter = (IDataExporter)Activator.CreateInstance(type);
                    _customExporters.Add(exporter);
                    MCPLogger.Log($\"Loaded exporter: {type.Name}\");
                }
                catch (Exception ex)
                {
                    MCPLogger.LogError($\"Failed to load exporter {type.Name}: {ex.Message}\");
                }
            }
        }

        public static void RefreshExporters()
        {
            _customExporters = null;
            LoadExporters();
        }
    }
}
```

## 🌐 マルチプロジェクト対応

### 1. プロジェクト切り替え機能

#### 設定ファイルでの複数プロジェクト管理
```json
// unity-mcp-node/mcp-config.json
{
  \"projects\": {
    \"MCPLearning\": {
      \"unityDataPath\": \"./MCPLearning/UnityMCP/Data\",
      \"description\": \"MCP学習用プロジェクト\"
    },
    \"MyGameProject\": {
      \"unityDataPath\": \"./MyGameProject/UnityMCP/Data\",
      \"description\": \"実際のゲームプロジェクト\"
    }
  },
  \"activeProject\": \"MCPLearning\",
  \"mcpServers\": {
    \"unity-mcp-prod\": {
      \"command\": \"node\",
      \"args\": [\"./unity-mcp-node/dist/index.js\"],
      \"cwd\": \".\"
    }
  }
}
```

#### 動的プロジェクト切り替え
```typescript
// プロジェクト管理クラス
class ProjectManager {
  private config: any;
  private currentProject: string;

  constructor() {
    this.loadConfig();
  }

  private loadConfig() {
    try {
      const configPath = path.join(__dirname, '..', 'mcp-config.json');
      this.config = JSON.parse(fs.readFileSync(configPath, 'utf-8'));
      this.currentProject = this.config.activeProject || 'MCPLearning';
    } catch (error) {
      log('Failed to load project config:', error);
    }
  }

  getCurrentDataPath(): string {
    const project = this.config.projects[this.currentProject];
    if (!project) {
      throw new Error(`Project not found: ${this.currentProject}`);
    }
    return path.resolve(__dirname, '..', '..', project.unityDataPath);
  }

  switchProject(projectName: string): boolean {
    if (!this.config.projects[projectName]) {
      return false;
    }
    this.currentProject = projectName;
    this.config.activeProject = projectName;
    this.saveConfig();
    return true;
  }

  private saveConfig() {
    const configPath = path.join(__dirname, '..', 'mcp-config.json');
    fs.writeFileSync(configPath, JSON.stringify(this.config, null, 2));
  }
}
```

### 2. プロジェクト切り替えツールの実装

```typescript
// MCPツールとして実装
case 'switch_unity_project':
  const projectName = params?.arguments?.projectName;
  if (!projectName) {
    return {
      content: [{
        type: 'text',
        text: 'Available projects:\\n' + Object.keys(projectManager.config.projects).join('\\n')
      }],
      isError: false
    };
  }

  const success = projectManager.switchProject(projectName);
  if (success) {
    // ファイル監視を再開
    startFileWatching();
    return {
      content: [{
        type: 'text',
        text: `Switched to project: ${projectName}`
      }],
      isError: false
    };
  } else {
    return {
      content: [{
        type: 'text',
        text: `Project not found: ${projectName}`
      }],
      isError: true
    };
  }
```

## 🔐 セキュリティ強化

### 1. データサニタイゼーション

#### 個人情報の自動除去
```csharp
// Assets/UnityMCP/Editor/Common/DataSanitizer.cs
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityMCP.Editor
{
    public static class DataSanitizer
    {
        private static readonly Regex PathRegex = new Regex(@\"[A-Za-z]:\\\\Users\\\\[^\\\\]+\", RegexOptions.Compiled);
        private static readonly Regex UnixPathRegex = new Regex(@\"/Users/[^/]+\", RegexOptions.Compiled);

        public static Dictionary<string, object> SanitizeData(Dictionary<string, object> data)
        {
            var sanitized = new Dictionary<string, object>();

            foreach (var kvp in data)
            {
                sanitized[kvp.Key] = SanitizeValue(kvp.Value);
            }

            return sanitized;
        }

        private static object SanitizeValue(object value)
        {
            if (value is string stringValue)
            {
                // パスから個人情報を除去
                stringValue = PathRegex.Replace(stringValue, \"C:\\\\Users\\\\[USER]\");
                stringValue = UnixPathRegex.Replace(stringValue, \"/Users/[USER]\");
                return stringValue;
            }
            
            if (value is Dictionary<string, object> dictValue)
            {
                return SanitizeData(dictValue);
            }

            return value;
        }
    }
}
```

### 2. アクセス制御

#### IP制限機能
```typescript
// MCPサーバーにアクセス制御を追加
class AccessController {
  private allowedSources: Set<string>;

  constructor() {
    this.allowedSources = new Set(['127.0.0.1', 'localhost']);
  }

  isAllowed(source: string): boolean {
    return this.allowedSources.has(source);
  }

  // 環境変数からの設定読み込み
  loadFromEnvironment() {
    const allowedIPs = process.env.MCP_ALLOWED_IPS;
    if (allowedIPs) {
      allowedIPs.split(',').forEach(ip => {
        this.allowedSources.add(ip.trim());
      });
    }
  }
}
```

## 📊 監視とメトリクス

### 1. 詳細ログ機能

#### 構造化ログ
```typescript
interface LogEntry {
  timestamp: string;
  level: 'DEBUG' | 'INFO' | 'WARN' | 'ERROR';
  category: string;
  message: string;
  data?: any;
}

class StructuredLogger {
  private logQueue: LogEntry[] = [];
  private readonly maxQueueSize = 1000;

  log(level: LogEntry['level'], category: string, message: string, data?: any) {
    const entry: LogEntry = {
      timestamp: new Date().toISOString(),
      level,
      category,
      message,
      data
    };

    this.logQueue.push(entry);
    
    if (this.logQueue.length > this.maxQueueSize) {
      this.logQueue.shift();
    }

    // コンソール出力
    console.error(`[${entry.timestamp}] [${entry.level}] [${entry.category}] ${entry.message}`);
  }

  getRecentLogs(count: number = 100): LogEntry[] {
    return this.logQueue.slice(-count);
  }
}
```

### 2. パフォーマンスメトリクス

#### メトリクス収集システム
```typescript
class MetricsCollector {
  private metrics: Map<string, number[]> = new Map();

  recordMetric(name: string, value: number) {
    if (!this.metrics.has(name)) {
      this.metrics.set(name, []);
    }
    
    const values = this.metrics.get(name)!;
    values.push(value);
    
    // 最新100件のみ保持
    if (values.length > 100) {
      values.shift();
    }
  }

  getMetricSummary(name: string) {
    const values = this.metrics.get(name) || [];
    if (values.length === 0) return null;

    const sum = values.reduce((a, b) => a + b, 0);
    const avg = sum / values.length;
    const min = Math.min(...values);
    const max = Math.max(...values);

    return { count: values.length, avg, min, max };
  }

  getAllMetrics() {
    const summary: any = {};
    for (const [name] of this.metrics) {
      summary[name] = this.getMetricSummary(name);
    }
    return summary;
  }
}

// 使用例
const metrics = new MetricsCollector();

function measureFileLoad(filename: string) {
  const start = Date.now();
  // ファイル読み込み処理
  const duration = Date.now() - start;
  metrics.recordMetric('file_load_time', duration);
}
```

## 🎨 UI/UXの改善

### 1. Unity側のUI改善

#### プログレスバー付きエクスポート
```csharp
// Assets/UnityMCP/Editor/MCPDataExporter.cs
public static void ExportAllDataWithProgress()
{
    var totalExporters = _exporters.Count;
    var current = 0;

    EditorUtility.DisplayProgressBar(\"MCP Data Export\", \"Preparing...\", 0f);

    try
    {
        foreach (var exporter in _exporters)
        {
            var progress = (float)current / totalExporters;
            EditorUtility.DisplayProgressBar(
                \"MCP Data Export\", 
                $\"Exporting {exporter.GetDataKey()}...\", 
                progress
            );

            ExportData(exporter);
            current++;
        }
    }
    finally
    {
        EditorUtility.ClearProgressBar();
    }

    MCPLogger.Log($\"[MCP] {totalExporters}データのエクスポートが完了しました\");
}
```

#### 設定ウィンドウ
```csharp
// Assets/UnityMCP/Editor/MCPSettingsWindow.cs
using UnityEngine;
using UnityEditor;

namespace UnityMCP.Editor
{
    public class MCPSettingsWindow : EditorWindow
    {
        private bool _autoExportEnabled;
        private float _exportInterval = 1.0f;
        private string _outputPath = \"../UnityMCP/Data\";

        [MenuItem(\"UnityMCP/Settings\")]
        public static void ShowWindow()
        {
            GetWindow<MCPSettingsWindow>(\"MCP Settings\");
        }

        void OnGUI()
        {
            GUILayout.Label(\"MCP Export Settings\", EditorStyles.boldLabel);

            _autoExportEnabled = EditorGUILayout.Toggle(\"Auto Export Enabled\", _autoExportEnabled);
            _exportInterval = EditorGUILayout.FloatField(\"Export Interval (sec)\", _exportInterval);
            _outputPath = EditorGUILayout.TextField(\"Output Path\", _outputPath);

            if (GUILayout.Button(\"Apply Settings\"))
            {
                ApplySettings();
            }

            if (GUILayout.Button(\"Reset to Defaults\"))
            {
                ResetSettings();
            }
        }

        private void ApplySettings()
        {
            // 設定の保存と適用
            EditorPrefs.SetBool(\"MCP_AutoExport\", _autoExportEnabled);
            EditorPrefs.SetFloat(\"MCP_ExportInterval\", _exportInterval);
            EditorPrefs.SetString(\"MCP_OutputPath\", _outputPath);
        }

        private void ResetSettings()
        {
            _autoExportEnabled = true;
            _exportInterval = 1.0f;
            _outputPath = \"../UnityMCP/Data\";
        }
    }
}
```

## 🚀 配布とデプロイメント

### 1. 配布パッケージの作成

#### ビルドスクリプト
```bash
#!/bin/bash
# build-release.sh

echo \"Building Unity MCP Release Package...\"

# Node.js依存関係のインストール
cd unity-mcp-node
npm install --production
npm run build

# 不要ファイルの除去
rm -rf node_modules/@types
rm -rf node_modules/typescript

# 配布用アーカイブの作成
cd ..
tar -czf unity-mcp-release.tar.gz \\
  --exclude='MCPLearning/Library' \\
  --exclude='MCPLearning/Temp' \\
  --exclude='MCPLearning/Logs' \\
  --exclude='MCPLearning/obj' \\
  --exclude='unity-mcp-node/node_modules/@types' \\
  --exclude='**/.DS_Store' \\
  unity-mcp-node/ \\
  MCPLearning/Assets/UnityMCP/ \\
  MCPLearning/ProjectSettings/ \\
  docs/

echo \"Release package created: unity-mcp-release.tar.gz\"
```

### 2. 自動インストーラー

#### macOS用インストールスクリプト
```bash
#!/bin/bash
# install.sh

echo \"Unity MCP Server Installer\"

# 前提条件チェック
if ! command -v node &> /dev/null; then
    echo \"Error: Node.js is not installed\"
    exit 1
fi

if ! command -v unity &> /dev/null; then
    echo \"Warning: Unity is not found in PATH\"
fi

# インストール先の確認
INSTALL_DIR=\"$HOME/UnityMCP\"
read -p \"Install directory [$INSTALL_DIR]: \" user_dir
if [ ! -z \"$user_dir\" ]; then
    INSTALL_DIR=\"$user_dir\"
fi

# ディレクトリ作成
mkdir -p \"$INSTALL_DIR\"
cd \"$INSTALL_DIR\"

# ファイルのコピー
echo \"Installing files...\"
cp -r unity-mcp-node/ \"$INSTALL_DIR/\"
cp -r MCPLearning/ \"$INSTALL_DIR/\"

# 依存関係のインストール
cd \"$INSTALL_DIR/unity-mcp-node\"
npm install

# Claude Desktop設定
echo \"Configuring Claude Desktop...\"
CLAUDE_CONFIG=\"$HOME/Library/Application Support/Claude/claude_desktop_config.json\"
mkdir -p \"$(dirname \"$CLAUDE_CONFIG\")\"

cat > \"$CLAUDE_CONFIG\" << EOF
{
  \"mcpServers\": {
    \"unity-mcp-server\": {
      \"command\": \"node\",
      \"args\": [\"$INSTALL_DIR/unity-mcp-node/dist/index.js\"],
      \"cwd\": \"$INSTALL_DIR\"
    }
  }
}
EOF

echo \"Installation complete!\"
echo \"Please restart Claude Desktop to activate the MCP server.\"
```

これらの高度な設定により、Unity MCPサーバーをより柔軟で強力なシステムに拡張できます。基本的な動作を確認した後、必要に応じてこれらの機能を追加実装してください。