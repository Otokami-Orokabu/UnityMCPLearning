using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityMCP.Editor
{
    /// <summary>
    /// Unity Console ログエクスポーター
    /// エラー、警告、通常ログを収集してMCPサーバーに提供
    /// </summary>
    [InitializeOnLoad]
    public class ConsoleLogExporter : IDataExporter, IChangeDetector
    {
        public string FileName => "console-logs.json";
        
        private static List<LogEntry> _collectedLogs = new List<LogEntry>();
        private static int _lastExportedCount = 0;
        private static int _maxLogsToKeep = 1000;
        private static ConsoleLogExporter _instance;
        
        static ConsoleLogExporter()
        {
            // Unity Console出力をリアルタイム収集
            Application.logMessageReceived += OnLogMessageReceived;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            
            // シングルトンインスタンス作成
            _instance = new ConsoleLogExporter();
            
            // 初回エクスポート
            EditorApplication.delayCall += () => {
                if (_instance != null)
                {
                    _instance.Export();
                }
            };
        }
        
        private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            lock (_collectedLogs)
            {
                _collectedLogs.Add(new LogEntry
                {
                    message = logString,
                    stackTrace = stackTrace,
                    type = type.ToString(),
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    unixTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
                });
                
                // 最新N件のみ保持（メモリ効率）
                if (_collectedLogs.Count > _maxLogsToKeep)
                {
                    _collectedLogs.RemoveAt(0);
                }
                
                // 自動エクスポート（デバウンス的な処理）
                EditorApplication.delayCall -= DelayedExport;
                EditorApplication.delayCall += DelayedExport;
            }
        }
        
        private static void DelayedExport()
        {
            if (_instance != null)
            {
                _instance.Export();
            }
        }
        
        public void Export()
        {
            if (!HasChanged())
            {
                MCPLogger.LogExportSkipped(FileName, "No new logs to export");
                return;
            }
            
            // ConsoleLogExporter専用の処理
            var consoleData = GatherConsoleData();
            var json = JsonUtility.ToJson(consoleData, true);
            var filePath = System.IO.Path.Combine(Application.dataPath, "../UnityMCP/Data", FileName);
            
            // ディレクトリ確認
            var directory = System.IO.Path.GetDirectoryName(filePath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            // ファイル書き込み
            System.IO.File.WriteAllText(filePath, json);
            MCPLogger.LogExportSuccess(FileName, new System.IO.FileInfo(filePath).Length, 0);
            
            MarkAsUpdated();
        }
        
        public bool HasChanged()
        {
            lock (_collectedLogs)
            {
                return _collectedLogs.Count != _lastExportedCount;
            }
        }
        
        public void MarkAsUpdated()
        {
            lock (_collectedLogs)
            {
                _lastExportedCount = _collectedLogs.Count;
            }
        }
        
        private ConsoleLogData GatherConsoleData()
        {
            lock (_collectedLogs)
            {
                var logs = _collectedLogs.ToList(); // コピー作成
                
                var errorCount = logs.Count(l => l.type == "Error" || l.type == "Exception");
                var warningCount = logs.Count(l => l.type == "Warning");
                var logCount = logs.Count(l => l.type == "Log");
                var assertCount = logs.Count(l => l.type == "Assert");
                var exceptionCount = logs.Count(l => l.type == "Exception");
                
                // JsonUtility用にシリアライズ可能な構造を作成
                return new ConsoleLogData
                {
                    logs = logs.ToArray(),
                    summary = new LogSummary
                    {
                        totalLogs = logs.Count,
                        errorCount = errorCount,
                        warningCount = warningCount,
                        logCount = logCount,
                        assertCount = assertCount,
                        exceptionCount = exceptionCount
                    },
                    lastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    unixTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
                };
            }
        }
        
        private Dictionary<string, object> GatherData()
        {
            lock (_collectedLogs)
            {
                var logs = _collectedLogs.ToList(); // コピー作成
                
                var errorCount = logs.Count(l => l.type == "Error" || l.type == "Exception");
                var warningCount = logs.Count(l => l.type == "Warning");
                var logCount = logs.Count(l => l.type == "Log");
                var assertCount = logs.Count(l => l.type == "Assert");
                var exceptionCount = logs.Count(l => l.type == "Exception");
                
                // JsonUtility用にシリアライズ可能な構造を作成
                var consoleData = new ConsoleLogData
                {
                    logs = logs.ToArray(),
                    summary = new LogSummary
                    {
                        totalLogs = logs.Count,
                        errorCount = errorCount,
                        warningCount = warningCount,
                        logCount = logCount,
                        assertCount = assertCount,
                        exceptionCount = exceptionCount
                    },
                    lastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    unixTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
                };
                
                // ConsoleLogDataをDictionaryに変換
                return new Dictionary<string, object>
                {
                    ["logs"] = consoleData.logs,
                    ["summary"] = consoleData.summary,
                    ["lastUpdate"] = consoleData.lastUpdate,
                    ["unixTimestamp"] = consoleData.unixTimestamp
                };
            }
        }
    }
    
    /// <summary>
    /// ログエントリーのデータ構造
    /// </summary>
    [System.Serializable]
    public class LogEntry
    {
        public string message;
        public string stackTrace;
        public string type;
        public string timestamp;
        public long unixTimestamp;
    }
    
    /// <summary>
    /// ログサマリーのデータ構造
    /// </summary>
    [System.Serializable]
    public class LogSummary
    {
        public int totalLogs;
        public int errorCount;
        public int warningCount;
        public int logCount;
        public int assertCount;
        public int exceptionCount;
    }
    
    /// <summary>
    /// コンソールログデータ全体の構造
    /// </summary>
    [System.Serializable]
    public class ConsoleLogData
    {
        public LogEntry[] logs;
        public LogSummary summary;
        public string lastUpdate;
        public long unixTimestamp;
    }
}