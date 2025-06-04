# Step 1: MCPサーバー通信確立とツール実装

## 概要

このステップでは、Claude DesktopとMCPサーバー間での完全な通信を確立し、MCPツール機能を実装しました。
JSON-RPC 2.0プロトコルとMCP 2024-11-05仕様に準拠したstdio通信を実現しています。

## 実装した機能

### 1. MCPプロトコル完全対応

- **プロトコルバージョン**: 2024-11-05
- **通信方式**: stdio（JSON-RPC 2.0）
- **キャパビリティ**: tools, resources, prompts対応
- **通知処理**: `notifications/initialized`対応

### 2. 実装したメソッド

#### initialize メソッド
MCPサーバーの初期化とキャパビリティの交換

**リクエスト例:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {},
    "clientInfo": {
      "name": "claude-ai",
      "version": "0.1.0"
    }
  }
}
```

**レスポンス例:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2024-11-05",
    "capabilities": {
      "tools": { "listChanged": false },
      "resources": {},
      "prompts": {}
    },
    "serverInfo": {
      "name": "unity-mcp-server",
      "version": "1.0.0"
    },
    "instructions": "Unity MCP Server for Unity Editor integration"
  }
}
```

#### tools/list メソッド
利用可能なツールのリストを取得

**レスポンス例:**
```json
{
  "tools": [
    {
      "name": "unity_info",
      "description": "Get Unity project information including scene, gameobjects, and editor details",
      "inputSchema": {
        "type": "object",
        "properties": {},
        "required": []
      }
    },
    {
      "name": "unity_info_dynamic",
      "description": "Get dynamic Unity project information with randomized data for testing",
      "inputSchema": {
        "type": "object",
        "properties": {},
        "required": []
      }
    },
    {
      "name": "ping",
      "description": "Test server connection",
      "inputSchema": {
        "type": "object",
        "properties": {},
        "required": []
      }
    }
  ]
}
```

#### tools/call メソッド
指定されたツールを実行

**Unity情報取得例:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "unity_info"
  }
}
```

### 3. Unityモックデータ

`src/unity-mock-data.ts`でモックデータを管理：

```typescript
export interface UnityProjectInfo {
  editorVersion: string;
  projectName: string;
  sceneName: string;
  platform: string;
  gameObjects: UnityGameObject[];
  assetCount: number;
  isPlaying: boolean;
  buildTarget: string;
  timestamp: string;
}
```

## プロジェクト構成

```
unity-mcp-node/
├── package.json          # Node.js設定
├── tsconfig.json         # TypeScript設定
├── src/
│   ├── index.ts          # MCPサーバーメイン
│   └── unity-mock-data.ts # Unityモックデータ
├── dist/                 # コンパイル済み
└── test-ping.sh          # テストスクリプト
```

## Claude Desktop統合

### 設定ファイル
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

### 利用可能なコマンド

Claude Desktopで以下のコマンドが使用可能：

1. **Unity情報取得**:
   ```
   Unityの情報を教えてください
   ```

2. **動的データ取得**:
   ```
   動的なUnity情報を取得してください
   ```

3. **接続確認**:
   ```
   MCPサーバーにpingを送信してください
   ```

## テスト方法

### 1. ビルドと実行

```bash
cd unity-mcp-node
npm run build
npm run start
```

### 2. 統合テスト

```bash
./test-ping.sh
```

### 3. Claude Desktopでの確認

Claude Desktopで「利用可能なツールを教えてください」と入力して、
MCPサーバーのツールが認識されることを確認。

## 達成した成果

✅ **通信確立**: Claude Desktop ↔ MCPサーバー  
✅ **プロトコル対応**: MCP 2024-11-05準拠  
✅ **ツール機能**: unity_info, unity_info_dynamic, ping  
✅ **データ構造**: 型安全なUnity情報取得  
✅ **エラーハンドリング**: 堅牢なエラー処理  
✅ **テスト環境**: 包括的なテストスイート  

## 次のステップ（Step 2）

- Unity Editor Scriptによる実際の情報出力
- ファイル監視による自動データ更新
- ./UnityMCP/ディレクトリでのデータ連携

## トラブルシューティング

### 接続エラー
1. Claude Desktopの完全再起動
2. MCPサーバーのプロセス確認: `ps aux | grep node`
3. 設定ファイルパスの確認

### データ取得エラー
1. Developer Toolsでのログ確認
2. 手動テスト実行: `./test-ping.sh`
3. ビルド状態の確認: `npm run build`