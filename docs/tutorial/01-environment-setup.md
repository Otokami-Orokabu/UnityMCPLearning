# 環境設定ガイド

## 📋 必要なツールの確認

### 1. Unity 6以上のインストール
- **Unity Hub**をインストール ([公式サイト](https://unity.com/download))
- **Unity 6000.1.5f1以上**をインストール
- **Visual Studio Code for Unity**（推奨エディター）

### 2. Node.js環境の準備
- **Node.js v18以上**をインストール ([公式サイト](https://nodejs.org/))
- インストール確認:
  ```bash
  node --version  # v18.0.0以上であること
  npm --version   # 確認
  ```

### 3. Claude Desktop のインストール
- **Claude Desktop最新版**をインストール ([公式サイト](https://claude.ai/download))
- アカウントでサインイン完了

### 4. 開発環境（推奨）
- **VS Code** ([公式サイト](https://code.visualstudio.com/))
- **Git** (バージョン管理用)

## 🛠 プロジェクトのセットアップ

### 1. プロジェクトの取得
```bash
# GitHubからクローン（または手動ダウンロード）
git clone [プロジェクトURL]
cd UnityMCPLearning
```

### 2. Node.js依存関係のインストール
```bash
cd unity-mcp-node
npm install
```

### 3. TypeScriptのビルド
```bash
npm run build
```

**確認**: `dist/index.js`ファイルが生成されていることを確認

### 4. Unityプロジェクトの開明
1. Unity Hubを開く
2. 「開く」→ `MCPLearning`フォルダを選択
3. Unityエディターが起動することを確認

## ⚙️ Claude Desktop設定

### 1. 設定ファイルの場所
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`

### 2. 設定ファイルの作成・編集

**macOS例**:
```bash
# ディレクトリを作成（存在しない場合）
mkdir -p ~/Library/Application\\ Support/Claude

# 設定ファイルを作成・編集
nano ~/Library/Application\\ Support/Claude/claude_desktop_config.json
```

### 3. 設定内容
以下の内容をコピー・貼り付け（**パスは自分の環境に合わせて変更**）:

```json
{
  \"mcpServers\": {
    \"unity-mcp-server\": {
      \"command\": \"node\",
      \"args\": [\"[あなたのパス]/UnityMCPLearning/unity-mcp-node/dist/index.js\"],
      \"cwd\": \"[あなたのパス]/UnityMCPLearning\"
    }
  }
}
```

**パス例**:
- **macOS**: `/Users/あなたのユーザー名/Projects/UnityMCPLearning`
- **Windows**: `C:\\Users\\あなたのユーザー名\\Projects\\UnityMCPLearning`

### 4. パス設定の自動化（推奨）

#### 方法1: 環境変数を使用
```bash
# 環境変数を設定
export UNITY_MCP_DATA_PATH=\"./MCPLearning/UnityMCP/Data\"

# 設定ファイルはシンプルに
{
  \"mcpServers\": {
    \"unity-mcp-server\": {
      \"command\": \"node\",
      \"args\": [\"./unity-mcp-node/dist/index.js\"],
      \"cwd\": \"[プロジェクトルートパス]\"
    }
  }
}
```

#### 方法2: 設定ファイルを使用
`unity-mcp-node/mcp-config.json`を編集:
```json
{
  \"mcpServers\": {
    \"unity-mcp-prod\": {
      \"command\": \"node\",
      \"args\": [\"./unity-mcp-node/dist/index.js\"],
      \"cwd\": \".\"
    }
  },
  \"unityDataPath\": \"./MCPLearning/UnityMCP/Data\"
}
```

## ✅ 動作確認

### 1. MCPサーバーの単体テスト
```bash
cd unity-mcp-node
echo '{\"jsonrpc\": \"2.0\", \"id\": 1, \"method\": \"ping\"}' | node dist/index.js
```

**期待結果**: pongレスポンスが返される

### 2. Claude Desktop接続テスト
1. Claude Desktopを**完全に再起動**
2. 新しい会話を開始
3. 「ping」と入力してMCPサーバーとの接続を確認

**期待結果**: 「Pong! Server is running...」のようなレスポンス

### 3. Unityプロジェクト確認
1. Unityエディターでプロジェクトを開く
2. コンソールにエラーがないことを確認
3. メニューバーに「UnityMCP」が表示されることを確認

## 🚨 トラブルシューティング

### よくある問題

#### 「Command not found: node」
**原因**: Node.jsがインストールされていない、またはPATHが通っていない
**解決**: 
1. Node.jsを再インストール
2. ターミナル/コマンドプロンプトを再起動

#### 「Module not found」エラー
**原因**: npm installが正常に完了していない
**解決**:
```bash
cd unity-mcp-node
rm -rf node_modules package-lock.json
npm install
npm run build
```

#### Claude Desktopで「Server disconnected」
**原因**: 設定ファイルのパスが間違っている
**解決**:
1. パスにスペースが含まれていないか確認
2. ファイルの存在を確認: `ls [設定したパス]/index.js`
3. JSON構文エラーを確認: `python3 -m json.tool claude_desktop_config.json`

#### Unityでコンパイルエラー
**原因**: Unity用のパッケージが不足している
**解決**:
1. Unity Package Managerで「Unity Logging」パッケージを確認
2. プロジェクト設定を確認

### デバッグのための確認事項
- [ ] Node.js v18以上がインストールされている
- [ ] `unity-mcp-node/dist/index.js`が存在する
- [ ] Claude Desktop設定ファイルのJSON構文が正しい
- [ ] ファイルパスにスペースが含まれていない（または適切にエスケープされている）
- [ ] Unityプロジェクトがエラーなく開ける

## 次のステップ
環境設定が完了したら、`02-step1-basic-communication.md`に進んでStep 1の実装を開始しましょう。