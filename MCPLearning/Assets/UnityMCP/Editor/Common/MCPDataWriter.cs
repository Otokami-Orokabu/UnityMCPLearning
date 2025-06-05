using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityMCP.Editor
{
    /// <summary>
    /// MCPデータのJSON書き込み共通処理
    /// </summary>
    public static class MCPDataWriter
    {
        private static readonly string DataPath = Path.Combine(Application.dataPath, "../UnityMCP/Data");
        
        public static void WriteJsonFile(string fileName, Dictionary<string, object> data)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            MCPLogger.LogExportStart(fileName);
            
            try
            {
                EnsureDataDirectory();
                
                // 時刻情報を追加
                data["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                
                var json = JsonUtility.ToJson(new SerializableDict(data), true);
                var filePath = Path.Combine(DataPath, fileName);
                
                File.WriteAllText(filePath, json);
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
    }
    
    [System.Serializable]
    public class KeyValuePair
    {
        public string key;
        public string value;
    }
}