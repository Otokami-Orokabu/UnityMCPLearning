using System;
using System.IO;
using UnityEngine;

namespace UnityMCP.Editor
{
    /// <summary>
    /// MCP Server設定管理クラス
    /// </summary>
    [Serializable]
    public class MCPServerSettings
    {
        [SerializeField] public string serverPath = "../unity-mcp-node";
        [SerializeField] public bool autoStartOnLaunch = true;
        [SerializeField] public int defaultPort = 3000;
        [SerializeField] public string lastModified = "";

        private const string LOG_PREFIX = "[MCPServerSettings]";
        private static string SettingsPath => Path.Combine(
            Application.dataPath,
            "..",
            "UnityMCP",
            "settings.json"
        );

        /// <summary>
        /// 設定ファイルを読み込む
        /// </summary>
        public static MCPServerSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonUtility.FromJson<MCPServerSettings>(json);
                    
                    Debug.Log($"{LOG_PREFIX} Settings loaded from: {SettingsPath}");
                    return settings;
                }
                else
                {
                    Debug.Log($"{LOG_PREFIX} Settings file not found, creating default settings");
                    var defaultSettings = new MCPServerSettings();
                    defaultSettings.Save();
                    return defaultSettings;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to load settings: {ex.Message}");
                return new MCPServerSettings();
            }
        }

        /// <summary>
        /// 設定ファイルを保存する
        /// </summary>
        public void Save()
        {
            try
            {
                // ディレクトリ作成
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // タイムスタンプ更新
                lastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                // JSON保存
                var json = JsonUtility.ToJson(this, true);
                File.WriteAllText(SettingsPath, json);

                Debug.Log($"{LOG_PREFIX} Settings saved to: {SettingsPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to save settings: {ex.Message}");
            }
        }

        /// <summary>
        /// サーバーパスの絶対パスを取得
        /// </summary>
        public string GetAbsoluteServerPath()
        {
            try
            {
                if (Path.IsPathRooted(serverPath))
                {
                    return serverPath;
                }
                else
                {
                    var projectPath = Path.GetDirectoryName(Application.dataPath);
                    return Path.GetFullPath(Path.Combine(projectPath, serverPath));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to resolve server path: {ex.Message}");
                return serverPath;
            }
        }

        /// <summary>
        /// サーバーパスが有効かチェック
        /// </summary>
        public bool IsServerPathValid()
        {
            try
            {
                var absolutePath = GetAbsoluteServerPath();
                var indexPath = Path.Combine(absolutePath, "dist", "index.js");
                var packagePath = Path.Combine(absolutePath, "package.json");
                
                return File.Exists(indexPath) || File.Exists(packagePath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 設定の妥当性をチェック
        /// </summary>
        public bool ValidateSettings()
        {
            if (string.IsNullOrEmpty(serverPath))
            {
                Debug.LogWarning($"{LOG_PREFIX} Server path is empty");
                return false;
            }

            if (defaultPort < 1000 || defaultPort > 65535)
            {
                Debug.LogWarning($"{LOG_PREFIX} Invalid port range: {defaultPort}");
                return false;
            }

            if (!IsServerPathValid())
            {
                Debug.LogWarning($"{LOG_PREFIX} Server path is invalid: {GetAbsoluteServerPath()}");
                return false;
            }

            return true;
        }
    }
}