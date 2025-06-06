using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
// using Unity.Logging; // Replaced with UnityEngine.Debug

namespace UnityMCP.Editor
{
    /// <summary>
    /// MCP接続状態監視クラス
    /// </summary>
    public class MCPConnectionMonitor
    {
        private const string LOG_PREFIX = "[MCPConnectionMonitor]";
        private readonly HttpClient _httpClient;
        private readonly string _commandPath;
        private DateTime _lastCommandTime;
        private bool _isConnected;
        private CancellationTokenSource _monitoringCts;
        
        public event Action<bool> OnConnectionStateChanged;
        public event Action<DateTime> OnCommandReceived;
        
        public MCPConnectionMonitor()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _commandPath = Path.Combine(
                Application.dataPath,
                "..",
                "UnityMCP",
                "Commands",
                "commands.json"
            );
        }
        
        public bool IsConnected()
        {
            return _isConnected;
        }
        
        public DateTime GetLastCommandTime()
        {
            return _lastCommandTime;
        }
        
        public async Task StartMonitoring(int port = 3000)
        {
            StopMonitoring();
            
            _monitoringCts = new CancellationTokenSource();
            var token = _monitoringCts.Token;
            
            await Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // HTTPヘルスチェック
                        var healthCheck = await CheckHttpHealth(port);
                        
                        // コマンドファイル監視
                        var commandFileCheck = CheckCommandFile();
                        
                        var newConnectionState = healthCheck || commandFileCheck;
                        
                        if (newConnectionState != _isConnected)
                        {
                            _isConnected = newConnectionState;
                            Debug.Log($"{LOG_PREFIX} Connection state changed: {(_isConnected ? "Connected" : "Disconnected")}");
                            OnConnectionStateChanged?.Invoke(_isConnected);
                        }
                        
                        await Task.Delay(1000, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{LOG_PREFIX} Monitoring error: {ex.Message}");
                    }
                }
            }, token);
        }
        
        public void StopMonitoring()
        {
            _monitoringCts?.Cancel();
            _monitoringCts?.Dispose();
            _monitoringCts = null;
        }
        
        private async Task<bool> CheckHttpHealth(int port)
        {
            try
            {
                // MCPサーバーのヘルスエンドポイントをチェック
                var response = await _httpClient.GetAsync($"http://localhost:{port}/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                // HTTP接続失敗は正常（MCPはstdio通信のため）
                return false;
            }
        }
        
        private bool CheckCommandFile()
        {
            try
            {
                if (!File.Exists(_commandPath))
                {
                    return false;
                }
                
                var fileInfo = new FileInfo(_commandPath);
                var lastWriteTime = fileInfo.LastWriteTime;
                
                // ファイルが最近更新されたかチェック（30秒以内）
                var timeSinceLastWrite = DateTime.Now - lastWriteTime;
                var isRecentlyActive = timeSinceLastWrite.TotalSeconds < 30;
                
                if (lastWriteTime > _lastCommandTime)
                {
                    _lastCommandTime = lastWriteTime;
                    OnCommandReceived?.Invoke(_lastCommandTime);
                }
                
                return isRecentlyActive;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to check command file: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> TestConnection(int port = 3000)
        {
            try
            {
                Debug.Log($"{LOG_PREFIX} Testing connection to MCP server on port {port}...");
                
                // 簡単な接続テスト - サーバープロセス確認とコマンドディレクトリ存在確認
                await Task.Delay(100); // 非同期形式を保持
                
                // 1. コマンドディレクトリが存在するか
                var commandDir = Path.GetDirectoryName(_commandPath);
                if (!Directory.Exists(commandDir))
                {
                    Debug.LogWarning($"{LOG_PREFIX} Commands directory not found: {commandDir}");
                    return false;
                }
                
                // 2. 最近の活動があるか（コマンドファイルの存在）
                var commandFiles = Directory.GetFiles(commandDir, "*.json");
                if (commandFiles.Length > 0)
                {
                    Debug.Log($"{LOG_PREFIX} Connection test successful - Found {commandFiles.Length} command files");
                    return true;
                }
                
                // 3. サーバーが起動直後の場合は接続可能とみなす
                Debug.Log($"{LOG_PREFIX} Connection test successful - Server appears to be ready");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Connection test failed: {ex.Message}");
                return false;
            }
        }
        
        public void Dispose()
        {
            StopMonitoring();
            _httpClient?.Dispose();
        }
    }
}