using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;
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

        // パス取得ヘルパーメソッド
        private static string GetUIAssetPath(string fileName)
        {
            try
            {
                var packagePath = MCPPackageResolver.GetPackageRootPath();
                MCPLogger.LogInfo($"[MCPServerManagerWindow] Package path resolved to: {packagePath}");
                
                var relativeAssetPath = Path.Combine(packagePath, "Scripts/Editor/Windows", fileName).Replace('\\', '/');
                MCPLogger.LogInfo($"[MCPServerManagerWindow] Trying relative path: {relativeAssetPath}");
                
                // まず相対パスで試す
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativeAssetPath);
                if (asset != null)
                {
                    MCPLogger.LogInfo($"[MCPServerManagerWindow] Successfully found {fileName} at: {relativeAssetPath}");
                    return relativeAssetPath;
                }
                
                // Server~ディレクトリ内での検索（リリースパッケージ用）
                var serverPath = MCPPackageResolver.GetServerPath();
                if (!string.IsNullOrEmpty(serverPath))
                {
                    var serverUIPath = Path.Combine(serverPath, "Scripts/Editor/Windows", fileName).Replace('\\', '/');
                    MCPLogger.LogInfo($"[MCPServerManagerWindow] Trying Server~ path: {serverUIPath}");
                    
                    asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(serverUIPath);
                    if (asset != null)
                    {
                        MCPLogger.LogInfo($"[MCPServerManagerWindow] Successfully found {fileName} at Server~ path: {serverUIPath}");
                        return serverUIPath;
                    }
                }
                
                // AssetDatabase.FindAssetsでファイルを検索
                var guids = AssetDatabase.FindAssets($"{Path.GetFileNameWithoutExtension(fileName)} t:{(fileName.EndsWith(".uxml") ? "VisualTreeAsset" : "StyleSheet")}");
                foreach (var guid in guids)
                {
                    var foundPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (foundPath.Contains("MCPServerManagerWindow") && foundPath.EndsWith(fileName))
                    {
                        MCPLogger.LogInfo($"[MCPServerManagerWindow] Found {fileName} via AssetDatabase.FindAssets: {foundPath}");
                        return foundPath;
                    }
                }
                
                // PackageInfo APIを使用した検索
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packagePath);
                if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.assetPath))
                {
                    var packageAssetPath = Path.Combine(packageInfo.assetPath, "Scripts/Editor/Windows", fileName).Replace('\\', '/');
                    MCPLogger.LogInfo($"[MCPServerManagerWindow] Trying PackageInfo path: {packageAssetPath}");
                    
                    asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(packageAssetPath);
                    if (asset != null)
                    {
                        MCPLogger.LogInfo($"[MCPServerManagerWindow] Successfully found {fileName} at PackageInfo path: {packageAssetPath}");
                        return packageAssetPath;
                    }
                }
                
                MCPLogger.LogWarning($"[MCPServerManagerWindow] Could not locate {fileName} in any expected location");
                return relativeAssetPath; // フォールバック
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"[MCPServerManagerWindow] Failed to resolve {fileName} path: {ex.Message}");
                return $"Assets/Packages/unity-mcp-learning/Scripts/Editor/Windows/{fileName}"; // 最終フォールバック
            }
        }

        private static string GetUXMLPath()
        {
            return GetUIAssetPath("MCPServerManagerWindow.uxml");
        }

        private static string GetUSSPath()
        {
            return GetUIAssetPath("MCPServerManagerWindow.uss");
        }

        // UI要素
        private Label _statusLabel;
        private Label _connectionStatusLabel;
        private Button _startServerButton;
        private Button _stopServerButton;
        private Button _refreshButton;
        private Button _testConnectionButton;
        private IntegerField _portField;
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

        // Phase 3 マルチプロジェクト機能UI要素
        private Label _projectIdLabel;
        private Label _projectPathLabel;
        private Button _copyProjectIdButton;
        private Button _regenerateProjectIdButton;
        
        private Label _assignedPortLabel;
        private Label _portStatusLabel;
        private Label _availablePortsCountLabel;
        private Button _changePortButton;
        private Button _releasePortButton;
        
        private Label _autoApproveStatusLabel;
        private Label _registeredProjectsCountLabel;
        private Label _portRangeInfoLabel;
        private Button _viewRegistryDetailsButton;
        
        private Button _refreshPortStatusButton;
        private Button _setupAutoApproveButton;
        private Button _generateMultiConfigButton;

        // Phase 4.3 Auto-Accept Configuration UI要素
        private Label _autoAcceptConfigStatusLabel;
        private Label _claudeConfigStatusLabel;
        private Label _approvedToolsCountLabel;
        private Label _lastConfiguredInfoLabel;
        private Button _enableAutoAcceptButton;
        private Button _disableAutoAcceptButton;
        private Button _checkAutoAcceptStatusButton;
        private Button _resetAutoAcceptButton;
        private Button _openClaudeConfigButton;
        private Button _viewAutoAcceptLogsButton;

        // Phase 4.4 Multi-Project Configuration Generator UI要素
        private Label _configProjectNameLabel;
        private Label _configStatusOverviewLabel;
        private Label _configServerPathLabel;
        private Label _targetServerDirLabel;
        private Label _targetProjectRootLabel;
        private Label _targetClaudeDesktopLabel;
        private Toggle _includeAutoAcceptToggle;
        private Toggle _packageDistributionToggle;
        private Toggle _createBackupToggle;
        private Toggle _forceRegenerateToggle;
        private Button _openServerDirButton;
        private Button _openProjectRootButton;
        private Button _openClaudeDirButton;
        private Button _previewConfigButton;
        private Button _validateSetupButton;
        private Button _checkExistingButton;
        private Button _generateAllConfigsButton;
        private Button _generateServerOnlyButton;
        private Button _cleanRegenerateButton;

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
        private float _lastMultiProjectUpdateTime = 0f;

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

            // UXMLロード（動的パス解決）
            var uxmlPath = GetUXMLPath();
            MCPLogger.LogInfo($"[MCPServerManagerWindow] Attempting to load UXML from: {uxmlPath}");
            
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (visualTree != null)
            {
                visualTree.CloneTree(root);
                MCPLogger.LogInfo("[MCPServerManagerWindow] Successfully loaded UXML-based UI");
            }
            else
            {
                // プログラマティックUI作成（フォールバック）
                MCPLogger.LogWarning($"[MCPServerManagerWindow] UXML not found at {uxmlPath}, creating programmatic UI");
                CreateProgrammaticUI(root);
            }

            // スタイルシート適用（動的パス解決）
            var ussPath = GetUSSPath();
            MCPLogger.LogInfo($"[MCPServerManagerWindow] Attempting to load USS from: {ussPath}");
            
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
                MCPLogger.LogInfo("[MCPServerManagerWindow] Successfully applied USS styles");
            }
            else
            {
                MCPLogger.LogWarning($"[MCPServerManagerWindow] USS file not found at {ussPath}, using default styles");
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

            // 削除: CreateControlSection()は存在しないため削除

            // マルチプロジェクト情報セクション（折りたたみ式）
            var multiProjectFoldout = new Foldout();
            multiProjectFoldout.text = "🔗 Multi-Project Support (Phase 3)";
            multiProjectFoldout.value = true; // デフォルト展開
            multiProjectFoldout.style.marginBottom = 10;
            
            var multiProjectContent = new VisualElement();
            multiProjectContent.style.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.3f);
            multiProjectContent.style.borderBottomWidth = multiProjectContent.style.borderTopWidth =
                multiProjectContent.style.borderLeftWidth = multiProjectContent.style.borderRightWidth = 1;
            multiProjectContent.style.borderBottomColor = multiProjectContent.style.borderTopColor =
                multiProjectContent.style.borderLeftColor = multiProjectContent.style.borderRightColor = new Color(0.2f, 0.2f, 0.5f);
            multiProjectContent.style.paddingLeft = multiProjectContent.style.paddingRight =
                multiProjectContent.style.paddingTop = multiProjectContent.style.paddingBottom = 10;
            multiProjectContent.style.borderBottomLeftRadius = multiProjectContent.style.borderBottomRightRadius =
                multiProjectContent.style.borderTopLeftRadius = multiProjectContent.style.borderTopRightRadius = 5;

            // プロジェクトID表示
            _projectIdLabel = new Label("Project ID: Loading...");
            _projectIdLabel.name = "project-id";
            _projectIdLabel.style.fontSize = 11;
            _projectIdLabel.style.marginBottom = 3;
            multiProjectContent.Add(_projectIdLabel);

            // 割り当てポート表示
            _assignedPortLabel = new Label("Assigned Port: Loading...");
            _assignedPortLabel.name = "assigned-port";
            _assignedPortLabel.style.fontSize = 11;
            _assignedPortLabel.style.marginBottom = 3;
            multiProjectContent.Add(_assignedPortLabel);

            // Auto-Accept状態表示
            _autoApproveStatusLabel = new Label("Auto-Accept: Checking...");
            _autoApproveStatusLabel.name = "auto-approve-status";
            _autoApproveStatusLabel.style.fontSize = 11;
            _autoApproveStatusLabel.style.marginBottom = 3;
            multiProjectContent.Add(_autoApproveStatusLabel);

            // 削除：_portRegistryStatusLabelは新しいUI構造で不要

            // マルチプロジェクト操作ボタン行
            var multiProjectButtonRow = new VisualElement();
            multiProjectButtonRow.style.flexDirection = FlexDirection.Row;
            multiProjectButtonRow.style.flexShrink = 0;
            multiProjectButtonRow.style.height = 25;

            _refreshPortStatusButton = new Button() { text = "Refresh Status" };
            _refreshPortStatusButton.name = "refresh-port-status-button";
            _refreshPortStatusButton.style.flexGrow = 1;
            _refreshPortStatusButton.style.marginRight = 3;
            _refreshPortStatusButton.style.height = 23;
            multiProjectButtonRow.Add(_refreshPortStatusButton);

            _setupAutoApproveButton = new Button() { text = "Setup Auto-Accept" };
            _setupAutoApproveButton.name = "setup-auto-approve-button";
            _setupAutoApproveButton.style.flexGrow = 1;
            _setupAutoApproveButton.style.marginLeft = 3;
            _setupAutoApproveButton.style.marginRight = 3;
            _setupAutoApproveButton.style.height = 23;
            _setupAutoApproveButton.style.backgroundColor = new Color(0.2f, 0.5f, 0.8f);
            multiProjectButtonRow.Add(_setupAutoApproveButton);

            _generateMultiConfigButton = new Button() { text = "Generate Multi-Config" };
            _generateMultiConfigButton.name = "generate-multi-config-button";
            _generateMultiConfigButton.style.flexGrow = 1;
            _generateMultiConfigButton.style.marginLeft = 3;
            _generateMultiConfigButton.style.height = 23;
            _generateMultiConfigButton.style.backgroundColor = new Color(0.3f, 0.6f, 0.3f);
            multiProjectButtonRow.Add(_generateMultiConfigButton);

            multiProjectContent.Add(multiProjectButtonRow);
            multiProjectFoldout.Add(multiProjectContent);

            root.Add(multiProjectFoldout);

            // コントロールセクション
            var serverControlSection = new VisualElement();
            serverControlSection.style.marginBottom = 15;

            // ボタン行
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.marginBottom = 10;
            buttonRow.style.flexShrink = 0;  // ボタン行は縮まない
            buttonRow.style.height = 28;     // ボタン行高さ固定

            _startServerButton = new Button() { text = "Start Server" };
            _startServerButton.name = "start-button";
            _startServerButton.style.flexGrow = 1;
            _startServerButton.style.marginRight = 5;
            _startServerButton.style.height = 26;  // ボタン高さ固定
            buttonRow.Add(_startServerButton);

            _stopServerButton = new Button() { text = "Stop Server" };
            _stopServerButton.name = "stop-button";
            _stopServerButton.style.flexGrow = 1;
            _stopServerButton.style.marginLeft = 5;
            _stopServerButton.style.marginRight = 5;
            _stopServerButton.style.height = 26;  // ボタン高さ固定
            buttonRow.Add(_stopServerButton);

            _refreshButton = new Button() { text = "Refresh" };
            _refreshButton.name = "refresh-button";
            _refreshButton.style.flexGrow = 1;
            _refreshButton.style.marginLeft = 5;
            _refreshButton.style.marginRight = 5;
            _refreshButton.style.height = 26;  // ボタン高さ固定
            buttonRow.Add(_refreshButton);

            var testConnectionButton = new Button() { text = "Test Connection" };
            testConnectionButton.name = "test-connection-button";
            testConnectionButton.style.flexGrow = 1;
            testConnectionButton.style.marginLeft = 5;
            testConnectionButton.style.height = 26;  // ボタン高さ固定
            buttonRow.Add(testConnectionButton);

            serverControlSection.Add(buttonRow);

            // 設定フィールド
            var portFieldBase = new IntegerField("Port");
            portFieldBase.name = "port-field";
            portFieldBase.value = 3000;
            _portField = portFieldBase;
            serverControlSection.Add(portFieldBase);

            _serverPathField = new TextField("Server Path");
            _serverPathField.name = "server-path";
            _serverPathField.value = Path.Combine(Application.dataPath, "..", "..", "unity-mcp-node");
            serverControlSection.Add(_serverPathField);

            _autoStartToggle = new Toggle("Auto Start on Unity Launch");
            _autoStartToggle.name = "auto-start";
            _autoStartToggle.value = EditorPrefs.GetBool("UnityMCP_AutoStart", false);
            serverControlSection.Add(_autoStartToggle);

            root.Add(serverControlSection);

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
            dataButtonRow1.style.flexShrink = 0;  // ボタン行は縮まない
            dataButtonRow1.style.height = 25;     // ボタン行高さ固定

            _exportDataButton = new Button() { text = "Export Data" };
            _exportDataButton.name = "export-data-button";
            _exportDataButton.style.flexGrow = 1;
            _exportDataButton.style.marginRight = 3;
            _exportDataButton.style.height = 23;  // ボタン高さ固定
            dataButtonRow1.Add(_exportDataButton);

            _forceExportButton = new Button() { text = "Force Export" };
            _forceExportButton.name = "force-export-button";
            _forceExportButton.style.flexGrow = 1;
            _forceExportButton.style.marginLeft = 3;
            _forceExportButton.style.marginRight = 3;
            _forceExportButton.style.height = 23;  // ボタン高さ固定
            dataButtonRow1.Add(_forceExportButton);

            _clearDataButton = new Button() { text = "Clear Data" };
            _clearDataButton.name = "clear-data-button";
            _clearDataButton.style.flexGrow = 1;
            _clearDataButton.style.marginLeft = 3;
            _clearDataButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
            _clearDataButton.style.height = 23;  // ボタン高さ固定
            dataButtonRow1.Add(_clearDataButton);

            dataSection.Add(dataButtonRow1);

            // Data操作ボタン行2
            var dataButtonRow2 = new VisualElement();
            dataButtonRow2.style.flexDirection = FlexDirection.Row;
            dataButtonRow2.style.marginBottom = 5;
            dataButtonRow2.style.flexShrink = 0;  // ボタン行は縮まない
            dataButtonRow2.style.height = 25;     // ボタン行高さ固定

            _openDataFolderButton = new Button() { text = "Open Data Folder" };
            _openDataFolderButton.name = "open-folder-button";
            _openDataFolderButton.style.flexGrow = 1;
            _openDataFolderButton.style.marginRight = 3;
            _openDataFolderButton.style.height = 23;  // ボタン高さ固定
            dataButtonRow2.Add(_openDataFolderButton);

            _generateTestLogsButton = new Button() { text = "Generate Test Logs" };
            _generateTestLogsButton.name = "test-logs-button";
            _generateTestLogsButton.style.flexGrow = 1;
            _generateTestLogsButton.style.marginLeft = 3;
            _generateTestLogsButton.style.marginRight = 3;
            _generateTestLogsButton.style.height = 23;  // ボタン高さ固定
            dataButtonRow2.Add(_generateTestLogsButton);

            _clearConsoleButton = new Button() { text = "Clear Console" };
            _clearConsoleButton.name = "clear-console-button";
            _clearConsoleButton.style.flexGrow = 1;
            _clearConsoleButton.style.marginLeft = 3;
            _clearConsoleButton.style.height = 23;  // ボタン高さ固定
            dataButtonRow2.Add(_clearConsoleButton);

            dataSection.Add(dataButtonRow2);

            root.Add(dataSection);

            // ログセクション
            var logSection = new VisualElement();
            logSection.style.flexGrow = 1;
            logSection.style.minHeight = 200;  // 最小高さを保証
            logSection.style.maxHeight = StyleKeyword.None;  // 最大高さ制限を解除

            var logHeader = new Label("Server Logs");
            logHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            logHeader.style.marginBottom = 5;
            logHeader.style.flexShrink = 0;  // ヘッダーは縮まない
            logSection.Add(logHeader);

            _logScrollView = new ScrollView();
            _logScrollView.name = "log-scroll";
            _logScrollView.style.flexGrow = 1;
            _logScrollView.style.flexShrink = 1;  // 必要に応じて縮む
            _logScrollView.style.minHeight = 150;  // スクロールビューの最小高さ
            _logScrollView.style.height = StyleKeyword.Auto;  // 自動高さ
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
            logButtonRow.style.flexShrink = 0;  // ボタン行は縮まない
            logButtonRow.style.height = 25;     // ボタン高さ固定

            _copyLogsButton = new Button() { text = "Copy Logs" };
            _copyLogsButton.name = "copy-logs-button";
            _copyLogsButton.style.flexGrow = 1;
            _copyLogsButton.style.marginRight = 5;
            _copyLogsButton.style.height = 23;  // ボタン高さ固定
            logButtonRow.Add(_copyLogsButton);

            _clearLogsButton = new Button() { text = "Clear Logs" };
            _clearLogsButton.name = "clear-logs-button";
            _clearLogsButton.style.flexGrow = 1;
            _clearLogsButton.style.marginLeft = 5;
            _clearLogsButton.style.height = 23;  // ボタン高さ固定
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

            // 基本UI要素
            _statusLabel = root.Q<Label>("server-status");
            _connectionStatusLabel = root.Q<Label>("connection-status");
            _lastUpdateLabel = root.Q<Label>("last-update");
            
            // コントロールボタン
            _startServerButton = root.Q<Button>("start-button");
            _stopServerButton = root.Q<Button>("stop-button");
            _refreshButton = root.Q<Button>("refresh-button");
            _testConnectionButton = root.Q<Button>("test-connection-button");
            
            // 設定フィールド
            _portField = root.Q<IntegerField>("port-field");
            _serverPathField = root.Q<TextField>("server-path");
            _autoStartToggle = root.Q<Toggle>("auto-start");
            
            // ログ表示
            _logScrollView = root.Q<ScrollView>("log-scroll");

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

            // Phase 3 マルチプロジェクト機能UI要素 - Project ID Section
            _projectIdLabel = root.Q<Label>("project-id");
            _projectPathLabel = root.Q<Label>("project-path");
            _copyProjectIdButton = root.Q<Button>("copy-project-id-button");
            _regenerateProjectIdButton = root.Q<Button>("regenerate-project-id-button");
            
            // Port Management Section
            _assignedPortLabel = root.Q<Label>("assigned-port");
            _portStatusLabel = root.Q<Label>("port-status");
            _availablePortsCountLabel = root.Q<Label>("available-ports-count");
            _changePortButton = root.Q<Button>("change-port-button");
            _releasePortButton = root.Q<Button>("release-port-button");
            
            // Registry Status Section
            _autoApproveStatusLabel = root.Q<Label>("auto-approve-status");
            _registeredProjectsCountLabel = root.Q<Label>("registered-projects-count");
            _portRangeInfoLabel = root.Q<Label>("port-range-info");
            _viewRegistryDetailsButton = root.Q<Button>("view-registry-details-button");
            
            // Action Buttons
            _refreshPortStatusButton = root.Q<Button>("refresh-port-status-button");
            _setupAutoApproveButton = root.Q<Button>("setup-auto-approve-button");
            _generateMultiConfigButton = root.Q<Button>("generate-multi-config-button");
            
            // Phase 4.3 Auto-Accept Configuration UI要素
            _autoAcceptConfigStatusLabel = root.Q<Label>("auto-accept-config-status");
            _claudeConfigStatusLabel = root.Q<Label>("claude-config-status");
            _approvedToolsCountLabel = root.Q<Label>("approved-tools-count");
            _lastConfiguredInfoLabel = root.Q<Label>("last-configured-info");
            _enableAutoAcceptButton = root.Q<Button>("enable-auto-accept-button");
            _disableAutoAcceptButton = root.Q<Button>("disable-auto-accept-button");
            _checkAutoAcceptStatusButton = root.Q<Button>("check-auto-accept-status-button");
            _resetAutoAcceptButton = root.Q<Button>("reset-auto-accept-button");
            _openClaudeConfigButton = root.Q<Button>("open-claude-config-button");
            _viewAutoAcceptLogsButton = root.Q<Button>("view-auto-accept-logs-button");
            
            // Phase 4.4 Multi-Project Configuration Generator UI要素
            _configProjectNameLabel = root.Q<Label>("config-project-name");
            _configStatusOverviewLabel = root.Q<Label>("config-status-overview");
            _configServerPathLabel = root.Q<Label>("config-server-path");
            _targetServerDirLabel = root.Q<Label>("target-server-dir");
            _targetProjectRootLabel = root.Q<Label>("target-project-root");
            _targetClaudeDesktopLabel = root.Q<Label>("target-claude-desktop");
            _includeAutoAcceptToggle = root.Q<Toggle>("include-auto-accept-toggle");
            _packageDistributionToggle = root.Q<Toggle>("package-distribution-toggle");
            _createBackupToggle = root.Q<Toggle>("create-backup-toggle");
            _forceRegenerateToggle = root.Q<Toggle>("force-regenerate-toggle");
            _openServerDirButton = root.Q<Button>("open-server-dir-button");
            _openProjectRootButton = root.Q<Button>("open-project-root-button");
            _openClaudeDirButton = root.Q<Button>("open-claude-dir-button");
            _previewConfigButton = root.Q<Button>("preview-config-button");
            _validateSetupButton = root.Q<Button>("validate-setup-button");
            _checkExistingButton = root.Q<Button>("check-existing-button");
            _generateAllConfigsButton = root.Q<Button>("generate-all-configs-button");
            _generateServerOnlyButton = root.Q<Button>("generate-server-only-button");
            _cleanRegenerateButton = root.Q<Button>("clean-regenerate-button");
            
            // UXML要素が見つからない場合の警告（プログラマティックUI使用時）
            if (_statusLabel == null) MCPLogger.LogWarning("[MCPServerManagerWindow] server-status element not found");
            if (_startServerButton == null) MCPLogger.LogWarning("[MCPServerManagerWindow] start-button element not found");
        }

        private void LoadSettingsToUI()
        {
            if (_settings == null) return;

            // サーバーパス設定
            if (_serverPathField != null)
                _serverPathField.value = _settings.serverPath;

            // ポート設定
            if (_portField != null)
                _portField.value = _settings.defaultPort;

            // 自動起動設定
            if (_autoStartToggle != null)
                _autoStartToggle.value = _settings.autoStartOnLaunch;
        }

        private void SetupEventHandlers()
        {
            // 基本コントロールイベント
            if (_startServerButton != null) _startServerButton.clicked += StartServer;
            if (_stopServerButton != null) _stopServerButton.clicked += StopServer;
            if (_refreshButton != null) _refreshButton.clicked += RefreshStatus;
            if (_testConnectionButton != null) _testConnectionButton.clicked += TestConnection;

            // Data管理イベント
            if (_exportDataButton != null) _exportDataButton.clicked += ExportData;
            if (_forceExportButton != null) _forceExportButton.clicked += ForceExportData;
            if (_clearDataButton != null) _clearDataButton.clicked += ClearAllData;
            if (_openDataFolderButton != null) _openDataFolderButton.clicked += OpenDataFolder;
            if (_generateTestLogsButton != null) _generateTestLogsButton.clicked += GenerateTestLogs;
            if (_clearConsoleButton != null) _clearConsoleButton.clicked += ClearConsole;

            // ログ操作イベント
            if (_copyLogsButton != null) _copyLogsButton.clicked += CopyLogsToClipboard;
            if (_clearLogsButton != null) _clearLogsButton.clicked += ClearServerLogs;

            // Phase 3 マルチプロジェクト機能イベント
            if (_refreshPortStatusButton != null) _refreshPortStatusButton.clicked += RefreshMultiProjectStatus;
            if (_setupAutoApproveButton != null) _setupAutoApproveButton.clicked += SetupAutoApprove;
            if (_generateMultiConfigButton != null) _generateMultiConfigButton.clicked += GenerateMultiProjectConfig;
            
            // Phase 4.2 新しいUI要素のイベント
            if (_copyProjectIdButton != null) _copyProjectIdButton.clicked += CopyProjectIdToClipboard;
            if (_regenerateProjectIdButton != null) _regenerateProjectIdButton.clicked += RegenerateProjectId;
            if (_changePortButton != null) _changePortButton.clicked += ChangeAssignedPort;
            if (_releasePortButton != null) _releasePortButton.clicked += ReleaseCurrentPort;
            if (_viewRegistryDetailsButton != null) _viewRegistryDetailsButton.clicked += ViewRegistryDetails;

            // Phase 4.3 Auto-Accept Configuration イベント
            if (_enableAutoAcceptButton != null) _enableAutoAcceptButton.clicked += EnableAutoAcceptOneClick;
            if (_disableAutoAcceptButton != null) _disableAutoAcceptButton.clicked += DisableAutoAcceptOneClick;
            if (_checkAutoAcceptStatusButton != null) _checkAutoAcceptStatusButton.clicked += CheckAutoAcceptStatusDetailed;
            if (_resetAutoAcceptButton != null) _resetAutoAcceptButton.clicked += ResetAutoAcceptConfiguration;
            if (_openClaudeConfigButton != null) _openClaudeConfigButton.clicked += OpenClaudeConfigFile;
            if (_viewAutoAcceptLogsButton != null) _viewAutoAcceptLogsButton.clicked += ViewAutoAcceptLogs;

            // Phase 4.4 Multi-Project Configuration Generator イベント
            if (_openServerDirButton != null) _openServerDirButton.clicked += OpenServerDirectory;
            if (_openProjectRootButton != null) _openProjectRootButton.clicked += OpenProjectRootDirectory;
            if (_openClaudeDirButton != null) _openClaudeDirButton.clicked += OpenClaudeConfigDirectory;
            if (_previewConfigButton != null) _previewConfigButton.clicked += PreviewMultiProjectConfig;
            if (_validateSetupButton != null) _validateSetupButton.clicked += ValidateMultiProjectSetup;
            if (_checkExistingButton != null) _checkExistingButton.clicked += CheckExistingConfigurations;
            if (_generateAllConfigsButton != null) _generateAllConfigsButton.clicked += GenerateAllConfigurations;
            if (_generateServerOnlyButton != null) _generateServerOnlyButton.clicked += GenerateServerConfiguration;
            if (_cleanRegenerateButton != null) _cleanRegenerateButton.clicked += CleanAndRegenerateConfiguration;

            // 値変更イベント
            if (_autoStartToggle != null)
            {
                _autoStartToggle.RegisterValueChangedCallback(evt =>
                {
                    if (_settings != null)
                    {
                        _settings.autoStartOnLaunch = evt.newValue;
                        _settings.Save();
                    }
                });
            }

            if (_portField != null)
            {
                _portField.RegisterValueChangedCallback(evt =>
                {
                    _serverManager?.UpdatePort(evt.newValue);
                    if (_settings != null)
                    {
                        _settings.defaultPort = evt.newValue;
                        _settings.Save();
                    }
                });
            }

            if (_serverPathField != null)
            {
                _serverPathField.RegisterValueChangedCallback(evt =>
                {
                    if (_settings != null)
                    {
                        _settings.serverPath = evt.newValue;
                        _settings.Save();
                    }
                });
            }
        }

        private void StartServer()
        {
            AddLogEntry("Starting MCP Server...", LogType.Log);

            // パッケージから自動的にサーバーパスを取得
            AddLogEntry("Starting server path resolution...", LogType.Log);
            
            // パッケージ情報をデバッグ出力
            MCPPackageResolver.LogPackageInfo();
            
            var serverPath = MCPPackageResolver.GetServerPath();
            AddLogEntry($"Using server path: {serverPath}", LogType.Log);
            
            // UIフィールドを更新
            if (_serverPathField != null)
            {
                _serverPathField.SetValueWithoutNotify(serverPath);
            }
            
            if (!Directory.Exists(serverPath))
            {
                AddLogEntry($"Server path not found: {serverPath}", LogType.Error);
                
                // 詳細なデバッグ情報を出力
                var packagePath = MCPPackageResolver.GetPackageRootPath();
                AddLogEntry($"Package path: {packagePath}", LogType.Warning);
                AddLogEntry($"Looking for Server~ at: {serverPath}", LogType.Warning);
                
                // パッケージディレクトリの内容を確認
                try
                {
                    var packageDir = Path.GetDirectoryName(Application.dataPath);
                    var fullPackagePath = Path.GetFullPath(Path.Combine(packageDir, packagePath));
                    AddLogEntry($"Full package path: {fullPackagePath}", LogType.Warning);
                    
                    if (Directory.Exists(fullPackagePath))
                    {
                        var contents = Directory.GetDirectories(fullPackagePath);
                        AddLogEntry($"Package directory contents: {string.Join(", ", contents.Select(Path.GetFileName))}", LogType.Warning);
                        
                        // Server~以外の可能性もチェック
                        var serverDirs = contents.Where(d => Path.GetFileName(d).Contains("Server")).ToArray();
                        if (serverDirs.Length > 0)
                        {
                            AddLogEntry($"Found server-related directories: {string.Join(", ", serverDirs.Select(Path.GetFileName))}", LogType.Warning);
                        }
                    }
                    else
                    {
                        AddLogEntry($"Package directory does not exist: {fullPackagePath}", LogType.Error);
                    }
                }
                catch (Exception ex)
                {
                    AddLogEntry($"Error inspecting package directory: {ex.Message}", LogType.Error);
                }
                
                // フォールバックパスを試す
                var alternativePath = _serverPathField?.value;
                if (!string.IsNullOrEmpty(alternativePath) && Directory.Exists(alternativePath))
                {
                    AddLogEntry($"Trying fallback path: {alternativePath}", LogType.Warning);
                    serverPath = alternativePath;
                }
                else
                {
                    return;
                }
            }

            int port = _portField?.value ?? 3000;
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
                var port = _portField?.value ?? 3000;
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

            // マルチプロジェクト情報更新（10秒間隔に制限）
            if (Time.realtimeSinceStartup - _lastMultiProjectUpdateTime > 10.0f)
            {
                _lastMultiProjectUpdateTime = Time.realtimeSinceStartup;
                UpdateMultiProjectStatus();
                UpdateAutoAcceptConfigurationStatus();
                UpdateMultiProjectConfigGeneratorStatus();
            }
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

                    // サイズに応じて色を変更（正常(緑)、注意(黄)、警告(赤)）
                    if (sizeInKB > 100)
                    {
                        _dataStatusLabel.style.color = new Color(1f, 0.5f, 0.5f); // 警告(赤)
                    }
                    else if (sizeInKB > 50)
                    {
                        _dataStatusLabel.style.color = new Color(1f, 1f, 0.5f); // 注意(黄)
                    }
                    else
                    {
                        _dataStatusLabel.style.color = new Color(0.5f, 1f, 0.5f); // 正常(緑)
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

        // Phase 3 マルチプロジェクト機能メソッド群

        /// <summary>
        /// マルチプロジェクト状態情報を更新
        /// </summary>
        private void UpdateMultiProjectStatus()
        {
            try
            {
                // プロジェクトID取得・表示
                var projectId = MCPProjectIdentifier.GetProjectId();
                if (_projectIdLabel != null)
                {
                    _projectIdLabel.text = $"Project ID: {projectId.Substring(0, Math.Min(16, projectId.Length))}...";
                    _projectIdLabel.style.color = new Color(0.8f, 0.8f, 1f);
                }
                
                // プロジェクトパス表示
                if (_projectPathLabel != null)
                {
                    var projectPath = Application.dataPath.Replace("/Assets", "");
                    var pathParts = projectPath.Split('/');
                    var shortPath = pathParts.Length > 2 ? $".../{pathParts[pathParts.Length-2]}/{pathParts[pathParts.Length-1]}" : projectPath;
                    _projectPathLabel.text = $"Project Path: {shortPath}";
                    _projectPathLabel.style.color = new Color(0.7f, 0.7f, 0.9f);
                }

                // 割り当てポート取得・表示
                var assignedPort = MCPPortManager.GetAvailablePort();
                if (_assignedPortLabel != null)
                {
                    _assignedPortLabel.text = $"Assigned Port: {assignedPort}";
                    _assignedPortLabel.style.color = assignedPort == 3000 ? 
                        new Color(0.8f, 1f, 0.8f) : new Color(1f, 1f, 0.8f);
                }
                
                // ポート状態表示
                if (_portStatusLabel != null)
                {
                    var isPortAvailable = MCPPortManager.IsPortAvailable(assignedPort);
                    _portStatusLabel.text = $"Port Status: {(isPortAvailable ? "Available" : "In Use")}";
                    _portStatusLabel.style.color = isPortAvailable ? 
                        new Color(0.8f, 1f, 0.8f) : new Color(1f, 0.8f, 0.8f);
                }

                // Auto-Accept状態確認・表示
                var autoApproveStatusInfo = MCPAutoApproveSetup.GetAutoApproveStatus();
                var autoApproveEnabled = false;
                
                if (autoApproveStatusInfo != null && autoApproveStatusInfo.ContainsKey("enabled"))
                {
                    autoApproveEnabled = autoApproveStatusInfo["enabled"] is bool && (bool)autoApproveStatusInfo["enabled"];
                }
                if (_autoApproveStatusLabel != null)
                {
                    _autoApproveStatusLabel.text = $"Auto-Accept: {(autoApproveEnabled ? "✅ Configured" : "❌ Not Configured")}";
                    _autoApproveStatusLabel.style.color = autoApproveEnabled ? 
                        new Color(0.8f, 1f, 0.8f) : new Color(1f, 0.8f, 0.8f);
                }

                // レジストリ情報を新しいUI要素に反映
                var portStatus = MCPPortManager.GetPortStatus();
                var registeredProjects = 0;
                var availablePortsCount = 0;
                
                if (portStatus != null)
                {
                    registeredProjects = portStatus.ContainsKey("registeredProjects") && portStatus["registeredProjects"] is int ? (int)portStatus["registeredProjects"] : 0;
                    availablePortsCount = portStatus.ContainsKey("availablePortsCount") && portStatus["availablePortsCount"] is int ? (int)portStatus["availablePortsCount"] : 0;
                }
                
                if (_registeredProjectsCountLabel != null)
                {
                    _registeredProjectsCountLabel.text = $"Registered Projects: {registeredProjects}";
                    _registeredProjectsCountLabel.style.color = registeredProjects > 0 ? 
                        new Color(0.8f, 1f, 0.8f) : new Color(1f, 1f, 0.8f);
                }
                
                if (_availablePortsCountLabel != null)
                {
                    _availablePortsCountLabel.text = $"Available Ports: {availablePortsCount}/101";
                    _availablePortsCountLabel.style.color = availablePortsCount > 50 ? 
                        new Color(0.8f, 1f, 0.8f) : availablePortsCount > 20 ? 
                        new Color(1f, 1f, 0.8f) : new Color(1f, 0.8f, 0.8f);
                }
            }
            catch (Exception ex)
            {
                if (_projectIdLabel != null)
                {
                    _projectIdLabel.text = $"Project ID: Error - {ex.Message}";
                    _projectIdLabel.style.color = new Color(1f, 0.5f, 0.5f);
                }
                AddLogEntry($"Multi-project status update failed: {ex.Message}", LogType.Error);
            }
        }

        /// <summary>
        /// マルチプロジェクト状態を手動で更新
        /// </summary>
        private void RefreshMultiProjectStatus()
        {
            try
            {
                AddLogEntry("Refreshing multi-project status...", LogType.Log);
                
                // ポートレジストリクリーンアップ
                MCPPortManager.CleanupRegistry();
                
                // 状態更新
                UpdateMultiProjectStatus();
                
                AddLogEntry("Multi-project status refreshed successfully", LogType.Log);
            }
            catch (Exception ex)
            {
                AddLogEntry($"Failed to refresh multi-project status: {ex.Message}", LogType.Error);
            }
        }

        /// <summary>
        /// Auto-Accept機能をセットアップ
        /// </summary>
        private void SetupAutoApprove()
        {
            try
            {
                AddLogEntry("Setting up Auto-Accept configuration...", LogType.Log);
                
                var statusInfo = MCPAutoApproveSetup.GetAutoApproveStatus();
                var isEnabled = statusInfo.ContainsKey("enabled") ? (bool)statusInfo["enabled"] : false;
                
                if (isEnabled)
                {
                    if (EditorUtility.DisplayDialog("Auto-Accept Already Configured",
                        "Auto-Accept is already configured for Claude Code CLI.\n\nDo you want to reconfigure it?",
                        "Reconfigure", "Cancel"))
                    {
                        MCPAutoApproveSetup.ConfigureAutoApprove(true);
                        AddLogEntry("Auto-Accept reconfigured successfully", LogType.Log);
                    }
                    else
                    {
                        AddLogEntry("Auto-Accept setup cancelled", LogType.Log);
                        return;
                    }
                }
                else
                {
                    MCPAutoApproveSetup.ConfigureAutoApprove(true);
                    AddLogEntry("Auto-Accept configured successfully for Claude Code CLI", LogType.Log);
                    
                    EditorUtility.DisplayDialog("Auto-Accept Setup Complete",
                        "Auto-Accept has been configured successfully!\n\nClaude Code CLI will now automatically accept MCP connections from this project.",
                        "OK");
                }
                
                // 状態更新
                UpdateMultiProjectStatus();
            }
            catch (Exception ex)
            {
                AddLogEntry($"Failed to setup Auto-Accept: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Auto-Accept Setup Failed",
                    $"Failed to configure Auto-Accept:\n\n{ex.Message}",
                    "OK");
            }
        }

        /// <summary>
        /// マルチプロジェクト設定ファイルを生成
        /// </summary>
        private void GenerateMultiProjectConfig()
        {
            try
            {
                AddLogEntry("Generating multi-project configuration...", LogType.Log);
                
                var projectId = MCPProjectIdentifier.GetProjectId();
                var assignedPort = MCPPortManager.GetAvailablePort();
                
                // マルチプロジェクト設定確認・修復
                MCPConfigGenerator.ValidateAndFixMultiProjectSetup();
                
                // プロジェクト設定ファイル生成
                MCPConfigGenerator.GenerateProjectConfig(true);
                
                AddLogEntry($"Multi-project configuration generated for project {projectId} on port {assignedPort}", LogType.Log);
                
                EditorUtility.DisplayDialog("Multi-Project Config Generated",
                    $"Configuration files have been generated successfully!\n\nProject ID: {projectId}\nPort: {assignedPort}\n\nConfig files are available in both Server~ directory and project root.",
                    "OK");
                
                // 状態更新
                UpdateMultiProjectStatus();
            }
            catch (Exception ex)
            {
                AddLogEntry($"Failed to generate multi-project config: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Config Generation Failed",
                    $"Failed to generate multi-project configuration:\n\n{ex.Message}",
                    "OK");
            }
        }
        
        // Phase 4.2 新機能メソッド群
        
        /// <summary>
        /// プロジェクトIDをクリップボードにコピー
        /// </summary>
        private void CopyProjectIdToClipboard()
        {
            try
            {
                var projectId = MCPProjectIdentifier.GetProjectId();
                EditorGUIUtility.systemCopyBuffer = projectId;
                AddLogEntry($"Project ID copied to clipboard: {projectId}", LogType.Log);
                
                EditorUtility.DisplayDialog("Project ID Copied",
                    $"Project ID has been copied to clipboard:\n\n{projectId}",
                    "OK");
            }
            catch (Exception ex)
            {
                AddLogEntry($"Failed to copy project ID: {ex.Message}", LogType.Error);
            }
        }
        
        /// <summary>
        /// プロジェクトIDを再生成
        /// </summary>
        private void RegenerateProjectId()
        {
            try
            {
                if (EditorUtility.DisplayDialog("Regenerate Project ID",
                    "Are you sure you want to regenerate the project ID?\n\nThis will create a new unique identifier for this project and may affect multi-project configurations.",
                    "Regenerate", "Cancel"))
                {
                    // プロジェクトID再生成（キャッシュクリア）
                    var oldProjectId = MCPProjectIdentifier.GetProjectId();
                    
                    // キャッシュをクリアして新しいIDを強制生成
                    // MCPProjectIdentifierのキャッシュをクリアするためにReflectionを使用
                    var type = typeof(MCPProjectIdentifier);
                    var cachedIdField = type.GetField("_cachedProjectId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    var cachedPathField = type.GetField("_cachedProjectPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    
                    if (cachedIdField != null) cachedIdField.SetValue(null, null);
                    if (cachedPathField != null) cachedPathField.SetValue(null, null);
                    
                    var newProjectId = MCPProjectIdentifier.GetProjectId();
                    
                    AddLogEntry($"Project ID regenerated: {oldProjectId} → {newProjectId}", LogType.Log);
                    
                    // UI更新
                    UpdateMultiProjectStatus();
                    
                    EditorUtility.DisplayDialog("Project ID Regenerated",
                        $"Project ID has been regenerated successfully!\n\nOld ID: {oldProjectId}\nNew ID: {newProjectId}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"Failed to regenerate project ID: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Regeneration Failed",
                    $"Failed to regenerate project ID:\n\n{ex.Message}",
                    "OK");
            }
        }
        
        /// <summary>
        /// 割り当てポートを変更
        /// </summary>
        private void ChangeAssignedPort()
        {
            try
            {
                var currentPort = MCPPortManager.GetAvailablePort();
                if (EditorUtility.DisplayDialog("Change Assigned Port",
                    $"Current assigned port: {currentPort}\n\nDo you want to auto-assign a new port?",
                    "Auto-assign New Port", "Cancel"))
                {
                    // 現在のポートを解放してから新しいポートを取得
                    MCPPortManager.ReleasePort();
                    var newPort = MCPPortManager.GetAvailablePort();
                    
                    AddLogEntry($"Port changed: {currentPort} → {newPort}", LogType.Log);
                    UpdateMultiProjectStatus();
                    
                    EditorUtility.DisplayDialog("Port Changed",
                        $"Port has been changed successfully!\n\nOld Port: {currentPort}\nNew Port: {newPort}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"Failed to change port: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Port Change Failed",
                    $"Failed to change assigned port:\n\n{ex.Message}",
                    "OK");
            }
        }
        
        /// <summary>
        /// 現在のポートを解放
        /// </summary>
        private void ReleaseCurrentPort()
        {
            try
            {
                var currentPort = MCPPortManager.GetAvailablePort();
                if (EditorUtility.DisplayDialog("Release Current Port",
                    $"Are you sure you want to release port {currentPort}?\n\nThis will make the port available for other projects.",
                    "Release", "Cancel"))
                {
                    MCPPortManager.ReleasePort();
                    AddLogEntry($"Port {currentPort} released successfully", LogType.Log);
                    
                    // UI更新
                    UpdateMultiProjectStatus();
                    
                    EditorUtility.DisplayDialog("Port Released",
                        $"Port {currentPort} has been released successfully!",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"Failed to release port: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Port Release Failed",
                    $"Failed to release current port:\n\n{ex.Message}",
                    "OK");
            }
        }
        
        /// <summary>
        /// レジストリ詳細を表示
        /// </summary>
        private void ViewRegistryDetails()
        {
            try
            {
                var portStatus = MCPPortManager.GetPortStatus();
                var details = new System.Text.StringBuilder();
                
                details.AppendLine("=== Multi-Project Registry Details ===\n");
                details.AppendLine($"Current Project ID: {MCPProjectIdentifier.GetProjectId()}");
                details.AppendLine($"Assigned Port: {MCPPortManager.GetAvailablePort()}");
                details.AppendLine($"Port Range: 3000-3100 (101 total ports)\n");
                
                if (portStatus.ContainsKey("registryDetails"))
                {
                    var registryDetails = portStatus["registryDetails"] as System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>;
                    if (registryDetails != null && registryDetails.Count > 0)
                    {
                        details.AppendLine("=== Registered Projects ===");
                        foreach (var project in registryDetails)
                        {
                            var projectId = project.ContainsKey("projectId") ? project["projectId"].ToString() : "Unknown";
                            var port = project.ContainsKey("port") ? project["port"].ToString() : "Unknown";
                            details.AppendLine($"• {projectId.Substring(0, Math.Min(8, projectId.Length))}... → Port {port}");
                        }
                    }
                    else
                    {
                        details.AppendLine("=== Registered Projects ===");
                        details.AppendLine("No other projects registered.");
                    }
                }
                
                details.AppendLine($"\n=== Port Statistics ===");
                var availablePortsCountDetails = portStatus != null && portStatus.ContainsKey("availablePortsCount") && portStatus["availablePortsCount"] is int ? (int)portStatus["availablePortsCount"] : 0;
                var registeredProjectsDetails = portStatus != null && portStatus.ContainsKey("registeredProjects") && portStatus["registeredProjects"] is int ? (int)portStatus["registeredProjects"] : 0;
                var timestampDetails = portStatus != null && portStatus.ContainsKey("timestamp") ? portStatus["timestamp"].ToString() : "Unknown";
                
                details.AppendLine($"Available Ports: {availablePortsCountDetails}");
                details.AppendLine($"Registered Projects: {registeredProjectsDetails}");
                details.AppendLine($"Last Updated: {timestampDetails}");
                
                EditorUtility.DisplayDialog("Registry Details", details.ToString(), "OK");
                AddLogEntry("Registry details displayed", LogType.Log);
            }
            catch (Exception ex)
            {
                AddLogEntry($"Failed to view registry details: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("View Details Failed",
                    $"Failed to view registry details:\n\n{ex.Message}",
                    "OK");
            }
        }
        
        // Phase 4.3 Auto-Accept Configuration メソッド群
        
        /// <summary>
        /// Auto-Accept設定状態を更新
        /// </summary>
        private void UpdateAutoAcceptConfigurationStatus()
        {
            try
            {
                var status = MCPAutoApproveSetup.GetAutoApproveStatus();
                
                // Configuration Status更新
                if (_autoAcceptConfigStatusLabel != null)
                {
                    var enabled = status != null && status.ContainsKey("enabled") && status["enabled"] is bool && (bool)status["enabled"];
                    _autoAcceptConfigStatusLabel.text = $"Status: {(enabled ? "✅ Enabled" : "❌ Disabled")}";
                    _autoAcceptConfigStatusLabel.style.color = enabled ? 
                        new Color(0.8f, 1f, 0.8f) : new Color(1f, 0.8f, 0.8f);
                }
                
                // Claude Config Status更新
                if (_claudeConfigStatusLabel != null)
                {
                    var claudeStatus = status.ContainsKey("claudeDesktopStatus") ? 
                        status["claudeDesktopStatus"] as System.Collections.Generic.Dictionary<string, object> : 
                        new System.Collections.Generic.Dictionary<string, object>();
                    
                    var configExists = false;
                    var hasValidJson = false;
                    
                    if (claudeStatus != null)
                    {
                        configExists = claudeStatus.ContainsKey("configExists") && claudeStatus["configExists"] is bool && (bool)claudeStatus["configExists"];
                        hasValidJson = claudeStatus.ContainsKey("hasValidJson") && claudeStatus["hasValidJson"] is bool && (bool)claudeStatus["hasValidJson"];
                    }
                    
                    if (configExists && hasValidJson)
                    {
                        _claudeConfigStatusLabel.text = "Claude Config: ✅ Found & Valid";
                        _claudeConfigStatusLabel.style.color = new Color(0.8f, 1f, 0.8f);
                    }
                    else if (configExists)
                    {
                        _claudeConfigStatusLabel.text = "Claude Config: ⚠️ Found but Invalid";
                        _claudeConfigStatusLabel.style.color = new Color(1f, 1f, 0.8f);
                    }
                    else
                    {
                        _claudeConfigStatusLabel.text = "Claude Config: ❌ Not Found";
                        _claudeConfigStatusLabel.style.color = new Color(1f, 0.8f, 0.8f);
                    }
                }
                
                // Approved Tools Count更新
                if (_approvedToolsCountLabel != null)
                {
                    var toolsCount = status != null && status.ContainsKey("approvedToolsCount") && status["approvedToolsCount"] is int ? (int)status["approvedToolsCount"] : 0;
                    _approvedToolsCountLabel.text = $"Approved Tools: {toolsCount} tools configured";
                    _approvedToolsCountLabel.style.color = toolsCount > 0 ? 
                        new Color(0.8f, 1f, 0.8f) : new Color(1f, 0.8f, 0.8f);
                }
                
                // Last Configured情報更新
                if (_lastConfiguredInfoLabel != null)
                {
                    var lastConfigured = status != null && status.ContainsKey("lastConfigured") ? status["lastConfigured"].ToString() : "";
                    if (!string.IsNullOrEmpty(lastConfigured))
                    {
                        try
                        {
                            var configuredTime = DateTime.Parse(lastConfigured);
                            _lastConfiguredInfoLabel.text = $"Last Configured: {configuredTime:yyyy-MM-dd HH:mm}";
                            _lastConfiguredInfoLabel.style.color = new Color(0.8f, 0.8f, 1f);
                        }
                        catch
                        {
                            _lastConfiguredInfoLabel.text = $"Last Configured: {lastConfigured}";
                            _lastConfiguredInfoLabel.style.color = new Color(0.8f, 0.8f, 1f);
                        }
                    }
                    else
                    {
                        _lastConfiguredInfoLabel.text = "Last Configured: Never";
                        _lastConfiguredInfoLabel.style.color = new Color(1f, 0.8f, 0.8f);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_autoAcceptConfigStatusLabel != null)
                {
                    _autoAcceptConfigStatusLabel.text = $"Status: Error - {ex.Message}";
                    _autoAcceptConfigStatusLabel.style.color = new Color(1f, 0.5f, 0.5f);
                }
                AddLogEntry($"Auto-Accept configuration status update failed: {ex.Message}", LogType.Error);
            }
        }
        
        /// <summary>
        /// Auto-Acceptをワンクリック有効化
        /// </summary>
        private void EnableAutoAcceptOneClick()
        {
            try
            {
                AddLogEntry("🚀 Enabling Auto-Accept configuration with one-click setup...", LogType.Log);
                
                var success = MCPAutoApproveSetup.ConfigureAutoApprove(true);
                
                if (success)
                {
                    AddLogEntry("✅ Auto-Accept enabled successfully - Claude Code CLI will skip confirmation dialogs!", LogType.Log);
                    EditorUtility.DisplayDialog("Auto-Accept Enabled", 
                        "🚀 Auto-Accept has been enabled successfully!\n\n✅ Claude Code CLI will now automatically approve MCP tool requests\n⚡ Instant development workflow activated\n🛠️ 8 development tools pre-approved", 
                        "Awesome!");
                }
                else
                {
                    AddLogEntry("⚠️ Auto-Accept setup completed with warnings - check logs for details", LogType.Warning);
                    EditorUtility.DisplayDialog("Auto-Accept Setup Warning", 
                        "Auto-Accept setup completed but with some warnings.\n\nPlease check the console logs for details.", 
                        "OK");
                }
                
                // UI更新
                UpdateAutoAcceptConfigurationStatus();
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to enable Auto-Accept: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Auto-Accept Setup Failed", 
                    $"Failed to enable Auto-Accept configuration:\n\n{ex.Message}\n\nPlease check Claude Desktop configuration manually.", 
                    "OK");
            }
        }
        
        /// <summary>
        /// Auto-Acceptをワンクリック無効化
        /// </summary>
        private void DisableAutoAcceptOneClick()
        {
            try
            {
                if (EditorUtility.DisplayDialog("Disable Auto-Accept", 
                    "Are you sure you want to disable Auto-Accept?\n\nClaude Code CLI will return to showing confirmation dialogs for all MCP tool requests.", 
                    "Disable", "Cancel"))
                {
                    AddLogEntry("❌ Disabling Auto-Accept configuration...", LogType.Log);
                    
                    var success = MCPAutoApproveSetup.ConfigureAutoApprove(false);
                    
                    if (success)
                    {
                        AddLogEntry("✅ Auto-Accept disabled successfully - Claude Code CLI will show confirmation dialogs", LogType.Log);
                        EditorUtility.DisplayDialog("Auto-Accept Disabled", 
                            "Auto-Accept has been disabled successfully.\n\nClaude Code CLI will now show confirmation dialogs for all MCP tool requests.", 
                            "OK");
                    }
                    else
                    {
                        AddLogEntry("⚠️ Auto-Accept disable completed with warnings - check logs for details", LogType.Warning);
                    }
                    
                    // UI更新
                    UpdateAutoAcceptConfigurationStatus();
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to disable Auto-Accept: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Auto-Accept Disable Failed", 
                    $"Failed to disable Auto-Accept configuration:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// Auto-Accept状態を詳細チェック
        /// </summary>
        private void CheckAutoAcceptStatusDetailed()
        {
            try
            {
                AddLogEntry("🔍 Checking Auto-Accept configuration status...", LogType.Log);
                
                var status = MCPAutoApproveSetup.GetAutoApproveStatus();
                var details = new System.Text.StringBuilder();
                
                details.AppendLine("=== Auto-Accept Configuration Status ===\n");
                
                var enabled = status.ContainsKey("enabled") ? (bool)status["enabled"] : false;
                var projectId = status.ContainsKey("projectId") ? status["projectId"].ToString() : "Unknown";
                var toolsCount = status.ContainsKey("approvedToolsCount") ? (int)status["approvedToolsCount"] : 0;
                
                details.AppendLine($"Project ID: {projectId}");
                details.AppendLine($"Auto-Accept Status: {(enabled ? "✅ Enabled" : "❌ Disabled")}");
                details.AppendLine($"Approved Tools: {toolsCount} tools configured\n");
                
                if (status.ContainsKey("claudeDesktopStatus"))
                {
                    var claudeStatus = status["claudeDesktopStatus"] as System.Collections.Generic.Dictionary<string, object>;
                    details.AppendLine("=== Claude Desktop Configuration ===");
                    
                    if (claudeStatus != null)
                    {
                        var configExists = claudeStatus.ContainsKey("configExists") && claudeStatus["configExists"] is bool && (bool)claudeStatus["configExists"];
                        var hasValidJson = claudeStatus.ContainsKey("hasValidJson") && claudeStatus["hasValidJson"] is bool && (bool)claudeStatus["hasValidJson"];
                        var hasMcpServers = claudeStatus.ContainsKey("hasMcpServers") && claudeStatus["hasMcpServers"] is bool && (bool)claudeStatus["hasMcpServers"];
                        var hasProjectServer = claudeStatus.ContainsKey("hasProjectServer") && claudeStatus["hasProjectServer"] is bool && (bool)claudeStatus["hasProjectServer"];
                        
                        details.AppendLine($"Config File Exists: {configExists}");
                        details.AppendLine($"Valid JSON: {hasValidJson}");
                        details.AppendLine($"Has MCP Servers: {hasMcpServers}");
                        details.AppendLine($"Project Server Configured: {hasProjectServer}\n");
                    }
                    else
                    {
                        details.AppendLine("Claude Desktop status unavailable\n");
                    }
                }
                
                if (status.ContainsKey("approvedTools"))
                {
                    var tools = status["approvedTools"] as string[];
                    if (tools != null && tools.Length > 0)
                    {
                        details.AppendLine("=== Auto-Approved Tools ===");
                        foreach (var tool in tools)
                        {
                            details.AppendLine($"• {tool}");
                        }
                        details.AppendLine();
                    }
                }
                
                var lastConfigured = status.ContainsKey("lastConfigured") ? status["lastConfigured"].ToString() : "";
                if (!string.IsNullOrEmpty(lastConfigured))
                {
                    details.AppendLine($"Last Configured: {lastConfigured}");
                }
                else
                {
                    details.AppendLine("Last Configured: Never");
                }
                
                EditorUtility.DisplayDialog("Auto-Accept Status Details", details.ToString(), "OK");
                AddLogEntry("✅ Auto-Accept status check completed", LogType.Log);
                
                // UI更新
                UpdateAutoAcceptConfigurationStatus();
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to check Auto-Accept status: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Status Check Failed", 
                    $"Failed to check Auto-Accept status:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// Auto-Accept設定をリセット
        /// </summary>
        private void ResetAutoAcceptConfiguration()
        {
            try
            {
                if (EditorUtility.DisplayDialog("Reset Auto-Accept Configuration", 
                    "Are you sure you want to reset Auto-Accept configuration?\n\nThis will disable Auto-Accept and clear all related settings.", 
                    "Reset", "Cancel"))
                {
                    AddLogEntry("🔄 Resetting Auto-Accept configuration...", LogType.Log);
                    
                    var success = MCPAutoApproveSetup.ResetAutoApprove();
                    
                    if (success)
                    {
                        AddLogEntry("✅ Auto-Accept configuration reset successfully", LogType.Log);
                        EditorUtility.DisplayDialog("Configuration Reset", 
                            "Auto-Accept configuration has been reset successfully.\n\nAll settings have been cleared and Auto-Accept is now disabled.", 
                            "OK");
                    }
                    else
                    {
                        AddLogEntry("⚠️ Auto-Accept reset completed with warnings - check logs for details", LogType.Warning);
                    }
                    
                    // UI更新
                    UpdateAutoAcceptConfigurationStatus();
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to reset Auto-Accept configuration: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Reset Failed", 
                    $"Failed to reset Auto-Accept configuration:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// Claude設定ファイルを開く
        /// </summary>
        private void OpenClaudeConfigFile()
        {
            try
            {
                var status = MCPAutoApproveSetup.GetAutoApproveStatus();
                var configPath = status.ContainsKey("claudeConfigPath") ? status["claudeConfigPath"].ToString() : "";
                
                if (!string.IsNullOrEmpty(configPath))
                {
                    var expandedPath = configPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    
                    if (File.Exists(expandedPath))
                    {
                        EditorUtility.RevealInFinder(expandedPath);
                        AddLogEntry($"📂 Opened Claude Desktop config file: {expandedPath}", LogType.Log);
                    }
                    else
                    {
                        AddLogEntry($"⚠️ Claude Desktop config file not found: {expandedPath}", LogType.Warning);
                        EditorUtility.DisplayDialog("Config File Not Found", 
                            $"Claude Desktop configuration file not found at:\n\n{expandedPath}\n\nPlease ensure Claude Desktop is installed.", 
                            "OK");
                    }
                }
                else
                {
                    AddLogEntry("❌ Could not determine Claude Desktop config file path", LogType.Error);
                    EditorUtility.DisplayDialog("Config Path Unknown", 
                        "Could not determine Claude Desktop configuration file path.\n\nPlease check if Claude Desktop is properly installed.", 
                        "OK");
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to open Claude Desktop config: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Open Config Failed", 
                    $"Failed to open Claude Desktop configuration:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// Auto-Acceptログを表示
        /// </summary>
        private void ViewAutoAcceptLogs()
        {
            try
            {
                AddLogEntry("📋 Displaying Auto-Accept configuration logs...", LogType.Log);
                
                var logs = new System.Text.StringBuilder();
                logs.AppendLine("=== Auto-Accept Configuration Logs ===\n");
                
                // ログセクションからAuto-Accept関連のエントリを抽出
                var autoAcceptLogs = new System.Collections.Generic.List<string>();
                foreach (VisualElement child in _logScrollView.contentContainer.Children())
                {
                    if (child is Label label && (label.text.Contains("Auto-Accept") || label.text.Contains("auto-accept") || label.text.Contains("MCPAutoApproveSetup")))
                    {
                        autoAcceptLogs.Add(label.text);
                    }
                }
                
                if (autoAcceptLogs.Count > 0)
                {
                    logs.AppendLine("Recent Auto-Accept Activity:");
                    foreach (var logEntry in autoAcceptLogs.Take(10)) // 最新10件
                    {
                        logs.AppendLine($"• {logEntry}");
                    }
                    
                    if (autoAcceptLogs.Count > 10)
                    {
                        logs.AppendLine($"\n... and {autoAcceptLogs.Count - 10} more entries");
                    }
                }
                else
                {
                    logs.AppendLine("No Auto-Accept related logs found in current session.");
                    logs.AppendLine("\nTip: Try configuring Auto-Accept to see activity logs here.");
                }
                
                logs.AppendLine("\nFor detailed logs, check Unity Console and Claude Desktop logs.");
                
                EditorUtility.DisplayDialog("Auto-Accept Logs", logs.ToString(), "OK");
                AddLogEntry("✅ Auto-Accept logs displayed", LogType.Log);
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to view Auto-Accept logs: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("View Logs Failed", 
                    $"Failed to view Auto-Accept logs:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        // Phase 4.4 Multi-Project Configuration Generator メソッド群
        
        /// <summary>
        /// Multi-Project Configuration Generator の状態を更新
        /// </summary>
        private void UpdateMultiProjectConfigGeneratorStatus()
        {
            try
            {
                var projectInfo = MCPProjectIdentifier.GetProjectInfo();
                var configInfo = MCPConfigGenerator.GetProjectConfigInfo();
                
                // Configuration Overview更新
                if (_configProjectNameLabel != null)
                {
                    var projectName = projectInfo.ContainsKey("projectName") ? projectInfo["projectName"].ToString() : "Unknown";
                    _configProjectNameLabel.text = $"Project Name: {projectName}";
                    _configProjectNameLabel.style.color = new Color(0.8f, 0.8f, 1f);
                }
                
                if (_configStatusOverviewLabel != null)
                {
                    var serverConfigExists = configInfo.ContainsKey("serverConfigPath") && File.Exists(configInfo["serverConfigPath"].ToString());
                    var rootConfigExists = configInfo.ContainsKey("rootConfigPath") && File.Exists(configInfo["rootConfigPath"].ToString());
                    
                    var status = serverConfigExists && rootConfigExists ? "✅ Complete" : 
                                serverConfigExists || rootConfigExists ? "⚠️ Partial" : "❌ Missing";
                    
                    _configStatusOverviewLabel.text = $"Config Status: {status}";
                    _configStatusOverviewLabel.style.color = serverConfigExists && rootConfigExists ? 
                        new Color(0.8f, 1f, 0.8f) : serverConfigExists || rootConfigExists ? 
                        new Color(1f, 1f, 0.8f) : new Color(1f, 0.8f, 0.8f);
                }
                
                if (_configServerPathLabel != null)
                {
                    var packagePath = MCPPackageResolver.GetPackagePath();
                    var shortPath = $".../{Path.GetFileName(Path.GetDirectoryName(packagePath))}/{Path.GetFileName(packagePath)}";
                    _configServerPathLabel.text = $"Server Path: {shortPath}";
                    _configServerPathLabel.style.color = new Color(0.7f, 0.9f, 0.7f);
                }
                
                // Target Locations更新
                if (_targetServerDirLabel != null)
                {
                    var serverDir = Path.Combine(MCPPackageResolver.GetPackagePath(), "Server~");
                    var serverDirExists = Directory.Exists(serverDir);
                    _targetServerDirLabel.text = $"Server Directory: {(serverDirExists ? "✅ Found" : "❌ Missing")}";
                    _targetServerDirLabel.style.color = serverDirExists ? new Color(0.8f, 1f, 0.8f) : new Color(1f, 0.8f, 0.8f);
                }
                
                if (_targetProjectRootLabel != null)
                {
                    var projectRoot = Application.dataPath.Replace("/Assets", "");
                    _targetProjectRootLabel.text = $"Project Root: {Path.GetFileName(projectRoot)}";
                    _targetProjectRootLabel.style.color = new Color(0.8f, 0.8f, 1f);
                }
                
                if (_targetClaudeDesktopLabel != null)
                {
                    var claudeConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                        "Library/Application Support/Claude/claude_desktop_config.json");
                    var claudeConfigExists = File.Exists(claudeConfigPath);
                    _targetClaudeDesktopLabel.text = $"Claude Desktop: {(claudeConfigExists ? "✅ Found" : "❌ Missing")}";
                    _targetClaudeDesktopLabel.style.color = claudeConfigExists ? new Color(0.8f, 1f, 0.8f) : new Color(1f, 0.8f, 0.8f);
                }
            }
            catch (Exception ex)
            {
                if (_configStatusOverviewLabel != null)
                {
                    _configStatusOverviewLabel.text = $"Config Status: Error - {ex.Message}";
                    _configStatusOverviewLabel.style.color = new Color(1f, 0.5f, 0.5f);
                }
                AddLogEntry($"Multi-Project Configuration Generator status update failed: {ex.Message}", LogType.Error);
            }
        }
        
        /// <summary>
        /// サーバーディレクトリを開く
        /// </summary>
        private void OpenServerDirectory()
        {
            try
            {
                var serverDir = Path.Combine(MCPPackageResolver.GetPackagePath(), "Server~");
                if (Directory.Exists(serverDir))
                {
                    EditorUtility.RevealInFinder(serverDir);
                    AddLogEntry($"📂 Opened server directory: {serverDir}", LogType.Log);
                }
                else
                {
                    EditorUtility.DisplayDialog("Directory Not Found", 
                        $"Server directory not found:\n\n{serverDir}", 
                        "OK");
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to open server directory: {ex.Message}", LogType.Error);
            }
        }
        
        /// <summary>
        /// プロジェクトルートディレクトリを開く
        /// </summary>
        private void OpenProjectRootDirectory()
        {
            try
            {
                var projectRoot = Application.dataPath.Replace("/Assets", "");
                EditorUtility.RevealInFinder(projectRoot);
                AddLogEntry($"📂 Opened project root: {projectRoot}", LogType.Log);
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to open project root: {ex.Message}", LogType.Error);
            }
        }
        
        /// <summary>
        /// Claude設定ディレクトリを開く
        /// </summary>
        private void OpenClaudeConfigDirectory()
        {
            try
            {
                var claudeDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                    "Library/Application Support/Claude");
                if (Directory.Exists(claudeDir))
                {
                    EditorUtility.RevealInFinder(claudeDir);
                    AddLogEntry($"📂 Opened Claude config directory: {claudeDir}", LogType.Log);
                }
                else
                {
                    EditorUtility.DisplayDialog("Directory Not Found", 
                        $"Claude Desktop configuration directory not found:\n\n{claudeDir}\n\nPlease ensure Claude Desktop is installed.", 
                        "OK");
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to open Claude config directory: {ex.Message}", LogType.Error);
            }
        }
        
        /// <summary>
        /// マルチプロジェクト設定をプレビュー
        /// </summary>
        private void PreviewMultiProjectConfig()
        {
            try
            {
                AddLogEntry("📋 Generating multi-project configuration preview...", LogType.Log);
                
                var configInfo = MCPConfigGenerator.GetProjectConfigInfo();
                var preview = new System.Text.StringBuilder();
                
                preview.AppendLine("=== Multi-Project Configuration Preview ===\n");
                
                if (configInfo.ContainsKey("project"))
                {
                    var projectInfo = configInfo["project"] as Dictionary<string, object>;
                    preview.AppendLine("📋 Project Information:");
                    preview.AppendLine($"  • Project ID: {projectInfo["projectId"]}");
                    preview.AppendLine($"  • Project Name: {projectInfo["projectName"]}");
                    preview.AppendLine($"  • Project Path: {projectInfo["projectPath"]}\n");
                }
                
                if (configInfo.ContainsKey("port"))
                {
                    var portInfo = configInfo["port"] as Dictionary<string, object>;
                    preview.AppendLine("🔌 Port Configuration:");
                    preview.AppendLine($"  • Assigned Port: {portInfo["assignedPort"]}");
                    preview.AppendLine($"  • Available Ports: {portInfo["availablePortsCount"]}/101\n");
                }
                
                preview.AppendLine("📂 Configuration Files:");
                preview.AppendLine($"  • Server Config: {configInfo["serverConfigPath"]}");
                preview.AppendLine($"  • Root Config: {configInfo["rootConfigPath"]}\n");
                
                preview.AppendLine("⚙️ Generation Options:");
                var includeAutoAccept = _includeAutoAcceptToggle?.value ?? true;
                var packageDistribution = _packageDistributionToggle?.value ?? false;
                var createBackup = _createBackupToggle?.value ?? true;
                var forceRegenerate = _forceRegenerateToggle?.value ?? false;
                
                preview.AppendLine($"  • Include Auto-Accept: {includeAutoAccept}");
                preview.AppendLine($"  • Package Distribution: {packageDistribution}");
                preview.AppendLine($"  • Create Backup: {createBackup}");
                preview.AppendLine($"  • Force Regenerate: {forceRegenerate}");
                
                EditorUtility.DisplayDialog("Multi-Project Configuration Preview", preview.ToString(), "OK");
                AddLogEntry("✅ Configuration preview generated", LogType.Log);
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to generate configuration preview: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Preview Failed", 
                    $"Failed to generate configuration preview:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// マルチプロジェクト設定を検証
        /// </summary>
        private void ValidateMultiProjectSetup()
        {
            try
            {
                AddLogEntry("🔍 Validating multi-project setup...", LogType.Log);
                
                var result = MCPConfigGenerator.ValidateAndFixMultiProjectSetup();
                var isValid = result.ContainsKey("isValid") ? (bool)result["isValid"] : false;
                var issues = result.ContainsKey("issues") ? (List<string>)result["issues"] : new List<string>();
                var fixes = result.ContainsKey("fixes") ? (List<string>)result["fixes"] : new List<string>();
                
                var details = new System.Text.StringBuilder();
                details.AppendLine("=== Multi-Project Setup Validation ===\n");
                
                if (isValid)
                {
                    details.AppendLine("✅ Multi-project setup is valid!\n");
                    details.AppendLine("All required components are properly configured.");
                }
                else
                {
                    details.AppendLine($"⚠️ Found {issues.Count} issue(s), {fixes.Count} fixed automatically\n");
                    
                    if (issues.Count > 0)
                    {
                        details.AppendLine("Issues Found:");
                        foreach (var issue in issues)
                        {
                            details.AppendLine($"  • {issue}");
                        }
                        details.AppendLine();
                    }
                    
                    if (fixes.Count > 0)
                    {
                        details.AppendLine("Auto-Fixes Applied:");
                        foreach (var fix in fixes)
                        {
                            details.AppendLine($"  • {fix}");
                        }
                    }
                }
                
                EditorUtility.DisplayDialog("Multi-Project Setup Validation", details.ToString(), "OK");
                AddLogEntry($"✅ Setup validation completed: {(isValid ? "Valid" : $"{issues.Count} issues, {fixes.Count} fixes")}", LogType.Log);
                
                // UI更新
                UpdateMultiProjectConfigGeneratorStatus();
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to validate multi-project setup: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Validation Failed", 
                    $"Failed to validate multi-project setup:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// 既存の設定ファイルをチェック
        /// </summary>
        private void CheckExistingConfigurations()
        {
            try
            {
                AddLogEntry("🔄 Checking existing configuration files...", LogType.Log);
                
                var configInfo = MCPConfigGenerator.GetProjectConfigInfo();
                var details = new System.Text.StringBuilder();
                
                details.AppendLine("=== Existing Configuration Check ===\n");
                
                // Server Config
                var serverConfigPath = configInfo["serverConfigPath"].ToString();
                var serverConfigExists = File.Exists(serverConfigPath);
                details.AppendLine($"📄 Server Config ({Path.GetFileName(serverConfigPath)}):");
                details.AppendLine($"  Status: {(serverConfigExists ? "✅ Found" : "❌ Missing")}");
                details.AppendLine($"  Path: {serverConfigPath}\n");
                
                // Root Config
                var rootConfigPath = configInfo["rootConfigPath"].ToString();
                var rootConfigExists = File.Exists(rootConfigPath);
                details.AppendLine($"📄 Root Config ({Path.GetFileName(rootConfigPath)}):");
                details.AppendLine($"  Status: {(rootConfigExists ? "✅ Found" : "❌ Missing")}");
                details.AppendLine($"  Path: {rootConfigPath}\n");
                
                // Claude Desktop Config
                var claudeConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                    "Library/Application Support/Claude/claude_desktop_config.json");
                var claudeConfigExists = File.Exists(claudeConfigPath);
                details.AppendLine($"📄 Claude Desktop Config:");
                details.AppendLine($"  Status: {(claudeConfigExists ? "✅ Found" : "❌ Missing")}");
                details.AppendLine($"  Path: {claudeConfigPath}\n");
                
                var allConfigsExist = serverConfigExists && rootConfigExists && claudeConfigExists;
                details.AppendLine($"Overall Status: {(allConfigsExist ? "✅ All configurations found" : "⚠️ Some configurations missing")}");
                
                EditorUtility.DisplayDialog("Existing Configuration Check", details.ToString(), "OK");
                AddLogEntry("✅ Configuration check completed", LogType.Log);
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to check existing configurations: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Check Failed", 
                    $"Failed to check existing configurations:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// 全設定ファイルを生成
        /// </summary>
        private void GenerateAllConfigurations()
        {
            try
            {
                AddLogEntry("🚀 Generating all configuration files...", LogType.Log);
                
                var forceRegenerate = _forceRegenerateToggle?.value ?? false;
                var includeAutoAccept = _includeAutoAcceptToggle?.value ?? true;
                var createBackup = _createBackupToggle?.value ?? true;
                
                // バックアップ作成
                if (createBackup)
                {
                    // Claude Desktop設定のバックアップは ConfigManager内で自動実行される
                }
                
                // マルチプロジェクト設定生成
                var success = MCPConfigGenerator.GenerateProjectConfig(forceRegenerate);
                
                if (success)
                {
                    // Auto-Accept設定
                    if (includeAutoAccept)
                    {
                        MCPAutoApproveSetup.ConfigureAutoApprove(true);
                        AddLogEntry("✅ Auto-Accept configuration included", LogType.Log);
                    }
                    
                    AddLogEntry("🎉 All configuration files generated successfully!", LogType.Log);
                    EditorUtility.DisplayDialog("Configuration Generation Complete", 
                        "🚀 All configuration files have been generated successfully!\n\n✅ Server configuration created\n✅ Project configuration created\n✅ Claude Desktop updated" + 
                        (includeAutoAccept ? "\n✅ Auto-Accept configured" : ""), 
                        "Awesome!");
                }
                else
                {
                    AddLogEntry("❌ Configuration generation failed", LogType.Error);
                    EditorUtility.DisplayDialog("Generation Failed", 
                        "Failed to generate configuration files.\n\nPlease check the console logs for details.", 
                        "OK");
                }
                
                // UI更新
                UpdateMultiProjectConfigGeneratorStatus();
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to generate all configurations: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Generation Failed", 
                    $"Failed to generate all configurations:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// サーバー設定のみ生成
        /// </summary>
        private void GenerateServerConfiguration()
        {
            try
            {
                AddLogEntry("🎯 Generating server configuration only...", LogType.Log);
                
                var forceRegenerate = _forceRegenerateToggle?.value ?? false;
                var success = MCPConfigGenerator.GenerateProjectConfig(forceRegenerate);
                
                if (success)
                {
                    AddLogEntry("✅ Server configuration generated successfully", LogType.Log);
                    EditorUtility.DisplayDialog("Server Configuration Complete", 
                        "Server configuration has been generated successfully!", 
                        "OK");
                }
                else
                {
                    AddLogEntry("❌ Server configuration generation failed", LogType.Error);
                    EditorUtility.DisplayDialog("Generation Failed", 
                        "Failed to generate server configuration.\n\nPlease check the console logs for details.", 
                        "OK");
                }
                
                // UI更新
                UpdateMultiProjectConfigGeneratorStatus();
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to generate server configuration: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Generation Failed", 
                    $"Failed to generate server configuration:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// クリーンアップして設定を再生成
        /// </summary>
        private void CleanAndRegenerateConfiguration()
        {
            try
            {
                if (EditorUtility.DisplayDialog("Clean & Regenerate Configuration", 
                    "This will clean up existing configurations and regenerate everything.\n\nAre you sure you want to proceed?", 
                    "Clean & Regenerate", "Cancel"))
                {
                    AddLogEntry("🧹 Cleaning and regenerating configuration...", LogType.Log);
                    
                    // ポートレジストリクリーンアップ
                    MCPPortManager.CleanupRegistry();
                    
                    // ポート競合解決
                    MCPConfigGenerator.ResolvePortConflicts();
                    
                    // 設定再生成
                    var success = MCPConfigGenerator.GenerateProjectConfig(true);
                    
                    if (success)
                    {
                        var includeAutoAccept = _includeAutoAcceptToggle?.value ?? true;
                        if (includeAutoAccept)
                        {
                            MCPAutoApproveSetup.ConfigureAutoApprove(true);
                        }
                        
                        AddLogEntry("🎉 Clean regeneration completed successfully!", LogType.Log);
                        EditorUtility.DisplayDialog("Clean Regeneration Complete", 
                            "Configuration has been cleaned and regenerated successfully!\n\n🧹 Old configurations cleared\n🚀 New configurations generated", 
                            "Excellent!");
                    }
                    else
                    {
                        AddLogEntry("❌ Clean regeneration failed", LogType.Error);
                        EditorUtility.DisplayDialog("Regeneration Failed", 
                            "Failed to clean and regenerate configuration.\n\nPlease check the console logs for details.", 
                            "OK");
                    }
                    
                    // UI更新
                    UpdateMultiProjectConfigGeneratorStatus();
                    UpdateMultiProjectStatus();
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"❌ Failed to clean and regenerate configuration: {ex.Message}", LogType.Error);
                EditorUtility.DisplayDialog("Clean Regeneration Failed", 
                    $"Failed to clean and regenerate configuration:\n\n{ex.Message}", 
                    "OK");
            }
        }
    }
}
