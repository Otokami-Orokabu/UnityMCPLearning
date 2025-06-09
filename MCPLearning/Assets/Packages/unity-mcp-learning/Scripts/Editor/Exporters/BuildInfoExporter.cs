using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

namespace UnityMCP.Editor
{
    /// <summary>
    /// ビルド設定情報エクスポーター
    /// </summary>
    public class BuildInfoExporter : IDataExporter, IChangeDetector
    {
        public string FileName => "build-info.json";
        
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
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            
            return new Dictionary<string, object>
            {
                ["buildTarget"] = EditorUserBuildSettings.activeBuildTarget.ToString(),
                ["buildTargetGroup"] = buildTargetGroup.ToString(),
                ["developmentBuild"] = EditorUserBuildSettings.development,
                ["scriptingBackend"] = PlayerSettings.GetScriptingBackend(namedBuildTarget).ToString(),
                ["apiCompatibilityLevel"] = PlayerSettings.GetApiCompatibilityLevel(namedBuildTarget).ToString(),
                ["architecture"] = PlayerSettings.GetArchitecture(namedBuildTarget),
                ["applicationIdentifier"] = PlayerSettings.applicationIdentifier,
                ["productName"] = PlayerSettings.productName,
                ["companyName"] = PlayerSettings.companyName
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