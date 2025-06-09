using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace UnityMCP.Editor
{
    /// <summary>
    /// アセット情報エクスポーター
    /// </summary>
    public class AssetInfoExporter : IDataExporter, IChangeDetector
    {
        public string FileName => "assets-info.json";
        
        private static string _lastAssetDatabaseRefresh;
        
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
            // AssetDatabaseの最終更新時刻で判定
            var currentRefresh = AssetDatabase.GetAssetDependencyHash("Assets").ToString();
            return _lastAssetDatabaseRefresh != currentRefresh;
        }
        
        public void MarkAsUpdated()
        {
            _lastAssetDatabaseRefresh = AssetDatabase.GetAssetDependencyHash("Assets").ToString();
        }
        
        private Dictionary<string, object> GatherData()
        {
            var allAssets = AssetDatabase.FindAssets("");
            
            // アセットタイプ別カウント
            var textureCount = AssetDatabase.FindAssets("t:Texture").Length;
            var scriptCount = AssetDatabase.FindAssets("t:Script").Length;
            var prefabCount = AssetDatabase.FindAssets("t:Prefab").Length;
            var materialCount = AssetDatabase.FindAssets("t:Material").Length;
            var audioCount = AssetDatabase.FindAssets("t:AudioClip").Length;
            var sceneCount = AssetDatabase.FindAssets("t:Scene").Length;
            
            // 最近変更されたアセット（最大5個）
            var recentAssets = new List<string>();
            try
            {
                var assetPaths = allAssets.Take(50)
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(path => !string.IsNullOrEmpty(path) && !path.StartsWith("Packages/"))
                    .Take(5);
                
                recentAssets.AddRange(assetPaths);
            }
            catch
            {
                recentAssets.Add("アセット情報の取得に失敗");
            }
            
            // Unity MCPアセットの存在チェック（動的パス解決）
            var hasUnityMCPAssets = 0;
            try
            {
                var packagePath = MCPPackageResolver.GetPackageRootPath();
                if (Directory.Exists(packagePath))
                {
                    hasUnityMCPAssets = AssetDatabase.FindAssets("", new[] { packagePath }).Length;
                }
            }
            catch
            {
                // パッケージパスが見つからない場合は0を返す
                hasUnityMCPAssets = 0;
            }
            
            return new Dictionary<string, object>
            {
                ["totalAssets"] = allAssets.Length,
                ["textureCount"] = textureCount,
                ["scriptCount"] = scriptCount,
                ["prefabCount"] = prefabCount,
                ["materialCount"] = materialCount,
                ["audioCount"] = audioCount,
                ["sceneCount"] = sceneCount,
                ["recentAssets"] = string.Join(", ", recentAssets),
                ["hasUnityMCPAssets"] = hasUnityMCPAssets
            };
        }
    }
}