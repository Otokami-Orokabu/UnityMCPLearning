using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace UnityMCP.Editor
{
    /// <summary>
    /// シーン情報エクスポーター
    /// </summary>
    public class SceneInfoExporter : IDataExporter, IChangeDetector
    {
        public string FileName => "scene-info.json";
        
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
            var activeScene = SceneManager.GetActiveScene();
            return new Dictionary<string, object>
            {
                ["currentScene"] = activeScene.name,
                ["scenePath"] = activeScene.path,
                ["isPlaying"] = Application.isPlaying,
                ["sceneObjectCount"] = activeScene.rootCount,
                ["isDirty"] = activeScene.isDirty,
                ["buildIndex"] = activeScene.buildIndex,
                ["isLoaded"] = activeScene.isLoaded
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