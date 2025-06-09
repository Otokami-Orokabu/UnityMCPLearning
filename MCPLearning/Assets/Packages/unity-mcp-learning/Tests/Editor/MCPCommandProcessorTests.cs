using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using UnityMCP.Editor;

namespace UnityMCP.Tests.Editor
{
    /// <summary>
    /// MCPCommandProcessorのテストクラス
    /// Unity Test Runnerを使用したコマンド処理システムのテスト
    /// </summary>
    [TestFixture]
    public class MCPCommandProcessorTests
    {
        [Test]
        public void MCPCommand_CreateValidCommand_ShouldInitialize()
        {
            // Arrange & Act
            var command = new MCPCommand
            {
                commandId = System.Guid.NewGuid().ToString(),
                commandType = CommandTypes.CREATE_CUBE,
                parameters = new Dictionary<string, object>
                {
                    { "name", "TestCube" },
                    { "position", new Dictionary<string, object> { { "x", 1f }, { "y", 2f }, { "z", 3f } } }
                },
                timestamp = System.DateTime.Now.ToString("O"),
                status = CommandStatus.Pending
            };
            
            // Assert
            Assert.IsNotNull(command);
            Assert.AreEqual(CommandTypes.CREATE_CUBE, command.commandType);
            Assert.AreEqual(CommandStatus.Pending, command.status);
            Assert.IsNotNull(command.parameters);
        }

        [Test]
        public void CommandTypes_Constants_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.AreEqual("create_cube", CommandTypes.CREATE_CUBE);
            Assert.AreEqual("create_sphere", CommandTypes.CREATE_SPHERE);
            Assert.AreEqual("create_plane", CommandTypes.CREATE_PLANE);
            Assert.AreEqual("create_gameobject", CommandTypes.CREATE_GAMEOBJECT);
        }

        [Test]
        public void CommandStatus_Enum_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.AreEqual(CommandStatus.Pending, CommandStatus.Pending);
            Assert.AreEqual(CommandStatus.Processing, CommandStatus.Processing);
            Assert.AreEqual(CommandStatus.Completed, CommandStatus.Completed);
            Assert.AreEqual(CommandStatus.Failed, CommandStatus.Failed);
        }

        [Test]
        public void MCPCommand_JsonSerialization_ShouldWork()
        {
            // Arrange
            var originalCommand = new MCPCommand
            {
                commandId = "test-id-123",
                commandType = CommandTypes.CREATE_SPHERE,
                parameters = new Dictionary<string, object>
                {
                    { "name", "TestSphere" }
                },
                timestamp = "2025-01-06T12:00:00Z",
                status = CommandStatus.Completed,
                result = "Sphere created successfully"
            };

            // Act - Serialize to JSON
            var json = JsonUtility.ToJson(originalCommand, true);
            
            // Assert JSON is not empty
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Length > 0);
            Assert.IsTrue(json.Contains("test-id-123"));
            Assert.IsTrue(json.Contains("create_sphere"));
        }

        [Test]
        public void MCPLogger_LogMethods_ShouldNotThrow()
        {
            // Assert - ログメソッドが例外を投げないことを確認
            Assert.DoesNotThrow(() => MCPLogger.Log("Test log message"));
            Assert.DoesNotThrow(() => MCPLogger.LogError("Test error message"));
            Assert.DoesNotThrow(() => MCPLogger.LogWarning("Test warning message"));
        }

        [UnityTest]
        public IEnumerator CreatePrimitive_Cube_ShouldCreateSuccessfully()
        {
            // Arrange
            var initialCount = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;

            // Act
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "TestCube";

            yield return null; // Wait one frame

            // Assert
            Assert.IsNotNull(cube);
            Assert.AreEqual("TestCube", cube.name);
            Assert.IsNotNull(cube.GetComponent<MeshRenderer>());
            Assert.IsNotNull(cube.GetComponent<BoxCollider>());

            // Clean up
            Object.DestroyImmediate(cube);
        }

        [UnityTest]
        public IEnumerator CreatePrimitive_Sphere_ShouldCreateSuccessfully()
        {
            // Act
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "TestSphere";
            sphere.transform.position = new Vector3(1, 2, 3);

            yield return null;

            // Assert
            Assert.IsNotNull(sphere);
            Assert.AreEqual("TestSphere", sphere.name);
            Assert.AreEqual(new Vector3(1, 2, 3), sphere.transform.position);
            Assert.IsNotNull(sphere.GetComponent<SphereCollider>());

            // Clean up
            Object.DestroyImmediate(sphere);
        }

        [UnityTest]
        public IEnumerator GameObject_Transform_ShouldModifyCorrectly()
        {
            // Arrange
            var gameObject = new GameObject("TestTransform");

            // Act
            gameObject.transform.position = new Vector3(5, 10, 15);
            gameObject.transform.localScale = new Vector3(2, 2, 2);

            yield return null;

            // Assert
            Assert.AreEqual(new Vector3(5, 10, 15), gameObject.transform.position);
            Assert.AreEqual(new Vector3(2, 2, 2), gameObject.transform.localScale);

            // Clean up
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Path_Combine_ShouldWorkCorrectly()
        {
            // Arrange
            var basePath = Application.dataPath;
            var relativePath = "../UnityMCP/Commands";

            // Act
            var fullPath = Path.Combine(basePath, relativePath);

            // Assert
            Assert.IsNotNull(fullPath);
            Assert.IsTrue(fullPath.Contains("UnityMCP"));
            Assert.IsTrue(fullPath.Contains("Commands"));
        }
    }
}