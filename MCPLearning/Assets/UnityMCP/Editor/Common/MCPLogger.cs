using System;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;

namespace UnityMCP.Editor
{
    /// <summary>
    /// MCP用ロガー設定
    /// </summary>
    public static class MCPLogger
    {
        private static readonly string LogFilePath;
        private static readonly object LogLock = new object();
        private static bool _isInitialized = false;
        
        static MCPLogger()
        {
            // ログファイルのパスを設定
            var logsDirectory = Path.Combine(Application.dataPath, "../Logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
            LogFilePath = Path.Combine(logsDirectory, "mcp-export.log");
            InitializeUnityLogging();
        }
        
        private static void InitializeUnityLogging()
        {
            if (_isInitialized) return;
            
            // Unity Loggingはコンパイルエラーを回避するため一旦無効化
            // ファイルログのみで適切なログ出力を実現
            _isInitialized = true;
        }
        
        /// <summary>
        /// 一般的な情報ログ
        /// </summary>
        public static void Log(string message)
        {
            LogToFile($"[INFO] {message}");
            // Unity Consoleへの出力はUnity Loggingパッケージが正しく設定された後に実装
        }
        
        public static void LogExportStart(string fileName)
        {
            LogToFile($"[INFO] Export started for {fileName}");
        }
        
        public static void LogExportSuccess(string fileName, long fileSize, double duration)
        {
            LogToFile($"[INFO] Export completed for {fileName} - Size: {fileSize}B, Duration: {duration:F2}ms");
        }
        
        public static void LogExportSkipped(string fileName, string reason)
        {
            LogToFile($"[DEBUG] Export skipped for {fileName} - Reason: {reason}");
        }
        
        /// <summary>
        /// 警告ログ
        /// </summary>
        public static void LogWarning(string message)
        {
            LogToFile($"[WARNING] {message}");
        }
        
        /// <summary>
        /// 一般的なエラーログ
        /// </summary>
        public static void LogError(string message)
        {
            LogToFile($"[ERROR] {message}");
        }
        
        public static void LogError(string fileName, string error)
        {
            LogToFile($"[ERROR] Export failed for {fileName} - Error: {error}");
        }
        
        public static void LogPerformanceMetrics(int totalExporters, int changedExporters, double totalDuration)
        {
            LogToFile($"[INFO] Export batch completed - Total: {totalExporters}, Changed: {changedExporters}, Duration: {totalDuration:F2}ms");
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
                // ファイルログ書き込みエラーは上位に伝播
                // Debug.Log使用禁止のため、エラーをスローして上位に伝播
                throw new InvalidOperationException($"Critical logging failure: {ex.Message}", ex);
            }
        }
    }
}