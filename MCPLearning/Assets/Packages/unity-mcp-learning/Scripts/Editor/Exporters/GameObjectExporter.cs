using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace UnityMCP.Editor
{
    /// <summary>
    /// GameObject情報エクスポーター
    /// </summary>
    public class GameObjectExporter : IDataExporter, IChangeDetector
    {
        public string FileName => "gameobjects.json";
        
        private static string _lastDataHash;
        
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
            var currentHash = CalculateSceneHash();
            return _lastDataHash != currentHash;
        }
        
        public void MarkAsUpdated()
        {
            _lastDataHash = CalculateSceneHash();
        }
        
        private Dictionary<string, object> GatherData()
        {
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var objectInfoList = new List<string>();
            
            foreach (var obj in rootObjects)
            {
                var objInfo = $"{obj.name}({obj.transform.position.x:F1},{obj.transform.position.y:F1},{obj.transform.position.z:F1})";
                if (!obj.activeInHierarchy) objInfo += "[Inactive]";
                objectInfoList.Add(objInfo);
            }
            
            return new Dictionary<string, object>
            {
                ["totalCount"] = rootObjects.Length,
                ["activeCount"] = rootObjects.Count(obj => obj.activeInHierarchy),
                ["objectsList"] = string.Join(", ", objectInfoList),
                ["sceneObjectNames"] = string.Join("|", rootObjects.Select(obj => obj.name))
            };
        }
        
        private string CalculateSceneHash()
        {
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var hashString = "";
            
            foreach (var obj in rootObjects)
            {
                hashString += obj.name + obj.transform.position.ToString() + 
                             obj.activeInHierarchy + obj.transform.childCount;
            }
            
            return hashString.GetHashCode().ToString();
        }
    }
}