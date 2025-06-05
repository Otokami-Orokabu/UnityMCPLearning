using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UnityMCP.Editor
{
    /// <summary>
    /// エディター状態エクスポーター
    /// </summary>
    public class EditorStateExporter : IDataExporter, IChangeDetector
    {
        public string FileName => "editor-state.json";
        
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
            var selectedObjects = Selection.objects;
            var selectedNames = new List<string>();
            
            foreach (var obj in selectedObjects)
            {
                if (obj != null)
                {
                    selectedNames.Add(obj.name);
                }
            }
            
            return new Dictionary<string, object>
            {
                ["editorFocused"] = InternalEditorUtility.isApplicationActive,
                ["compilingAssemblies"] = EditorApplication.isCompiling,
                ["playModeState"] = EditorApplication.isPlaying ? "Playing" : 
                                    EditorApplication.isPaused ? "Paused" : "Stopped",
                ["selectedObjectsCount"] = selectedObjects.Length,
                ["selectedObjectNames"] = string.Join("|", selectedNames),
                ["isPlayingOrWillChangePlaymode"] = EditorApplication.isPlayingOrWillChangePlaymode,
                ["isPaused"] = EditorApplication.isPaused,
                ["timeSinceStartup"] = EditorApplication.timeSinceStartup.ToString("F2")
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