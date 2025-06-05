# Step 1: 基本通信の確立

## 🎯 このステップの目標
- Claude Desktop ↔ MCPサーバー間の基本通信を確立
- JSON-RPC 2.0プロトコルの実装
- MCPツール機能の基本動作確認

## 📋 このステップで学ぶこと
- MCPプロトコルの基本
- TypeScriptでのサーバー実装
- Claude Desktopとの連携方法

## 🔧 実装内容

### 1. MCPサーバーの基本構造

`unity-mcp-node/src/index.ts`に以下の機能を実装します：

#### JSON-RPC 2.0メッセージタイプの定義
```typescript
interface JsonRpcRequest {
  jsonrpc: '2.0';
  id: string | number;
  method: string;
  params?: any;
}

interface JsonRpcResponse {
  jsonrpc: '2.0';
  id: string | number;
  result?: any;
  error?: {
    code: number;
    message: string;
    data?: any;
  };
}
```

#### 基本的なメソッドハンドラー
```typescript
async function handleMethod(method: string, params: any): Promise<any> {
  switch (method) {
    case 'ping':
      return {
        message: 'pong',
        timestamp: new Date().toISOString(),
        id: randomUUID()
      };
    
    case 'initialize':
      return {
        protocolVersion: '2024-11-05',
        capabilities: {
          tools: { listChanged: false },
          resources: {},
          prompts: {}
        },
        serverInfo: {
          name: 'unity-mcp-server',
          version: '1.0.0'
        }
      };
    
    case 'tools/list':
      return {
        tools: [
          {
            name: 'ping',
            description: 'Test server connection',
            inputSchema: {
              type: 'object',
              properties: {},
              required: []
            }
          }
        ]
      };
  }
}
```

### 2. 標準入出力での通信処理

#### メッセージの読み取りと処理
```typescript
const rl = createInterface({
  input: process.stdin,
  terminal: false
});

rl.on('line', async (line) => {
  try {
    const message = JSON.parse(line);
    
    // リクエストかどうかを判定
    if ('id' in message) {
      const result = await handleMethod(message.method, message.params);
      sendResponse({
        jsonrpc: '2.0',
        id: message.id,
        result
      });
    }
  } catch (error) {
    // エラーハンドリング
  }
});
```

## 🧪 動作テスト

### 1. サーバー単体テスト

#### コマンドラインでの基本テスト
```bash
cd unity-mcp-node

# pingテスト
echo '{\"jsonrpc\": \"2.0\", \"id\": 1, \"method\": \"ping\"}' | node dist/index.js
```

**期待される出力**:
```json
{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"message\":\"pong\",\"timestamp\":\"2025-06-05T...\",\"id\":\"...\"}}
```

#### initializeメソッドのテスト
```bash
echo '{\"jsonrpc\": \"2.0\", \"id\": 1, \"method\": \"initialize\", \"params\": {\"protocolVersion\": \"2024-11-05\"}}' | node dist/index.js
```

### 2. Claude Desktop統合テスト

#### 1. Claude Desktop再起動
- Claude Desktopを完全に終了
- 10秒待機
- 再度起動

#### 2. 接続確認
新しい会話で以下をテスト：

**テスト1: ping機能**
```
ping
```

**期待結果**: 「Pong! Server is running. Timestamp: [時刻]」

**テスト2: サーバー情報取得**
```
server/infoでサーバーの詳細情報を教えて
```

**期待結果**: サーバー名、バージョン、稼働時間などの情報表示

## 📊 実装されるツール一覧

### 基本ツール

| ツール名 | 機能 | 入力パラメータ |
|---------|------|---------------|
| `ping` | 接続テスト | なし |
| `unity_info_realtime` | Unity情報取得（リアルタイムデータ） | category (オプション) |

**注意**: 現在は`unity_info_realtime`のみ実装されています。Step 1の段階では実際のUnityデータを取得します。

### モックデータについて

Step 1では実際のUnityデータではなく、モックデータを使用します：

```typescript
// unity-mock-data.ts
export function generateMockUnityData() {
  return {
    editorVersion: \"Unity 6000.1.5f1\",
    projectName: \"MCPLearning\",
    sceneName: \"SampleScene\",
    gameObjects: [
      { name: \"Main Camera\", position: [0, 1, -10] },
      { name: \"Directional Light\", position: [0, 3, 0] }
    ],
    assetCount: 156,
    isPlaying: false
  };
}
```

## 🐛 トラブルシューティング

### よくある問題

#### 1. 「Module not found」エラー
**症状**: TypeScriptコンパイル時にモジュールが見つからない
**解決方法**:
```bash
rm -rf node_modules package-lock.json
npm install
npm run build
```

#### 2. Claude Desktopでツールが表示されない
**症状**: pingコマンドを実行しても反応がない
**原因と解決**:
- **設定ファイルの確認**: `claude_desktop_config.json`のパスが正しいか
- **JSON構文チェック**: `python3 -m json.tool claude_desktop_config.json`
- **Claude Desktop再起動**: 完全終了後に再起動

#### 3. 「Parse error」が発生
**症状**: JSONパース時にエラーが発生
**原因**: JSONメッセージの形式が正しくない
**デバッグ方法**:
```bash
# ログ出力を追加してデバッグ
console.error('[DEBUG]', 'Received message:', line);
```

#### 4. stdio通信がうまくいかない
**症状**: メッセージのやり取りが途切れる
**確認事項**:
- 標準入力が正しく読み取られているか
- レスポンスが標準出力に出力されているか
- エラーメッセージが標準エラー出力に送られているか

### デバッグログの活用

#### ログ出力の追加
```typescript
function log(...args: any[]) {
  console.error('[MCP Server]', ...args);
}

// 使用例
log('Received request:', request);
log('Sending response:', response);
```

#### ログ確認方法
```bash
# ログファイルに出力して確認
node dist/index.js 2> debug.log &
tail -f debug.log
```

## ✅ Step 1完了チェックリスト

動作確認後、以下の項目をチェック：

- [ ] TypeScriptプロジェクトが正常にビルドできる
- [ ] `ping`メソッドが正常に応答する
- [ ] `initialize`メソッドが適切なキャパビリティを返す
- [ ] `tools/list`メソッドが実装済みツール一覧を返す
- [ ] Claude DesktopでMCPサーバーが認識される
- [ ] Claude Desktopで`ping`ツールが実行できる
- [ ] モックデータを使った`unity_info`ツールが動作する
- [ ] エラーハンドリングが適切に動作する

## 🚀 次のステップ

Step 1が完了したら、`03-step2-unity-integration.md`に進んでUnityとの実際の連携を実装しましょう。

### Step 2で実装する内容のプレビュー
- Unityプロジェクトからの実データエクスポート
- リアルタイムファイル監視
- 6種類のデータカテゴリ（プロジェクト、シーン、オブジェクト、アセット、ビルド、エディター）
- 自動変更検知システム

Step 1の基本通信が確立できれば、Step 2でより高度なUnity連携を実現できます！