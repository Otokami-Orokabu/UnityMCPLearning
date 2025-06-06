using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnityMCP.Editor
{
    /// <summary>
    /// MCPデータエクスポートメインクラス
    /// </summary>
    public static class MCPDataExporter
    {
        private static readonly List<IDataExporter> _exporters = new()
        {
            new ProjectInfoExporter(),
            new SceneInfoExporter(),
            new GameObjectExporter(),
            new AssetInfoExporter(),
            new BuildInfoExporter(),
            new EditorStateExporter(),
            new ConsoleLogExporter()
        };
        
        [MenuItem("UnityMCP/Export All Data")]
        public static void ExportAllData()
        {
            ExportChangedData();
        }
        
        [MenuItem("UnityMCP/Force Export All Data")]
        public static void ForceExportAllData()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            foreach (var exporter in _exporters)
            {
                exporter.Export();
                if (exporter is IChangeDetector detector)
                    detector.MarkAsUpdated();
            }
            
            stopwatch.Stop();
            MCPLogger.LogPerformanceMetrics(_exporters.Count, _exporters.Count, stopwatch.Elapsed.TotalMilliseconds);
            
            MCPLogger.Log($"全データのエクスポートが完了しました。({stopwatch.Elapsed.TotalMilliseconds:F2}ms)");
        }
        
        public static void ExportChangedData()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var changedExporters = new List<IDataExporter>();
            
            foreach (var exporter in _exporters)
            {
                if (exporter is IChangeDetector detector && detector.HasChanged())
                {
                    changedExporters.Add(exporter);
                    exporter.Export();
                    detector.MarkAsUpdated();
                }
                else if (exporter is not IChangeDetector)
                {
                    // 変更検知機能がない場合は常に実行
                    changedExporters.Add(exporter);
                    exporter.Export();
                }
            }
            
            stopwatch.Stop();
            
            if (changedExporters.Count > 0)
            {
                MCPLogger.LogPerformanceMetrics(_exporters.Count, changedExporters.Count, stopwatch.Elapsed.TotalMilliseconds);
                MCPLogger.Log($"{changedExporters.Count}個のデータをエクスポートしました。({stopwatch.Elapsed.TotalMilliseconds:F2}ms)");
            }
            else
            {
                MCPLogger.Log("変更されたデータはありません。");
            }
        }
        
        // 自動更新機能
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            MCPLogger.Log("データエクスポーターが初期化されました。");
        }
        
        private static void OnHierarchyChanged()
        {
            // シーンとGameObject関連のみ更新
            ExportSpecificData(typeof(SceneInfoExporter), typeof(GameObjectExporter), typeof(EditorStateExporter));
        }
        
        private static void OnProjectChanged()
        {
            // アセット関連のみ更新
            ExportSpecificData(typeof(AssetInfoExporter));
        }
        
        private static void OnSelectionChanged()
        {
            // エディター状態のみ更新
            ExportSpecificData(typeof(EditorStateExporter));
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // プレイモード変更時は全データ更新
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                ExportChangedData();
            }
        }
        
        private static void ExportSpecificData(params System.Type[] exporterTypes)
        {
            var targetExporters = _exporters.Where(e => exporterTypes.Contains(e.GetType())).ToList();
            
            foreach (var exporter in targetExporters)
            {
                if (exporter is IChangeDetector detector && detector.HasChanged())
                {
                    exporter.Export();
                    detector.MarkAsUpdated();
                }
            }
        }
        
        [MenuItem("UnityMCP/Open Data Folder")]
        public static void OpenDataFolder()
        {
            var dataPath = System.IO.Path.Combine(Application.dataPath, "../UnityMCP/Data");
            if (!System.IO.Directory.Exists(dataPath))
            {
                System.IO.Directory.CreateDirectory(dataPath);
            }
            EditorUtility.RevealInFinder(dataPath);
        }
    }
}