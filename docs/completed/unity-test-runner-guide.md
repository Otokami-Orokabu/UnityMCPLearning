# Unity Test Runner 導入ガイド

## 概要

Unity Test RunnerはUnity標準のテストフレームワークで、NUnit 3.5をベースにしています。エディターテストとプレイモードテストの両方をサポートし、Unity固有の機能をテストするための専用APIを提供します。

## Unity Test Runnerの利点

1. **Unity統合** - Unity Editorに完全統合
2. **2つのテストモード**
   - Edit Mode Tests: エディター機能のテスト
   - Play Mode Tests: ランタイム動作のテスト
3. **Unity固有のアサーション** - GameObject、コンポーネントなどのテスト
4. **Test Runner Window** - GUIでテスト実行・結果確認

## セットアップ手順

### 1. テスト用フォルダ構造の作成

```
Assets/
└── UnityMCP/
    ├── Editor/          # 既存のエディターコード
    └── Tests/           # テストコード用（新規作成）
        ├── Editor/      # エディターテスト
        └── Runtime/     # ランタイムテスト
```

### 2. Assembly Definition Files の作成

#### Editor Tests用 (Assets/UnityMCP/Tests/Editor/UnityMCP.Editor.Tests.asmdef)
```json
{
    "name": "UnityMCP.Editor.Tests",
    "rootNamespace": "UnityMCP.Tests.Editor",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "UnityMCP.Editor"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### 3. 最初のテストクラス作成

#### データエクスポーターのテスト例

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityMCP.Editor.Exporters;

namespace UnityMCP.Tests.Editor
{
    [TestFixture]
    public class ProjectInfoExporterTests
    {
        private ProjectInfoExporter exporter;

        [SetUp]
        public void Setup()
        {
            exporter = new ProjectInfoExporter();
        }

        [Test]
        public void Export_ReturnsNonNullDictionary()
        {
            // Act
            var result = exporter.Export();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<Dictionary<string, object>>(result);
        }

        [Test]
        public void Export_ContainsRequiredKeys()
        {
            // Act
            var result = exporter.Export();

            // Assert
            Assert.IsTrue(result.ContainsKey("projectName"));
            Assert.IsTrue(result.ContainsKey("unityVersion"));
            Assert.IsTrue(result.ContainsKey("projectPath"));
            Assert.IsTrue(result.ContainsKey("timestamp"));
        }

        [Test]
        public void Export_ProjectNameIsNotEmpty()
        {
            // Act
            var result = exporter.Export();

            // Assert
            var projectName = result["projectName"] as string;
            Assert.IsNotEmpty(projectName);
        }
    }
}
```

#### コマンドプロセッサーのテスト例

```csharp
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityMCP.Editor;

namespace UnityMCP.Tests.Editor
{
    [TestFixture]
    public class MCPCommandProcessorTests
    {
        [Test]
        public void ParseCommand_ValidJSON_ReturnsCommand()
        {
            // Arrange
            string jsonContent = @"{
                ""command"": ""CREATE_CUBE"",
                ""parameters"": {
                    ""name"": ""TestCube"",
                    ""position"": {""x"": 1, ""y"": 2, ""z"": 3}
                }
            }";

            // Act
            var command = MCPCommand.FromJson(jsonContent);

            // Assert
            Assert.IsNotNull(command);
            Assert.AreEqual(CommandType.CREATE_CUBE, command.command);
            Assert.AreEqual("TestCube", command.parameters["name"]);
        }

        [Test]
        public void ParseCommand_InvalidJSON_ThrowsException()
        {
            // Arrange
            string invalidJson = "{ invalid json }";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
            {
                MCPCommand.FromJson(invalidJson);
            });
        }

        [UnityTest]
        public IEnumerator CreateCube_CreatesGameObjectInScene()
        {
            // Arrange
            var processor = new MCPCommandProcessor();
            var command = new MCPCommand
            {
                command = CommandType.CREATE_CUBE,
                parameters = new Dictionary<string, object>
                {
                    ["name"] = "TestCube",
                    ["x"] = 1.0f,
                    ["y"] = 2.0f,
                    ["z"] = 3.0f
                }
            };

            // Act
            yield return processor.ExecuteCommand(command);

            // Assert
            var cube = GameObject.Find("TestCube");
            Assert.IsNotNull(cube);
            Assert.AreEqual(new Vector3(1, 2, 3), cube.transform.position);

            // Cleanup
            if (cube != null)
                Object.DestroyImmediate(cube);
        }
    }
}
```

### 4. Test Runner Windowの使用

1. **開く方法**: `Window > General > Test Runner`
2. **テストの実行**:
   - 個別テスト: テスト名をダブルクリック
   - 全テスト実行: "Run All"ボタン
3. **テストモード切り替え**: EditMode / PlayMode タブ

### 5. コマンドラインからのテスト実行

```bash
# Unity Editor のパス (macOS の例)
/Applications/Unity/Hub/Editor/6000.1.5f1/Unity.app/Contents/MacOS/Unity

# エディターテストの実行
Unity -batchmode -nographics -projectPath ./MCPLearning -runTests -testPlatform EditMode -testResults ./test-results.xml

# プレイモードテストの実行
Unity -batchmode -nographics -projectPath ./MCPLearning -runTests -testPlatform PlayMode -testResults ./test-results.xml
```

### 6. CI/CD統合（GitHub Actions）

```yaml
name: Unity Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - uses: game-ci/unity-test-runner@v2
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
        with:
          projectPath: MCPLearning
          testMode: EditMode
          artifactsPath: test-artifacts
          
      - uses: actions/upload-artifact@v3
        if: always()
        with:
          name: Test results
          path: test-artifacts
```

## ベストプラクティス

### 1. テストの命名規則
```csharp
[Test]
public void MethodName_StateUnderTest_ExpectedBehavior()
{
    // 例: Export_WithValidData_ReturnsCorrectFormat
}
```

### 2. テストの構造（AAA Pattern）
```csharp
[Test]
public void TestMethod()
{
    // Arrange - テストの準備
    var exporter = new DataExporter();
    
    // Act - テスト対象の実行
    var result = exporter.Export();
    
    // Assert - 結果の検証
    Assert.IsNotNull(result);
}
```

### 3. Unity固有のテスト

#### GameObjectのテスト
```csharp
[UnityTest]
public IEnumerator GameObject_Creation_Test()
{
    // GameObjectを作成
    var go = new GameObject("TestObject");
    
    // 1フレーム待機
    yield return null;
    
    // 検証
    Assert.IsNotNull(GameObject.Find("TestObject"));
    
    // クリーンアップ
    Object.DestroyImmediate(go);
}
```

#### エディター機能のテスト
```csharp
[Test]
public void EditorPrefs_SaveAndLoad_Test()
{
    // 保存
    EditorPrefs.SetString("TestKey", "TestValue");
    
    // 読み込み
    var value = EditorPrefs.GetString("TestKey");
    
    // 検証
    Assert.AreEqual("TestValue", value);
    
    // クリーンアップ
    EditorPrefs.DeleteKey("TestKey");
}
```

## トラブルシューティング

### 1. Assembly Definition が認識されない
- Unity を再起動
- `Reimport All` を実行

### 2. テストが実行されない
- Assembly Definition の参照を確認
- `UNITY_INCLUDE_TESTS` 定義を確認

### 3. UnityTest が動作しない
- Play Mode Tests 用の Assembly Definition を確認
- `yield return` ステートメントが含まれているか確認

## まとめ

Unity Test Runnerを使用することで：
- Unity固有の機能を適切にテスト可能
- エディター統合により開発効率向上
- CI/CD パイプラインとの統合が容易

## ✅ 実装完了状況（2025年1月6日）

このガイドで説明された全てのセットアップが完了済みです：

### **完了済み実装**
- [x] **テスト用フォルダ構造**: `Assets/UnityMCP/Tests/Editor/`作成完了
- [x] **Assembly Definition Files**: `UnityMCP.Editor.Tests.asmdef`作成完了  
- [x] **Unity Test Runner設定**: Test Runner Window完全設定
- [x] **エディターテスト実装**: MCPエクスポーター系のテストクラス作成完了
- [x] **Test Runner統合**: Unity Test Runnerでの自動テスト実行環境完備

### **達成された効果**
- ✅ **Unity固有機能のテスト**: データエクスポーター、コマンドプロセッサー等
- ✅ **開発効率向上**: Unity Editor統合によるリアルタイムテスト実行
- ✅ **品質保証**: エディター機能の確実な動作検証

---

**📅 完了日**: 2025年1月6日  
**📊 実装状況**: このガイドに基づくUnity Test Runner環境が完全に構築され、実用レベルで稼働中。*

これにより、Unity MCPプロジェクトの品質向上と保守性の改善が期待できます。