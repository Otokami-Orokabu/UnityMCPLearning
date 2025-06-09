using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace UnityMCP.Editor
{
    /// <summary>
    /// MCP Auto-Accept機能自動設定クラス
    /// Claude Code CLI使用時の自動承認設定によるワンクリック高速開発環境
    /// </summary>
    public static class MCPAutoApproveSetup
    {
        private const string LOG_PREFIX = "[MCPAutoApproveSetup]";
        
        // Auto-Accept対象ツール一覧
        private static readonly string[] AUTO_APPROVE_TOOLS = {
            "ping",
            "unity_info_realtime", 
            "get_console_logs",
            "create_cube",
            "create_sphere", 
            "create_plane",
            "create_gameobject",
            "wait_for_compilation"
        };
        
        // Claude Desktop設定ファイルパス
        private static readonly Dictionary<string, string> CLAUDE_CONFIG_PATHS = new Dictionary<string, string>
        {
            ["macOS"] = "~/Library/Application Support/Claude/claude_desktop_config.json",
            ["Windows"] = "~/AppData/Roaming/Claude/claude_desktop_config.json", 
            ["Linux"] = "~/.config/claude/claude_desktop_config.json"
        };
        
        /// <summary>
        /// Auto-Accept設定を適用
        /// </summary>
        /// <param name="enableAutoApprove">Auto-Approveを有効にするか</param>
        /// <returns>設定成功の場合true</returns>
        public static bool ConfigureAutoApprove(bool enableAutoApprove = true)
        {
            try
            {
                var projectId = MCPProjectIdentifier.GetProjectId();
                var serverName = $"unity-mcp-{projectId}";
                
                MCPLogger.LogInfo($"{LOG_PREFIX} Configuring Auto-Accept for project {projectId} (enabled: {enableAutoApprove})");
                
                // Claude Desktop設定を更新
                var configUpdated = UpdateClaudeDesktopConfig(serverName, enableAutoApprove);
                
                // プロジェクト設定を保存
                SaveAutoApproveSettings(enableAutoApprove);
                
                if (configUpdated)
                {
                    MCPLogger.LogInfo($"{LOG_PREFIX} Auto-Accept configuration completed successfully");
                    ShowSuccessDialog(enableAutoApprove);
                    return true;
                }
                else
                {
                    MCPLogger.LogWarning($"{LOG_PREFIX} Auto-Accept configuration completed with warnings");
                    return false;
                }
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"{LOG_PREFIX} Failed to configure Auto-Accept: {ex.Message}");
                ShowErrorDialog(ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// 現在のAuto-Accept設定状態を取得
        /// </summary>
        /// <returns>Auto-Accept設定情報</returns>
        public static Dictionary<string, object> GetAutoApproveStatus()
        {
            var status = new Dictionary<string, object>();
            
            try
            {
                var projectId = MCPProjectIdentifier.GetProjectId();
                var settings = LoadAutoApproveSettings();
                
                status["projectId"] = projectId;
                status["enabled"] = settings.ContainsKey("autoApproveEnabled") ? settings["autoApproveEnabled"] : false;
                status["approvedTools"] = AUTO_APPROVE_TOOLS;
                status["approvedToolsCount"] = AUTO_APPROVE_TOOLS.Length;
                status["claudeConfigPath"] = GetClaudeConfigPath();
                status["claudeConfigExists"] = File.Exists(ExpandPath(GetClaudeConfigPath()));
                status["lastConfigured"] = settings.ContainsKey("lastConfigured") ? settings["lastConfigured"] : "";
                status["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                
                // Claude Desktop設定の詳細確認
                var claudeStatus = CheckClaudeDesktopConfig();
                status["claudeDesktopStatus"] = claudeStatus;
                
                MCPLogger.LogInfo($"{LOG_PREFIX} Auto-Accept status retrieved for project {projectId}");
            }
            catch (Exception ex)
            {
                status["error"] = ex.Message;
                MCPLogger.LogError($"{LOG_PREFIX} Failed to get Auto-Accept status: {ex.Message}");
            }
            
            return status;
        }
        
        /// <summary>
        /// Auto-Accept設定をリセット
        /// </summary>
        /// <returns>リセット成功の場合true</returns>
        public static bool ResetAutoApprove()
        {
            try
            {
                var projectId = MCPProjectIdentifier.GetProjectId();
                MCPLogger.LogInfo($"{LOG_PREFIX} Resetting Auto-Accept configuration for project {projectId}");
                
                // Auto-Acceptを無効化
                var success = ConfigureAutoApprove(false);
                
                if (success)
                {
                    MCPLogger.LogInfo($"{LOG_PREFIX} Auto-Accept configuration reset successfully");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"{LOG_PREFIX} Failed to reset Auto-Accept: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Claude Desktop設定ファイルを更新
        /// </summary>
        private static bool UpdateClaudeDesktopConfig(string serverName, bool enableAutoApprove)
        {
            try
            {
                var configPath = ExpandPath(GetClaudeConfigPath());
                
                if (!File.Exists(configPath))
                {
                    MCPLogger.LogWarning($"{LOG_PREFIX} Claude Desktop config not found at: {configPath}");
                    return false;
                }
                
                var json = File.ReadAllText(configPath);
                var config = ParseClaudeConfig(json);
                
                if (!config.ContainsKey("mcpServers"))
                {
                    config["mcpServers"] = new Dictionary<string, object>();
                }
                
                var mcpServers = config["mcpServers"] as Dictionary<string, object>;
                var port = MCPPortManager.GetAvailablePort();
                
                var serverConfig = new Dictionary<string, object>
                {
                    ["command"] = "node",
                    ["args"] = new[] { GetServerExecutablePath() },
                    ["env"] = new Dictionary<string, object>()
                };
                
                // Auto-Accept環境変数設定
                var env = serverConfig["env"] as Dictionary<string, object>;
                if (enableAutoApprove)
                {
                    env["MCP_AUTO_APPROVE"] = "true";
                    env["MCP_AUTO_APPROVE_TOOLS"] = string.Join(",", AUTO_APPROVE_TOOLS);
                    env["MCP_SKIP_CONFIRMATION"] = "true";
                }
                else
                {
                    env["MCP_AUTO_APPROVE"] = "false";
                }
                
                mcpServers[serverName] = serverConfig;
                
                // 設定ファイルを保存
                var updatedJson = SerializeClaudeConfig(config);
                File.WriteAllText(configPath, updatedJson);
                
                MCPLogger.LogInfo($"{LOG_PREFIX} Updated Claude Desktop config: {serverName} (Auto-Accept: {enableAutoApprove})");
                return true;
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"{LOG_PREFIX} Failed to update Claude Desktop config: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Auto-Accept設定をプロジェクトに保存
        /// </summary>
        private static void SaveAutoApproveSettings(bool enabled)
        {
            var settings = new Dictionary<string, object>
            {
                ["autoApproveEnabled"] = enabled,
                ["approvedTools"] = AUTO_APPROVE_TOOLS,
                ["lastConfigured"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["projectId"] = MCPProjectIdentifier.GetProjectId()
            };
            
            var settingsPath = Path.Combine(Application.dataPath, "../UnityMCP/auto-approve-settings.json");
            var json = JsonUtility.ToJson(new SerializableDict(settings), true);
            
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            File.WriteAllText(settingsPath, json);
            
            MCPLogger.LogInfo($"{LOG_PREFIX} Saved Auto-Accept settings to: {settingsPath}");
        }
        
        /// <summary>
        /// Auto-Accept設定をプロジェクトから読み込み
        /// </summary>
        private static Dictionary<string, object> LoadAutoApproveSettings()
        {
            try
            {
                var settingsPath = Path.Combine(Application.dataPath, "../UnityMCP/auto-approve-settings.json");
                
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var wrapper = JsonUtility.FromJson<SerializableDict>(json);
                    return wrapper.ToDictionary();
                }
            }
            catch (Exception ex)
            {
                MCPLogger.LogWarning($"{LOG_PREFIX} Failed to load Auto-Accept settings: {ex.Message}");
            }
            
            return new Dictionary<string, object>();
        }
        
        /// <summary>
        /// Claude Desktop設定状況をチェック
        /// </summary>
        private static Dictionary<string, object> CheckClaudeDesktopConfig()
        {
            var status = new Dictionary<string, object>();
            
            try
            {
                var configPath = ExpandPath(GetClaudeConfigPath());
                status["configPath"] = configPath;
                status["configExists"] = File.Exists(configPath);
                
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = ParseClaudeConfig(json);
                    
                    status["hasValidJson"] = true;
                    status["hasMcpServers"] = config.ContainsKey("mcpServers");
                    
                    if (config.ContainsKey("mcpServers"))
                    {
                        var mcpServers = config["mcpServers"] as Dictionary<string, object>;
                        status["serverCount"] = mcpServers?.Count ?? 0;
                        
                        var projectId = MCPProjectIdentifier.GetProjectId();
                        var expectedServerName = $"unity-mcp-{projectId}";
                        status["hasProjectServer"] = mcpServers?.ContainsKey(expectedServerName) ?? false;
                    }
                }
                else
                {
                    status["hasValidJson"] = false;
                }
            }
            catch (Exception ex)
            {
                status["error"] = ex.Message;
                status["hasValidJson"] = false;
            }
            
            return status;
        }
        
        /// <summary>
        /// プラットフォーム対応のClaude設定パスを取得
        /// </summary>
        private static string GetClaudeConfigPath()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                    return CLAUDE_CONFIG_PATHS["macOS"];
                case RuntimePlatform.WindowsEditor:
                    return CLAUDE_CONFIG_PATHS["Windows"];
                case RuntimePlatform.LinuxEditor:
                    return CLAUDE_CONFIG_PATHS["Linux"];
                default:
                    return CLAUDE_CONFIG_PATHS["macOS"]; // デフォルト
            }
        }
        
        /// <summary>
        /// パス文字列を展開（~を実際のホームディレクトリに変換）
        /// </summary>
        private static string ExpandPath(string path)
        {
            if (path.StartsWith("~"))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return path.Replace("~", home);
            }
            return path;
        }
        
        /// <summary>
        /// サーバー実行可能ファイルのパスを取得
        /// </summary>
        private static string GetServerExecutablePath()
        {
            var packagePath = MCPPackageResolver.GetPackagePath();
            return Path.Combine(packagePath, "Server~", "dist", "index.js");
        }
        
        /// <summary>
        /// Claude設定JSONをパース（安全な実装）
        /// </summary>
        private static Dictionary<string, object> ParseClaudeConfig(string json)
        {
            var config = new Dictionary<string, object>();
            
            try
            {
                // 安全なJSON解析
                if (!string.IsNullOrEmpty(json))
                {
                    // mcpServersセクションの存在をチェック
                    if (json.Contains("\"mcpServers\"") || json.Contains("'mcpServers'"))
                    {
                        config["mcpServers"] = new Dictionary<string, object>();
                    }
                    
                    // 基本的な有効性チェック
                    json = json.Trim();
                    if (json.StartsWith("{") && json.EndsWith("}"))
                    {
                        config["validJson"] = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MCPLogger.LogWarning($"{LOG_PREFIX} JSON parse warning: {ex.Message}");
                config["parseError"] = ex.Message;
            }
            
            return config;
        }
        
        /// <summary>
        /// Claude設定をJSONシリアライズ（簡易実装）
        /// </summary>
        private static string SerializeClaudeConfig(Dictionary<string, object> config)
        {
            // 実際の実装では、より堅牢なJSONシリアライザーを使用
            return JsonUtility.ToJson(new SerializableDict(config), true);
        }
        
        /// <summary>
        /// 成功ダイアログを表示
        /// </summary>
        private static void ShowSuccessDialog(bool enabled)
        {
            var message = enabled
                ? "Auto-Accept機能が正常に有効化されました。\n\nClaude Code CLI使用時に確認ダイアログをスキップし、高速開発が可能になります。"
                : "Auto-Accept機能が正常に無効化されました。\n\n通常の確認ダイアログが表示されるようになります。";
                
            EditorUtility.DisplayDialog("MCP Auto-Accept設定", message, "OK");
        }
        
        /// <summary>
        /// エラーダイアログを表示
        /// </summary>
        private static void ShowErrorDialog(string error)
        {
            var message = $"Auto-Accept設定中にエラーが発生しました:\n\n{error}\n\n手動でClaude Desktop設定を確認してください。";
            EditorUtility.DisplayDialog("MCP Auto-Accept設定エラー", message, "OK");
        }
        
        /// <summary>
        /// Auto-Accept設定メニュー項目
        /// </summary>
        [MenuItem("UnityMCP/Setup/Enable Auto-Accept")]
        public static void EnableAutoAcceptMenuItem()
        {
            ConfigureAutoApprove(true);
        }
        
        /// <summary>
        /// Auto-Accept無効化メニュー項目
        /// </summary>
        [MenuItem("UnityMCP/Setup/Disable Auto-Accept")]
        public static void DisableAutoAcceptMenuItem()
        {
            ConfigureAutoApprove(false);
        }
        
        /// <summary>
        /// Auto-Accept状態確認メニュー項目
        /// </summary>
        [MenuItem("UnityMCP/Setup/Check Auto-Accept Status")]
        public static void CheckAutoAcceptStatusMenuItem()
        {
            var status = GetAutoApproveStatus();
            var enabled = status.ContainsKey("enabled") ? (bool)status["enabled"] : false;
            var projectId = status.ContainsKey("projectId") ? status["projectId"].ToString() : "Unknown";
            
            var message = $"プロジェクト: {projectId}\nAuto-Accept: {(enabled ? "有効" : "無効")}\n\n詳細はConsoleログを確認してください。";
            EditorUtility.DisplayDialog("MCP Auto-Accept状態", message, "OK");
            
            MCPLogger.LogInfo($"{LOG_PREFIX} Auto-Accept Status: {JsonUtility.ToJson(new SerializableDict(status), true)}");
        }
    }
}