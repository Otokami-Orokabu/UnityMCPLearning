using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

namespace UnityMCP.Editor
{
    /// <summary>
    /// Unity コンパイル状態監視システム
    /// コンパイル開始・完了・エラーをリアルタイムで監視し、JSONファイルに出力
    /// </summary>
    [InitializeOnLoad]
    public static class CompileStatusMonitor
    {
        private static readonly string CompileStatusPath = Path.Combine(Application.dataPath, "../UnityMCP/Data/compile-status.json");
        private static DateTime _compilationStartTime;
        
        static CompileStatusMonitor()
        {
            // コンパイル完了をリアルタイム監視
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            
            MCPLogger.Log("CompileStatusMonitor initialized - monitoring compilation events");
        }
        
        /// <summary>
        /// コンパイル開始時の処理
        /// </summary>
        static void OnCompilationStarted(object obj)
        {
            _compilationStartTime = DateTime.Now;
            
            var compileStatus = new CompileStatus
            {
                status = "COMPILING",
                startTime = _compilationStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                unixStartTime = ((DateTimeOffset)_compilationStartTime).ToUnixTimeSeconds(),
                message = "Unity compilation started...",
                duration = 0,
                errorCount = 0,
                warningCount = 0,
                messages = new CompileMessage[0]
            };
            
            ExportCompileStatus(compileStatus);
            MCPLogger.Log($"Compilation started at {compileStatus.startTime}");
        }
        
        /// <summary>
        /// コンパイル完了時の処理
        /// </summary>
        static void OnCompilationFinished(object obj)
        {
            var endTime = DateTime.Now;
            var duration = (endTime - _compilationStartTime).TotalMilliseconds;
            
            // エディターでコンパイル結果を取得
            var hasErrors = HasCompilationErrors();
            var errorCount = GetCompilationErrorCount();
            var warningCount = GetCompilationWarningCount();
            
            var compileStatus = new CompileStatus
            {
                status = hasErrors ? "FAILED" : "SUCCESS",
                startTime = _compilationStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                endTime = endTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                unixStartTime = ((DateTimeOffset)_compilationStartTime).ToUnixTimeSeconds(),
                unixEndTime = ((DateTimeOffset)endTime).ToUnixTimeSeconds(),
                duration = (long)duration,
                errorCount = errorCount,
                warningCount = warningCount,
                messages = GetCompilationMessages(),
                message = hasErrors 
                    ? $"Compilation failed with {errorCount} error(s) and {warningCount} warning(s) in {duration:F1}ms"
                    : $"Compilation succeeded in {duration:F1}ms ({warningCount} warning(s))"
            };
            
            ExportCompileStatus(compileStatus);
            
            if (hasErrors)
            {
                MCPLogger.LogError("CompileStatusMonitor", $"Compilation failed: {errorCount} errors, {warningCount} warnings");
            }
            else
            {
                MCPLogger.Log($"Compilation succeeded in {duration:F1}ms with {warningCount} warnings");
            }
        }
        
        /// <summary>
        /// コンパイル状態をJSONファイルに出力
        /// </summary>
        static void ExportCompileStatus(CompileStatus status)
        {
            try
            {
                // ディレクトリ確認
                var directory = Path.GetDirectoryName(CompileStatusPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // JSONシリアライズ
                var json = JsonUtility.ToJson(status, true);
                File.WriteAllText(CompileStatusPath, json);
                
                MCPLogger.Log($"Compile status exported: {status.status} ({status.duration}ms)");
            }
            catch (Exception ex)
            {
                MCPLogger.LogError("CompileStatusMonitor", $"Failed to export compile status: {ex.Message}");
            }
        }
        
        /// <summary>
        /// コンパイルエラーの有無を確認
        /// </summary>
        static bool HasCompilationErrors()
        {
            // Unity 2020.1以降のAPI使用
            return CompilationPipeline.codeOptimization == CodeOptimization.Debug && 
                   EditorUtility.scriptCompilationFailed;
        }
        
        /// <summary>
        /// コンパイルエラー数を取得
        /// </summary>
        static int GetCompilationErrorCount()
        {
            // コンソールからエラー数を推定（近似値）
            return HasCompilationErrors() ? 1 : 0; // 簡易実装
        }
        
        /// <summary>
        /// コンパイル警告数を取得
        /// </summary>
        static int GetCompilationWarningCount()
        {
            // コンソールから警告数を推定（近似値）
            return 0; // 簡易実装
        }
        
        /// <summary>
        /// コンパイルメッセージを取得
        /// </summary>
        static CompileMessage[] GetCompilationMessages()
        {
            // 簡易実装：実際のメッセージ取得はより複雑
            if (HasCompilationErrors())
            {
                return new CompileMessage[]
                {
                    new CompileMessage
                    {
                        file = "Unknown",
                        line = 0,
                        column = 0,
                        message = "Compilation failed - check Unity Console for details",
                        type = "Error"
                    }
                };
            }
            
            return new CompileMessage[0];
        }
        
        /// <summary>
        /// 手動でコンパイル状態をクリア（テスト用）
        /// 注意: このメニューアイテムはMCP Server Managerに統合されました
        /// </summary>
        // [MenuItem("UnityMCP/Clear Compile Status")]
        // public static void ClearCompileStatus()
        // {
        //     if (File.Exists(CompileStatusPath))
        //     {
        //         File.Delete(CompileStatusPath);
        //         MCPLogger.Log("Compile status file cleared");
        //     }
        //     else
        //     {
        //         MCPLogger.Log("No compile status file found");
        //     }
        // }
    }
    
    /// <summary>
    /// コンパイル状態データ構造
    /// </summary>
    [System.Serializable]
    public class CompileStatus
    {
        public string status;           // "COMPILING" | "SUCCESS" | "FAILED"
        public string startTime;        // 開始時刻（文字列）
        public string endTime;          // 終了時刻（文字列）
        public long unixStartTime;      // 開始時刻（Unix）
        public long unixEndTime;        // 終了時刻（Unix）
        public long duration;           // 実行時間（ミリ秒）
        public int errorCount;          // エラー数
        public int warningCount;        // 警告数
        public string message;          // 状態メッセージ
        public CompileMessage[] messages; // 詳細メッセージ
    }
    
    /// <summary>
    /// コンパイルメッセージ構造
    /// </summary>
    [System.Serializable]
    public class CompileMessage
    {
        public string file;             // ファイルパス
        public int line;                // 行番号
        public int column;              // 列番号
        public string message;          // メッセージ内容
        public string type;             // "Error" | "Warning"
    }
}