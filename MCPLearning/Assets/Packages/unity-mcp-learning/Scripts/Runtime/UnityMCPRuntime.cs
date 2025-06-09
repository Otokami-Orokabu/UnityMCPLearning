using UnityEngine;

namespace UnityMCP.Runtime
{
    /// <summary>
    /// Unity MCP Learning Runtime API
    /// パッケージ利用者向けの基本的なAPI提供
    /// </summary>
    public static class UnityMCPRuntime
    {
        /// <summary>
        /// パッケージ情報
        /// </summary>
        public static class PackageInfo
        {
            public const string Name = "Unity MCP Learning";
            public const string Version = "1.0.0";
            public const string Description = "AI-driven Unity development environment using Model Context Protocol";
        }
        
        /// <summary>
        /// パッケージが正常にインストールされているかチェック
        /// </summary>
        /// <returns>インストール状態</returns>
        public static bool IsPackageInstalled()
        {
            // パッケージの存在確認
            return true; // 基本的には常にtrue（このコードが実行されている時点でインストール済み）
        }
        
        /// <summary>
        /// パッケージ情報をログ出力
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogPackageInfo()
        {
            Debug.Log($"[Unity MCP] Package: {PackageInfo.Name} v{PackageInfo.Version}");
            Debug.Log($"[Unity MCP] Description: {PackageInfo.Description}");
        }
        
        /// <summary>
        /// ランタイムでのパッケージ状態確認
        /// 主にデバッグ・開発用途
        /// </summary>
        public static void CheckRuntimeStatus()
        {
            if (Application.isEditor)
            {
                Debug.Log("[Unity MCP] Running in Unity Editor");
            }
            else
            {
                Debug.Log("[Unity MCP] Running in built application");
            }
        }
    }
}