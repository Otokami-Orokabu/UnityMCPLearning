using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
// using Unity.Logging; // Replaced with UnityEngine.Debug
using Debug = UnityEngine.Debug;

namespace UnityMCP.Editor
{
    /// <summary>
    /// Unity MCP Server管理ウィンドウ (UI Toolkit版)
    /// </summary>
    public class MCPServerManagerWindow : EditorWindow
    {
        private const string WINDOW_TITLE = "MCP Server Manager";
        private const string USS_PATH = "Assets/UnityMCP/Editor/Windows/MCPServerManager.uss";
        private const string UXML_PATH = "Assets/UnityMCP/Editor/Windows/MCPServerManager.uxml";
        
        // UI要素
        private Label _statusLabel;
        private Label _connectionStatusLabel;
        private Button _startServerButton;
        private Button _stopServerButton;
        private Button _refreshButton;
        private Button _testConnectionButton;
        private TextField _portField;
        private TextField _serverPathField;
        private Toggle _autoStartToggle;
        private ScrollView _logScrollView;
        private Label _lastUpdateLabel;
        
        // 新しいUI要素
        private Button _exportDataButton;
        private Button _forceExportButton;
        private Button _clearDataButton;
        private Button _openDataFolderButton;
        private Button _generateTestLogsButton;
        private Button _clearConsoleButton;
        private Label _dataStatusLabel;
        private VisualElement _dataStatusGuide;
        private Button _copyLogsButton;
        private Button _clearLogsButton;
        
        // サーバー管理
        private MCPServerManager _serverManager;
        private MCPConnectionMonitor _connectionMonitor;
        private ClaudeDesktopConfigManager _configManager;
        private MCPServerSettings _settings;
        
        // 状態
        private bool _isServerRunning = false;
        private int _logLineCount = 0;
        private const int MAX_LOG_LINES = 100;
        private float _lastUpdateTime = 0f;
        
        [MenuItem("UnityMCP/MCP Server Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<MCPServerManagerWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE, EditorGUIUtility.IconContent("d_NetworkAnimator Icon").image);
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        
        private void CreateGUI()
        {
            // 設定読み込み
            _settings = MCPServerSettings.Load();
            
            // マネージャー初期化
            _serverManager = new MCPServerManager();
            _connectionMonitor = new MCPConnectionMonitor();
            _configManager = new ClaudeDesktopConfigManager();
            
            // UIセットアップ
            var root = rootVisualElement;
            
            // UXMLロード（存在する場合）
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML_PATH);
            if (visualTree != null)
            {
                visualTree.CloneTree(root);
            }
            else
            {
                // プログラマティックUI作成
                CreateProgrammaticUI(root);
            }
            
            // スタイルシート適用
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(USS_PATH);
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
            
            // UI要素取得
            BindUIElements();
            
            // 設定値をUIに反映
            LoadSettingsToUI();
            
            // イベント設定
            SetupEventHandlers();
            
            // サーバーマネージャーのログイベントをUIに接続
            SetupServerLogHandlers();
            
            // 初期状態更新
            UpdateServerStatus();
            
            // 自動起動チェック
            CheckAutoStart();
            
            // 定期更新開始
            EditorApplication.update += OnEditorUpdate;
        }
        
        private void CreateProgrammaticUI(VisualElement root)
        {
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            
            // ヘッダー
            var header = new Label("Unity MCP Server Manager");
            header.style.fontSize = 20;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 15;
            root.Add(header);
            
            // ステータスセクション
            var statusSection = new VisualElement();
            statusSection.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            statusSection.style.borderBottomWidth = statusSection.style.borderTopWidth = 
                statusSection.style.borderLeftWidth = statusSection.style.borderRightWidth = 1;
            statusSection.style.borderBottomColor = statusSection.style.borderTopColor = 
                statusSection.style.borderLeftColor = statusSection.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            statusSection.style.paddingLeft = statusSection.style.paddingRight = 
                statusSection.style.paddingTop = statusSection.style.paddingBottom = 10;
            statusSection.style.marginBottom = 10;
            statusSection.style.borderBottomLeftRadius = statusSection.style.borderBottomRightRadius = 
                statusSection.style.borderTopLeftRadius = statusSection.style.borderTopRightRadius = 5;
            
            _statusLabel = new Label("Server Status: Checking...");
            _statusLabel.name = "server-status";
            statusSection.Add(_statusLabel);
            
            _connectionStatusLabel = new Label("Connection: Not Connected");
            _connectionStatusLabel.name = "connection-status";
            statusSection.Add(_connectionStatusLabel);
            
            _lastUpdateLabel = new Label("Last Update: Never");
            _lastUpdateLabel.name = "last-update";
            _lastUpdateLabel.style.fontSize = 10;
            _lastUpdateLabel.style.opacity = 0.7f;
            statusSection.Add(_lastUpdateLabel);
            
            root.Add(statusSection);
            
            // コントロールセクション
            var controlSection = new VisualElement();
            controlSection.style.marginBottom = 15;
            
            // ボタン行
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.marginBottom = 10;
            
            _startServerButton = new Button() { text = "Start Server" };
            _startServerButton.name = "start-button";
            _startServerButton.style.flexGrow = 1;
            _startServerButton.style.marginRight = 5;
            buttonRow.Add(_startServerButton);
            
            _stopServerButton = new Button() { text = "Stop Server" };
            _stopServerButton.name = "stop-button";
            _stopServerButton.style.flexGrow = 1;
            _stopServerButton.style.marginLeft = 5;
            _stopServerButton.style.marginRight = 5;
            buttonRow.Add(_stopServerButton);
            
            _refreshButton = new Button() { text = "Refresh" };
            _refreshButton.name = "refresh-button";
            _refreshButton.style.flexGrow = 1;
            _refreshButton.style.marginLeft = 5;
            _refreshButton.style.marginRight = 5;
            buttonRow.Add(_refreshButton);
            
            var testConnectionButton = new Button() { text = "Test Connection" };
            testConnectionButton.name = "test-connection-button";
            testConnectionButton.style.flexGrow = 1;
            testConnectionButton.style.marginLeft = 5;
            buttonRow.Add(testConnectionButton);
            
            controlSection.Add(buttonRow);
            
            // 設定フィールド
            _portField = new TextField("Port");
            _portField.name = "port-field";
            _portField.value = "3000";
            controlSection.Add(_portField);
            
            _serverPathField = new TextField("Server Path");
            _serverPathField.name = "server-path";
            _serverPathField.value = Path.Combine(Application.dataPath, "..", "..", "unity-mcp-node");
            controlSection.Add(_serverPathField);
            
            _autoStartToggle = new Toggle("Auto Start on Unity Launch");
            _autoStartToggle.name = "auto-start";
            _autoStartToggle.value = EditorPrefs.GetBool("UnityMCP_AutoStart", false);
            controlSection.Add(_autoStartToggle);
            
            root.Add(controlSection);
            
            // Data管理セクション
            var dataSection = new VisualElement();
            dataSection.style.marginBottom = 15;
            dataSection.style.backgroundColor = new Color(0.1f, 0.2f, 0.1f, 0.3f);
            dataSection.style.borderBottomWidth = dataSection.style.borderTopWidth = 
                dataSection.style.borderLeftWidth = dataSection.style.borderRightWidth = 1;
            dataSection.style.borderBottomColor = dataSection.style.borderTopColor = 
                dataSection.style.borderLeftColor = dataSection.style.borderRightColor = new Color(0.2f, 0.4f, 0.2f);
            dataSection.style.paddingLeft = dataSection.style.paddingRight = 
                dataSection.style.paddingTop = dataSection.style.paddingBottom = 10;
            dataSection.style.borderBottomLeftRadius = dataSection.style.borderBottomRightRadius = 
                dataSection.style.borderTopLeftRadius = dataSection.style.borderTopRightRadius = 5;
            
            var dataHeader = new Label("Data Management");
            dataHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            dataHeader.style.marginBottom = 10;
            dataSection.Add(dataHeader);
            
            _dataStatusLabel = new Label("Data Status: Ready");
            _dataStatusLabel.name = "data-status";
            _dataStatusLabel.style.marginBottom = 5;
            dataSection.Add(_dataStatusLabel);
            
            // Data Status色の説明ガイド
            _dataStatusGuide = new VisualElement();
            _dataStatusGuide.name = "data-status-guide";
            _dataStatusGuide.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
            _dataStatusGuide.style.borderBottomWidth = _dataStatusGuide.style.borderTopWidth = 
                _dataStatusGuide.style.borderLeftWidth = _dataStatusGuide.style.borderRightWidth = 1;
            _dataStatusGuide.style.borderBottomColor = _dataStatusGuide.style.borderTopColor = 
                _dataStatusGuide.style.borderLeftColor = _dataStatusGuide.style.borderRightColor = new Color(0.25f, 0.25f, 0.25f);
            _dataStatusGuide.style.paddingLeft = _dataStatusGuide.style.paddingRight = 
                _dataStatusGuide.style.paddingTop = _dataStatusGuide.style.paddingBottom = 8;
            _dataStatusGuide.style.marginBottom = 10;
            _dataStatusGuide.style.borderBottomLeftRadius = _dataStatusGuide.style.borderBottomRightRadius = 
                _dataStatusGuide.style.borderTopLeftRadius = _dataStatusGuide.style.borderTopRightRadius = 3;
            
            var guideTitle = new Label("📊 Data Size Guide:");
            guideTitle.style.fontSize = 11;
            guideTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            guideTitle.style.marginBottom = 3;
            _dataStatusGuide.Add(guideTitle);
            
            // 色の説明行を作成
            var colorGuideRow1 = new VisualElement();
            colorGuideRow1.style.flexDirection = FlexDirection.Row;
            colorGuideRow1.style.marginBottom = 2;
            
            var grayIndicator = new VisualElement();
            grayIndicator.style.width = 12;
            grayIndicator.style.height = 12;
            grayIndicator.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
            grayIndicator.style.marginRight = 5;
            grayIndicator.style.marginTop = 1;
            grayIndicator.style.borderBottomLeftRadius = grayIndicator.style.borderBottomRightRadius = 
                grayIndicator.style.borderTopLeftRadius = grayIndicator.style.borderTopRightRadius = 2;
            colorGuideRow1.Add(grayIndicator);
            
            var grayLabel = new Label("< 50KB (Normal)");
            grayLabel.style.fontSize = 10;
            colorGuideRow1.Add(grayLabel);
            
            _dataStatusGuide.Add(colorGuideRow1);
            
            var colorGuideRow2 = new VisualElement();
            colorGuideRow2.style.flexDirection = FlexDirection.Row;
            colorGuideRow2.style.marginBottom = 2;
            
            var yellowIndicator = new VisualElement();
            yellowIndicator.style.width = 12;
            yellowIndicator.style.height = 12;
            yellowIndicator.style.backgroundColor = new Color(1f, 1f, 0.5f);
            yellowIndicator.style.marginRight = 5;
            yellowIndicator.style.marginTop = 1;
            yellowIndicator.style.borderBottomLeftRadius = yellowIndicator.style.borderBottomRightRadius = 
                yellowIndicator.style.borderTopLeftRadius = yellowIndicator.style.borderTopRightRadius = 2;
            colorGuideRow2.Add(yellowIndicator);
            
            var yellowLabel = new Label("50-100KB (Caution)");
            yellowLabel.style.fontSize = 10;
            colorGuideRow2.Add(yellowLabel);
            
            _dataStatusGuide.Add(colorGuideRow2);
            
            var colorGuideRow3 = new VisualElement();
            colorGuideRow3.style.flexDirection = FlexDirection.Row;
            colorGuideRow3.style.marginBottom = 2;
            
            var orangeIndicator = new VisualElement();
            orangeIndicator.style.width = 12;
            orangeIndicator.style.height = 12;
            orangeIndicator.style.backgroundColor = new Color(1f, 0.8f, 0.3f);
            orangeIndicator.style.marginRight = 5;
            orangeIndicator.style.marginTop = 1;
            orangeIndicator.style.borderBottomLeftRadius = orangeIndicator.style.borderBottomRightRadius = 
                orangeIndicator.style.borderTopLeftRadius = orangeIndicator.style.borderTopRightRadius = 2;
            colorGuideRow3.Add(orangeIndicator);
            
            var orangeLabel = new Label("> 100KB (High Token Use)");
            orangeLabel.style.fontSize = 10;
            colorGuideRow3.Add(orangeLabel);
            
            _dataStatusGuide.Add(colorGuideRow3);
            
            var tipLabel = new Label("💡 Tip: Use 'Clear Data' to reduce token consumption");
            tipLabel.style.fontSize = 10;
            tipLabel.style.opacity = 0.8f;
            tipLabel.style.marginTop = 3;
            tipLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            _dataStatusGuide.Add(tipLabel);
            
            dataSection.Add(_dataStatusGuide);
            
            // Data操作ボタン行1
            var dataButtonRow1 = new VisualElement();
            dataButtonRow1.style.flexDirection = FlexDirection.Row;
            dataButtonRow1.style.marginBottom = 5;
            
            _exportDataButton = new Button() { text = "Export Data" };
            _exportDataButton.name = "export-data-button";
            _exportDataButton.style.flexGrow = 1;
            _exportDataButton.style.marginRight = 3;
            dataButtonRow1.Add(_exportDataButton);
            
            _forceExportButton = new Button() { text = "Force Export" };
            _forceExportButton.name = "force-export-button";
            _forceExportButton.style.flexGrow = 1;
            _forceExportButton.style.marginLeft = 3;
            _forceExportButton.style.marginRight = 3;
            dataButtonRow1.Add(_forceExportButton);
            
            _clearDataButton = new Button() { text = "Clear Data" };
            _clearDataButton.name = "clear-data-button";
            _clearDataButton.style.flexGrow = 1;
            _clearDataButton.style.marginLeft = 3;
            _clearDataButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
            dataButtonRow1.Add(_clearDataButton);
            
            dataSection.Add(dataButtonRow1);
            
            // Data操作ボタン行2
            var dataButtonRow2 = new VisualElement();
            dataButtonRow2.style.flexDirection = FlexDirection.Row;
            dataButtonRow2.style.marginBottom = 5;
            
            _openDataFolderButton = new Button() { text = "Open Data Folder" };
            _openDataFolderButton.name = "open-folder-button";
            _openDataFolderButton.style.flexGrow = 1;
            _openDataFolderButton.style.marginRight = 3;
            dataButtonRow2.Add(_openDataFolderButton);
            
            _generateTestLogsButton = new Button() { text = "Generate Test Logs" };
            _generateTestLogsButton.name = "test-logs-button";
            _generateTestLogsButton.style.flexGrow = 1;
            _generateTestLogsButton.style.marginLeft = 3;
            _generateTestLogsButton.style.marginRight = 3;
            dataButtonRow2.Add(_generateTestLogsButton);
            
            _clearConsoleButton = new Button() { text = "Clear Console" };
            _clearConsoleButton.name = "clear-console-button";
            _clearConsoleButton.style.flexGrow = 1;
            _clearConsoleButton.style.marginLeft = 3;
            dataButtonRow2.Add(_clearConsoleButton);
            
            dataSection.Add(dataButtonRow2);
            
            root.Add(dataSection);
            
            // ログセクション
            var logSection = new VisualElement();
            logSection.style.flexGrow = 1;
            
            var logHeader = new Label("Server Logs");
            logHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            logHeader.style.marginBottom = 5;
            logSection.Add(logHeader);
            
            _logScrollView = new ScrollView();
            _logScrollView.name = "log-scroll";
            _logScrollView.style.flexGrow = 1;
            _logScrollView.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            _logScrollView.style.borderBottomWidth = _logScrollView.style.borderTopWidth = 
                _logScrollView.style.borderLeftWidth = _logScrollView.style.borderRightWidth = 1;
            _logScrollView.style.borderBottomColor = _logScrollView.style.borderTopColor = 
                _logScrollView.style.borderLeftColor = _logScrollView.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            _logScrollView.style.paddingLeft = _logScrollView.style.paddingRight = 
                _logScrollView.style.paddingTop = _logScrollView.style.paddingBottom = 5;
            
            logSection.Add(_logScrollView);
            
            // ログ操作ボタン
            var logButtonRow = new VisualElement();
            logButtonRow.style.flexDirection = FlexDirection.Row;
            logButtonRow.style.marginTop = 5;
            
            _copyLogsButton = new Button() { text = "Copy Logs" };
            _copyLogsButton.name = "copy-logs-button";
            _copyLogsButton.style.flexGrow = 1;
            _copyLogsButton.style.marginRight = 5;
            logButtonRow.Add(_copyLogsButton);
            
            _clearLogsButton = new Button() { text = "Clear Logs" };
            _clearLogsButton.name = "clear-logs-button";
            _clearLogsButton.style.flexGrow = 1;
            _clearLogsButton.style.marginLeft = 5;
            logButtonRow.Add(_clearLogsButton);
            
            logSection.Add(logButtonRow);
            
            root.Add(logSection);
            
            // フッター
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.marginTop = 10;
            footer.style.paddingTop = 10;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            
            var configButton = new Button(OpenConfiguration) { text = "Open Config" };
            footer.Add(configButton);
            
            var helpButton = new Button(ShowHelp) { text = "Help" };
            footer.Add(helpButton);
            
            root.Add(footer);
        }
        
        private void BindUIElements()
        {
            var root = rootVisualElement;
            
            _statusLabel = root.Q<Label>("server-status");
            _connectionStatusLabel = root.Q<Label>("connection-status");
            _startServerButton = root.Q<Button>("start-button");
            _stopServerButton = root.Q<Button>("stop-button");
            _refreshButton = root.Q<Button>("refresh-button");
            _testConnectionButton = root.Q<Button>("test-connection-button");
            _portField = root.Q<TextField>("port-field");
            _serverPathField = root.Q<TextField>("server-path");
            _autoStartToggle = root.Q<Toggle>("auto-start");
            _logScrollView = root.Q<ScrollView>("log-scroll");
            _lastUpdateLabel = root.Q<Label>("last-update");
            
            // Data管理UI要素
            _dataStatusLabel = root.Q<Label>("data-status");
            _exportDataButton = root.Q<Button>("export-data-button");
            _forceExportButton = root.Q<Button>("force-export-button");
            _clearDataButton = root.Q<Button>("clear-data-button");
            _openDataFolderButton = root.Q<Button>("open-folder-button");
            _generateTestLogsButton = root.Q<Button>("test-logs-button");
            _clearConsoleButton = root.Q<Button>("clear-console-button");
            
            // ログ操作UI要素
            _copyLogsButton = root.Q<Button>("copy-logs-button");
            _clearLogsButton = root.Q<Button>("clear-logs-button");
        }
        
        private void LoadSettingsToUI()
        {
            if (_settings == null) return;
            
            // サーバーパス設定
            if (_serverPathField != null)
                _serverPathField.value = _settings.serverPath;
            
            // ポート設定
            if (_portField != null)
                _portField.value = _settings.defaultPort.ToString();
            
            // 自動起動設定
            if (_autoStartToggle != null)
                _autoStartToggle.value = _settings.autoStartOnLaunch;
        }
        
        private void SetupEventHandlers()
        {
            _startServerButton.clicked += StartServer;
            _stopServerButton.clicked += StopServer;
            _refreshButton.clicked += RefreshStatus;
            _testConnectionButton.clicked += TestConnection;
            
            // Data管理イベント
            _exportDataButton.clicked += ExportData;
            _forceExportButton.clicked += ForceExportData;
            _clearDataButton.clicked += ClearAllData;
            _openDataFolderButton.clicked += OpenDataFolder;
            _generateTestLogsButton.clicked += GenerateTestLogs;
            _clearConsoleButton.clicked += ClearConsole;
            
            // ログ操作イベント
            _copyLogsButton.clicked += CopyLogsToClipboard;
            _clearLogsButton.clicked += ClearServerLogs;
            
            _autoStartToggle.RegisterValueChangedCallback(evt =>
            {
                if (_settings != null)
                {
                    _settings.autoStartOnLaunch = evt.newValue;
                    _settings.Save();
                }
            });
            
            _portField.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out int port))
                {
                    _serverManager?.UpdatePort(port);
                    if (_settings != null)
                    {
                        _settings.defaultPort = port;
                        _settings.Save();
                    }
                }
            });
            
            _serverPathField.RegisterValueChangedCallback(evt =>
            {
                if (_settings != null)
                {
                    _settings.serverPath = evt.newValue;
                    _settings.Save();
                }
            });
        }
        
        private void StartServer()
        {
            AddLogEntry("Starting MCP Server...", LogType.Log);
            
            var serverPath = _serverPathField.value;
            if (!Directory.Exists(serverPath))
            {
                AddLogEntry($"Server path not found: {serverPath}", LogType.Error);
                return;
            }
            
            if (int.TryParse(_portField.value, out int port))
            {
                bool success = _serverManager.StartServer(serverPath, port);
                if (success)
                {
                    AddLogEntry($"Server started on port {port}", LogType.Log);
                    
                    // Claude Desktop設定更新
                    _configManager.UpdateConfiguration(port);
                }
                else
                {
                    AddLogEntry("Failed to start server", LogType.Error);
                }
                
                EditorApplication.delayCall += UpdateServerStatus;
            }
            else
            {
                AddLogEntry("Invalid port number", LogType.Error);
            }
        }
        
        private void StopServer()
        {
            AddLogEntry("Stopping MCP Server...", LogType.Log);
            _serverManager.StopServer();
            
            EditorApplication.delayCall += UpdateServerStatus;
        }
        
        private void RefreshStatus()
        {
            UpdateServerStatus();
            AddLogEntry("Status refreshed", LogType.Log);
        }
        
        private async void TestConnection()
        {
            AddLogEntry("Testing MCP connection...", LogType.Log);
            
            try
            {
                var port = int.Parse(_portField.value);
                var isConnected = await _connectionMonitor.TestConnection(port);
                
                if (isConnected)
                {
                    AddLogEntry("Connection test successful!", LogType.Log);
                }
                else
                {
                    AddLogEntry("Connection test failed", LogType.Warning);
                }
                
                // 状態を即座に更新
                EditorApplication.delayCall += UpdateServerStatus;
            }
            catch (Exception ex)
            {
                AddLogEntry($"Connection test error: {ex.Message}", LogType.Error);
            }
        }
        
        private void UpdateServerStatus()
        {
            _isServerRunning = _serverManager.IsServerRunning();
            
            // ステータス更新
            _statusLabel.text = $"Server Status: {(_isServerRunning ? "Running" : "Stopped")}";
            _statusLabel.style.color = _isServerRunning ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
            
            // 接続状態更新
            var isConnected = _connectionMonitor.IsConnected();
            _connectionStatusLabel.text = $"Connection: {(isConnected ? "Connected" : "Not Connected")}";
            _connectionStatusLabel.style.color = isConnected ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.8f, 0.3f);
            
            // ボタン状態更新
            _startServerButton.SetEnabled(!_isServerRunning);
            _stopServerButton.SetEnabled(_isServerRunning);
            
            // 最終更新時刻
            _lastUpdateLabel.text = $"Last Update: {DateTime.Now:HH:mm:ss}";
            
            // Data Status更新
            UpdateDataStatus();
        }
        
        private void AddLogEntry(string message, LogType logType)
        {
            var logEntry = new Label($"[{DateTime.Now:HH:mm:ss}] {message}");
            logEntry.style.marginBottom = 2;
            
            // ログタイプによる色分け
            switch (logType)
            {
                case LogType.Error:
                case LogType.Exception:
                    logEntry.style.color = new Color(1f, 0.5f, 0.5f);
                    break;
                case LogType.Warning:
                    logEntry.style.color = new Color(1f, 1f, 0.5f);
                    break;
                default:
                    logEntry.style.color = new Color(0.8f, 0.8f, 0.8f);
                    break;
            }
            
            _logScrollView.contentContainer.Add(logEntry);
            _logLineCount++;
            
            // 最大行数制限
            if (_logLineCount > MAX_LOG_LINES)
            {
                _logScrollView.contentContainer.RemoveAt(0);
                _logLineCount--;
            }
            
            // 最下部へスクロール
            _logScrollView.ScrollTo(logEntry);
        }
        
        private void OpenConfiguration()
        {
            var configPath = _configManager.GetConfigurationPath();
            if (File.Exists(configPath))
            {
                EditorUtility.RevealInFinder(configPath);
            }
            else
            {
                AddLogEntry("Configuration file not found", LogType.Warning);
            }
        }
        
        private void ShowHelp()
        {
            Application.OpenURL("https://github.com/Otokami-Orokabu/UnityMCPLearning/wiki/MCP-Server-Manager");
        }
        
        private void OnEditorUpdate()
        {
            // 定期的な状態更新（2秒ごと、パフォーマンス改善）
            if (Time.realtimeSinceStartup - _lastUpdateTime > 2.0f)
            {
                _lastUpdateTime = Time.realtimeSinceStartup;
                UpdateServerStatus();
            }
        }
        
        private void OnDestroy()
        {
            EditorApplication.update -= OnEditorUpdate;
            
            // 自動停止オプション
            if (EditorPrefs.GetBool("UnityMCP_StopOnClose", true) && _isServerRunning)
            {
                _serverManager.StopServer();
            }
        }
        
        // Data管理メソッド
        private void ExportData()
        {
            try
            {
                MCPDataExporter.ExportAllData();
                AddLogEntry("Data export completed successfully", LogType.Log);
                UpdateDataStatus();
            }
            catch (System.Exception ex)
            {
                AddLogEntry($"Data export failed: {ex.Message}", LogType.Error);
            }
        }
        
        private void ForceExportData()
        {
            try
            {
                MCPDataExporter.ForceExportAllData();
                AddLogEntry("Force data export completed successfully", LogType.Log);
                UpdateDataStatus();
            }
            catch (System.Exception ex)
            {
                AddLogEntry($"Force data export failed: {ex.Message}", LogType.Error);
            }
        }
        
        private void ClearAllData()
        {
            if (EditorUtility.DisplayDialog("Clear All Data", 
                "Are you sure you want to clear all JSON data files?\n\nThis action cannot be undone and will help reduce token consumption.",
                "Clear Data", "Cancel"))
            {
                try
                {
                    var dataPath = Path.Combine(Application.dataPath, "../UnityMCP/Data");
                    if (Directory.Exists(dataPath))
                    {
                        var jsonFiles = Directory.GetFiles(dataPath, "*.json");
                        foreach (var file in jsonFiles)
                        {
                            File.Delete(file);
                        }
                        
                        AddLogEntry($"Cleared {jsonFiles.Length} JSON files", LogType.Log);
                        _dataStatusLabel.text = $"Data Status: Cleared ({jsonFiles.Length} files removed)";
                        _dataStatusLabel.style.color = new Color(0.5f, 1f, 0.5f);
                    }
                    else
                    {
                        AddLogEntry("Data directory not found", LogType.Warning);
                    }
                }
                catch (System.Exception ex)
                {
                    AddLogEntry($"Failed to clear data: {ex.Message}", LogType.Error);
                }
            }
        }
        
        private void OpenDataFolder()
        {
            try
            {
                MCPDataExporter.OpenDataFolder();
                AddLogEntry("Opened data folder", LogType.Log);
            }
            catch (System.Exception ex)
            {
                AddLogEntry($"Failed to open data folder: {ex.Message}", LogType.Error);
            }
        }
        
        private void GenerateTestLogs()
        {
            try
            {
                // TestConsoleOutput の機能を統合
                UnityEngine.Debug.Log("Test Log Message - Generated from MCP Server Manager");
                UnityEngine.Debug.LogWarning("Test Warning Message - MCP Integration Test");
                UnityEngine.Debug.LogError("Test Error Message - Error Handling Test");
                
                AddLogEntry("Generated test console logs", LogType.Log);
            }
            catch (System.Exception ex)
            {
                AddLogEntry($"Failed to generate test logs: {ex.Message}", LogType.Error);
            }
        }
        
        private void ClearConsole()
        {
            try
            {
                // Unityコンソールをクリア（リフレクション使用）
                var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
                var type = assembly.GetType("UnityEditor.LogEntries");
                var method = type.GetMethod("Clear");
                method.Invoke(new object(), null);
                
                AddLogEntry("Cleared Unity console", LogType.Log);
            }
            catch (System.Exception ex)
            {
                AddLogEntry($"Failed to clear console: {ex.Message}", LogType.Error);
            }
        }
        
        private void UpdateDataStatus()
        {
            try
            {
                var dataPath = Path.Combine(Application.dataPath, "../UnityMCP/Data");
                if (Directory.Exists(dataPath))
                {
                    var jsonFiles = Directory.GetFiles(dataPath, "*.json");
                    var totalSize = jsonFiles.Sum(f => new FileInfo(f).Length);
                    var sizeInKB = totalSize / 1024.0;
                    
                    _dataStatusLabel.text = $"Data Status: {jsonFiles.Length} files ({sizeInKB:F1} KB)";
                    
                    // サイズに応じて色を変更（大きいファイルは警告色）
                    if (sizeInKB > 100)
                    {
                        _dataStatusLabel.style.color = new Color(1f, 0.8f, 0.3f); // オレンジ
                    }
                    else if (sizeInKB > 50)
                    {
                        _dataStatusLabel.style.color = new Color(1f, 1f, 0.5f); // 黄色
                    }
                    else
                    {
                        _dataStatusLabel.style.color = new Color(0.8f, 0.8f, 0.8f); // グレー
                    }
                }
                else
                {
                    _dataStatusLabel.text = "Data Status: No data directory";
                    _dataStatusLabel.style.color = new Color(1f, 0.5f, 0.5f);
                }
            }
            catch (System.Exception ex)
            {
                _dataStatusLabel.text = $"Data Status: Error - {ex.Message}";
                _dataStatusLabel.style.color = new Color(1f, 0.5f, 0.5f);
            }
        }
        
        // サーバーログハンドラー設定
        private void SetupServerLogHandlers()
        {
            if (_serverManager != null)
            {
                _serverManager.OnServerOutput += (output) =>
                {
                    AddLogEntry(output, LogType.Log);
                };
                
                _serverManager.OnServerError += (error) =>
                {
                    AddLogEntry(error, LogType.Error);
                };
            }
        }
        
        // ログをクリップボードにコピー
        private void CopyLogsToClipboard()
        {
            try
            {
                var logTexts = new System.Text.StringBuilder();
                foreach (VisualElement child in _logScrollView.contentContainer.Children())
                {
                    if (child is Label label)
                    {
                        logTexts.AppendLine(label.text);
                    }
                }
                
                EditorGUIUtility.systemCopyBuffer = logTexts.ToString();
                AddLogEntry("Server logs copied to clipboard", LogType.Log);
            }
            catch (System.Exception ex)
            {
                AddLogEntry($"Failed to copy logs: {ex.Message}", LogType.Error);
            }
        }
        
        // サーバーログをクリア
        private void ClearServerLogs()
        {
            try
            {
                _logScrollView.contentContainer.Clear();
                _logLineCount = 0;
                AddLogEntry("Server logs cleared", LogType.Log);
            }
            catch (System.Exception ex)
            {
                AddLogEntry($"Failed to clear logs: {ex.Message}", LogType.Error);
            }
        }
        
        // 自動起動チェック
        private void CheckAutoStart()
        {
            if (EditorPrefs.GetBool("UnityMCP_AutoStart", false))
            {
                AddLogEntry("Auto Start enabled - checking server status...", LogType.Log);
                
                if (!_serverManager.IsServerRunning())
                {
                    AddLogEntry("Auto starting MCP Server for Claude Code integration...", LogType.Log);
                    StartServer();
                }
                else
                {
                    AddLogEntry("MCP Server already running", LogType.Log);
                }
            }
        }
        
        // Claude Code統合用の追加メソッド
        [MenuItem("UnityMCP/Quick Start for Claude Code")]
        public static void QuickStartForClaudeCode()
        {
            // MCP Server Manager を開く
            var window = GetWindow<MCPServerManagerWindow>();
            window.titleContent = new GUIContent("MCP Server Manager", EditorGUIUtility.IconContent("d_NetworkAnimator Icon").image);
            window.minSize = new Vector2(600, 400);
            window.Show();
            
            // 自動起動を有効化
            EditorPrefs.SetBool("UnityMCP_AutoStart", true);
            
            // ウィンドウが表示されたら自動でサーバー起動
            EditorApplication.delayCall += () =>
            {
                if (window._serverManager != null && !window._serverManager.IsServerRunning())
                {
                    window.StartServer();
                }
            };
        }
    }
}