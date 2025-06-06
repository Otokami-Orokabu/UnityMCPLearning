// PathSecurityValidator - Fixed MCPLogger references
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityMCP.Editor;

namespace UnityMCP.Common
{
    /// <summary>
    /// パストラバーサル攻撃防止とファイルアクセス安全性検証
    /// 
    /// Unity MCP Learningにおけるファイルアクセスのセキュリティを担保し、
    /// 外部からの不正なパス指定による機密ファイルアクセスを防止します。
    /// </summary>
    public static class PathSecurityValidator
    {
        #region 許可ディレクトリ定義
        
        /// <summary>
        /// 出力が許可されるディレクトリのリスト
        /// Assets外の安全な領域に限定
        /// </summary>
        private static readonly string[] ALLOWED_OUTPUT_DIRECTORIES = {
            "UnityMCP/Data",
            "UnityMCP/Commands",
            "UnityMCP/Logs",
            "Temp/UnityMCP"
        };
        
        /// <summary>
        /// 読み取りが許可されるディレクトリのリスト
        /// プロジェクト内の必要最小限に限定
        /// </summary>
        private static readonly string[] ALLOWED_READ_DIRECTORIES = {
            "Assets",
            "ProjectSettings",
            "Packages",
            "UnityMCP"
        };
        
        /// <summary>
        /// 危険なパターンのリスト
        /// </summary>
        private static readonly string[] DANGEROUS_PATTERNS = {
            "..",           // パストラバーサル
            "~",            // ホームディレクトリ
            "%",            // 環境変数
            "$",            // 環境変数（Unix）
            ":",            // ドライブ指定（Windows）
            "//",           // UNCパス
            "\\\\",         // UNCパス
            ".ssh",         // SSH設定
            ".aws",         // AWS設定
            ".env",         // 環境変数ファイル
            "password",     // パスワードファイル
            "secret",       // シークレットファイル
            "private",      // プライベートファイル
            "config"        // 設定ファイル（一部制限）
        };
        
        #endregion
        
        #region 公開API
        
        /// <summary>
        /// 出力用パスの安全性を検証
        /// </summary>
        /// <param name="relativePath">プロジェクトルートからの相対パス</param>
        /// <returns>安全な場合はtrue</returns>
        public static bool ValidateOutputPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                UnityMCP.Editor.MCPLogger.LogWarning("出力パスが空です");
                return false;
            }
            
            // 基本的な危険パターンチェック
            if (!IsBasicallySafe(relativePath))
            {
                UnityMCP.Editor.MCPLogger.LogError($"危険なパターンが検出されました: {relativePath}");
                return false;
            }
            
            // 許可ディレクトリチェック
            bool isAllowed = ALLOWED_OUTPUT_DIRECTORIES.Any(allowedDir => 
                relativePath.Replace("\\", "/").StartsWith(allowedDir, StringComparison.OrdinalIgnoreCase));
            
            if (!isAllowed)
            {
                UnityMCP.Editor.MCPLogger.LogError($"許可されていない出力ディレクトリです: {relativePath}");
                UnityMCP.Editor.MCPLogger.Log($"許可ディレクトリ: {string.Join(", ", ALLOWED_OUTPUT_DIRECTORIES)}");
                return false;
            }
            
            // フルパス解決による追加検証
            return ValidateResolvedPath(relativePath, PathType.Output);
        }
        
        /// <summary>
        /// 読み取り用パスの安全性を検証
        /// </summary>
        /// <param name="relativePath">プロジェクトルートからの相対パス</param>
        /// <returns>安全な場合はtrue</returns>
        public static bool ValidateReadPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                UnityMCP.Editor.MCPLogger.LogWarning("読み取りパスが空です");
                return false;
            }
            
            // 基本的な危険パターンチェック
            if (!IsBasicallySafe(relativePath))
            {
                UnityMCP.Editor.MCPLogger.LogError($"危険なパターンが検出されました: {relativePath}");
                return false;
            }
            
            // 許可ディレクトリチェック
            bool isAllowed = ALLOWED_READ_DIRECTORIES.Any(allowedDir => 
                relativePath.Replace("\\", "/").StartsWith(allowedDir, StringComparison.OrdinalIgnoreCase));
            
            if (!isAllowed)
            {
                UnityMCP.Editor.MCPLogger.LogError($"許可されていない読み取りディレクトリです: {relativePath}");
                return false;
            }
            
            // フルパス解決による追加検証
            return ValidateResolvedPath(relativePath, PathType.Read);
        }
        
        /// <summary>
        /// 安全なファイルパスを生成
        /// </summary>
        /// <param name="baseDirectory">ベースディレクトリ（許可リスト内）</param>
        /// <param name="filename">ファイル名</param>
        /// <returns>安全なフルパス</returns>
        public static string CreateSafePath(string baseDirectory, string filename)
        {
            // ベースディレクトリの検証
            if (!ALLOWED_OUTPUT_DIRECTORIES.Contains(baseDirectory))
            {
                throw new ArgumentException($"許可されていないベースディレクトリ: {baseDirectory}");
            }
            
            // ファイル名のサニタイズ
            string safeFilename = SanitizeFilename(filename);
            
            // プロジェクトルート取得
            string projectRoot = GetProjectRoot();
            
            // パス結合
            string fullPath = Path.Combine(projectRoot, baseDirectory, safeFilename);
            
            // 最終検証
            string relativePath = Path.Combine(baseDirectory, safeFilename);
            if (!ValidateOutputPath(relativePath))
            {
                throw new InvalidOperationException($"安全なパス生成に失敗: {relativePath}");
            }
            
            return fullPath;
        }
        
        /// <summary>
        /// ディレクトリの安全な作成
        /// </summary>
        /// <param name="relativePath">作成するディレクトリの相対パス</param>
        /// <returns>作成に成功した場合はtrue</returns>
        public static bool CreateSafeDirectory(string relativePath)
        {
            if (!ValidateOutputPath(relativePath))
            {
                return false;
            }
            
            try
            {
                string projectRoot = GetProjectRoot();
                string fullPath = Path.Combine(projectRoot, relativePath);
                
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    UnityMCP.Editor.MCPLogger.Log($"安全なディレクトリを作成: {relativePath}");
                }
                
                return true;
            }
            catch (Exception e)
            {
                UnityMCP.Editor.MCPLogger.LogError($"ディレクトリ作成エラー: {e.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region プライベートメソッド
        
        /// <summary>
        /// パスの種類
        /// </summary>
        private enum PathType
        {
            Output,
            Read
        }
        
        /// <summary>
        /// 基本的な危険パターンのチェック
        /// </summary>
        private static bool IsBasicallySafe(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            
            // 絶対パスチェック
            if (Path.IsPathRooted(path))
            {
                UnityMCP.Editor.MCPLogger.LogError($"絶対パスは許可されていません: {path}");
                return false;
            }
            
            // 危険パターンチェック
            string normalizedPath = path.Replace("\\", "/").ToLowerInvariant();
            
            foreach (string dangerousPattern in DANGEROUS_PATTERNS)
            {
                if (normalizedPath.Contains(dangerousPattern.ToLowerInvariant()))
                {
                    UnityMCP.Editor.MCPLogger.LogError($"危険なパターン '{dangerousPattern}' が検出されました: {path}");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// フルパス解決による詳細検証
        /// </summary>
        private static bool ValidateResolvedPath(string relativePath, PathType pathType)
        {
            try
            {
                string projectRoot = GetProjectRoot();
                string fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
                string projectRootFull = Path.GetFullPath(projectRoot);
                
                // プロジェクトルート内に限定
                if (!fullPath.StartsWith(projectRootFull, StringComparison.OrdinalIgnoreCase))
                {
                    UnityMCP.Editor.MCPLogger.LogError($"プロジェクトルート外への アクセス試行: {fullPath}");
                    return false;
                }
                
                // パス長制限（Windows対応）
                if (fullPath.Length > 260)
                {
                    UnityMCP.Editor.MCPLogger.LogError($"パスが長すぎます: {fullPath.Length} characters");
                    return false;
                }
                
                UnityMCP.Editor.MCPLogger.Log($"パス検証成功 ({pathType}): {relativePath} -> {fullPath}");
                return true;
            }
            catch (Exception e)
            {
                UnityMCP.Editor.MCPLogger.LogError($"パス解決エラー: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// ファイル名のサニタイズ
        /// </summary>
        private static string SanitizeFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("ファイル名が空です");
            }
            
            // 無効な文字を除去
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = filename;
            
            foreach (char invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }
            
            // 危険な文字を追加除去（OSによってはPath.GetInvalidFileNameCharsに含まれない場合がある）
            sanitized = sanitized.Replace("..", "_");
            sanitized = sanitized.Replace("~", "_");
            sanitized = sanitized.Replace("<", "_");
            sanitized = sanitized.Replace(">", "_");
            sanitized = sanitized.Replace("|", "_");
            
            // 長さ制限
            if (sanitized.Length > 100)
            {
                string extension = Path.GetExtension(sanitized);
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
                sanitized = nameWithoutExtension.Substring(0, 100 - extension.Length) + extension;
            }
            
            return sanitized;
        }
        
        /// <summary>
        /// プロジェクトルートパスの取得
        /// </summary>
        private static string GetProjectRoot()
        {
            return Application.dataPath.Replace("/Assets", "");
        }
        
        #endregion
        
        #region 設定・統計
        
        /// <summary>
        /// セキュリティ設定の情報を取得
        /// </summary>
        public static SecurityInfo GetSecurityInfo()
        {
            return new SecurityInfo
            {
                AllowedOutputDirectories = ALLOWED_OUTPUT_DIRECTORIES,
                AllowedReadDirectories = ALLOWED_READ_DIRECTORIES,
                DangerousPatterns = DANGEROUS_PATTERNS,
                ProjectRoot = GetProjectRoot()
            };
        }
        
        /// <summary>
        /// セキュリティ設定情報
        /// </summary>
        public class SecurityInfo
        {
            public string[] AllowedOutputDirectories { get; set; }
            public string[] AllowedReadDirectories { get; set; }
            public string[] DangerousPatterns { get; set; }
            public string ProjectRoot { get; set; }
        }
        
        #endregion
    }
}