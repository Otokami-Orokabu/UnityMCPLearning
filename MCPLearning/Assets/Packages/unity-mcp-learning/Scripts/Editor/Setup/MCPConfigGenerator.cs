using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace UnityMCP.Editor
{
    /// <summary>
    /// MCP設定ファイル生成・管理クラス
    /// マルチプロジェクト対応・動的ポート管理・パッケージ構造対応
    /// </summary>
    public static class MCPConfigGenerator
    {
        private const string LOG_PREFIX = "[MCPConfigGenerator]";
        
        /// <summary>
        /// プロジェクト用mcp-config.jsonを生成
        /// </summary>
        /// <param name="forceRegenerate">強制再生成フラグ</param>
        /// <returns>生成成功の場合true</returns>
        public static bool GenerateProjectConfig(bool forceRegenerate = false)
        {
            try
            {
                var projectId = MCPProjectIdentifier.GetProjectId();
                var projectInfo = MCPProjectIdentifier.GetProjectInfo();
                
                MCPLogger.LogInfo($"{LOG_PREFIX} Generating MCP config for project {projectId}");
                
                // Server~ディレクトリの設定ファイルを更新
                var serverConfigPath = GetServerConfigPath();
                var serverConfig = CreateServerConfig(projectInfo);
                
                if (!File.Exists(serverConfigPath) || forceRegenerate)
                {
                    SaveConfigFile(serverConfigPath, serverConfig);
                    MCPLogger.LogInfo($"{LOG_PREFIX} Generated server config: {serverConfigPath}");
                }
                
                // プロジェクトルートの設定ファイルを更新（レガシー対応）
                var rootConfigPath = GetRootConfigPath();
                var rootConfig = CreateRootConfig(projectInfo);
                
                if (!File.Exists(rootConfigPath) || forceRegenerate)
                {
                    SaveConfigFile(rootConfigPath, rootConfig);
                    MCPLogger.LogInfo($"{LOG_PREFIX} Generated root config: {rootConfigPath}");
                }
                
                // Claude Desktop設定を更新
                UpdateClaudeDesktopConfig(projectInfo);
                
                return true;
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"{LOG_PREFIX} Failed to generate MCP config: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// マルチプロジェクト環境の設定を確認・修復
        /// </summary>
        /// <returns>確認・修復結果</returns>
        public static Dictionary<string, object> ValidateAndFixMultiProjectSetup()
        {
            var result = new Dictionary<string, object>();
            var issues = new List<string>();
            var fixes = new List<string>();
            
            try
            {
                var projectId = MCPProjectIdentifier.GetProjectId();
                var availablePort = MCPPortManager.GetAvailablePort();
                var portStatus = MCPPortManager.GetPortStatus();
                
                result["projectId"] = projectId;
                result["assignedPort"] = availablePort;
                result["portStatus"] = portStatus;
                
                // 設定ファイルの確認
                var serverConfigPath = GetServerConfigPath();
                var rootConfigPath = GetRootConfigPath();
                
                result["serverConfigExists"] = File.Exists(serverConfigPath);
                result["rootConfigExists"] = File.Exists(rootConfigPath);
                
                if (!File.Exists(serverConfigPath))
                {
                    issues.Add("Server config file missing");
                    GenerateProjectConfig(true);
                    fixes.Add("Generated server config file");
                }
                
                if (!File.Exists(rootConfigPath))
                {
                    issues.Add("Root config file missing");
                    fixes.Add("Generated root config file");
                }
                
                // ディレクトリ構造の確認
                var dataPath = MCPProjectIdentifier.GetProjectDataPath();
                var commandsPath = MCPProjectIdentifier.GetProjectCommandsPath();
                
                result["dataDirectoryExists"] = Directory.Exists(dataPath);
                result["commandsDirectoryExists"] = Directory.Exists(commandsPath);
                
                if (!Directory.Exists(dataPath))
                {
                    issues.Add("Project data directory missing");
                    Directory.CreateDirectory(dataPath);
                    fixes.Add("Created project data directory");
                }
                
                if (!Directory.Exists(commandsPath))
                {
                    issues.Add("Project commands directory missing");
                    Directory.CreateDirectory(commandsPath);
                    fixes.Add("Created project commands directory");
                }
                
                // ポート競合の確認
                if (!MCPPortManager.IsPortAvailable(availablePort))
                {
                    issues.Add($"Port {availablePort} is not available");
                    MCPPortManager.CleanupRegistry();
                    fixes.Add("Cleaned up port registry");
                }
                
                result["issues"] = issues;
                result["fixes"] = fixes;
                result["isValid"] = issues.Count == 0;
                result["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                
                MCPLogger.LogInfo($"{LOG_PREFIX} Multi-project setup validation completed. Issues: {issues.Count}, Fixes: {fixes.Count}");
                
            }
            catch (Exception ex)
            {
                result["error"] = ex.Message;
                MCPLogger.LogError($"{LOG_PREFIX} Failed to validate multi-project setup: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// プロジェクト間でのポート競合を解決
        /// </summary>
        /// <returns>解決成功の場合true</returns>
        public static bool ResolvePortConflicts()
        {
            try
            {
                MCPLogger.LogInfo($"{LOG_PREFIX} Resolving port conflicts...");
                
                // ポートレジストリをクリーンアップ
                MCPPortManager.CleanupRegistry();
                
                // 新しいポートを取得
                var newPort = MCPPortManager.GetAvailablePort();
                var projectId = MCPProjectIdentifier.GetProjectId();
                
                // 設定ファイルを更新
                GenerateProjectConfig(true);
                
                MCPLogger.LogInfo($"{LOG_PREFIX} Port conflict resolved. Project {projectId} assigned port {newPort}");
                return true;
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"{LOG_PREFIX} Failed to resolve port conflicts: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// プロジェクト設定の詳細情報を取得
        /// </summary>
        /// <returns>設定情報</returns>
        public static Dictionary<string, object> GetProjectConfigInfo()
        {
            var info = new Dictionary<string, object>();
            
            try
            {
                var projectInfo = MCPProjectIdentifier.GetProjectInfo();
                var portStatus = MCPPortManager.GetPortStatus();
                var autoApproveStatus = MCPAutoApproveSetup.GetAutoApproveStatus();
                
                info["project"] = projectInfo;
                info["port"] = portStatus;
                info["autoApprove"] = autoApproveStatus;
                info["packagePath"] = MCPPackageResolver.GetPackagePath();
                info["serverConfigPath"] = GetServerConfigPath();
                info["rootConfigPath"] = GetRootConfigPath();
                info["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                
                MCPLogger.LogInfo($"{LOG_PREFIX} Project config info retrieved");
            }
            catch (Exception ex)
            {
                info["error"] = ex.Message;
                MCPLogger.LogError($"{LOG_PREFIX} Failed to get project config info: {ex.Message}");
            }
            
            return info;
        }
        
        /// <summary>
        /// Server~用の設定ファイル作成
        /// </summary>
        private static Dictionary<string, object> CreateServerConfig(Dictionary<string, object> projectInfo)
        {
            var port = MCPPortManager.GetAvailablePort();
            
            return new Dictionary<string, object>
            {
                ["mcpServers"] = new Dictionary<string, object>
                {
                    [$"unity-mcp-{projectInfo["projectId"]}"] = new Dictionary<string, object>
                    {
                        ["command"] = "node",
                        ["args"] = new[] { "dist/index.js" }
                    }
                },
                ["unityDataPath"] = "../../../../UnityMCP/Data",
                ["unityCommandsPath"] = "../../../../UnityMCP/Commands",
                ["projectId"] = projectInfo["projectId"],
                ["projectName"] = projectInfo["projectName"],
                ["port"] = port,
                ["logLevel"] = "info",
                ["timeout"] = new Dictionary<string, object>
                {
                    ["unityCommandTimeout"] = 30000
                },
                ["features"] = new Dictionary<string, object>
                {
                    ["multiProject"] = true,
                    ["dynamicPorts"] = true,
                    ["autoApprove"] = false
                }
            };
        }
        
        /// <summary>
        /// プロジェクトルート用の設定ファイル作成（レガシー対応）
        /// </summary>
        private static Dictionary<string, object> CreateRootConfig(Dictionary<string, object> projectInfo)
        {
            var port = MCPPortManager.GetAvailablePort();
            
            return new Dictionary<string, object>
            {
                ["mcpServers"] = new Dictionary<string, object>
                {
                    [$"unity-mcp-{projectInfo["projectId"]}"] = new Dictionary<string, object>
                    {
                        ["command"] = "node",
                        ["args"] = new[] { "Assets/Packages/unity-mcp-learning/Server~/dist/index.js" }
                    }
                },
                ["unityDataPath"] = "UnityMCP/Data",
                ["unityCommandsPath"] = "UnityMCP/Commands",
                ["projectId"] = projectInfo["projectId"],
                ["projectName"] = projectInfo["projectName"],
                ["port"] = port
            };
        }
        
        /// <summary>
        /// Claude Desktop設定を更新
        /// </summary>
        private static void UpdateClaudeDesktopConfig(Dictionary<string, object> projectInfo)
        {
            try
            {
                var port = MCPPortManager.GetAvailablePort();
                var configManager = new ClaudeDesktopConfigManager();
                configManager.UpdateConfiguration(port);
                
                MCPLogger.LogInfo($"{LOG_PREFIX} Updated Claude Desktop config for port {port}");
            }
            catch (Exception ex)
            {
                MCPLogger.LogWarning($"{LOG_PREFIX} Failed to update Claude Desktop config: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Server~の設定ファイルパスを取得
        /// </summary>
        private static string GetServerConfigPath()
        {
            var packagePath = MCPPackageResolver.GetPackagePath();
            return Path.Combine(packagePath, "Server~", "mcp-config.json");
        }
        
        /// <summary>
        /// プロジェクトルートの設定ファイルパスを取得
        /// </summary>
        private static string GetRootConfigPath()
        {
            return Path.Combine(Application.dataPath, "..", "mcp-config.json");
        }
        
        /// <summary>
        /// 設定ファイルを保存
        /// </summary>
        private static void SaveConfigFile(string path, Dictionary<string, object> config)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = JsonUtility.ToJson(new SerializableDict(config), true);
            File.WriteAllText(path, json);
        }
        
        /// <summary>
        /// MCP設定生成メニュー項目
        /// </summary>
        [MenuItem("UnityMCP/Setup/Generate MCP Config")]
        public static void GenerateConfigMenuItem()
        {
            var success = GenerateProjectConfig(true);
            
            if (success)
            {
                EditorUtility.DisplayDialog("MCP設定生成", "MCP設定ファイルが正常に生成されました。", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("MCP設定生成エラー", "MCP設定ファイルの生成に失敗しました。\nConsoleログを確認してください。", "OK");
            }
        }
        
        /// <summary>
        /// マルチプロジェクト設定確認メニュー項目
        /// </summary>
        [MenuItem("UnityMCP/Setup/Validate Multi-Project Setup")]
        public static void ValidateMultiProjectMenuItem()
        {
            var result = ValidateAndFixMultiProjectSetup();
            var isValid = result.ContainsKey("isValid") ? (bool)result["isValid"] : false;
            var issuesCount = result.ContainsKey("issues") ? ((List<string>)result["issues"]).Count : 0;
            var fixesCount = result.ContainsKey("fixes") ? ((List<string>)result["fixes"]).Count : 0;
            
            var message = isValid 
                ? "マルチプロジェクト設定は正常です。"
                : $"問題が {issuesCount} 件検出され、{fixesCount} 件を自動修復しました。\n\n詳細はConsoleログを確認してください。";
                
            EditorUtility.DisplayDialog("マルチプロジェクト設定確認", message, "OK");
            
            MCPLogger.LogInfo($"{LOG_PREFIX} Multi-Project Setup Result: {JsonUtility.ToJson(new SerializableDict(result), true)}");
        }
        
        /// <summary>
        /// ポート競合解決メニュー項目
        /// </summary>
        [MenuItem("UnityMCP/Setup/Resolve Port Conflicts")]
        public static void ResolvePortConflictsMenuItem()
        {
            var success = ResolvePortConflicts();
            
            var message = success
                ? "ポート競合が正常に解決されました。"
                : "ポート競合の解決に失敗しました。\nConsoleログを確認してください。";
                
            EditorUtility.DisplayDialog("ポート競合解決", message, "OK");
        }
        
        /// <summary>
        /// プロジェクト設定情報表示メニュー項目
        /// </summary>
        [MenuItem("UnityMCP/Setup/Show Project Config Info")]
        public static void ShowProjectConfigInfoMenuItem()
        {
            var info = GetProjectConfigInfo();
            var projectId = info.ContainsKey("project") ? 
                ((Dictionary<string, object>)info["project"])["projectId"].ToString() : "Unknown";
            var port = info.ContainsKey("port") ?
                ((Dictionary<string, object>)info["port"])["assignedPort"].ToString() : "Unknown";
                
            var message = $"プロジェクトID: {projectId}\n割り当てポート: {port}\n\n詳細はConsoleログを確認してください。";
            EditorUtility.DisplayDialog("プロジェクト設定情報", message, "OK");
            
            MCPLogger.LogInfo($"{LOG_PREFIX} Project Config Info: {JsonUtility.ToJson(new SerializableDict(info), true)}");
        }
    }
}