# トラブルシューティングガイド

## 🚨 よくある問題と解決方法

### 環境設定関連

#### ❌ 「Command not found: node」
**症状**: `node`コマンドが認識されない
**原因**: Node.jsがインストールされていない、またはPATHが通っていない

**解決方法**:
```bash
# Node.jsのインストール確認
node --version

# インストールされていない場合
# 公式サイトからダウンロード: https://nodejs.org/
```

#### ❌ 「Module not found」エラー
**症状**: npm installまたはビルド時にモジュールが見つからない
**原因**: 依存関係が正しくインストールされていない

**解決方法**:
```bash
cd unity-mcp-node
rm -rf node_modules package-lock.json
npm install
npm run build
```

### Claude Desktop設定関連

#### ❌ 「Server disconnected」または「Server not found」
**症状**: Claude DesktopがMCPサーバーに接続できない
**原因**: 設定ファイルの問題

**診断手順**:
1. **設定ファイルの存在確認**
   ```bash
   # macOS
   ls -la ~/Library/Application\\ Support/Claude/claude_desktop_config.json
   
   # Windows
   dir \"%APPDATA%\\Claude\\claude_desktop_config.json\"
   ```

2. **JSON構文チェック**
   ```bash
   python3 -m json.tool claude_desktop_config.json
   ```

3. **ファイルパス確認**
   ```bash
   # 設定で指定したパスのファイルが存在するか確認
   ls -la /path/to/your/project/unity-mcp-node/dist/index.js
   ```

**よくある設定ミス**:
- パスにスペースが含まれている（エスケープが必要）
- 相対パスと絶対パスの混在
- JSON構文エラー（カンマ、クォートの間違い）

#### ❌ Claude Desktopでツールが表示されない
**症状**: `ping`や`unity_info_realtime`ツールが認識されない

**解決方法（段階的）**:
1. **Claude Desktop完全再起動**
   - Command+Q（macOS）またはAlt+F4（Windows）で完全終了
   - 10秒待機
   - 再起動

2. **MCPサーバー単体テスト**
   ```bash
   cd unity-mcp-node
   echo '{\"jsonrpc\": \"2.0\", \"id\": 1, \"method\": \"ping\"}' | node dist/index.js
   ```

3. **設定ファイル再確認**
   ```json
   {
     \"mcpServers\": {
       \"unity-mcp-server\": {
         \"command\": \"node\",
         \"args\": [\"/absolute/path/to/unity-mcp-node/dist/index.js\"],
         \"cwd\": \"/absolute/path/to/project/root\"
       }
     }
   }
   ```

### パス設定関連の問題

#### ❌ 「Unity data directory not found」
**症状**: MCPサーバーがUnityデータファイルを見つけられない
**原因**: パス解決の問題

**診断手順**:
```typescript
// デバッグログを追加（index.ts）
log(`Working directory: ${process.cwd()}`);
log(`Data path: ${dataPath}`);
log(`Data path exists: ${fs.existsSync(dataPath)}`);
```

**解決方法（優先順）**:

1. **環境変数を使用**
   ```bash
   export UNITY_MCP_DATA_PATH=\"/absolute/path/to/MCPLearning/UnityMCP/Data\"
   ```

2. **設定ファイルで指定**
   ```json
   // unity-mcp-node/mcp-config.json
   {
     \"unityDataPath\": \"./MCPLearning/UnityMCP/Data\"
   }
   ```

3. **絶対パス指定**（一時的な解決）
   ```typescript
   // index.ts（デバッグ用）
   const dataPath = '/absolute/path/to/MCPLearning/UnityMCP/Data';
   ```

#### ❌ 相対パスでデータが取得できない
**症状**: 設定ファイルで相対パスを指定しても動作しない
**原因**: Claude Desktop起動時の作業ディレクトリが異なる

**解決方法**:
```typescript
// 改良されたパス解決ロジック
const getUnityDataPath = () => {
  // 環境変数優先
  if (process.env.UNITY_MCP_DATA_PATH) {
    return path.resolve(process.env.UNITY_MCP_DATA_PATH);
  }
  
  // 設定ファイルから取得（相対パス対応）
  try {
    const configPath = path.join(__dirname, '..', 'mcp-config.json');
    if (fs.existsSync(configPath)) {
      const config = JSON.parse(fs.readFileSync(configPath, 'utf-8'));
      if (config.unityDataPath) {
        const configDir = path.dirname(configPath);
        const resolvedPath = path.resolve(configDir, '..', config.unityDataPath);
        return resolvedPath;
      }
    }
  } catch (error) {
    log('Config file read error:', error);
  }
  
  // フォールバック
  return path.resolve(process.cwd(), 'MCPLearning/UnityMCP/Data');
};
```

### Unity側の問題

#### ❌ 「UnityMCPメニューが表示されない」
**症状**: UnityエディターにMCPメニューが出現しない
**原因**: スクリプトエラーまたはフォルダ構成の問題

**解決方法**:
1. **コンパイルエラー確認**
   - Unityコンソールでエラーがないか確認
   - 赤いエラーメッセージをすべて解決

2. **フォルダ構成確認**
   ```
   Assets/UnityMCP/Editor/ ← Editorフォルダが必須
   ```

3. **Unity Loggingパッケージ確認**
   - Window > Package Manager
   - Unity Loggingがインストールされているか確認

#### ❌ 「JSONファイルが生成されない」
**症状**: `Export All Data`実行しても出力ファイルが作成されない

**診断手順**:
```csharp
// MCPDataWriter.csにデバッグログ追加
public static void WriteData(string fileName, Dictionary<string, object> data)
{
    var outputDir = GetOutputDirectory();
    Debug.Log($\"[MCP] Output directory: {outputDir}\");
    Debug.Log($\"[MCP] Directory exists: {Directory.Exists(outputDir)}\");
    
    var filePath = Path.Combine(outputDir, fileName);
    Debug.Log($\"[MCP] Writing to: {filePath}\");
    
    // 既存のWriteDataロジック...
}
```

**解決方法**:
1. **出力ディレクトリ確認**
   - `UnityMCP/Data/`フォルダが存在するか
   - ファイル書き込み権限があるか

2. **パス区切り文字の問題**
   ```csharp
   // Windowsでは\\\\ または Path.Combineを使用
   var outputPath = Path.Combine(Application.dataPath, \"..\", \"UnityMCP\", \"Data\");
   ```

#### ❌ 「自動エクスポートが動作しない」
**症状**: GameObjectを追加してもJSONファイルが更新されない

**解決方法**:
1. **自動エクスポート有効化確認**
   ```
   UnityMCP > Toggle Auto Export
   ```

2. **イベント登録確認**
   ```csharp
   // MCPDataExporter.csで確認
   private static void InitializeAutoExport()
   {
       Debug.Log(\"[MCP] InitializeAutoExport called\");
       EnableAutoExport();
   }
   ```

### MCPサーバー側の問題

#### ❌ 「ファイル変更が検知されない」
**症状**: UnityでデータをエクスポートしてもMCPサーバーが反応しない

**診断手順**:
```typescript
// ファイル監視デバッグ
function startFileWatching() {
  const fullPath = path.resolve(dataPath);
  log(`Watching directory: ${fullPath}`);
  
  if (!fs.existsSync(fullPath)) {
    log(`ERROR: Directory does not exist: ${fullPath}`);
    return;
  }
  
  fs.watch(fullPath, { recursive: false }, (eventType, filename) => {
    log(`File event: ${eventType}, file: ${filename}`);
    if (filename && filename.endsWith('.json')) {
      loadDataFile(filename);
    }
  });
}
```

**解決方法**:
1. **ディレクトリ存在確認**
2. **ファイル権限確認**
3. **ファイル監視の制限確認**（一部のファイルシステムでは制限あり）

#### ❌ 「JSON Parse error」
**症状**: MCPサーバーでJSONファイル読み込み時にエラー

**解決方法**:
```typescript
function loadDataFile(filename: string) {
  try {
    const filePath = path.join(path.resolve(dataPath), filename);
    const content = fs.readFileSync(filePath, 'utf-8');
    log(`Raw content: ${content.substring(0, 200)}...`); // デバッグ用
    
    const rawData = JSON.parse(content);
    // 処理続行...
  } catch (error) {
    log(`JSON parse error for ${filename}:`, error);
    log(`File content:`, fs.readFileSync(filePath, 'utf-8'));
  }
}
```

### データ取得関連

#### ❌ 「Unity project data is not available」
**症状**: `unity_info_realtime`ツールでデータが取得できない

**診断手順**:
```typescript
// cachedDataの状態確認
case 'unity_info_realtime':
  log(`Cached data keys: ${Object.keys(cachedData)}`);
  log(`Data path: ${dataPath}`);
  log(`Working directory: ${process.cwd()}`);
  
  const hasData = Object.keys(cachedData).length > 0;
  if (!hasData) {
    log('No cached data, attempting reload...');
    loadAllData();
  }
```

**解決方法（段階的）**:
1. **Unityでデータエクスポート実行**
2. **JSONファイル存在確認**
3. **MCPサーバー再起動**
4. **パス設定見直し**

## 🔧 高度なデバッグ手法

### ログレベルの追加

#### MCPサーバー側
```typescript
enum LogLevel {
  ERROR = 0,
  WARN = 1,
  INFO = 2,
  DEBUG = 3
}

function log(level: LogLevel, ...args: any[]) {
  const timestamp = new Date().toISOString();
  console.error(`[${timestamp}] [${LogLevel[level]}]`, ...args);
}

// 使用例
log(LogLevel.DEBUG, 'Loading file:', filename);
log(LogLevel.ERROR, 'Failed to parse JSON:', error);
```

#### Unity側
```csharp
public static class MCPLogger
{
    public enum LogLevel { Error, Warning, Info, Debug }
    
    public static void Log(LogLevel level, string message)
    {
        var timestamp = DateTime.Now.ToString(\"yyyy-MM-dd HH:mm:ss\");
        var logMessage = $\"[{timestamp}] [{level}] {message}\";
        
        switch (level)
        {
            case LogLevel.Error:
                Debug.LogError(logMessage);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(logMessage);
                break;
            default:
                Debug.Log(logMessage);
                break;
        }
    }
}
```

### ファイルベースのデバッグ

#### MCPサーバー
```typescript
// ログファイル出力
const fs = require('fs');
const logFile = path.join(__dirname, '..', 'debug.log');

function fileLog(...args: any[]) {
  const timestamp = new Date().toISOString();
  const message = `[${timestamp}] ${args.join(' ')}\\n`;
  fs.appendFileSync(logFile, message);
}
```

#### Unity側
```csharp
// ファイルログ出力
private static void WriteLogFile(string message)
{
    var logPath = Path.Combine(Application.dataPath, \"..\", \"Logs\", \"mcp-debug.log\");
    Directory.CreateDirectory(Path.GetDirectoryName(logPath));
    File.AppendAllText(logPath, $\"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\\n\");
}
```

### 環境情報の収集

#### システム診断スクリプト
```bash
#!/bin/bash
echo \"=== Unity MCP Diagnostics ===\"
echo \"Node.js version: $(node --version)\"
echo \"NPM version: $(npm --version)\"
echo \"Working directory: $(pwd)\"
echo \"Unity project exists: $(test -d MCPLearning && echo 'Yes' || echo 'No')\"
echo \"MCP server exists: $(test -f unity-mcp-node/dist/index.js && echo 'Yes' || echo 'No')\"
echo \"Unity data directory: $(test -d MCPLearning/UnityMCP/Data && echo 'Yes' || echo 'No')\"
echo \"Claude config exists: $(test -f ~/Library/Application\\ Support/Claude/claude_desktop_config.json && echo 'Yes' || echo 'No')\"
```

## 📞 サポートが必要な場合

### 問題報告のための情報収集

#### 必須情報
- **OS**: macOS/Windows/Linux + バージョン
- **Node.js**: `node --version`
- **Unity**: エディターバージョン
- **エラーメッセージ**: 完全なエラーログ
- **設定ファイル**: `claude_desktop_config.json`の内容（個人情報は除去）

#### ログファイルの場所
- **MCPサーバー**: プロジェクトルートの`debug.log`
- **Unity**: `Logs/mcp-debug.log`
- **Claude Desktop**: システムログ（OS依存）

### 段階的な問題切り分け

#### Step 1: 基本環境確認
1. Node.js動作確認
2. TypeScriptコンパイル確認
3. MCPサーバー単体動作確認

#### Step 2: Claude Desktop連携確認
1. 設定ファイル構文確認
2. パス存在確認
3. Claude Desktop再起動

#### Step 3: Unity連携確認
1. Unityプロジェクト起動確認
2. スクリプトコンパイル確認
3. JSONファイル生成確認

#### Step 4: データ流れ確認
1. Unity → JSON確認
2. JSON → MCPサーバー確認
3. MCPサーバー → Claude Desktop確認

この手順で問題を段階的に切り分けることで、効率的にトラブルシューティングできます。