using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using System.Linq;

namespace UnityMCP.Editor
{
    /// <summary>
    /// MCPコマンドプロセッサー - コマンドファイルの監視と実行
    /// </summary>
    [InitializeOnLoad]
    public static class MCPCommandProcessor
    {
        private static FileSystemWatcher _commandWatcher;
        private static readonly string COMMAND_DIR = Path.Combine(Application.dataPath, "..", "UnityMCP", "Commands");
        private static readonly string COMMAND_FILE = "command-queue.json";
        private static readonly string RESULT_FILE = "command-result.json";
        
        static MCPCommandProcessor()
        {
            InitializeCommandProcessor();
        }

        private static void InitializeCommandProcessor()
        {
            try
            {
                // コマンドディレクトリを作成
                Directory.CreateDirectory(COMMAND_DIR);
                
                // ファイル監視を開始
                StartCommandWatching();
                
                MCPLogger.Log("[CommandProcessor] コマンドプロセッサーを初期化しました");
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"[CommandProcessor] 初期化エラー: {ex.Message}");
            }
        }

        private static void StartCommandWatching()
        {
            try
            {
                _commandWatcher = new FileSystemWatcher(COMMAND_DIR)
                {
                    Filter = COMMAND_FILE,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true
                };

                _commandWatcher.Changed += OnCommandFileChanged;
                _commandWatcher.Created += OnCommandFileChanged;
                
                MCPLogger.Log($"[CommandProcessor] コマンドファイル監視開始: {COMMAND_DIR}");
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"[CommandProcessor] ファイル監視エラー: {ex.Message}");
            }
        }

        private static void OnCommandFileChanged(object sender, FileSystemEventArgs e)
        {
            // メインスレッドで実行するためEditorApplication.updateを使用
            EditorApplication.delayCall += () => ProcessCommandFile(e.FullPath);
        }

        private static async void ProcessCommandFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return;

                var json = await ReadFileAsync(filePath);
                var command = JsonUtility.FromJson<MCPCommand>(json);
                
                if (command == null)
                {
                    MCPLogger.LogError("[CommandProcessor] 無効なコマンドフォーマット");
                    return;
                }

                MCPLogger.Log($"[CommandProcessor] コマンド受信: {command.commandType} (ID: {command.commandId})");
                
                // コマンドを実行
                await ExecuteCommandAsync(command);
                
                // 結果を書き込み
                await WriteCommandResultAsync(command);
                
                // コマンドファイルを削除
                await DeleteFileAsync(filePath);
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"[CommandProcessor] コマンド処理エラー: {ex.Message}");
            }
        }

        private static async Task ExecuteCommandAsync(MCPCommand command)
        {
            command.status = CommandStatus.Processing;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                await Task.Yield(); // Ensure async context
                
                // コマンドの事前検証
                ValidateCommand(command);
                
                MCPLogger.Log($"[CommandProcessor] コマンド実行開始: {command.commandType} (ID: {command.commandId})");
                
                switch (command.commandType)
                {
                    case CommandTypes.CREATE_CUBE:
                        ExecuteCreateCubeWithValidation(command);
                        break;
                    
                    case CommandTypes.CREATE_SPHERE:
                        ExecuteCreateSphereWithValidation(command);
                        break;
                    
                    case CommandTypes.CREATE_PLANE:
                        ExecuteCreatePlaneWithValidation(command);
                        break;
                    
                    case CommandTypes.CREATE_GAMEOBJECT:
                        ExecuteCreateGameObjectWithValidation(command);
                        break;
                    
                    default:
                        throw new NotSupportedException($"サポートされていないコマンド: {command.commandType}");
                }
                
                stopwatch.Stop();
                command.status = CommandStatus.Completed;
                command.result += $" (実行時間: {stopwatch.ElapsedMilliseconds}ms)";
                MCPLogger.Log($"[CommandProcessor] コマンド実行完了: {command.commandType} ({stopwatch.ElapsedMilliseconds}ms)");
            }
            catch (ArgumentException ex)
            {
                stopwatch.Stop();
                command.status = CommandStatus.Failed;
                command.errorMessage = $"パラメーターエラー: {ex.Message}";
                MCPLogger.LogError($"[CommandProcessor] パラメーターエラー: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                stopwatch.Stop();
                command.status = CommandStatus.Failed;
                command.errorMessage = $"操作エラー: {ex.Message}";
                MCPLogger.LogError($"[CommandProcessor] 操作エラー: {ex.Message}");
            }
            catch (NotSupportedException ex)
            {
                stopwatch.Stop();
                command.status = CommandStatus.Failed;
                command.errorMessage = $"サポートされていないコマンド: {ex.Message}";
                MCPLogger.LogError($"[CommandProcessor] サポートされていないコマンド: {ex.Message}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                command.status = CommandStatus.Failed;
                command.errorMessage = $"予期せぬエラー: {ex.Message}";
                MCPLogger.LogError($"[CommandProcessor] 予期せぬエラー: {ex.GetType().Name} - {ex.Message}");
                MCPLogger.LogError($"[CommandProcessor] スタックトレース: {ex.StackTrace}");
            }
        }

        private static void ExecuteCreateCubeWithValidation(MCPCommand command)
        {
            try
            {
                ExecuteCreateCube(command);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cube作成に失敗しました: {ex.Message}", ex);
            }
        }
        
        private static void ExecuteCreateCube(MCPCommand command)
        {
            var name = GetParameterString(command, "name", "Cube");
            var position = GetParameterVector3(command, "position", Vector3.zero);
            var scale = GetParameterVector3(command, "scale", Vector3.one);
            var color = GetParameterColor(command, "color", Color.white);

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;

            // マテリアルの色を設定
            if (color != Color.white)
            {
                var renderer = cube.GetComponent<Renderer>();
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = color;
                renderer.material = material;
            }

            // Undoに登録
            Undo.RegisterCreatedObjectUndo(cube, $"Create {name}");
            Selection.activeGameObject = cube;

            command.result = $"Cube '{name}' created at {position}";
        }

        private static void ExecuteCreateSphereWithValidation(MCPCommand command)
        {
            try
            {
                ExecuteCreateSphere(command);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Sphere作成に失敗しました: {ex.Message}", ex);
            }
        }
        
        private static void ExecuteCreateSphere(MCPCommand command)
        {
            var name = GetParameterString(command, "name", "Sphere");
            var position = GetParameterVector3(command, "position", Vector3.zero);
            var scale = GetParameterVector3(command, "scale", Vector3.one);

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.position = position;
            sphere.transform.localScale = scale;

            Undo.RegisterCreatedObjectUndo(sphere, $"Create {name}");
            Selection.activeGameObject = sphere;

            command.result = $"Sphere '{name}' created at {position}";
        }

        private static void ExecuteCreatePlaneWithValidation(MCPCommand command)
        {
            try
            {
                ExecuteCreatePlane(command);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Plane作成に失敗しました: {ex.Message}", ex);
            }
        }
        
        private static void ExecuteCreatePlane(MCPCommand command)
        {
            var name = GetParameterString(command, "name", "Plane");
            var position = GetParameterVector3(command, "position", Vector3.zero);
            var scale = GetParameterVector3(command, "scale", Vector3.one);

            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = name;
            plane.transform.position = position;
            plane.transform.localScale = scale;

            Undo.RegisterCreatedObjectUndo(plane, $"Create {name}");
            Selection.activeGameObject = plane;

            command.result = $"Plane '{name}' created at {position}";
        }

        private static void ExecuteCreateGameObjectWithValidation(MCPCommand command)
        {
            try
            {
                ExecuteCreateGameObject(command);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"GameObject作成に失敗しました: {ex.Message}", ex);
            }
        }
        
        private static void ExecuteCreateGameObject(MCPCommand command)
        {
            var name = GetParameterString(command, "name", "GameObject");
            var position = GetParameterVector3(command, "position", Vector3.zero);

            var gameObject = new GameObject(name);
            gameObject.transform.position = position;

            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
            Selection.activeGameObject = gameObject;

            command.result = $"GameObject '{name}' created at {position}";
        }

        private static async Task WriteCommandResultAsync(MCPCommand command)
        {
            try
            {
                var resultPath = Path.Combine(COMMAND_DIR, RESULT_FILE);
                var json = JsonUtility.ToJson(command, true);
                await WriteFileAsync(resultPath, json);
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"[CommandProcessor] 結果書き込みエラー: {ex.Message}");
            }
        }

        // ヘルパーメソッド
        private static string GetParameterString(MCPCommand command, string key, string defaultValue)
        {
            return command.parameters.ContainsKey(key) ? command.parameters[key].ToString() : defaultValue;
        }

        private static Vector3 GetParameterVector3(MCPCommand command, string key, Vector3 defaultValue)
        {
            if (!command.parameters.ContainsKey(key))
                return defaultValue;

            try
            {
                var dict = command.parameters[key] as Dictionary<string, object>;
                if (dict != null)
                {
                    var x = Convert.ToSingle(dict.ContainsKey("x") ? dict["x"] : 0);
                    var y = Convert.ToSingle(dict.ContainsKey("y") ? dict["y"] : 0);
                    var z = Convert.ToSingle(dict.ContainsKey("z") ? dict["z"] : 0);
                    return new Vector3(x, y, z);
                }
            }
            catch
            {
                // パースエラーの場合はデフォルト値を返す
            }
            
            return defaultValue;
        }

        private static Color GetParameterColor(MCPCommand command, string key, Color defaultValue)
        {
            if (!command.parameters.ContainsKey(key))
                return defaultValue;

            try
            {
                var colorString = command.parameters[key].ToString().ToLower();
                switch (colorString)
                {
                    case "red": return Color.red;
                    case "green": return Color.green;
                    case "blue": return Color.blue;
                    case "yellow": return Color.yellow;
                    case "magenta": return Color.magenta;
                    case "cyan": return Color.cyan;
                    case "black": return Color.black;
                    default: return defaultValue;
                }
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 手動でコマンドプロセッサーを停止
        /// </summary>
        public static void StopCommandProcessor()
        {
            _commandWatcher?.Dispose();
            _commandWatcher = null;
            MCPLogger.Log("[CommandProcessor] コマンドプロセッサーを停止しました");
        }
        
        /// <summary>
        /// コマンドの事前検証
        /// </summary>
        private static void ValidateCommand(MCPCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command), "コマンドがnullです");
                
            if (string.IsNullOrEmpty(command.commandId))
                throw new ArgumentException("コマンドIDが空です", nameof(command.commandId));
                
            if (string.IsNullOrEmpty(command.commandType))
                throw new ArgumentException("コマンドタイプが空です", nameof(command.commandType));
                
            if (command.parameters == null)
                command.parameters = new Dictionary<string, object>();
                
            // コマンドタイプの検証
            var validTypes = new[] { 
                CommandTypes.CREATE_CUBE, 
                CommandTypes.CREATE_SPHERE, 
                CommandTypes.CREATE_PLANE, 
                CommandTypes.CREATE_GAMEOBJECT 
            };
            
            if (!validTypes.Any(t => t == command.commandType))
            {
                throw new NotSupportedException($"サポートされていないコマンドタイプ: {command.commandType}");
            }
            
            // パラメータの基本検証
            ValidateCommandParameters(command);
        }
        
        /// <summary>
        /// コマンドパラメータの検証
        /// </summary>
        private static void ValidateCommandParameters(MCPCommand command)
        {
            switch (command.commandType)
            {
                case CommandTypes.CREATE_CUBE:
                case CommandTypes.CREATE_SPHERE:
                case CommandTypes.CREATE_PLANE:
                case CommandTypes.CREATE_GAMEOBJECT:
                    ValidateNameParameter(command);
                    ValidatePositionParameter(command);
                    ValidateScaleParameter(command);
                    
                    if (command.commandType == CommandTypes.CREATE_CUBE)
                    {
                        ValidateColorParameter(command);
                    }
                    break;
            }
        }
        
        private static void ValidateNameParameter(MCPCommand command)
        {
            if (command.parameters.ContainsKey("name"))
            {
                var name = command.parameters["name"].ToString();
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("名前は空にできません");
                    
                if (name.Length > 50)
                    throw new ArgumentException("名前は50文字以内で入力してください");
                    
                // 無効な文字のチェック
                if (name.IndexOfAny(new char[] { '<', '>', ':', '"', '|', '?', '*' }) >= 0)
                    throw new ArgumentException("名前に無効な文字が含まれています");
            }
        }
        
        private static void ValidatePositionParameter(MCPCommand command)
        {
            if (command.parameters.ContainsKey("position"))
            {
                ValidateVector3Parameter(command.parameters["position"], "position", -1000f, 1000f);
            }
        }
        
        private static void ValidateScaleParameter(MCPCommand command)
        {
            if (command.parameters.ContainsKey("scale"))
            {
                ValidateVector3Parameter(command.parameters["scale"], "scale", 0.01f, 100f);
            }
        }
        
        private static void ValidateColorParameter(MCPCommand command)
        {
            if (command.parameters.ContainsKey("color"))
            {
                var color = command.parameters["color"].ToString().ToLower();
                var validColors = new[] { "red", "green", "blue", "yellow", "magenta", "cyan", "black", "white" };
                
                if (!validColors.Any(c => c == color))
                {
                    throw new ArgumentException($"無効な色です。有効な色: {string.Join(", ", validColors)}");
                }
            }
        }
        
        private static void ValidateVector3Parameter(object vectorObj, string paramName, float min, float max)
        {
            if (vectorObj is Dictionary<string, object> dict)
            {
                foreach (var axis in new[] { "x", "y", "z" })
                {
                    if (dict.ContainsKey(axis))
                    {
                        if (!float.TryParse(dict[axis].ToString(), out float value))
                            throw new ArgumentException($"{paramName}.{axis}は数値である必要があります");
                            
                        if (value < min || value > max)
                            throw new ArgumentException($"{paramName}.{axis}は{min}から{max}の間である必要があります");
                    }
                }
            }
        }
        
        // Async file operations using Unity's awaiter pattern
        private static async Task<string> ReadFileAsync(string filePath)
        {
            await Task.Yield(); // Switch to background thread
            return File.ReadAllText(filePath);
        }
        
        private static async Task WriteFileAsync(string filePath, string content)
        {
            await Task.Yield(); // Switch to background thread
            File.WriteAllText(filePath, content);
        }
        
        private static async Task DeleteFileAsync(string filePath)
        {
            await Task.Yield(); // Switch to background thread
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}