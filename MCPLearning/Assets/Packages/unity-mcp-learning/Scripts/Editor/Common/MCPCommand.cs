using System;
using System.Collections.Generic;

namespace UnityMCP.Editor
{
    /// <summary>
    /// MCPコマンドの基本構造
    /// </summary>
    [System.Serializable]
    public class MCPCommand
    {
        public string commandId;
        public string commandType;
        public Dictionary<string, object> parameters;
        public string timestamp;
        public CommandStatus status = CommandStatus.Pending;
        public string result;
        public string errorMessage;

        public MCPCommand()
        {
            commandId = System.Guid.NewGuid().ToString();
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            parameters = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// コマンドの実行状態
    /// </summary>
    public enum CommandStatus
    {
        Pending,    // 待機中
        Processing, // 実行中
        Completed,  // 完了
        Failed      // 失敗
    }

    /// <summary>
    /// サポートされるコマンドタイプ
    /// </summary>
    public static class CommandTypes
    {
        public const string CREATE_GAMEOBJECT = "create_gameobject";
        public const string DELETE_GAMEOBJECT = "delete_gameobject";
        public const string MODIFY_TRANSFORM = "modify_transform";
        public const string CREATE_CUBE = "create_cube";
        public const string CREATE_SPHERE = "create_sphere";
        public const string CREATE_PLANE = "create_plane";
    }
}