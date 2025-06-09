using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;

namespace UnityMCP.Editor
{
    /// <summary>
    /// MCPデータのJSON書き込み共通処理
    /// </summary>
    public static class MCPDataWriter
    {
        // プロジェクト固有のデータパスを動的に取得
        private static string DataPath => MCPProjectIdentifier.GetProjectDataPath();
        
        public static void WriteJsonFile(string fileName, Dictionary<string, object> data)
        {
            WriteJsonFileAsync(fileName, data).ConfigureAwait(false);
        }
        
        public static async Task WriteJsonFileAsync(string fileName, Dictionary<string, object> data)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            MCPLogger.LogExportStart(fileName);
            
            try
            {
                await EnsureDataDirectoryAsync();
                
                // 時刻情報を追加
                data["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                
                var json = JsonUtility.ToJson(new SerializableDict(data), true);
                var filePath = Path.Combine(DataPath, fileName);
                
                await WriteFileAsync(filePath, json);
                stopwatch.Stop();
                
                var fileInfo = new FileInfo(filePath);
                MCPLogger.LogExportSuccess(fileName, fileInfo.Length, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                MCPLogger.LogError(fileName, ex.Message);
                throw;
            }
        }
        
        private static void EnsureDataDirectory()
        {
            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }
        }
        
        private static async Task EnsureDataDirectoryAsync()
        {
            await Task.Yield(); // Switch to background thread
            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }
        }
        
        private static async Task WriteFileAsync(string filePath, string content)
        {
            await Task.Yield(); // Switch to background thread
            File.WriteAllText(filePath, content);
        }
    }
    
    [System.Serializable]
    public class SerializableDict
    {
        public List<KeyValuePair> items = new List<KeyValuePair>();
        
        public SerializableDict(Dictionary<string, object> data)
        {
            foreach (var kvp in data)
            {
                items.Add(new KeyValuePair { key = kvp.Key, value = kvp.Value?.ToString() ?? "" });
            }
        }
        
        /// <summary>
        /// Dictionary<string, object>に変換
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var result = new Dictionary<string, object>();
            foreach (var item in items)
            {
                result[item.key] = item.value;
            }
            return result;
        }
    }
    
    [System.Serializable]
    public class KeyValuePair
    {
        public string key;
        public string value;
    }
}