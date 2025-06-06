using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityMCP.Editor
{
    /// <summary>
    /// Claude Desktop設定管理クラス（簡易JSON実装）
    /// </summary>
    public class ClaudeDesktopConfigManager
    {
        private const string LOG_PREFIX = "[ClaudeDesktopConfigManager]";
        private readonly string _configPath;
        
        public ClaudeDesktopConfigManager()
        {
            _configPath = GetClaudeDesktopConfigPath();
        }
        
        private string GetClaudeDesktopConfigPath()
        {
            string basePath;
            
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(basePath, "Claude", "claude_desktop_config.json");
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                basePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                return Path.Combine(basePath, "Library", "Application Support", "Claude", "claude_desktop_config.json");
            }
            else
            {
                basePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                return Path.Combine(basePath, ".config", "Claude", "claude_desktop_config.json");
            }
        }
        
        public string GetConfigurationPath()
        {
            return _configPath;
        }
        
        public bool ConfigurationExists()
        {
            return File.Exists(_configPath);
        }
        
        public bool UpdateConfiguration(int port)
        {
            try
            {
                var serverScriptPath = GetServerScriptPath();
                var nodeExecutable = GetNodeExecutable();
                
                // 簡易JSON構築
                var mcpServerJson = $@"{{
    ""command"": ""{nodeExecutable}"",
    ""args"": [""{serverScriptPath.Replace("\\", "\\\\").Replace("\"", "\\\"")}""],
    ""env"": {{
      ""PORT"": ""{port}""
    }}
  }}";
                
                var configJson = $@"{{
  ""mcpServers"": {{
    ""unity-mcp"": {mcpServerJson}
  }}
}}";
                
                // バックアップ作成
                if (File.Exists(_configPath))
                {
                    var backupPath = _configPath + ".backup";
                    File.Copy(_configPath, backupPath, true);
                    Debug.Log($"{LOG_PREFIX} Created backup at: {backupPath}");
                }
                
                // 設定を保存
                var directory = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(_configPath, configJson);
                
                Debug.Log($"{LOG_PREFIX} Updated Claude Desktop configuration with port: {port}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to update configuration: {ex.Message}");
                return false;
            }
        }
        
        public bool RemoveConfiguration()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    return false;
                }
                
                var content = File.ReadAllText(_configPath);
                
                // 簡易的な削除（unity-mcpセクションを削除）
                if (content.Contains("\"unity-mcp\""))
                {
                    // 完全に新しい設定で上書き
                    var emptyConfigJson = @"{
  ""mcpServers"": {},
  ""globalShortcuts"": {}
}";
                    File.WriteAllText(_configPath, emptyConfigJson);
                    Debug.Log($"{LOG_PREFIX} Removed Unity MCP from Claude Desktop configuration");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to remove configuration: {ex.Message}");
                return false;
            }
        }
        
        private string GetNodeExecutable()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return "node.exe";
            }
            return "node";
        }
        
        private string GetServerScriptPath()
        {
            var projectPath = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectPath, "unity-mcp-node", "dist", "index.js");
        }
        
        public bool ValidateConfiguration()
        {
            try
            {
                if (!ConfigurationExists())
                {
                    Debug.LogWarning($"{LOG_PREFIX} Configuration file does not exist");
                    return false;
                }
                
                var content = File.ReadAllText(_configPath);
                
                if (!content.Contains("\"unity-mcp\""))
                {
                    Debug.LogWarning($"{LOG_PREFIX} Unity MCP server not found in configuration");
                    return false;
                }
                
                var scriptPath = GetServerScriptPath();
                if (!File.Exists(scriptPath))
                {
                    Debug.LogError($"{LOG_PREFIX} Server script not found: {scriptPath}");
                    return false;
                }
                
                Debug.Log($"{LOG_PREFIX} Configuration validation successful");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Configuration validation failed: {ex.Message}");
                return false;
            }
        }
    }
}