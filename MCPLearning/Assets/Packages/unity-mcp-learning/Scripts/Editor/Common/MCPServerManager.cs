using System;
// using System.Diagnostics; // Commented to avoid ambiguity with UnityEngine.Debug
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
// using Unity.Logging; // Replaced with UnityEngine.Debug

namespace UnityMCP.Editor
{
    /// <summary>
    /// MCPサーバープロセス管理クラス
    /// </summary>
    public class MCPServerManager
    {
        private System.Diagnostics.Process _serverProcess;
        private int _currentPort = 3000;
        private string _serverPath;
        private const string LOG_PREFIX = "[MCPServerManager]";
        private string _lastErrorOutput = "";
        private string _lastStandardOutput = "";
        
        public event Action<string> OnServerOutput;
        public event Action<string> OnServerError;
        public event Action<bool> OnServerStateChanged;
        
        public bool IsServerRunning()
        {
            try
            {
                return _serverProcess != null && !_serverProcess.HasExited;
            }
            catch (System.InvalidOperationException)
            {
                // プロセスが既に終了している場合
                _serverProcess = null;
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Error checking server status: {ex.Message}");
                _serverProcess = null;
                return false;
            }
        }
        
        public int GetCurrentPort()
        {
            return _currentPort;
        }
        
        public void UpdatePort(int port)
        {
            if (port < 1024 || port > 65535)
            {
                Debug.LogError($"{LOG_PREFIX} Invalid port number: {port}. Must be between 1024 and 65535.");
                return;
            }
            
            _currentPort = port;
            Debug.Log($"{LOG_PREFIX} Port updated to: {port}");
        }
        
        public bool StartServer(string serverPath, int port)
        {
            if (IsServerRunning())
            {
                Debug.LogWarning($"{LOG_PREFIX} Server is already running");
                return false;
            }
            
            // unity-mcp-nodeディレクトリを優先的に使用
            var unityMcpNodePath = MCPPackageResolver.GetUnityMcpNodePath();
            if (!string.IsNullOrEmpty(unityMcpNodePath))
            {
                Debug.Log($"{LOG_PREFIX} Using unity-mcp-node directory: {unityMcpNodePath}");
                _serverPath = unityMcpNodePath;
            }
            else
            {
                Debug.LogWarning($"{LOG_PREFIX} unity-mcp-node not found, using Server~ directory: {serverPath}");
                _serverPath = serverPath;
            }
            
            _currentPort = port;
            
            try
            {
                // package.jsonの存在確認
                var packageJsonPath = Path.Combine(serverPath, "package.json");
                if (!File.Exists(packageJsonPath))
                {
                    Debug.LogError($"{LOG_PREFIX} package.json not found at: {packageJsonPath}");
                    OnServerError?.Invoke("package.json not found. Is this a valid Node.js project?");
                    return false;
                }
                
                // distディレクトリの存在確認
                var distPath = Path.Combine(serverPath, "dist");
                if (!Directory.Exists(distPath))
                {
                    Debug.LogWarning($"{LOG_PREFIX} dist directory not found. Running build...");
                    if (!BuildProject(serverPath))
                    {
                        return false;
                    }
                }
                
                // サーバー起動
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = GetNodeExecutable(),
                    Arguments = $"dist/index.js",
                    WorkingDirectory = serverPath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,   // 標準入力をリダイレクト
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Environment =
                    {
                        ["PORT"] = port.ToString(),
                        ["NODE_ENV"] = "production"
                    }
                };
                
                // PATH環境変数にNode.jsのディレクトリを追加
                var currentPath = startInfo.EnvironmentVariables.ContainsKey("PATH") 
                    ? startInfo.EnvironmentVariables["PATH"] 
                    : System.Environment.GetEnvironmentVariable("PATH");
                startInfo.EnvironmentVariables["PATH"] = "/usr/local/bin:/opt/homebrew/bin:" + currentPath;
                
                _serverProcess = new System.Diagnostics.Process { StartInfo = startInfo };
                
                // 出力ハンドラー設定
                _serverProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _lastStandardOutput = e.Data;
                        Debug.Log($"{LOG_PREFIX} [MCP Server] {e.Data}");
                        OnServerOutput?.Invoke(e.Data);
                    }
                };
                
                _serverProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _lastErrorOutput = e.Data;
                        // MCPサーバーの情報メッセージと実際のエラーを区別
                        if (e.Data.Contains("[MCP Server]") || 
                            e.Data.Contains("Starting MCP Server") ||
                            e.Data.Contains("Configuration loaded") ||
                            e.Data.Contains("Data loaded") ||
                            e.Data.Contains("Watching Unity"))
                        {
                            Debug.Log($"{LOG_PREFIX} [MCP Server Info] {e.Data}");
                            OnServerOutput?.Invoke(e.Data);
                        }
                        else
                        {
                            Debug.LogError($"{LOG_PREFIX} [MCP Server Error] {e.Data}");
                            OnServerError?.Invoke(e.Data);
                        }
                    }
                };
                
                _serverProcess.Exited += (sender, e) =>
                {
                    Debug.Log($"{LOG_PREFIX} MCP Server process exited");
                    OnServerStateChanged?.Invoke(false);
                };
                
                // プロセス開始
                if (_serverProcess.Start())
                {
                    _serverProcess.BeginOutputReadLine();
                    _serverProcess.BeginErrorReadLine();
                    
                    Debug.Log($"{LOG_PREFIX} MCP Server started on port {port}");
                    OnServerStateChanged?.Invoke(true);
                    
                    // 短い待機でプロセス状態確認
                    System.Threading.Thread.Sleep(1000);
                    
                    // プロセスが即座に終了していないかチェック
                    if (_serverProcess.HasExited)
                    {
                        Debug.LogError($"{LOG_PREFIX} MCP Server process exited immediately with code: {_serverProcess.ExitCode}");
                        
                        // エラー出力の最後の部分を取得
                        try
                        {
                            if (!string.IsNullOrEmpty(_lastErrorOutput))
                            {
                                Debug.LogError($"{LOG_PREFIX} Server error output: {_lastErrorOutput}");
                            }
                            if (!string.IsNullOrEmpty(_lastStandardOutput))
                            {
                                Debug.LogError($"{LOG_PREFIX} Server standard output: {_lastStandardOutput}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"{LOG_PREFIX} Failed to get error output: {ex.Message}");
                        }
                        
                        return false;
                    }
                    
                    Debug.Log($"{LOG_PREFIX} MCP Server is running (PID: {_serverProcess.Id})");
                    
                    // 標準入力を開いたままにしてサーバーを維持
                    // MCPサーバーは標準入力が閉じられると終了するため
                    Task.Run(async () =>
                    {
                        try
                        {
                            while (_serverProcess != null && !_serverProcess.HasExited)
                            {
                                if (_serverProcess.StandardInput != null && _serverProcess.StandardInput.BaseStream.CanWrite)
                                {
                                    // 標準入力に改行を送信して接続を維持
                                    await _serverProcess.StandardInput.WriteLineAsync("");
                                }
                                await Task.Delay(5000); // 5秒ごと
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // プロセスが既に破棄されている - 正常終了
                        }
                        catch (InvalidOperationException)
                        {
                            // プロセスが既に終了している - 正常終了
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"{LOG_PREFIX} Standard input writer error: {ex.Message}");
                        }
                    });
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to start server: {ex.Message}");
                OnServerError?.Invoke($"Failed to start server: {ex.Message}");
            }
            
            return false;
        }
        
        public void StopServer()
        {
            if (_serverProcess == null || _serverProcess.HasExited)
            {
                Debug.Log($"{LOG_PREFIX} Server is not running");
                return;
            }
            
            try
            {
                // Graceful shutdown - 標準入力を閉じてMCPサーバーを終了
                _serverProcess.StandardInput?.Close();
                
                if (!_serverProcess.WaitForExit(5000))
                {
                    // Force kill if graceful shutdown fails
                    _serverProcess.Kill();
                    Debug.LogWarning($"{LOG_PREFIX} Server was forcefully terminated");
                }
                else
                {
                    Debug.Log($"{LOG_PREFIX} Server stopped gracefully");
                }
                
                _serverProcess.Dispose();
                _serverProcess = null;
                
                OnServerStateChanged?.Invoke(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Error stopping server: {ex.Message}");
            }
        }
        
        private bool BuildProject(string projectPath)
        {
            try
            {
                var npmPath = GetNpmExecutable();
                
                // node_modulesの存在確認
                var nodeModulesPath = Path.Combine(projectPath, "node_modules");
                if (!Directory.Exists(nodeModulesPath))
                {
                    // npm install
                    Debug.Log($"{LOG_PREFIX} Running npm install...");
                    var installStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = npmPath,
                        Arguments = "install",
                        WorkingDirectory = projectPath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    
                    // PATH環境変数にNode.jsのディレクトリを追加
                    var currentPath = installStartInfo.EnvironmentVariables.ContainsKey("PATH") 
                        ? installStartInfo.EnvironmentVariables["PATH"] 
                        : System.Environment.GetEnvironmentVariable("PATH");
                    installStartInfo.EnvironmentVariables["PATH"] = "/usr/local/bin:/opt/homebrew/bin:" + currentPath;
                    
                    var installProcess = System.Diagnostics.Process.Start(installStartInfo);
                
                string output = installProcess.StandardOutput.ReadToEnd();
                string error = installProcess.StandardError.ReadToEnd();
                
                installProcess.WaitForExit();
                
                if (installProcess.ExitCode != 0)
                {
                    Debug.LogError($"{LOG_PREFIX} npm install failed with exit code {installProcess.ExitCode}");
                    Debug.LogError($"{LOG_PREFIX} npm install output: {output}");
                    Debug.LogError($"{LOG_PREFIX} npm install error: {error}");
                    return false;
                }
                }
                else
                {
                    Debug.Log($"{LOG_PREFIX} node_modules already exists, skipping npm install");
                }
                
                // distディレクトリの存在確認
                var distPath = Path.Combine(projectPath, "dist");
                if (!Directory.Exists(distPath))
                {
                    // npm run build
                    Debug.Log($"{LOG_PREFIX} Running npm run build...");
                    var buildStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = npmPath,
                        Arguments = "run build",
                        WorkingDirectory = projectPath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    
                    // PATH環境変数にNode.jsのディレクトリを追加
                    var currentPath = buildStartInfo.EnvironmentVariables.ContainsKey("PATH") 
                        ? buildStartInfo.EnvironmentVariables["PATH"] 
                        : System.Environment.GetEnvironmentVariable("PATH");
                    buildStartInfo.EnvironmentVariables["PATH"] = "/usr/local/bin:/opt/homebrew/bin:" + currentPath;
                    
                    var buildProcess = System.Diagnostics.Process.Start(buildStartInfo);
                    
                    buildProcess.WaitForExit();
                    
                    if (buildProcess.ExitCode != 0)
                    {
                        Debug.LogError($"{LOG_PREFIX} npm run build failed");
                        return false;
                    }
                }
                else
                {
                    Debug.Log($"{LOG_PREFIX} dist directory already exists, skipping npm run build");
                }
                
                Debug.Log($"{LOG_PREFIX} Build completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Build failed: {ex.Message}");
                return false;
            }
        }
        
        private string GetNodeExecutable()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return "node.exe";
            }
            
            // macOS/Linuxでは複数の場所をチェック
            var possiblePaths = new[]
            {
                "/usr/local/bin/node",  // Homebrew
                "/opt/homebrew/bin/node", // Apple Silicon Homebrew
                "/usr/bin/node",        // システム標準
                "node"                  // PATH環境変数
            };
            
            foreach (var path in possiblePaths)
            {
                if (path == "node" || System.IO.File.Exists(path))
                {
                    Debug.Log($"{LOG_PREFIX} Using Node.js executable: {path}");
                    return path;
                }
            }
            
            Debug.LogWarning($"{LOG_PREFIX} Node.js not found in common locations. Falling back to 'node'");
            return "node";
        }
        
        private string GetNpmExecutable()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return "npm.cmd";
            }
            
            // macOS/Linuxでは複数の場所をチェック
            var possiblePaths = new[]
            {
                "/usr/local/bin/npm",   // Homebrew
                "/opt/homebrew/bin/npm", // Apple Silicon Homebrew
                "/usr/bin/npm",         // システム標準
                "npm"                   // PATH環境変数
            };
            
            foreach (var path in possiblePaths)
            {
                Debug.Log($"{LOG_PREFIX} Checking npm path: {path} - Exists: {(path == "npm" ? "unknown" : System.IO.File.Exists(path).ToString())}");
                if (path == "npm" || System.IO.File.Exists(path))
                {
                    Debug.Log($"{LOG_PREFIX} Using npm executable: {path}");
                    return path;
                }
            }
            
            Debug.LogWarning($"{LOG_PREFIX} npm not found in common locations. Falling back to 'npm'");
            return "npm";
        }
        
        public void Dispose()
        {
            StopServer();
        }
    }
}