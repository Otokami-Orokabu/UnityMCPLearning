# Step 3: Unity制御システム実装

## 🎯 このステップで実現すること

Claude DesktopからUnity Editorに対して直接コマンドを送信し、GameObjectを作成できるようになります。

### 実装済み機能
- **Cube作成**: `create a cube`
- **Sphere作成**: `create a sphere`
- **Plane作成**: `create a plane`
- **GameObject作成**: `create a gameobject`
- **リアルタイム実行**: 即座にUnity側で実行
- **エラーハンドリング**: 詳細な検証とエラー分類

## 🏗️ システム構成

```
Claude Desktop
     ↓ (MCP Protocol)
MCP Server (Node.js)
     ↓ (JSON File Communication)
Unity Editor (C#)
     ↓ (GameObject Creation)
Unity Scene
```

## 📋 前提条件

- Step 1とStep 2が完了していること
- Unity Editorが起動していること
- MCPサーバーが正常に動作していること

## 🛠️ 実装内容

### **MCPサーバー側（Node.js）**

#### **新しいツールの追加**
`unity-mcp-node/src/index.ts`に以下のツールが実装されています：

```typescript
// GameObject作成ツール
{
  name: 'create_cube',
  description: 'Create a cube in Unity scene',
  inputSchema: {
    type: 'object',
    properties: {
      name: { type: 'string', default: 'Cube' },
      position: {
        type: 'object',
        properties: {
          x: { type: 'number', default: 0 },
          y: { type: 'number', default: 0 },
          z: { type: 'number', default: 0 }
        }
      },
      scale: {
        type: 'object', 
        properties: {
          x: { type: 'number', default: 1 },
          y: { type: 'number', default: 1 },
          z: { type: 'number', default: 1 }
        }
      }
    }
  }
}
```

#### **コマンド実行システム**
```typescript
async function executeUnityCommand(commandType: string, args: any): Promise<any> {
  // 1. パラメータ検証
  const validatedParams = validateCommandParameters(commandType, args);
  
  // 2. コマンドファイル作成
  const command = {
    commandId: randomUUID(),
    commandType: commandType,
    parameters: validatedParams,
    timestamp: new Date().toISOString(),
    status: 'Pending'
  };
  
  // 3. Unity側に送信
  fs.writeFileSync(commandFilePath, JSON.stringify(command, null, 2));
  
  // 4. 結果待機
  const result = await waitForCommandResult(commandPath, command.commandId, 15000);
  
  return result;
}
```

### **Unity側（C#）**

#### **コマンドプロセッサー**
`Assets/UnityMCP/Editor/Common/MCPCommandProcessor.cs`:

```csharp
[InitializeOnLoad]
public static class MCPCommandProcessor
{
    private static FileSystemWatcher _commandWatcher;
    
    static MCPCommandProcessor()
    {
        InitializeCommandProcessor();
    }
    
    private static void StartCommandWatching()
    {
        _commandWatcher = new FileSystemWatcher(COMMAND_DIR)
        {
            Filter = COMMAND_FILE,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };
        
        _commandWatcher.Changed += OnCommandFileChanged;
        _commandWatcher.Created += OnCommandFileChanged;
    }
}
```

#### **GameObject作成実装**
```csharp
private static void ExecuteCreateCube(MCPCommand command)
{
    var name = GetParameterString(command, "name", "Cube");
    var position = GetParameterVector3(command, "position", Vector3.zero);
    var scale = GetParameterVector3(command, "scale", Vector3.one);
    
    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    cube.name = name;
    cube.transform.position = position;
    cube.transform.localScale = scale;
    
    // Undoに登録
    Undo.RegisterCreatedObjectUndo(cube, $"Create {name}");
    Selection.activeGameObject = cube;
    
    command.result = $"Cube '{name}' created at {position}";
}
```

## 🚀 動作テスト

### **Step 1: 接続確認**
Claude Desktopで実行：
```
ping
```

期待結果：
```
✅ Pong! Server is running. Timestamp: 2025-06-05T13:48:34.655Z
```

### **Step 2: Cube作成**
```
create a cube
```

期待結果：
```
✅ Unity Command executed successfully: Cube 'Cube' created at (0, 0, 0)
Command ID: 73db08cd-63aa-49cf-bb3b-ab364d11fbf5
Duration: 87ms
```

Unity側確認：
- Hierarchyウィンドウに「Cube」が追加
- Scene viewで立方体が表示
- Undoスタックに操作が記録

### **Step 3: 複数オブジェクト作成**
```
create a sphere
create a plane
```

Unity側確認：
- 複数のオブジェクトが順次作成される
- 各オブジェクトが正しい位置に配置される

## 🛡️ エラーハンドリング

### **実装済みエラー処理**

#### **入力検証**
- コマンドタイプの事前チェック
- パラメータの型・範囲検証
- 無効な文字のフィルタリング

#### **実行時エラー**
- タイムアウト処理（15秒）
- ファイルシステムエラー
- Unity Editor未起動エラー

#### **エラー分類**
```typescript
// エラーカテゴリー
enum ErrorCategory {
  Timeout = 'Timeout',
  FileSystem = 'FileSystem', 
  ValidationError = 'ValidationError',
  InvalidCommand = 'InvalidCommand',
  Unknown = 'Unknown'
}
```

### **ログシステム**

#### **Unity側ログ**
- Debug.Log使用禁止対応
- ファイルベースログ実装
- 詳細なエラー情報記録

#### **ログファイル確認**
```bash
cat MCPLearning/Logs/mcp-export.log
```

成功例：
```
[CommandProcessor] コマンド受信: create_cube (ID: 73db08cd-...)
[CommandProcessor] コマンド実行完了: create_cube
```

## 🔧 カスタマイズ

### **新しいコマンド追加**

#### **1. MCPサーバー側ツール定義**
```typescript
{
  name: 'create_custom_object',
  description: 'Create a custom object',
  inputSchema: {
    type: 'object',
    properties: {
      // カスタムパラメータ定義
    }
  }
}
```

#### **2. Unity側実装**
```csharp
case CommandTypes.CREATE_CUSTOM_OBJECT:
    ExecuteCreateCustomObject(command);
    break;
```

#### **3. 実行関数実装**
```csharp
private static void ExecuteCreateCustomObject(MCPCommand command)
{
    // カスタム実装
}
```

## 📊 パフォーマンス

### **測定結果**
- **コマンド実行時間**: 平均50-100ms
- **ファイル監視**: リアルタイム更新
- **メモリ使用量**: 非同期処理により最小化
- **エラー処理**: 詳細分類と迅速な対応

### **最適化ポイント**
- 非同期ファイルI/O
- 効率的なファイル監視
- バッチ処理による負荷分散

## 🎉 完了確認

### ✅ チェックリスト
- [ ] `ping` コマンドが成功する
- [ ] `create a cube` でCubeが作成される
- [ ] `create a sphere` でSphereが作成される
- [ ] `create a plane` でPlaneが作成される
- [ ] エラー時に適切なメッセージが表示される
- [ ] Undoが正常に動作する

### 🎊 Step 3完了！

**おめでとうございます！** Claude DesktopからUnityを直接制御できるようになりました。

これで以下が可能になりました：
- 自然言語でのUnity操作
- リアルタイムのGameObject作成
- 安全なエラーハンドリング
- 包括的なログ記録

## 📈 次のステップ

### **機能拡張のアイデア**
1. **色指定**: `create a red cube`
2. **位置指定**: `create a cube at (1,0,1)`
3. **マテリアル適用**: `apply texture to cube`
4. **Transform操作**: `move cube to (2,0,0)`

### **学習の発展**
1. C#とTypeScriptコードの理解
2. Unity Editorスクリプティングの習得
3. MCP Protocol仕様の深い理解

**Step 3実装により、Unity MCP Learningプロジェクトの中核機能が完成しました！**🚀✨