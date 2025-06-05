# Unity MCP管理システム設計

## 🎯 必要性

配布版Unity MCP Learningでは、Unity Editor内からMCPサーバーの設定・管理が完結する必要がある。

### 現在の問題点
- ✅ 手動でClaude Desktop設定ファイル編集が必要
- ✅ パス設定を手動で調整
- ✅ MCPサーバーの起動・停止が手動
- ✅ エラー時のデバッグが困難

### 理想的なUX
```
Unity Editor内で完結:
設定 → 起動 → 接続確認 → 使用開始
```

## 🛠️ システム設計

### コンポーネント構成

```
MCPManagerSystem
├── MCPServerManager          # サーバープロセス管理
├── ClaudeDesktopConfigManager # Claude設定自動更新
├── MCPConnectionMonitor      # 接続状態監視  
├── MCPServerWindow          # 統合管理UI
└── MCPSettingsProvider      # 設定管理
```

## 🔧 実装詳細

### 1. MCPServerManager.cs

```csharp
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

[CreateAssetMenu(fileName = "MCPServerSettings", menuName = "UnityMCP/Server Settings")]
public class MCPServerManager : ScriptableObject
{
    [Header("Server Configuration")]
    [SerializeField] private string serverPath = "";
    [SerializeField] private bool autoStart = true;
    [SerializeField] private int serverPort = 3000;
    [SerializeField] private bool enableLogging = true;
    
    [Header("Runtime State")]
    [SerializeField, ReadOnly] private bool isServerRunning = false;
    [SerializeField, ReadOnly] private int serverProcessId = -1;
    
    private Process serverProcess;
    
    // Events
    public System.Action<bool> OnServerStateChanged;
    public System.Action<string> OnServerLog;
    public System.Action<string> OnServerError;
    
    #region Public API
    
    public bool StartServer()
    {
        if (isServerRunning) 
        {
            MCPLogger.LogWarning("MCPサーバーは既に起動中です");
            return true;
        }
        
        if (!ValidateServerPath())
        {
            MCPLogger.LogError("MCPサーバーパスが無効です");
            return false;
        }
        
        try
        {
            StartServerProcess();
            isServerRunning = true;
            serverProcessId = serverProcess.Id;
            OnServerStateChanged?.Invoke(true);
            MCPLogger.Log("MCPサーバーを起動しました");
            return true;
        }
        catch (System.Exception e)
        {
            MCPLogger.LogError($"MCPサーバー起動失敗: {e.Message}");
            return false;
        }
    }
    
    public void StopServer()
    {
        if (!isServerRunning) return;
        
        try
        {
            if (serverProcess != null && !serverProcess.HasExited)
            {
                serverProcess.Kill();
                serverProcess.WaitForExit(5000); // 5秒タイムアウト
            }
            
            CleanupServerProcess();
            MCPLogger.Log("MCPサーバーを停止しました");
        }
        catch (System.Exception e)
        {
            MCPLogger.LogError($"MCPサーバー停止エラー: {e.Message}");
        }
    }
    
    public void RestartServer()
    {
        StopServer();
        System.Threading.Thread.Sleep(1000); // 1秒待機
        StartServer();
    }
    
    public ServerStatus GetServerStatus()
    {
        if (!isServerRunning) return ServerStatus.Stopped;
        
        if (serverProcess == null || serverProcess.HasExited)
        {
            // プロセスが終了している場合は状態を更新
            CleanupServerProcess();
            return ServerStatus.Stopped;
        }
        
        // 接続テスト（簡易）
        if (MCPConnectionMonitor.TestConnection())
        {
            return ServerStatus.Running;
        }
        else
        {
            return ServerStatus.StartingOrError;
        }
    }
    
    #endregion
    
    #region Private Methods
    
    private bool ValidateServerPath()
    {
        if (string.IsNullOrEmpty(serverPath)) return false;
        
        string indexPath = Path.Combine(serverPath, "dist", "index.js");
        return File.Exists(indexPath);
    }
    
    private void StartServerProcess()
    {
        string nodePath = FindNodePath();
        string scriptPath = Path.Combine(serverPath, "dist", "index.js");
        
        serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = nodePath,
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = serverPath,
                UseShellExecute = false,
                RedirectStandardOutput = enableLogging,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Environment = 
                {
                    ["UNITY_PROJECT_PATH"] = Application.dataPath,
                    ["MCP_SERVER_PORT"] = serverPort.ToString()
                }
            }
        };
        
        if (enableLogging)
        {
            serverProcess.OutputDataReceived += OnServerOutputReceived;
            serverProcess.ErrorDataReceived += OnServerErrorReceived;
        }
        
        serverProcess.Start();
        
        if (enableLogging)
        {
            serverProcess.BeginOutputReadLine();
            serverProcess.BeginErrorReadLine();
        }
    }
    
    private void CleanupServerProcess()
    {
        if (serverProcess != null)
        {
            serverProcess.OutputDataReceived -= OnServerOutputReceived;
            serverProcess.ErrorDataReceived -= OnServerErrorReceived;
            serverProcess.Dispose();
            serverProcess = null;
        }
        
        isServerRunning = false;
        serverProcessId = -1;
        OnServerStateChanged?.Invoke(false);
    }
    
    private string FindNodePath()
    {
        // Node.jsパスの自動検出
        string[] possiblePaths = {
            "/usr/local/bin/node",      // macOS Homebrew
            "/opt/homebrew/bin/node",   // macOS M1 Homebrew
            "C:\\Program Files\\nodejs\\node.exe", // Windows default
            "node" // PATH環境変数
        };
        
        foreach (string path in possiblePaths)
        {
            if (File.Exists(path)) return path;
        }
        
        return "node"; // フォールバック
    }
    
    private void OnServerOutputReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            string logMessage = $"[MCP Server] {e.Data}";
            MCPLogger.Log(logMessage);
            OnServerLog?.Invoke(logMessage);
        }
    }
    
    private void OnServerErrorReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            string errorMessage = $"[MCP Server Error] {e.Data}";
            MCPLogger.LogError(errorMessage);
            OnServerError?.Invoke(errorMessage);
        }
    }
    
    #endregion
    
    #region Unity Events
    
    private void OnEnable()
    {
        // Unity Editor終了時の自動停止
        EditorApplication.quitting += StopServer;
        
        // Play Mode変更時の処理
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    private void OnDisable()
    {
        StopServer();
        EditorApplication.quitting -= StopServer;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
    
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Play Mode中もサーバーを継続稼働
        // 必要に応じて挙動を調整
    }
    
    #endregion
}

public enum ServerStatus
{
    Stopped,
    StartingOrError,
    Running
}
```

### 2. ClaudeDesktopConfigManager.cs

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class ClaudeDesktopConfigManager
{
    private const string CONFIG_BACKUP_SUFFIX = ".backup";
    
    public static bool UpdateClaudeConfig(string mcpServerPath, bool createBackup = true)
    {
        try
        {
            string configPath = GetClaudeConfigPath();
            
            if (createBackup && File.Exists(configPath))
            {
                CreateConfigBackup(configPath);
            }
            
            var config = LoadOrCreateConfig(configPath);
            UpdateMCPServerConfig(config, mcpServerPath);
            SaveConfig(configPath, config);
            
            MCPLogger.Log("Claude Desktop設定を更新しました");
            return true;
        }
        catch (System.Exception e)
        {
            MCPLogger.LogError($"Claude Desktop設定更新失敗: {e.Message}");
            return false;
        }
    }
    
    public static bool RestoreClaudeConfig()
    {
        try
        {
            string configPath = GetClaudeConfigPath();
            string backupPath = configPath + CONFIG_BACKUP_SUFFIX;
            
            if (!File.Exists(backupPath))
            {
                MCPLogger.LogWarning("バックアップファイルが見つかりません");
                return false;
            }
            
            File.Copy(backupPath, configPath, true);
            MCPLogger.Log("Claude Desktop設定をバックアップから復元しました");
            return true;
        }
        catch (System.Exception e)
        {
            MCPLogger.LogError($"設定復元失敗: {e.Message}");
            return false;
        }
    }
    
    public static bool ValidateClaudeConfig()
    {
        try
        {
            string configPath = GetClaudeConfigPath();
            if (!File.Exists(configPath)) return false;
            
            string json = File.ReadAllText(configPath);
            var config = JsonUtility.FromJson<ClaudeConfig>(json);
            
            return config?.mcpServers?.ContainsKey("unity-mcp") == true;
        }
        catch
        {
            return false;
        }
    }
    
    private static string GetClaudeConfigPath()
    {
        string homeDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        
        // OS別のパス
        if (Application.platform == RuntimePlatform.OSXEditor)
        {
            return Path.Combine(homeDir, "Library/Application Support/Claude/claude_desktop_config.json");
        }
        else if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            return Path.Combine(homeDir, "AppData/Roaming/Claude/claude_desktop_config.json");
        }
        else
        {
            // Linux
            return Path.Combine(homeDir, ".config/claude/claude_desktop_config.json");
        }
    }
    
    private static void CreateConfigBackup(string configPath)
    {
        string backupPath = configPath + CONFIG_BACKUP_SUFFIX;
        File.Copy(configPath, backupPath, true);
    }
    
    private static ClaudeConfig LoadOrCreateConfig(string configPath)
    {
        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            return JsonUtility.FromJson<ClaudeConfig>(json) ?? new ClaudeConfig();
        }
        else
        {
            // 設定ディレクトリを作成
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));
            return new ClaudeConfig();
        }
    }
    
    private static void UpdateMCPServerConfig(ClaudeConfig config, string mcpServerPath)
    {
        if (config.mcpServers == null)
        {
            config.mcpServers = new Dictionary<string, MCPServerConfig>();
        }
        
        string scriptPath = Path.Combine(mcpServerPath, "dist", "index.js");
        
        config.mcpServers["unity-mcp"] = new MCPServerConfig
        {
            command = "node",
            args = new[] { scriptPath },
            env = new Dictionary<string, string>()
        };
    }
    
    private static void SaveConfig(string configPath, ClaudeConfig config)
    {
        string json = JsonUtility.ToJson(config, true);
        File.WriteAllText(configPath, json);
    }
}

[System.Serializable]
public class ClaudeConfig
{
    public Dictionary<string, MCPServerConfig> mcpServers;
}

[System.Serializable]
public class MCPServerConfig
{
    public string command;
    public string[] args;
    public Dictionary<string, string> env;
}
```

### 3. MCPServerWindow.cs

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MCPServerWindow : EditorWindow
{
    private MCPServerManager serverManager;
    private Vector2 logScrollPosition;
    private List<string> serverLogs = new List<string>();
    private bool autoScroll = true;
    
    private GUIStyle statusStyle;
    private GUIStyle logStyle;
    
    [MenuItem("UnityMCP/Server Manager")]
    public static void OpenWindow()
    {
        MCPServerWindow window = GetWindow<MCPServerWindow>("MCP Server Manager");
        window.minSize = new Vector2(400, 300);
    }
    
    private void OnEnable()
    {
        LoadServerManager();
        if (serverManager != null)
        {
            serverManager.OnServerStateChanged += OnServerStateChanged;
            serverManager.OnServerLog += OnServerLog;
            serverManager.OnServerError += OnServerError;
        }
    }
    
    private void OnDisable()
    {
        if (serverManager != null)
        {
            serverManager.OnServerStateChanged -= OnServerStateChanged;
            serverManager.OnServerLog -= OnServerLog;
            serverManager.OnServerError -= OnServerError;
        }
    }
    
    private void OnGUI()
    {
        InitializeStyles();
        
        EditorGUILayout.Space();
        DrawHeader();
        EditorGUILayout.Space();
        
        DrawServerStatus();
        EditorGUILayout.Space();
        
        DrawServerSettings();
        EditorGUILayout.Space();
        
        DrawServerControls();
        EditorGUILayout.Space();
        
        DrawClaudeDesktopConfig();
        EditorGUILayout.Space();
        
        DrawServerLogs();
    }
    
    private void InitializeStyles()
    {
        if (statusStyle == null)
        {
            statusStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
        }
        
        if (logStyle == null)
        {
            logStyle = new GUIStyle(EditorStyles.textArea)
            {
                fontSize = 10,
                wordWrap = false
            };
        }
    }
    
    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Unity MCP Server Manager", EditorStyles.boldLabel);
    }
    
    private void DrawServerStatus()
    {
        EditorGUILayout.LabelField("Server Status", EditorStyles.boldLabel);
        
        if (serverManager == null)
        {
            EditorGUILayout.HelpBox("Server Manager not found. Please check settings.", MessageType.Error);
            return;
        }
        
        ServerStatus status = serverManager.GetServerStatus();
        string statusText = GetStatusText(status);
        Color statusColor = GetStatusColor(status);
        
        GUI.backgroundColor = statusColor;
        EditorGUILayout.LabelField(statusText, statusStyle, GUILayout.Height(30));
        GUI.backgroundColor = Color.white;
    }
    
    private void DrawServerSettings()
    {
        EditorGUILayout.LabelField("Server Settings", EditorStyles.boldLabel);
        
        if (serverManager == null) return;
        
        // SerializedObjectを使用してInspector風の編集
        SerializedObject serializedObject = new SerializedObject(serverManager);
        SerializedProperty serverPath = serializedObject.FindProperty("serverPath");
        SerializedProperty autoStart = serializedObject.FindProperty("autoStart");
        SerializedProperty serverPort = serializedObject.FindProperty("serverPort");
        SerializedProperty enableLogging = serializedObject.FindProperty("enableLogging");
        
        EditorGUILayout.PropertyField(serverPath, new GUIContent("Server Path"));
        EditorGUILayout.PropertyField(autoStart, new GUIContent("Auto Start"));
        EditorGUILayout.PropertyField(serverPort, new GUIContent("Server Port"));
        EditorGUILayout.PropertyField(enableLogging, new GUIContent("Enable Logging"));
        
        if (serializedObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(serverManager);
        }
        
        // パス自動検出ボタン
        if (GUILayout.Button("Auto Detect Server Path"))
        {
            AutoDetectServerPath();
        }
    }
    
    private void DrawServerControls()
    {
        EditorGUILayout.LabelField("Server Controls", EditorStyles.boldLabel);
        
        if (serverManager == null) return;
        
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = !serverManager.GetServerStatus().Equals(ServerStatus.Running);
        if (GUILayout.Button("Start Server"))
        {
            serverManager.StartServer();
        }
        
        GUI.enabled = serverManager.GetServerStatus().Equals(ServerStatus.Running);
        if (GUILayout.Button("Stop Server"))
        {
            serverManager.StopServer();
        }
        
        GUI.enabled = true;
        if (GUILayout.Button("Restart Server"))
        {
            serverManager.RestartServer();
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawClaudeDesktopConfig()
    {
        EditorGUILayout.LabelField("Claude Desktop Configuration", EditorStyles.boldLabel);
        
        bool isConfigValid = ClaudeDesktopConfigManager.ValidateClaudeConfig();
        
        if (isConfigValid)
        {
            EditorGUILayout.HelpBox("Claude Desktop configuration is valid.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Claude Desktop configuration needs to be updated.", MessageType.Warning);
        }
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Update Claude Config"))
        {
            if (serverManager != null && !string.IsNullOrEmpty(serverManager.serverPath))
            {
                ClaudeDesktopConfigManager.UpdateClaudeConfig(serverManager.serverPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please set server path first.", "OK");
            }
        }
        
        if (GUILayout.Button("Restore Backup"))
        {
            ClaudeDesktopConfigManager.RestoreClaudeConfig();
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawServerLogs()
    {
        EditorGUILayout.LabelField("Server Logs", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        autoScroll = EditorGUILayout.Toggle("Auto Scroll", autoScroll);
        if (GUILayout.Button("Clear Logs"))
        {
            serverLogs.Clear();
        }
        EditorGUILayout.EndHorizontal();
        
        // ログ表示エリア
        logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.Height(150));
        
        foreach (string log in serverLogs)
        {
            EditorGUILayout.LabelField(log, logStyle);
        }
        
        EditorGUILayout.EndScrollView();
        
        // 自動スクロール
        if (autoScroll && serverLogs.Count > 0)
        {
            logScrollPosition.y = Mathf.Infinity;
        }
    }
    
    #region Helper Methods
    
    private void LoadServerManager()
    {
        // ServerManagerのScriptableObjectを検索・読み込み
        string[] guids = AssetDatabase.FindAssets("t:MCPServerManager");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            serverManager = AssetDatabase.LoadAssetAtPath<MCPServerManager>(path);
        }
        
        if (serverManager == null)
        {
            // 新規作成
            serverManager = CreateInstance<MCPServerManager>();
            AssetDatabase.CreateAsset(serverManager, "Assets/UnityMCP/Settings/MCPServerSettings.asset");
            AssetDatabase.SaveAssets();
        }
    }
    
    private void AutoDetectServerPath()
    {
        // プロジェクトルートから相対的にサーバーパスを検出
        string projectRoot = Application.dataPath.Replace("/Assets", "");
        string[] possiblePaths = {
            Path.Combine(projectRoot, "unity-mcp-node"),
            Path.Combine(projectRoot, "../unity-mcp-node"),
            Path.Combine(projectRoot, "mcp-server")
        };
        
        foreach (string path in possiblePaths)
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "dist", "index.js")))
            {
                serverManager.serverPath = path;
                EditorUtility.SetDirty(serverManager);
                MCPLogger.Log($"Server path auto-detected: {path}");
                return;
            }
        }
        
        MCPLogger.LogWarning("Could not auto-detect server path. Please set manually.");
    }
    
    private string GetStatusText(ServerStatus status)
    {
        switch (status)
        {
            case ServerStatus.Stopped: return "● STOPPED";
            case ServerStatus.StartingOrError: return "⚠ STARTING / ERROR";
            case ServerStatus.Running: return "● RUNNING";
            default: return "● UNKNOWN";
        }
    }
    
    private Color GetStatusColor(ServerStatus status)
    {
        switch (status)
        {
            case ServerStatus.Stopped: return Color.gray;
            case ServerStatus.StartingOrError: return Color.yellow;
            case ServerStatus.Running: return Color.green;
            default: return Color.white;
        }
    }
    
    private void OnServerStateChanged(bool isRunning)
    {
        Repaint(); // UIを再描画
    }
    
    private void OnServerLog(string log)
    {
        serverLogs.Add($"[{System.DateTime.Now:HH:mm:ss}] {log}");
        
        // ログ数制限（メモリ対策）
        if (serverLogs.Count > 1000)
        {
            serverLogs.RemoveAt(0);
        }
        
        Repaint();
    }
    
    private void OnServerError(string error)
    {
        serverLogs.Add($"[{System.DateTime.Now:HH:mm:ss}] ERROR: {error}");
        
        if (serverLogs.Count > 1000)
        {
            serverLogs.RemoveAt(0);
        }
        
        Repaint();
    }
    
    #endregion
}
```

## 🎯 期待効果

### ユーザビリティ
- ✅ **ワンクリック設定**: 複雑な手動設定が不要
- ✅ **リアルタイム状態監視**: サーバー状態が一目でわかる
- ✅ **統合ログ**: Unityエディター内でサーバーログ確認
- ✅ **自動復旧**: エラー時の自動診断・復旧機能

### 配布適性
- ✅ **初心者フレンドリー**: 技術知識なしでもセットアップ可能
- ✅ **エラー耐性**: 設定問題の自動検出・修正
- ✅ **クロスプラットフォーム**: Windows/macOS/Linux対応
- ✅ **メンテナンス性**: 設定バックアップ・復元機能

---

**優先度**: 🔥 配布成功の必須要件  
**実装期間**: 2-3週間程度  
**Phase**: 配布パッケージのPhase 0として最優先実装

Unity MCP管理システムにより、真に使いやすい配布版の実現が可能になる。