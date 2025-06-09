using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using UnityEngine;

namespace UnityMCP.Editor
{
    /// <summary>
    /// MCP サーバーポート動的管理クラス
    /// 3000-3100範囲での自動ポート割り当て・競合回避
    /// </summary>
    public static class MCPPortManager
    {
        private const string LOG_PREFIX = "[MCPPortManager]";
        private const int PORT_RANGE_START = 3000;
        private const int PORT_RANGE_END = 3100;
        private const int DEFAULT_PORT = 3000;
        
        // ポートステータスキャッシュ（重い処理を避けるため）
        private static Dictionary<string, object> _cachedPortStatus;
        private static DateTime _lastPortStatusUpdate = DateTime.MinValue;
        private static readonly TimeSpan PORT_STATUS_CACHE_DURATION = TimeSpan.FromSeconds(30);
        
        private static readonly string PORT_REGISTRY_FILE = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Unity", "MCPPortRegistry.json"
        );
        
        /// <summary>
        /// 現在のプロジェクト用の利用可能ポートを取得
        /// </summary>
        /// <returns>利用可能なポート番号</returns>
        public static int GetAvailablePort()
        {
            var projectId = MCPProjectIdentifier.GetProjectId();
            
            try
            {
                // 既存の登録されたポートを確認
                var registeredPort = GetRegisteredPort(projectId);
                if (registeredPort != -1 && IsPortAvailable(registeredPort))
                {
                    MCPLogger.LogInfo($"{LOG_PREFIX} Using registered port {registeredPort} for project {projectId}");
                    return registeredPort;
                }
                
                // 新しいポートを検索
                var availablePort = FindAvailablePortInRange();
                if (availablePort != -1)
                {
                    RegisterPort(projectId, availablePort);
                    MCPLogger.LogInfo($"{LOG_PREFIX} Assigned new port {availablePort} to project {projectId}");
                    return availablePort;
                }
                
                MCPLogger.LogWarning($"{LOG_PREFIX} No available ports in range {PORT_RANGE_START}-{PORT_RANGE_END}, using default {DEFAULT_PORT}");
                return DEFAULT_PORT;
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"{LOG_PREFIX} Failed to get available port: {ex.Message}");
                return DEFAULT_PORT;
            }
        }
        
        /// <summary>
        /// プロジェクトのポートを解放
        /// </summary>
        public static void ReleasePort()
        {
            var projectId = MCPProjectIdentifier.GetProjectId();
            
            try
            {
                var registry = LoadPortRegistry();
                if (registry.ContainsKey(projectId))
                {
                    var releasedPort = registry[projectId];
                    registry.Remove(projectId);
                    SavePortRegistry(registry);
                    MCPLogger.LogInfo($"{LOG_PREFIX} Released port {releasedPort} for project {projectId}");
                }
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"{LOG_PREFIX} Failed to release port: {ex.Message}");
            }
        }
        
        /// <summary>
        /// ポートが利用可能かチェック
        /// </summary>
        /// <param name="port">チェックするポート番号</param>
        /// <returns>利用可能な場合true</returns>
        public static bool IsPortAvailable(int port)
        {
            try
            {
                // TCP接続テストによる簡易ポートチェック
                using (var tcpClient = new System.Net.Sockets.TcpClient())
                {
                    var connectTask = tcpClient.ConnectAsync("127.0.0.1", port);
                    var completed = connectTask.Wait(100); // 100ms タイムアウト
                    
                    if (completed && tcpClient.Connected)
                    {
                        // 接続できた = ポートが使用中
                        return false;
                    }
                }
                
                // 接続できなかった = ポートが利用可能（と仮定）
                return true;
            }
            catch (System.Net.Sockets.SocketException)
            {
                // 接続エラー = ポートが利用可能
                return true;
            }
            catch (System.AggregateException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
            {
                // AggregateException内のSocketException = ポートが利用可能（Connection refused）
                return true;
            }
            catch (Exception ex)
            {
                // その他の予期しないエラーのみ警告として記録
                MCPLogger.LogDebug($"{LOG_PREFIX} Could not check port {port} availability: {ex.Message}");
                return true; // 不明な場合は利用可能と仮定
            }
        }
        
        /// <summary>
        /// 現在使用中のポート一覧を取得（キャッシュ機能付き）
        /// </summary>
        /// <returns>ポート使用情報</returns>
        public static Dictionary<string, object> GetPortStatus()
        {
            // キャッシュチェック
            var now = DateTime.UtcNow;
            if (_cachedPortStatus != null && (now - _lastPortStatusUpdate) < PORT_STATUS_CACHE_DURATION)
            {
                return _cachedPortStatus;
            }
            
            var status = new Dictionary<string, object>();
            var projectId = MCPProjectIdentifier.GetProjectId();
            
            try
            {
                var registry = LoadPortRegistry();
                var assignedPort = registry.ContainsKey(projectId) ? registry[projectId] : -1;
                
                status["currentProjectId"] = projectId;
                status["assignedPort"] = assignedPort;
                status["portAvailable"] = assignedPort != -1 ? IsPortAvailable(assignedPort) : false;
                status["registeredProjects"] = registry.Count;
                status["portRange"] = $"{PORT_RANGE_START}-{PORT_RANGE_END}";
                
                // 利用可能ポート数は推定値を使用（重い処理を避ける）
                var estimatedAvailablePorts = (PORT_RANGE_END - PORT_RANGE_START + 1) - registry.Count;
                status["availablePortsCount"] = Math.Max(0, estimatedAvailablePorts);
                
                status["timestamp"] = now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                
                // 詳細情報（登録済みプロジェクトのみ）
                var details = new List<Dictionary<string, object>>();
                foreach (var kvp in registry)
                {
                    details.Add(new Dictionary<string, object>
                    {
                        ["projectId"] = kvp.Key,
                        ["port"] = kvp.Value,
                        ["available"] = "unknown" // 重い処理を避けるため
                    });
                }
                status["registryDetails"] = details;
                
                // キャッシュ更新
                _cachedPortStatus = status;
                _lastPortStatusUpdate = now;
                
            }
            catch (Exception ex)
            {
                status["error"] = ex.Message;
                MCPLogger.LogError($"{LOG_PREFIX} Failed to get port status: {ex.Message}");
            }
            
            return status;
        }
        
        /// <summary>
        /// ポートレジストリをクリーンアップ（使用されていないポートを削除）
        /// </summary>
        public static void CleanupRegistry()
        {
            try
            {
                var registry = LoadPortRegistry();
                var toRemove = new List<string>();
                
                foreach (var kvp in registry)
                {
                    if (IsPortAvailable(kvp.Value))
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var key in toRemove)
                {
                    registry.Remove(key);
                }
                
                if (toRemove.Count > 0)
                {
                    SavePortRegistry(registry);
                    MCPLogger.LogInfo($"{LOG_PREFIX} Cleaned up {toRemove.Count} unused port registrations");
                }
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"{LOG_PREFIX} Failed to cleanup registry: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 指定範囲内で利用可能なポートを検索
        /// </summary>
        private static int FindAvailablePortInRange()
        {
            var registry = LoadPortRegistry();
            var usedPorts = new HashSet<int>(registry.Values);
            
            for (int port = PORT_RANGE_START; port <= PORT_RANGE_END; port++)
            {
                if (!usedPorts.Contains(port) && IsPortAvailable(port))
                {
                    return port;
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// プロジェクトに登録されたポートを取得
        /// </summary>
        private static int GetRegisteredPort(string projectId)
        {
            try
            {
                var registry = LoadPortRegistry();
                return registry.ContainsKey(projectId) ? registry[projectId] : -1;
            }
            catch
            {
                return -1;
            }
        }
        
        /// <summary>
        /// プロジェクトポートを登録
        /// </summary>
        private static void RegisterPort(string projectId, int port)
        {
            try
            {
                var registry = LoadPortRegistry();
                registry[projectId] = port;
                SavePortRegistry(registry);
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"{LOG_PREFIX} Failed to register port: {ex.Message}");
            }
        }
        
        /// <summary>
        /// ポートレジストリを読み込み
        /// </summary>
        private static Dictionary<string, int> LoadPortRegistry()
        {
            try
            {
                if (File.Exists(PORT_REGISTRY_FILE))
                {
                    var json = File.ReadAllText(PORT_REGISTRY_FILE);
                    var wrapper = JsonUtility.FromJson<PortRegistryWrapper>(json);
                    
                    var result = new Dictionary<string, int>();
                    foreach (var entry in wrapper.entries)
                    {
                        result[entry.projectId] = entry.port;
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                MCPLogger.LogWarning($"{LOG_PREFIX} Failed to load port registry: {ex.Message}");
            }
            
            return new Dictionary<string, int>();
        }
        
        /// <summary>
        /// ポートレジストリを保存
        /// </summary>
        private static void SavePortRegistry(Dictionary<string, int> registry)
        {
            try
            {
                var entries = new List<PortRegistryEntry>();
                foreach (var kvp in registry)
                {
                    entries.Add(new PortRegistryEntry { projectId = kvp.Key, port = kvp.Value });
                }
                
                var wrapper = new PortRegistryWrapper { entries = entries.ToArray() };
                var json = JsonUtility.ToJson(wrapper, true);
                
                Directory.CreateDirectory(Path.GetDirectoryName(PORT_REGISTRY_FILE));
                File.WriteAllText(PORT_REGISTRY_FILE, json);
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"{LOG_PREFIX} Failed to save port registry: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 利用可能ポート数をカウント
        /// </summary>
        private static int CountAvailablePorts()
        {
            int count = 0;
            for (int port = PORT_RANGE_START; port <= PORT_RANGE_END; port++)
            {
                if (IsPortAvailable(port))
                {
                    count++;
                }
            }
            return count;
        }
        
        [Serializable]
        private class PortRegistryWrapper
        {
            public PortRegistryEntry[] entries;
        }
        
        [Serializable]
        private class PortRegistryEntry
        {
            public string projectId;
            public int port;
        }
    }
}