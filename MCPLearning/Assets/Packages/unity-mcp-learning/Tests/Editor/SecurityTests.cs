using NUnit.Framework;
using UnityMCP.Common;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP.Tests.Editor
{
    /// <summary>
    /// セキュリティ機能のUnity Test Runner テストケース
    /// </summary>
    public class SecurityTests
    {
        #region PathSecurityValidator Tests
        
        [Test]
        public void PathSecurityValidator_ValidOutputPath_ReturnsTrue()
        {
            // Arrange
            string validPath = "UnityMCP/Data/test.json";
            
            // Act
            bool result = PathSecurityValidator.ValidateOutputPath(validPath);
            
            // Assert
            Assert.IsTrue(result, "有効な出力パスが拒否されました");
        }
        
        [Test]
        public void PathSecurityValidator_PathTraversalAttack_ReturnsFalse()
        {
            // Arrange
            string[] dangerousPaths = {
                "../../../etc/passwd",
                "UnityMCP/Data/../../../secrets.txt",
                "UnityMCP\\Data\\..\\..\\config.ini",
                "~/Documents/secret.txt",
                "%USERPROFILE%/secret.txt"
            };
            
            // Act & Assert
            foreach (string dangerousPath in dangerousPaths)
            {
                bool result = PathSecurityValidator.ValidateOutputPath(dangerousPath);
                Assert.IsFalse(result, $"危険なパス '{dangerousPath}' が許可されました");
            }
        }
        
        [Test]
        public void PathSecurityValidator_UnauthorizedDirectory_ReturnsFalse()
        {
            // Arrange
            string[] unauthorizedPaths = {
                "Assets/Scripts/test.cs",
                "ProjectSettings/test.asset",
                "Library/test.cache",
                "System32/test.dll"
            };
            
            // Act & Assert
            foreach (string unauthorizedPath in unauthorizedPaths)
            {
                bool result = PathSecurityValidator.ValidateOutputPath(unauthorizedPath);
                Assert.IsFalse(result, $"許可されていないディレクトリ '{unauthorizedPath}' が許可されました");
            }
        }
        
        [Test]
        public void PathSecurityValidator_CreateSafePath_GeneratesValidPath()
        {
            // Arrange
            string baseDirectory = "UnityMCP/Data";
            string filename = "test-file.json";
            
            // Act
            string safePath = PathSecurityValidator.CreateSafePath(baseDirectory, filename);
            
            // Assert
            Assert.IsNotNull(safePath);
            Assert.IsTrue(safePath.Contains(baseDirectory));
            Assert.IsTrue(safePath.Contains(filename));
        }
        
        [Test]
        public void PathSecurityValidator_SanitizeFilename_RemovesDangerousCharacters()
        {
            // Arrange & Act
            string safePath = PathSecurityValidator.CreateSafePath("UnityMCP/Data", "test<>|file.json");
            
            // Assert
            Assert.IsNotNull(safePath);
            // ファイル名部分のみを抽出してチェック
            string filename = System.IO.Path.GetFileName(safePath);
            Assert.IsFalse(filename.Contains("<"), $"< character not sanitized in: {filename}");
            Assert.IsFalse(filename.Contains(">"), $"> character not sanitized in: {filename}");
            Assert.IsFalse(filename.Contains("|"), $"| character not sanitized in: {filename}");
        }
        
        [Test]
        public void PathSecurityValidator_ValidReadPath_ReturnsTrue()
        {
            // Arrange
            string[] validReadPaths = {
                "Assets/Scripts/test.cs",
                "ProjectSettings/ProjectSettings.asset",
                "UnityMCP/Data/test.json"
            };
            
            // Act & Assert
            foreach (string validPath in validReadPaths)
            {
                bool result = PathSecurityValidator.ValidateReadPath(validPath);
                Assert.IsTrue(result, $"有効な読み取りパス '{validPath}' が拒否されました");
            }
        }
        
        [Test]
        public void PathSecurityValidator_CreateSafeDirectory_CreatesOnlyAllowedDirectories()
        {
            // Arrange
            string validDirectory = "UnityMCP/Logs/test";
            string invalidDirectory = "Assets/test";
            
            // Act
            bool validResult = PathSecurityValidator.CreateSafeDirectory(validDirectory);
            bool invalidResult = PathSecurityValidator.CreateSafeDirectory(invalidDirectory);
            
            // Assert
            Assert.IsTrue(validResult, "有効なディレクトリの作成が失敗しました");
            Assert.IsFalse(invalidResult, "無効なディレクトリの作成が許可されました");
        }
        
        #endregion
        
        #region SensitiveDataFilter Tests
        
        [Test]
        public void SensitiveDataFilter_ApiKeyDetection_DetectsApiKeys()
        {
            // Arrange
            string textWithApiKey = "API_KEY=sk-1234567890abcdef1234567890abcdef12345678 config";
            
            // Act
            var detections = SensitiveDataFilter.DetectSensitiveData(textWithApiKey);
            
            // Assert
            Assert.IsTrue(detections.Any(d => d.Type == SensitiveDataFilter.SensitiveDataType.ApiKey), 
                "APIキーが検出されませんでした");
        }
        
        [Test]
        public void SensitiveDataFilter_PasswordMasking_MasksPasswords()
        {
            // Arrange
            string textWithPassword = "password=mySecretPassword123";
            var options = new SensitiveDataFilter.FilterOptions
            {
                MaskSensitiveData = true,
                MaskCharacter = "*"
            };
            
            // Act
            string filteredText = SensitiveDataFilter.FilterSensitiveData(textWithPassword, options);
            
            // Assert
            Assert.IsFalse(filteredText.Contains("mySecretPassword123"), 
                "パスワードがマスクされていません");
            Assert.IsTrue(filteredText.Contains("*"), 
                "マスク文字が使用されていません");
        }
        
        [Test]
        public void SensitiveDataFilter_EmailDetection_DetectsEmails()
        {
            // Arrange
            string textWithEmail = "Contact us at admin@example.com for support";
            
            // Act
            var detections = SensitiveDataFilter.DetectSensitiveData(textWithEmail);
            
            // Assert
            Assert.IsTrue(detections.Any(d => d.Type == SensitiveDataFilter.SensitiveDataType.Email), 
                "Eメールアドレスが検出されませんでした");
        }
        
        [Test]
        public void SensitiveDataFilter_PersonalInfoFiltering_FiltersUserPaths()
        {
            // Arrange
            string textWithPersonalInfo = "File located at /Users/johndoe/Documents/secret.txt";
            
            // Act
            string filteredText = SensitiveDataFilter.FilterSensitiveData(textWithPersonalInfo);
            
            // Assert
            Assert.IsFalse(filteredText.Contains("johndoe"), 
                "ユーザー名がフィルタリングされていません");
        }
        
        [Test]
        public void SensitiveDataFilter_JsonSensitiveDataFiltering_FiltersJsonFields()
        {
            // Arrange
            string jsonWithSensitiveData = @"{""password"": ""secretPass123"", ""username"": ""admin""}";
            
            // Act
            string filteredJson = SensitiveDataFilter.FilterJsonSensitiveData(jsonWithSensitiveData);
            
            // Assert
            Assert.IsFalse(filteredJson.Contains("secretPass123"), 
                "JSONパスワードフィールドがフィルタリングされていません");
        }
        
        [Test]
        public void SensitiveDataFilter_FilePathFiltering_CleansFilePaths()
        {
            // Arrange
            string[] personalPaths = {
                "/Users/testuser/Documents/file.txt",
                "C:\\Users\\testuser\\Documents\\file.txt",
                "~/Documents/file.txt"
            };
            
            // Act & Assert
            foreach (string personalPath in personalPaths)
            {
                string filteredPath = SensitiveDataFilter.FilterSensitiveFilePath(personalPath);
                Assert.IsFalse(filteredPath.Contains("testuser"), 
                    $"ファイルパス '{personalPath}' でユーザー名がフィルタリングされていません");
            }
        }
        
        [Test]
        public void SensitiveDataFilter_MultipleDataTypes_DetectsAllTypes()
        {
            // Arrange
            string complexText = @"
                password=secret123
                token=abc123def456
                email=user@domain.com
                path=/Users/john/file.txt
                api_key=sk-1234567890abcdef
            ";
            
            // Act
            var detections = SensitiveDataFilter.DetectSensitiveData(complexText);
            
            // Assert
            var detectedTypes = detections.Select(d => d.Type).Distinct().ToList();
            Assert.IsTrue(detectedTypes.Count >= 3, 
                $"複数の機密データタイプが検出されませんでした。検出されたタイプ: {detectedTypes.Count}");
        }
        
        [Test]
        public void SensitiveDataFilter_RemoveMode_RemovesSensitiveData()
        {
            // Arrange
            string textWithSensitive = "password=secret123 and token=abc456";
            var options = new SensitiveDataFilter.FilterOptions
            {
                RemoveSensitiveData = true,
                MaskSensitiveData = false
            };
            
            // Act
            string filteredText = SensitiveDataFilter.FilterSensitiveData(textWithSensitive, options);
            
            // Assert
            Assert.IsTrue(filteredText.Contains("[REMOVED]"), 
                "機密データが除去されていません");
            Assert.IsFalse(filteredText.Contains("secret123"), 
                "機密データが残存しています");
        }
        
        [Test]
        public void SensitiveDataFilter_CreateSafeConfig_GeneratesSafeConfiguration()
        {
            // Arrange
            string unsafeConfig = @"
                server_url=https://api.example.com
                api_key=sk-1234567890abcdef1234567890abcdef12345678
                password=myPassword123
                debug_mode=true
            ";
            
            // Act
            string safeConfig = SensitiveDataFilter.CreateSafeConfig(unsafeConfig);
            
            // Assert
            Assert.IsFalse(safeConfig.Contains("sk-1234567890abcdef1234567890abcdef12345678"), 
                "APIキーが安全に変換されていません");
            Assert.IsFalse(safeConfig.Contains("myPassword123"), 
                "パスワードが安全に変換されていません");
            Assert.IsTrue(safeConfig.Contains("https://api.example.com"), 
                "非機密情報が不適切に変換されています");
            Assert.IsTrue(safeConfig.Contains("debug_mode=true"), 
                "設定項目が不適切に変換されています");
        }
        
        [Test]
        public void SensitiveDataFilter_GetFilterStatistics_ReturnsValidStatistics()
        {
            // Act
            var statistics = SensitiveDataFilter.GetFilterStatistics();
            
            // Assert
            Assert.IsNotNull(statistics, "統計情報がnullです");
            Assert.Greater(statistics.SupportedTypes.Length, 0, "サポートされているタイプがありません");
            Assert.Greater(statistics.PatternCount, 0, "パターン数が0です");
        }
        
        #endregion
        
        #region 統合テスト
        
        [Test]
        public void Security_IntegratedPathAndDataFiltering_WorksTogether()
        {
            // Arrange
            string sensitiveContent = @"{
                ""file_path"": ""/Users/admin/secrets/config.json"",
                ""api_key"": ""sk-1234567890abcdef"",
                ""data_path"": ""UnityMCP/Data/output.json""
            }";
            
            // Act
            string filteredContent = SensitiveDataFilter.FilterJsonSensitiveData(sensitiveContent);
            bool isPathSafe = PathSecurityValidator.ValidateOutputPath("UnityMCP/Data/output.json");
            bool isPathUnsafe = PathSecurityValidator.ValidateOutputPath("/Users/admin/secrets/config.json");
            
            // Assert
            Assert.IsFalse(filteredContent.Contains("/Users/admin"), 
                "ユーザーパスがフィルタリングされていません");
            Assert.IsFalse(filteredContent.Contains("sk-1234567890abcdef"), 
                "APIキーがフィルタリングされていません");
            Assert.IsTrue(isPathSafe, "安全なパスが拒否されました");
            Assert.IsFalse(isPathUnsafe, "危険なパスが許可されました");
        }
        
        [Test]
        public void Security_Performance_FilteringLargeText()
        {
            // Arrange
            var largeText = string.Join("\n", Enumerable.Repeat("password=secret123 token=abc456 /Users/test/file.txt", 1000));
            
            // Act
            var startTime = System.DateTime.Now;
            string filteredText = SensitiveDataFilter.FilterSensitiveData(largeText);
            var duration = System.DateTime.Now - startTime;
            
            // Assert
            Assert.Less(duration.TotalSeconds, 5.0, "フィルタリングが遅すぎます");
            Assert.IsFalse(filteredText.Contains("secret123"), "大きなテキストでフィルタリングが失敗しました");
        }
        
        #endregion
    }
}