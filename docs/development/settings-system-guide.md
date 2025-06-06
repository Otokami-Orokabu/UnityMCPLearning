# Unity MCP Settings System 詳細ガイド

## 🎯 概要

Unity MCP Learning の設定システムは、ユーザーの環境設定を永続化し、Unity 起動時に自動復元する仕組みです。JSON形式での設定保存と、リアルタイムでの設定変更同期を実現しています。

## 📁 ファイル構成

### 設定ファイルの場所
```
UnityMCP/settings.json
```

### 設定管理クラス
```
Assets/UnityMCP/Editor/Common/MCPServerSettings.cs
```

## 🔧 MCPServerSettings クラス詳細

### クラス定義

```csharp
[Serializable]
public class MCPServerSettings
{
    [SerializeField] public string serverPath = "../unity-mcp-node";
    [SerializeField] public bool autoStartOnLaunch = true;
    [SerializeField] public int defaultPort = 3000;
    [SerializeField] public string lastModified = "";
}
```

### 主要メソッド

#### Load() - 設定読み込み
```csharp
public static MCPServerSettings Load()
```
**機能**:
- 設定ファイルの存在確認
- JSON から設定オブジェクトへのデシリアライゼーション
- ファイルが存在しない場合のデフォルト設定作成

**エラーハンドリング**:
- ファイル読み込み失敗時はデフォルト設定を返却
- JSON パースエラー時の適切な例外処理

#### Save() - 設定保存
```csharp
public void Save()
```
**機能**:
- 設定ディレクトリの自動作成
- タイムスタンプの自動更新
- JSON形式での設定保存

**保存内容**:
```json
{
    "serverPath": "../unity-mcp-node",
    "autoStartOnLaunch": true,
    "defaultPort": 3000,
    "lastModified": "2025-06-06T14:42:47Z"
}
```

#### GetAbsoluteServerPath() - パス解決
```csharp
public string GetAbsoluteServerPath()
```
**機能**:
- 相対パスの絶対パス変換
- パス正規化処理
- エラー時の安全な処理

#### IsServerPathValid() - パス検証
```csharp
public bool IsServerPathValid()
```
**検証内容**:
- `dist/index.js` ファイルの存在確認
- `package.json` ファイルの存在確認
- パスアクセス権限の確認

#### ValidateSettings() - 設定妥当性チェック
```csharp
public bool ValidateSettings()
```
**検証項目**:
- Server Path の空チェック
- Port 番号の範囲確認（1000-65535）
- Server Path の有効性確認

## 🔄 UI 統合システム

### 設定読み込みフロー

1. **Unity 起動時**
   ```csharp
   private void CreateGUI()
   {
       // 設定読み込み
       _settings = MCPServerSettings.Load();
       
       // UI要素作成とバインド
       BindUIElements();
       
       // 設定値をUIに反映
       LoadSettingsToUI();
   }
   ```

2. **LoadSettingsToUI() 処理**
   ```csharp
   private void LoadSettingsToUI()
   {
       if (_settings == null) return;
       
       // 各フィールドに設定値を反映
       _serverPathField.value = _settings.serverPath;
       _portField.value = _settings.defaultPort.ToString();
       _autoStartToggle.value = _settings.autoStartOnLaunch;
   }
   ```

### 設定変更時の保存

#### Auto Start Toggle 変更
```csharp
_autoStartToggle.RegisterValueChangedCallback(evt =>
{
    if (_settings != null)
    {
        _settings.autoStartOnLaunch = evt.newValue;
        _settings.Save();
    }
});
```

#### Port 設定変更
```csharp
_portField.RegisterValueChangedCallback(evt =>
{
    if (int.TryParse(evt.newValue, out int port))
    {
        _serverManager?.UpdatePort(port);
        if (_settings != null)
        {
            _settings.defaultPort = port;
            _settings.Save();
        }
    }
});
```

#### Server Path 変更
```csharp
_serverPathField.RegisterValueChangedCallback(evt =>
{
    if (_settings != null)
    {
        _settings.serverPath = evt.newValue;
        _settings.Save();
    }
});
```

## 🛡️ セキュリティ考慮事項

### パス検証
- パストラバーサル攻撃の防止
- 許可されたディレクトリ外への書き込み防止
- ファイル存在確認による安全性担保

### 設定値検証
- Port 番号の範囲制限
- Server Path の妥当性チェック
- 不正な設定値の自動修正

### エラーハンドリング
- ファイル読み書きエラーの適切な処理
- JSON パースエラーの安全な回復
- デフォルト設定への自動フォールバック

## 📊 デバッグとログ

### ログ出力内容

#### 設定読み込み時
```
[MCPServerSettings] Settings loaded from: /path/to/settings.json
```

#### 設定保存時
```
[MCPServerSettings] Settings saved to: /path/to/settings.json
```

#### エラー時
```
[MCPServerSettings] Failed to load settings: <error details>
[MCPServerSettings] Failed to save settings: <error details>
```

### デバッグ方法

1. **Unity Console での確認**
   - 設定読み込み/保存ログの確認
   - エラーメッセージの詳細確認

2. **設定ファイルの直接確認**
   ```bash
   cat UnityMCP/settings.json
   ```

3. **パス解決の確認**
   ```csharp
   Debug.Log($"Absolute path: {_settings.GetAbsoluteServerPath()}");
   Debug.Log($"Path valid: {_settings.IsServerPathValid()}");
   ```

## 🔧 カスタマイズとエクステンション

### 新しい設定項目の追加

1. **MCPServerSettings.cs に追加**
   ```csharp
   [SerializeField] public bool enableAdvancedLogging = false;
   ```

2. **LoadSettingsToUI() に追加**
   ```csharp
   _advancedLoggingToggle.value = _settings.enableAdvancedLogging;
   ```

3. **イベントハンドラーに追加**
   ```csharp
   _advancedLoggingToggle.RegisterValueChangedCallback(evt =>
   {
       _settings.enableAdvancedLogging = evt.newValue;
       _settings.Save();
   });
   ```

### 設定の検証ルール追加

```csharp
public bool ValidateSettings()
{
    // 既存の検証...
    
    // 新しい検証ルール
    if (enableAdvancedLogging && !IsLoggingDirectoryWritable())
    {
        Debug.LogWarning($"{LOG_PREFIX} Logging directory not writable");
        return false;
    }
    
    return true;
}
```

## 🚀 パフォーマンス最適化

### 設定保存の最適化

1. **デバウンス処理**
   - 連続する設定変更の保存頻度制限
   - 最後の変更から一定時間後に保存実行

2. **差分検出**
   - 設定値が実際に変更された場合のみ保存
   - 不要な I/O 操作の削減

### メモリ使用量最適化

1. **Singleton パターン**
   - 設定インスタンスの単一化
   - メモリ使用量の削減

2. **遅延読み込み**
   - 必要時のみ設定ファイルを読み込み
   - 起動時間の短縮

## 📋 ベストプラクティス

### 設定ファイル管理

1. **バックアップ**
   - 重要な設定変更前のバックアップ作成
   - 設定破損時の復旧手順確立

2. **バージョン管理**
   - 設定ファイルの Git 管理
   - 環境固有設定の除外

### UI 設計

1. **即座フィードバック**
   - 設定変更の即座反映
   - 保存完了の視覚的通知

2. **エラー表示**
   - 無効な設定値の即座通知
   - 修正提案の表示

## 🔍 トラブルシューティング

### よくある問題

#### 設定が保存されない
**原因**:
- ディレクトリの書き込み権限不足
- JSON シリアライゼーションエラー

**解決方法**:
- フォルダ権限の確認
- 設定値の妥当性チェック

#### 設定が読み込まれない
**原因**:
- ファイルパスの不正
- JSON 形式の破損

**解決方法**:
- ファイル存在確認
- 設定ファイルの再作成

#### 相対パスが解決されない
**原因**:
- 作業ディレクトリの認識不正
- パス区切り文字の違い

**解決方法**:
- 絶対パスでの確認
- プラットフォーム固有パス処理

---

この設定システムにより、ユーザーフレンドリーで堅牢な設定管理を実現しています。カスタマイズや拡張時は、このガイドを参考に安全で効率的な実装を心がけてください。