using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace UnityMCP.Editor
{
    /// <summary>
    /// Unity プロジェクト一意識別子管理クラス
    /// SHA256ベースのプロジェクトID生成によるマルチプロジェクト対応
    /// </summary>
    public static class MCPProjectIdentifier
    {
        private const string LOG_PREFIX = "[MCPProjectIdentifier]";
        private static string _cachedProjectId = null;
        private static string _cachedProjectPath = null;
        
        /// <summary>
        /// 現在のプロジェクトの一意識別子を取得
        /// </summary>
        /// <returns>SHA256ベースの一意識別子（短縮版16文字）</returns>
        public static string GetProjectId()
        {
            var currentProjectPath = GetProjectPath();
            
            // キャッシュ確認
            if (_cachedProjectId != null && _cachedProjectPath == currentProjectPath)
            {
                return _cachedProjectId;
            }
            
            try
            {
                // プロジェクトパスからSHA256ハッシュを生成
                var projectIdSeed = $"{currentProjectPath}|{GetProjectName()}|{GetUnityVersion()}";
                var hash = ComputeSHA256Hash(projectIdSeed);
                
                // 短縮版（16文字）を使用
                var shortId = hash.Substring(0, 16);
                
                // キャッシュ更新
                _cachedProjectId = shortId;
                _cachedProjectPath = currentProjectPath;
                
                Debug.Log($"{LOG_PREFIX} Generated project ID: {shortId} for project: {GetProjectName()}");
                return shortId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to generate project ID: {ex.Message}");
                // フォールバック：プロジェクト名ベース
                return GenerateFallbackId();
            }
        }
        
        /// <summary>
        /// プロジェクト固有のデータディレクトリパスを取得
        /// </summary>
        /// <returns>プロジェクトID付きデータディレクトリパス</returns>
        public static string GetProjectDataPath()
        {
            var basePath = Path.Combine(Application.dataPath, "../UnityMCP/Data");
            var projectId = GetProjectId();
            var projectDataPath = Path.Combine(basePath, $"project-{projectId}");
            
            try
            {
                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(projectDataPath))
                {
                    Directory.CreateDirectory(projectDataPath);
                    Debug.Log($"{LOG_PREFIX} Created project data directory: {projectDataPath}");
                }
                
                return projectDataPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to create project data directory: {ex.Message}");
                // フォールバック：通常のDataディレクトリ
                return basePath;
            }
        }
        
        /// <summary>
        /// プロジェクト固有のコマンドディレクトリパスを取得
        /// </summary>
        /// <returns>プロジェクトID付きコマンドディレクトリパス</returns>
        public static string GetProjectCommandsPath()
        {
            var basePath = Path.Combine(Application.dataPath, "../UnityMCP/Commands");
            var projectId = GetProjectId();
            var projectCommandsPath = Path.Combine(basePath, $"project-{projectId}");
            
            try
            {
                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(projectCommandsPath))
                {
                    Directory.CreateDirectory(projectCommandsPath);
                    Debug.Log($"{LOG_PREFIX} Created project commands directory: {projectCommandsPath}");
                }
                
                return projectCommandsPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to create project commands directory: {ex.Message}");
                // フォールバック：通常のCommandsディレクトリ
                return basePath;
            }
        }
        
        /// <summary>
        /// プロジェクト情報の詳細を取得
        /// </summary>
        /// <returns>プロジェクト情報ディクショナリ</returns>
        public static System.Collections.Generic.Dictionary<string, object> GetProjectInfo()
        {
            return new System.Collections.Generic.Dictionary<string, object>
            {
                ["projectId"] = GetProjectId(),
                ["projectName"] = GetProjectName(),
                ["projectPath"] = GetProjectPath(),
                ["unityVersion"] = GetUnityVersion(),
                ["dataPath"] = GetProjectDataPath(),
                ["commandsPath"] = GetProjectCommandsPath(),
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }
        
        /// <summary>
        /// プロジェクトIDをリセット（キャッシュクリア）
        /// </summary>
        public static void ResetProjectId()
        {
            _cachedProjectId = null;
            _cachedProjectPath = null;
            Debug.Log($"{LOG_PREFIX} Project ID cache cleared");
        }
        
        /// <summary>
        /// プロジェクトパスを取得
        /// </summary>
        private static string GetProjectPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }
        
        /// <summary>
        /// プロジェクト名を取得
        /// </summary>
        private static string GetProjectName()
        {
            return Path.GetFileName(GetProjectPath());
        }
        
        /// <summary>
        /// Unityバージョンを取得
        /// </summary>
        private static string GetUnityVersion()
        {
            return Application.unityVersion;
        }
        
        /// <summary>
        /// SHA256ハッシュを計算
        /// </summary>
        private static string ComputeSHA256Hash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
        
        /// <summary>
        /// フォールバックID生成
        /// </summary>
        private static string GenerateFallbackId()
        {
            var fallbackSeed = $"{GetProjectName()}-{GetUnityVersion()}-{DateTime.UtcNow:yyyyMMdd}";
            var simpleHash = fallbackSeed.GetHashCode().ToString("x8");
            Debug.LogWarning($"{LOG_PREFIX} Using fallback ID: {simpleHash}");
            return simpleHash;
        }
    }
}