using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;

namespace UnityMCP.Editor
{
    /// <summary>
    /// Unity MCP Learning パッケージのパス解決クラス
    /// PackageInfo APIを使用して動的にパッケージパスを解決
    /// </summary>
    public static class MCPPackageResolver
    {
        private const string LOG_PREFIX = "[MCPPackageResolver]";
        private const string PACKAGE_NAME = "com.orlab.unity-mcp-learning";
        
        private static string _cachedPackagePath;
        private static bool _pathResolved = false;
        
        // Static constructor to reset cache when script reloads
        static MCPPackageResolver()
        {
            _cachedPackagePath = null;
            _pathResolved = false;
        }
        
        /// <summary>
        /// パッケージのルートパスを取得
        /// </summary>
        /// <returns>パッケージルートパス（例: "Packages/com.orlab.unity-mcp-learning"）</returns>
        public static string GetPackageRootPath()
        {
            if (_pathResolved && !string.IsNullOrEmpty(_cachedPackagePath))
            {
                return _cachedPackagePath;
            }
            
            try
            {
                // PackageInfo APIを使用してパッケージ情報を取得
                var listRequest = Client.List(true, false);
                
                // 同期的に待機（エディター環境なので許可）
                while (!listRequest.IsCompleted)
                {
                    System.Threading.Thread.Sleep(10);
                }
                
                if (listRequest.Status == StatusCode.Success)
                {
                    foreach (var package in listRequest.Result)
                    {
                        if (package.name == PACKAGE_NAME)
                        {
                            // パッケージが見つかった場合
                            _cachedPackagePath = package.assetPath;
                            _pathResolved = true;
                            
                            MCPLogger.LogInfo($"{LOG_PREFIX} Package found at: {_cachedPackagePath}");
                            return _cachedPackagePath;
                        }
                    }
                }
                
                // PackageInfo APIで見つからない場合のフォールバック
                var fallbackPath = $"Packages/{PACKAGE_NAME}";
                if (Directory.Exists(Path.Combine(Application.dataPath, "..", fallbackPath)))
                {
                    _cachedPackagePath = fallbackPath;
                    _pathResolved = true;
                    
                    MCPLogger.LogWarning($"{LOG_PREFIX} Package found via fallback: {_cachedPackagePath}");
                    return _cachedPackagePath;
                }
                
                // 相対パス指定でインストールされた場合の動的検索
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                MCPLogger.LogInfo($"{LOG_PREFIX} Searching for package in relative paths from: {projectRoot}");
                
                // 親ディレクトリを段階的に検索
                for (int depth = 1; depth <= 3; depth++)
                {
                    var searchRoot = projectRoot;
                    for (int i = 0; i < depth; i++)
                    {
                        searchRoot = Path.GetDirectoryName(searchRoot);
                        if (string.IsNullOrEmpty(searchRoot)) break;
                    }
                    
                    if (string.IsNullOrEmpty(searchRoot) || !Directory.Exists(searchRoot))
                        continue;
                    
                    MCPLogger.LogInfo($"{LOG_PREFIX} Searching depth {depth} at: {searchRoot}");
                    
                    try
                    {
                        // searchRoot内の全ディレクトリを検索
                        var directories = Directory.GetDirectories(searchRoot, "*", SearchOption.AllDirectories);
                        
                        foreach (var dir in directories)
                        {
                            // unity-mcp-learningディレクトリを探す
                            if (Path.GetFileName(dir) == "unity-mcp-learning")
                            {
                                // package.jsonが存在するかチェック
                                var packageJsonPath = Path.Combine(dir, "package.json");
                                if (File.Exists(packageJsonPath))
                                {
                                    // 相対パスを計算
                                    var relativePath = Path.GetRelativePath(projectRoot, dir).Replace('\\', '/');
                                    
                                    _cachedPackagePath = relativePath;
                                    _pathResolved = true;
                                    
                                    MCPLogger.LogWarning($"{LOG_PREFIX} Package found via dynamic search: {_cachedPackagePath} -> {dir}");
                                    return _cachedPackagePath;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MCPLogger.LogWarning($"{LOG_PREFIX} Error searching depth {depth}: {ex.Message}");
                    }
                }
                
                // 開発環境用フォールバック（Assets/Packages内）
                var devPath = "Assets/Packages/unity-mcp-learning";
                if (Directory.Exists(Path.Combine(Application.dataPath, "Packages", "unity-mcp-learning")))
                {
                    _cachedPackagePath = devPath;
                    _pathResolved = true;
                    
                    MCPLogger.LogWarning($"{LOG_PREFIX} Package found in development path: {_cachedPackagePath}");
                    return _cachedPackagePath;
                }
                
                throw new Exception($"Package '{PACKAGE_NAME}' not found in any expected location");
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"{LOG_PREFIX} Failed to resolve package path: {ex.Message}");
                
                // 最終フォールバック
                _cachedPackagePath = $"Packages/{PACKAGE_NAME}";
                _pathResolved = false;
                
                return _cachedPackagePath;
            }
        }
        
        /// <summary>
        /// Server~ディレクトリのパスを取得
        /// </summary>
        /// <returns>Server~ディレクトリパス</returns>
        public static string GetServerPath()
        {
            var packagePath = GetPackageRootPath();
            MCPLogger.LogInfo($"{LOG_PREFIX} GetServerPath() called with package path: {packagePath}");
            
            // パッケージマネージャー経由の場合は絶対パスに変換
            if (packagePath.StartsWith("Packages/"))
            {
                MCPLogger.LogInfo($"{LOG_PREFIX} Package is in Packages/ folder, trying PackageInfo resolution...");
                
                // Package Manager経由でインストールされた場合のパス解決
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packagePath);
                if (packageInfo != null)
                {
                    MCPLogger.LogInfo($"{LOG_PREFIX} PackageInfo found: {packageInfo.name}, resolvedPath: {packageInfo.resolvedPath}");
                    
                    // resolvedPathは絶対パスを返す
                    var resolvedPath = packageInfo.resolvedPath;
                    if (!string.IsNullOrEmpty(resolvedPath))
                    {
                        var serverPath = Path.Combine(resolvedPath, "Server~").Replace('\\', '/');
                        MCPLogger.LogInfo($"{LOG_PREFIX} Checking Server~ at resolved path: {serverPath}");
                        
                        if (Directory.Exists(serverPath))
                        {
                            MCPLogger.LogInfo($"{LOG_PREFIX} Server~ found at resolved path: {serverPath}");
                            return serverPath;
                        }
                        else
                        {
                            MCPLogger.LogWarning($"{LOG_PREFIX} Server~ not found at resolved path: {serverPath}");
                            
                            // resolvedPath内のディレクトリを確認
                            try
                            {
                                if (Directory.Exists(resolvedPath))
                                {
                                    var dirs = Directory.GetDirectories(resolvedPath);
                                    MCPLogger.LogInfo($"{LOG_PREFIX} Available directories in resolved path: {string.Join(", ", dirs.Select(Path.GetFileName))}");
                                }
                            }
                            catch (Exception ex)
                            {
                                MCPLogger.LogError($"{LOG_PREFIX} Error listing resolved path directories: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    MCPLogger.LogWarning($"{LOG_PREFIX} PackageInfo not found for asset path: {packagePath}");
                }
                
                // Library/PackageCache内のパスを試す
                MCPLogger.LogInfo($"{LOG_PREFIX} Trying Library/PackageCache...");
                var projectPath = Path.GetDirectoryName(Application.dataPath);
                var packageCachePath = Path.Combine(projectPath, "Library", "PackageCache");
                
                if (Directory.Exists(packageCachePath))
                {
                    var packageDirs = Directory.GetDirectories(packageCachePath, $"{PACKAGE_NAME}@*");
                    MCPLogger.LogInfo($"{LOG_PREFIX} Found {packageDirs.Length} matching package directories in PackageCache");
                    
                    if (packageDirs.Length > 0)
                    {
                        var serverPath = Path.Combine(packageDirs[0], "Server~").Replace('\\', '/');
                        MCPLogger.LogInfo($"{LOG_PREFIX} Checking Server~ in PackageCache: {serverPath}");
                        
                        if (Directory.Exists(serverPath))
                        {
                            MCPLogger.LogInfo($"{LOG_PREFIX} Server~ found in PackageCache: {serverPath}");
                            return serverPath;
                        }
                        else
                        {
                            MCPLogger.LogWarning($"{LOG_PREFIX} Server~ not found in PackageCache: {serverPath}");
                            
                            // PackageCacheディレクトリの内容を確認
                            try
                            {
                                var dirs = Directory.GetDirectories(packageDirs[0]);
                                MCPLogger.LogInfo($"{LOG_PREFIX} Available directories in PackageCache: {string.Join(", ", dirs.Select(Path.GetFileName))}");
                            }
                            catch (Exception ex)
                            {
                                MCPLogger.LogError($"{LOG_PREFIX} Error listing PackageCache directories: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    MCPLogger.LogWarning($"{LOG_PREFIX} PackageCache directory not found: {packageCachePath}");
                }
            }
            
            // ローカルパッケージやAssets内、相対パス指定の場合
            MCPLogger.LogInfo($"{LOG_PREFIX} Trying local package path...");
            var defaultServerPath = Path.Combine(packagePath, "Server~").Replace('\\', '/');
            
            // 相対パスの場合は絶対パスに変換
            if (!Path.IsPathRooted(defaultServerPath))
            {
                var projectPath = Path.GetDirectoryName(Application.dataPath);
                defaultServerPath = Path.GetFullPath(Path.Combine(projectPath, defaultServerPath)).Replace('\\', '/');
            }
            
            MCPLogger.LogInfo($"{LOG_PREFIX} Final server path: {defaultServerPath}");
            
            // Force check directory existence with debug info
            MCPLogger.LogInfo($"{LOG_PREFIX} Checking Server~ directory at: {defaultServerPath}");
            var directoryExists = Directory.Exists(defaultServerPath);
            MCPLogger.LogInfo($"{LOG_PREFIX} Directory.Exists result: {directoryExists}");
            
            if (directoryExists)
            {
                MCPLogger.LogInfo($"{LOG_PREFIX} Server~ found at default path: {defaultServerPath}");
            }
            else
            {
                MCPLogger.LogWarning($"{LOG_PREFIX} Server~ not found at default path: {defaultServerPath}");
                
                // Git URL配布でServer~が除外される問題への対策
                MCPLogger.LogError($"{LOG_PREFIX} Server~ directory not found in package. This indicates the package was not properly built with GitHub Actions.");
                MCPLogger.LogError($"{LOG_PREFIX} Please use the release package from GitHub Releases instead of Git URL installation.");
                MCPLogger.LogError($"{LOG_PREFIX} Release packages include the complete MCP server files.");
                
                // 相対パス指定の場合のServer~ディレクトリ検索をもう少し詳細に
                if (packagePath.StartsWith("../"))
                {
                    MCPLogger.LogInfo($"{LOG_PREFIX} Package is specified with relative path, checking parent package directory...");
                    var packageDir = Path.GetDirectoryName(defaultServerPath);
                    
                    if (Directory.Exists(packageDir))
                    {
                        try
                        {
                            var dirs = Directory.GetDirectories(packageDir);
                            MCPLogger.LogInfo($"{LOG_PREFIX} Available directories in package: {string.Join(", ", dirs.Select(Path.GetFileName))}");
                            
                            // Server~以外の可能性もチェック（Server、server、Server-など）
                            var serverDir = dirs.FirstOrDefault(d => 
                                Path.GetFileName(d).Equals("Server~", StringComparison.OrdinalIgnoreCase) ||
                                Path.GetFileName(d).Equals("Server", StringComparison.OrdinalIgnoreCase) ||
                                Path.GetFileName(d).StartsWith("Server", StringComparison.OrdinalIgnoreCase));
                            
                            if (!string.IsNullOrEmpty(serverDir))
                            {
                                MCPLogger.LogInfo($"{LOG_PREFIX} Found server directory: {serverDir}");
                                return serverDir.Replace('\\', '/');
                            }
                        }
                        catch (Exception ex)
                        {
                            MCPLogger.LogError($"{LOG_PREFIX} Error checking package directory contents: {ex.Message}");
                        }
                    }
                }
            }
            
            return defaultServerPath;
        }
        
        /// <summary>
        /// MCPサーバー実行ファイルのパスを取得
        /// </summary>
        /// <returns>index.jsファイルパス</returns>
        public static string GetServerExecutablePath()
        {
            var serverPath = GetServerPath();
            return Path.Combine(serverPath, "dist", "index.js").Replace('\\', '/');
        }
        
        /// <summary>
        /// パッケージ内の設定ファイルパスを取得
        /// </summary>
        /// <param name="configFileName">設定ファイル名</param>
        /// <returns>設定ファイルパス</returns>
        public static string GetConfigPath(string configFileName)
        {
            var serverPath = GetServerPath();
            return Path.Combine(serverPath, configFileName).Replace('\\', '/');
        }
        
        /// <summary>
        /// パッケージパスを取得（GetPackageRootPathのエイリアス）
        /// </summary>
        /// <returns>パッケージパス</returns>
        public static string GetPackagePath()
        {
            return GetPackageRootPath();
        }
        
        /// <summary>
        /// パッケージの絶対パスを取得
        /// </summary>
        /// <returns>絶対パス</returns>
        public static string GetAbsolutePackagePath()
        {
            var relativePath = GetPackageRootPath();
            
            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }
            
            var projectPath = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectPath, relativePath));
        }
        
        /// <summary>
        /// Server~ディレクトリの絶対パスを取得
        /// </summary>
        /// <returns>Server~ディレクトリ絶対パス</returns>
        public static string GetAbsoluteServerPath()
        {
            var absolutePackagePath = GetAbsolutePackagePath();
            return Path.Combine(absolutePackagePath, "Server~");
        }
        
        /// <summary>
        /// パッケージパス解決状態をリセット（テスト用）
        /// </summary>
        public static void ResetCache()
        {
            _cachedPackagePath = null;
            _pathResolved = false;
            MCPLogger.LogInfo($"{LOG_PREFIX} Package path cache reset");
        }
        
        /// <summary>
        /// パッケージが正常に解決されているかチェック
        /// </summary>
        /// <returns>解決状態</returns>
        public static bool IsPackageResolved()
        {
            return _pathResolved && !string.IsNullOrEmpty(_cachedPackagePath);
        }
        
        /// <summary>
        /// パッケージ情報の詳細をログ出力
        /// </summary>
        public static void LogPackageInfo()
        {
            try
            {
                var packagePath = GetPackageRootPath();
                var serverPath = GetServerPath();
                var executablePath = GetServerExecutablePath();
                var absolutePath = GetAbsolutePackagePath();
                
                MCPLogger.LogInfo($"{LOG_PREFIX} Package Information:");
                MCPLogger.LogInfo($"  - Package Path: {packagePath}");
                MCPLogger.LogInfo($"  - Server Path: {serverPath}");
                MCPLogger.LogInfo($"  - Executable: {executablePath}");
                MCPLogger.LogInfo($"  - Absolute Path: {absolutePath}");
                MCPLogger.LogInfo($"  - Resolved: {IsPackageResolved()}");
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"{LOG_PREFIX} Failed to log package info: {ex.Message}");
            }
        }
    }
}