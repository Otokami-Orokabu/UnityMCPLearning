using System;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace UnityMCP.Editor
{
    /// <summary>
    /// ログレベル列挙型
    /// </summary>
    public enum MCPLogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        None = 4
    }

    /// <summary>
    /// MCP用統一ロガー（UnityEngine.Debug + ファイルログ + レート制限）
    /// </summary>
    public static class MCPLogger
    {
        private static readonly string LogFilePath;
        private static readonly object LogLock = new object();
        private static bool _isInitialized = false;
        
        // ログレベル設定（デフォルトはInfo）
        private static MCPLogLevel _currentLogLevel = MCPLogLevel.Info;
        
        // レート制限用
        private static readonly Dictionary<string, DateTime> _lastLogTimes = new Dictionary<string, DateTime>();
        private static readonly TimeSpan _rateLimitInterval = TimeSpan.FromSeconds(1); // 1秒間隔制限
        
        static MCPLogger()
        {
            // ログファイルのパスを設定
            var logsDirectory = Path.Combine(Application.dataPath, "../Logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
            LogFilePath = Path.Combine(logsDirectory, "mcp-export.log");
            InitializeLogger();
        }
        
        private static void InitializeLogger()
        {
            if (_isInitialized) return;
            
            try
            {
                // 開発環境とプロダクション環境でログレベルを自動調整
#if UNITY_EDITOR
                _currentLogLevel = MCPLogLevel.Warning; // エディターでも警告レベル以上のみ表示
#else
                _currentLogLevel = MCPLogLevel.Error; // ビルド版ではエラーのみ
#endif
                LogToFile("[INFO] MCP Logger initialized with rate limiting");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Failed to initialize logger: {ex.Message}");
                _isInitialized = true; // フォールバック
            }
        }
        
        /// <summary>
        /// ログレベル設定
        /// </summary>
        public static void SetLogLevel(MCPLogLevel level)
        {
            _currentLogLevel = level;
            LogToFile($"[INFO] Log level set to: {level}");
        }
        
        /// <summary>
        /// 現在のログレベル取得
        /// </summary>
        public static MCPLogLevel GetLogLevel()
        {
            return _currentLogLevel;
        }
        
        /// <summary>
        /// レート制限チェック
        /// </summary>
        private static bool IsRateLimited(string message)
        {
            lock (LogLock)
            {
                var now = DateTime.Now;
                var key = message.Length > 50 ? message.Substring(0, 50) : message;
                
                if (_lastLogTimes.TryGetValue(key, out var lastTime))
                {
                    if (now - lastTime < _rateLimitInterval)
                    {
                        return true;
                    }
                }
                
                _lastLogTimes[key] = now;
                
                // 古いエントリをクリーンアップ（メモリリーク防止）
                if (_lastLogTimes.Count > 100)
                {
                    var cutoff = now - TimeSpan.FromMinutes(5);
                    var keysToRemove = new List<string>();
                    foreach (var kvp in _lastLogTimes)
                    {
                        if (kvp.Value < cutoff)
                        {
                            keysToRemove.Add(kvp.Key);
                        }
                    }
                    foreach (var k in keysToRemove)
                    {
                        _lastLogTimes.Remove(k);
                    }
                }
                
                return false;
            }
        }
        
        /// <summary>
        /// 一般的な情報ログ
        /// </summary>
        public static void Log(string message)
        {
            LogWithLevel(MCPLogLevel.Info, message);
        }
        
        public static void LogExportStart(string fileName)
        {
            var message = $"Export started for {fileName}";
            LogWithLevel(MCPLogLevel.Debug, message);
        }
        
        public static void LogExportSuccess(string fileName, long fileSize, double duration)
        {
            var message = $"Export completed for {fileName} - Size: {fileSize}B, Duration: {duration:F2}ms";
            LogWithLevel(MCPLogLevel.Debug, message);
        }
        
        public static void LogExportSkipped(string fileName, string reason)
        {
            var message = $"Export skipped for {fileName} - Reason: {reason}";
            LogWithLevel(MCPLogLevel.Debug, message);
        }
        
        /// <summary>
        /// 詳細情報ログ
        /// </summary>
        public static void LogInfo(string message)
        {
            LogWithLevel(MCPLogLevel.Info, message);
        }
        
        /// <summary>
        /// 警告ログ
        /// </summary>
        public static void LogWarning(string message)
        {
            LogWithLevel(MCPLogLevel.Warning, message);
        }
        
        /// <summary>
        /// 一般的なエラーログ
        /// </summary>
        public static void LogError(string message)
        {
            LogWithLevel(MCPLogLevel.Error, message);
        }
        
        public static void LogError(string fileName, string error)
        {
            var message = $"Export failed for {fileName} - Error: {error}";
            LogWithLevel(MCPLogLevel.Error, message);
        }
        
        public static void LogPerformanceMetrics(int totalExporters, int changedExporters, double totalDuration)
        {
            var message = $"Export batch completed - Total: {totalExporters}, Changed: {changedExporters}, Duration: {totalDuration:F2}ms";
            LogWithLevel(MCPLogLevel.Debug, message);
        }
        
        private static void LogToFile(string message)
        {
            try
            {
                lock (LogLock)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logMessage = $"{timestamp} {message}";
                    File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // ファイルログ失敗時は最終手段としてUnityEngine.Debug.LogError
                Debug.LogError($"[MCP] Critical logging failure: {ex.Message}");
            }
        }
        
        /// <summary>
        /// デバッグ用ログ（開発時のみ表示）
        /// </summary>
        public static void LogDebug(string message)
        {
            LogWithLevel(MCPLogLevel.Debug, message);
        }
        
        /// <summary>
        /// ログレベルに応じた出力制御
        /// </summary>
        private static void LogWithLevel(MCPLogLevel level, string message)
        {
            // ログレベルチェック
            if (level < _currentLogLevel)
            {
                return;
            }
            
            // レート制限チェック（Errorレベルは除外）
            if (level != MCPLogLevel.Error && IsRateLimited(message))
            {
                return;
            }
            
            // レベルに応じた出力
            var levelPrefix = GetLevelPrefix(level);
            var fullMessage = $"[MCP] {message}";
            
            switch (level)
            {
                case MCPLogLevel.Debug:
#if UNITY_EDITOR
                    Debug.Log(fullMessage);
#endif
                    break;
                case MCPLogLevel.Info:
                    Debug.Log(fullMessage);
                    break;
                case MCPLogLevel.Warning:
                    Debug.LogWarning(fullMessage);
                    break;
                case MCPLogLevel.Error:
                    Debug.LogError(fullMessage);
                    break;
            }
            
            // ファイルログ
            LogToFile($"[{levelPrefix}] {message}");
        }
        
        /// <summary>
        /// ログレベルのプレフィックス取得
        /// </summary>
        private static string GetLevelPrefix(MCPLogLevel level)
        {
            switch (level)
            {
                case MCPLogLevel.Debug: return "DEBUG";
                case MCPLogLevel.Info: return "INFO";
                case MCPLogLevel.Warning: return "WARNING";
                case MCPLogLevel.Error: return "ERROR";
                default: return "UNKNOWN";
            }
        }
        
        /// <summary>
        /// ログファイルパスを取得
        /// </summary>
        public static string GetLogFilePath()
        {
            return LogFilePath;
        }
        
        /// <summary>
        /// ログファイルをクリア
        /// </summary>
        public static void ClearLogFile()
        {
            try
            {
                lock (LogLock)
                {
                    File.WriteAllText(LogFilePath, string.Empty);
                }
                LogInfo("Log file cleared");
            }
            catch (Exception ex)
            {
                LogError($"Failed to clear log file: {ex.Message}");
            }
        }
    }
}