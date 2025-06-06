using UnityEngine;
using UnityEditor;

namespace UnityMCP.Editor
{
    /// <summary>
    /// Console出力テスト用メニュー
    /// </summary>
    public static class TestConsoleOutput
    {
        [MenuItem("UnityMCP/Test Console/Generate Test Logs")]
        public static void GenerateTestLogs()
        {
            // 通常のログメッセージ
            Debug.Log("This is a test log message from UnityMCP");
            Debug.Log("Console integration is working properly!");
            
            // 警告メッセージ
            Debug.LogWarning("This is a warning message - something might need attention");
            Debug.LogWarning("Performance warning: Operation took longer than expected");
            
            // エラーメッセージ
            Debug.LogError("This is an error message - something went wrong!");
            Debug.LogError("Failed to load resource: TestResource.asset");
            
            // Assertメッセージ
            Debug.LogAssertion("This is an assertion message");
            
            // オブジェクト参照付きログ
            if (Selection.activeGameObject != null)
            {
                Debug.Log($"Selected GameObject: {Selection.activeGameObject.name}", Selection.activeGameObject);
            }
            
            MCPLogger.Log("Test logs have been generated successfully!");
        }
        
        [MenuItem("UnityMCP/Test Console/Generate Compilation Error")]
        public static void GenerateCompilationError()
        {
            // わざとコンパイルエラーを生成するコードを作成
            var testFilePath = System.IO.Path.Combine(Application.dataPath, "UnityMCP/Editor/TempCompileError.cs");
            var errorCode = @"using UnityEngine;

namespace UnityMCP.Editor
{
    public class TempCompileError
    {
        public void TestMethod()
        {
            // This will cause a compilation error
            InvalidType myVariable = new InvalidType();
            Debug.Log(myVariable);
        }
    }
}";
            
            System.IO.File.WriteAllText(testFilePath, errorCode);
            AssetDatabase.Refresh();
            
            MCPLogger.Log("Compilation error file created. Unity will now show compilation errors.");
            EditorUtility.DisplayDialog("Compile Error Generated", 
                "A file with compilation errors has been created.\n" +
                "Check the Console for error messages.\n" +
                "Use 'Clear Compilation Error' menu to remove it.", 
                "OK");
        }
        
        [MenuItem("UnityMCP/Test Console/Clear Compilation Error")]
        public static void ClearCompilationError()
        {
            var testFilePath = System.IO.Path.Combine(Application.dataPath, "UnityMCP/Editor/TempCompileError.cs");
            if (System.IO.File.Exists(testFilePath))
            {
                System.IO.File.Delete(testFilePath);
                AssetDatabase.Refresh();
                MCPLogger.Log("Compilation error file removed.");
            }
            else
            {
                MCPLogger.Log("No compilation error file found.");
            }
        }
    }
}