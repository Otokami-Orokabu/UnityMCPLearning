using System.Collections.Generic;
using UnityEngine;

namespace UnityMCP.Editor
{
    /// <summary>
    /// プロジェクト基本情報エクスポーター
    /// </summary>
    public class ProjectInfoExporter : IDataExporter, IChangeDetector
    {
        public string FileName => "project-info.json";
        
        private static Dictionary<string, object> _lastData;
        
        public void Export()
        {
            if (!HasChanged())
            {
                MCPLogger.LogExportSkipped(FileName, "No changes detected");
                return;
            }
            
            var data = GatherData();
            MCPDataWriter.WriteJsonFile(FileName, data);
            MarkAsUpdated();
        }
        
        public bool HasChanged()
        {
            var currentData = GatherData();
            return !DataEquals(_lastData, currentData);
        }
        
        public void MarkAsUpdated()
        {
            _lastData = GatherData();
        }
        
        private Dictionary<string, object> GatherData()
        {
            return new Dictionary<string, object>
            {
                ["projectName"] = Application.productName,
                ["unityVersion"] = Application.unityVersion,
                ["platform"] = Application.platform.ToString(),
                ["companyName"] = Application.companyName,
                ["dataPath"] = Application.dataPath,
                ["persistentDataPath"] = Application.persistentDataPath
            };
        }
        
        private bool DataEquals(Dictionary<string, object> a, Dictionary<string, object> b)
        {
            if (a == null || b == null) return a == b;
            if (a.Count != b.Count) return false;
            
            foreach (var kvp in a)
            {
                if (!b.ContainsKey(kvp.Key) || !Equals(b[kvp.Key], kvp.Value))
                    return false;
            }
            return true;
        }
    }
}