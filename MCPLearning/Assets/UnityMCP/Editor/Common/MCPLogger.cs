using System;
using System.IO;
using UnityEngine;

namespace UnityMCP.Editor
{
    /// <summary>
    /// MCP用ロガー設定
    /// </summary>
    public static class MCPLogger
    {
        private static readonly string LogFilePath;
        private static readonly object LogLock = new object();
        
        static MCPLogger()
        {
            // ログファイルのパスを設定
            var logsDirectory = Path.Combine(Application.dataPath, "../Logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
            LogFilePath = Path.Combine(logsDirectory, "mcp-export.log");
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
        
        public static void LogError(string fileName, string error)
        {
            LogToFile($"[ERROR] Export failed for {fileName} - Error: {error}");
            Debug.LogError($"[MCP] Export failed for {fileName} - Error: {error}");
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
                Debug.LogError($"[MCP] Failed to write to log file: {ex.Message}");
            }
        }
    }
}