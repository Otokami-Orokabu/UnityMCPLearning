using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityMCP.Editor;

namespace UnityMCP.Common
{
    /// <summary>
    /// 機密データ検出・除外システム
    /// 
    /// Unity MCP Learningで扱うデータから機密情報を自動検出し、
    /// 安全な形式に変換または除外することでプライバシーを保護します。
    /// </summary>
    public static class SensitiveDataFilter
    {
        #region 機密パターン定義
        
        /// <summary>
        /// 機密データパターンの定義
        /// </summary>
        private static readonly Dictionary<SensitiveDataType, Regex[]> SENSITIVE_PATTERNS = new Dictionary<SensitiveDataType, Regex[]>
        {
            [SensitiveDataType.ApiKey] = new[]
            {
                new Regex(@"[A-Za-z0-9]{32,}", RegexOptions.Compiled),                    // 一般的なAPIキー
                new Regex(@"sk-[A-Za-z0-9]{48}", RegexOptions.Compiled),                  // OpenAI APIキー
                new Regex(@"AIza[A-Za-z0-9_-]{35}", RegexOptions.Compiled),               // Google APIキー
                new Regex(@"AKIA[A-Z0-9]{16}", RegexOptions.Compiled),                    // AWS Access Key
                new Regex(@"gh[pousr]_[A-Za-z0-9]{36}", RegexOptions.Compiled),           // GitHub Token
            },
            
            [SensitiveDataType.Password] = new[]
            {
                new Regex(@"password\s*[:=]\s*['""]?([^'"";\s]+)['""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"passwd\s*[:=]\s*['""]?([^'"";\s]+)['""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"pwd\s*[:=]\s*['""]?([^'"";\s]+)['""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            },
            
            [SensitiveDataType.Token] = new[]
            {
                new Regex(@"token\s*[:=]\s*['""]?([A-Za-z0-9_-]{16,})['""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"auth\s*[:=]\s*['""]?([A-Za-z0-9_-]{16,})['""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"bearer\s+([A-Za-z0-9_-]{16,})", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            },
            
            [SensitiveDataType.Email] = new[]
            {
                new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled),
            },
            
            [SensitiveDataType.PersonalInfo] = new[]
            {
                new Regex(@"/Users/[^/\s]+", RegexOptions.Compiled),                       // macOS ユーザーパス
                new Regex(@"C:\\Users\\[^\\s]+", RegexOptions.Compiled),                   // Windows ユーザーパス
                new Regex(@"~[^/\s]*", RegexOptions.Compiled),                             // ホームディレクトリ
            },
            
            [SensitiveDataType.Secret] = new[]
            {
                new Regex(@"secret\s*[:=]\s*['""]?([^'"";\s]+)['""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"private_key\s*[:=]\s*['""]?([^'"";\s]+)['""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"client_secret\s*[:=]\s*['""]?([^'"";\s]+)['""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            }
        };
        
        /// <summary>
        /// 機密データの種類
        /// </summary>
        public enum SensitiveDataType
        {
            ApiKey,
            Password,
            Token,
            Email,
            PersonalInfo,
            Secret
        }
        
        /// <summary>
        /// フィルタリング設定
        /// </summary>
        public class FilterOptions
        {
            public bool MaskSensitiveData { get; set; } = true;
            public bool RemoveSensitiveData { get; set; } = false;
            public bool LogDetections { get; set; } = true;
            public string MaskCharacter { get; set; } = "*";
            public int PreserveLength { get; set; } = 4; // 末尾何文字を保持するか
            public HashSet<SensitiveDataType> EnabledTypes { get; set; } = new HashSet<SensitiveDataType>(Enum.GetValues(typeof(SensitiveDataType)).Cast<SensitiveDataType>());
        }
        
        #endregion
        
        #region 公開API
        
        /// <summary>
        /// テキストから機密データをフィルタリング
        /// </summary>
        /// <param name="text">フィルタリング対象のテキスト</param>
        /// <param name="options">フィルタリングオプション</param>
        /// <returns>フィルタリング済みテキスト</returns>
        public static string FilterSensitiveData(string text, FilterOptions options = null)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            options = options ?? new FilterOptions();
            string filteredText = text;
            var detections = new List<SensitiveDataDetection>();
            
            foreach (var dataType in options.EnabledTypes)
            {
                if (SENSITIVE_PATTERNS.TryGetValue(dataType, out Regex[] patterns))
                {
                    foreach (var pattern in patterns)
                    {
                        var matches = pattern.Matches(filteredText);
                        foreach (Match match in matches)
                        {
                            var detection = new SensitiveDataDetection
                            {
                                Type = dataType,
                                OriginalValue = match.Value,
                                Position = match.Index,
                                Length = match.Length
                            };
                            
                            detections.Add(detection);
                            
                            if (options.RemoveSensitiveData)
                            {
                                filteredText = filteredText.Replace(match.Value, "[REMOVED]");
                            }
                            else if (options.MaskSensitiveData)
                            {
                                string maskedValue = MaskValue(match.Value, options.MaskCharacter, options.PreserveLength);
                                filteredText = filteredText.Replace(match.Value, maskedValue);
                            }
                        }
                    }
                }
            }
            
            if (options.LogDetections && detections.Count > 0)
            {
                LogDetections(detections);
            }
            
            return filteredText;
        }
        
        /// <summary>
        /// JSONデータの機密情報フィルタリング
        /// </summary>
        /// <param name="jsonText">JSONテキスト</param>
        /// <param name="options">フィルタリングオプション</param>
        /// <returns>フィルタリング済みJSONテキスト</returns>
        public static string FilterJsonSensitiveData(string jsonText, FilterOptions options = null)
        {
            options = options ?? new FilterOptions();
            
            // JSON特有の機密フィールドを追加チェック
            var jsonSpecificPatterns = new[]
            {
                new Regex(@"""password""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"""token""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"""secret""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"""api_key""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"""auth""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            };
            
            string filteredJson = FilterSensitiveData(jsonText, options);
            
            // JSON特有のパターンも処理
            foreach (var pattern in jsonSpecificPatterns)
            {
                filteredJson = pattern.Replace(filteredJson, match =>
                {
                    string fieldName = match.Groups[0].Value.Split(':')[0];
                    string maskedValue = MaskValue(match.Groups[1].Value, options.MaskCharacter, options.PreserveLength);
                    return $"{fieldName}: \"{maskedValue}\"";
                });
            }
            
            return filteredJson;
        }
        
        /// <summary>
        /// ファイルパスの機密情報フィルタリング
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>フィルタリング済みファイルパス</returns>
        public static string FilterSensitiveFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return filePath;
            
            string filtered = filePath;
            
            // ユーザー名を置換
            filtered = Regex.Replace(filtered, @"/Users/[^/]+", "/Users/[USER]", RegexOptions.IgnoreCase);
            filtered = Regex.Replace(filtered, @"C:\\Users\\[^\\]+", "C:\\Users\\[USER]", RegexOptions.IgnoreCase);
            
            // ホームディレクトリを置換
            filtered = Regex.Replace(filtered, @"~[^/]*", "~", RegexOptions.IgnoreCase);
            
            return filtered;
        }
        
        /// <summary>
        /// 機密データ検出のみ実行（フィルタリングなし）
        /// </summary>
        /// <param name="text">検査対象テキスト</param>
        /// <param name="enabledTypes">検出対象の機密データタイプ</param>
        /// <returns>検出された機密データのリスト</returns>
        public static List<SensitiveDataDetection> DetectSensitiveData(string text, HashSet<SensitiveDataType> enabledTypes = null)
        {
            if (string.IsNullOrEmpty(text))
                return new List<SensitiveDataDetection>();
            
            enabledTypes = enabledTypes ?? new HashSet<SensitiveDataType>(Enum.GetValues(typeof(SensitiveDataType)).Cast<SensitiveDataType>());
            var detections = new List<SensitiveDataDetection>();
            
            foreach (var dataType in enabledTypes)
            {
                if (SENSITIVE_PATTERNS.TryGetValue(dataType, out Regex[] patterns))
                {
                    foreach (var pattern in patterns)
                    {
                        var matches = pattern.Matches(text);
                        foreach (Match match in matches)
                        {
                            detections.Add(new SensitiveDataDetection
                            {
                                Type = dataType,
                                OriginalValue = match.Value,
                                Position = match.Index,
                                Length = match.Length
                            });
                        }
                    }
                }
            }
            
            return detections;
        }
        
        /// <summary>
        /// 安全な設定値の生成
        /// </summary>
        /// <param name="originalConfig">元の設定値</param>
        /// <returns>機密情報がフィルタリングされた設定値</returns>
        public static string CreateSafeConfig(string originalConfig)
        {
            var options = new FilterOptions
            {
                MaskSensitiveData = true,
                RemoveSensitiveData = false,
                LogDetections = true,
                PreserveLength = 2
            };
            
            return FilterSensitiveData(originalConfig, options);
        }
        
        #endregion
        
        #region プライベートメソッド
        
        /// <summary>
        /// 値をマスク化
        /// </summary>
        private static string MaskValue(string value, string maskChar, int preserveLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            
            if (value.Length <= preserveLength)
            {
                return new string(maskChar[0], value.Length);
            }
            
            string preserved = value.Substring(Math.Max(0, value.Length - preserveLength));
            string masked = new string(maskChar[0], Math.Max(0, value.Length - preserveLength));
            
            return masked + preserved;
        }
        
        /// <summary>
        /// 検出結果をログ出力
        /// </summary>
        private static void LogDetections(List<SensitiveDataDetection> detections)
        {
            var groupedDetections = detections.GroupBy(d => d.Type);
            
            foreach (var group in groupedDetections)
            {
                MCPLogger.LogWarning($"機密データ検出: {group.Key} - {group.Count()}件");
            }
            
            MCPLogger.Log($"機密データフィルタリング完了: 総計{detections.Count}件の機密データを処理");
        }
        
        #endregion
        
        #region データ構造
        
        /// <summary>
        /// 機密データ検出情報
        /// </summary>
        public class SensitiveDataDetection
        {
            public SensitiveDataType Type { get; set; }
            public string OriginalValue { get; set; }
            public int Position { get; set; }
            public int Length { get; set; }
            public DateTime DetectedAt { get; set; } = DateTime.Now;
        }
        
        #endregion
        
        #region 設定・統計
        
        /// <summary>
        /// フィルタ統計情報
        /// </summary>
        public static FilterStatistics GetFilterStatistics()
        {
            return new FilterStatistics
            {
                SupportedTypes = Enum.GetValues(typeof(SensitiveDataType)).Cast<SensitiveDataType>().ToArray(),
                PatternCount = SENSITIVE_PATTERNS.Values.SelectMany(patterns => patterns).Count(),
                LastUpdated = DateTime.Now
            };
        }
        
        /// <summary>
        /// フィルタ統計情報
        /// </summary>
        public class FilterStatistics
        {
            public SensitiveDataType[] SupportedTypes { get; set; }
            public int PatternCount { get; set; }
            public DateTime LastUpdated { get; set; }
        }
        
        #endregion
    }
}