# Claude Desktop設定手順（最新版）

## 概要

Unity MCPサーバーをClaude Desktopに統合し、ツール機能を利用可能にする手順を説明します。
MCP 2024-11-05プロトコルに対応した設定を行います。

## 前提条件

- Node.js 18以上がインストール済み
- TypeScriptプロジェクトがビルド済み
- Claude Desktop最新版がインストール済み

## 設定手順

### 1. プロジェクトのビルド確認

設定前に、MCPサーバーが正常にビルドされていることを確認:

```bash
cd /Users/USERNAME/ProjectGit/UnityMCPLearning/unity-mcp-node
npm run build
```

### 2. Claude Desktopの設定ファイルを編集

macOSの場合、以下の設定ファイルを編集:

```
~/Library/Application Support/Claude/claude_desktop_config.json
```

### 3. MCPサーバーの設定を追加

**推奨設定（本番用）:**
```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "node",
      "args": ["/Users/USERNAME/ProjectGit/UnityMCPLearning/unity-mcp-node/dist/index.js"]
    }
  }
}
```

**開発用設定（オプション）:**
```json
{
  "mcpServers": {
    "unity-mcp-dev": {
      "command": "npm",
      "args": ["run", "dev"],
      "cwd": "/Users/USERNAME/ProjectGit/UnityMCPLearning/unity-mcp-node"
    }
  }
}
```

**注意点:**
- 本番環境では絶対パスを使用することを推奨
- `cwd`は開発時のファイル監視が必要な場合のみ使用
- 既存のMCPサーバーがある場合は、カンマで区切って追加

### 4. Claude Desktopの再起動

設定を反映させるために：

1. `Claude` メニュー → `Quit Claude`で完全終了
2. Claude Desktopを再起動

### 5. 接続確認とテスト

#### 基本接続確認
Claude Desktopで以下をテスト:

```
利用可能なツールを教えてください
```

期待される応答: MCPサーバーのツール一覧が表示される

#### 機能テスト

1. **Unity情報取得**:
   ```
   Unityプロジェクトの情報を教えてください
   ```

2. **動的データテスト**:
   ```
   unity_info_dynamicツールを使って動的なUnity情報を取得してください
   ```

3. **接続テスト**:
   ```
   MCPサーバーにpingを送信してください
   ```

### 6. 詳細確認（開発者向け）

#### Developer Toolsでの確認

1. `View` → `Developer` → `Toggle Developer Tools`
2. **Console**タブ: エラーログの確認
3. **Network**タブ: stdio通信の確認

#### 手動テスト

```bash
cd /Users/USERNAME/ProjectGit/UnityMCPLearning/unity-mcp-node
./test-ping.sh
```

## 利用可能な機能

### MCPツール一覧

| ツール名 | 説明 | 用途 |
|----------|------|------|
| `unity_info` | Unity基本情報取得 | プロジェクト情報、GameObject一覧 |
| `unity_info_dynamic` | 動的Unity情報 | テスト用の変化するデータ |
| `ping` | 接続確認 | サーバー生存確認 |

### Claude Desktopでの利用例

```
# 基本的な情報取得
Unityの現在のシーン情報を教えてください

# 詳細な分析
プロジェクト内のGameObjectの配置を分析してください

# 動的テスト
ランダムな位置データでテストしてください
```

## トラブルシューティング

### 接続エラー

**症状**: MCPサーバーが認識されない

**解決方法**:
1. 設定ファイルのJSON構文確認
2. ファイルパスの確認（絶対パス推奨）
3. Node.jsのバージョン確認: `node --version`
4. ビルド状態確認: `npm run build`

### データ取得エラー

**症状**: ツールが実行されるがエラーが発生

**解決方法**:
1. Developer Toolsでのエラーログ確認
2. 手動テスト: `./test-ping.sh`
3. プロセス確認: `ps aux | grep node`

### パフォーマンス問題

**症状**: レスポンスが遅い

**解決方法**:
1. 本番用設定（コンパイル済み）の使用
2. 不要なプロセスの終了
3. ログレベルの調整

## セキュリティ注意事項

- 設定ファイルに機密情報を含めない
- MCPサーバーは信頼できるローカル環境でのみ実行
- 本番環境では適切なアクセス制御を実装

## 次のステップ（Step 2準備）

### 計画されている機能拡張

- Unity Editor Script連携
- リアルタイムファイル監視
- GameObject制御コマンド
- シーン操作機能

### 開発環境の準備

Step 2の実装には以下が必要:
- Unity 2023.3.0f1以上
- Unity Editor Script作成
- ./UnityMCP/ディレクトリ設定

現在のStep 1が完全に動作していることを確認後、Step 2の実装を開始できます。