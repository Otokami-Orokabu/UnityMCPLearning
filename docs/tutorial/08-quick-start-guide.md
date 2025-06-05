# クイックスタートガイド - 5分で体験！

## 🎯 目標

**5分でUnity MCP Learningの主要機能を体験**し、Claude DesktopからUnityを操作できることを確認します。

## ⏰ 所要時間: 5分

- セットアップ: 2分
- 基本操作: 2分  
- 確認・まとめ: 1分

## 🏁 前提条件

### ✅ 必須環境
- Unity 6.0+ インストール済み
- Node.js 18.0+ インストール済み
- Claude Desktop インストール済み・設定完了

### 📂 準備確認
```bash
# プロジェクトディレクトリの存在確認
ls UnityMCPLearning/
# → MCPLearning/ unity-mcp-node/ docs/ が見える

# Node.js確認  
node --version
# → v18.0.0以降

# Unity確認
# Unity Hub でプロジェクトが見える
```

## 🚀 5分間クイックスタート

### **⏱️ 1-2分: システム起動**

#### **Step 1: Unity起動**
```bash
# Unity Hub → MCPLearning プロジェクトを開く
# SampleScene が読み込まれることを確認
```

#### **Step 2: MCPサーバー準備**
```bash
cd unity-mcp-node

# 初回のみ
npm install && npm run build

# 確認（distフォルダが作成されているか）
ls dist/index.js
```

#### **Step 3: Claude Desktop**
```bash
# Claude Desktop 起動
# 下部にMCP接続インジケーターがあることを確認
```

### **⏱️ 3-4分: 基本操作テスト**

#### **テスト1: 接続確認 (30秒)**
Claude Desktopで実行：
```
ping
```

✅ **成功例:**
```
Pong! Server is running. Timestamp: 2025-06-05T13:48:34.655Z
```

❌ **失敗した場合:**
- Claude Desktop再起動
- MCP設定確認
- MCPサーバー再ビルド

#### **テスト2: GameObject作成 (90秒)**
```
create a cube
```

✅ **成功例:**
```
✅ Unity Command executed successfully: Cube 'Cube' created at (0, 0, 0)
Command ID: 73db08cd-63aa-49cf-bb3b-ab364d11fbf5
Duration: 87ms
```

Unity側確認：
- Hierarchyに「Cube」追加
- Scene viewで立方体表示

```
create a sphere
```

Unity側確認：
- 2つのオブジェクトがシーンに表示

#### **テスト3: 情報取得 (60秒)**
```
unity_info_realtime
```

✅ **成功例:**
```json
{
  "sceneName": "SampleScene",
  "gameObjectCount": 5,
  "activeGameObjects": 5,
  "lightCount": 1,
  "cameraCount": 1
}
```

```
get gameobjects
```

作成したオブジェクト一覧が表示されることを確認

### **⏱️ 5分: 完了確認**

#### **✅ 成功チェックリスト**
- [ ] `ping` コマンドが成功
- [ ] Cubeが Unity シーンに作成された
- [ ] Sphereが Unity シーンに作成された
- [ ] `unity_info_realtime` で情報取得できた

#### **🎊 全て成功した場合**
**おめでとうございます！** Unity MCP Learning が正常に動作しています。

**次のステップ:**
- [詳細ガイド](./01-environment-setup.md) でさらに学習
- [現在の機能一覧](./07-current-capabilities.md) で全機能確認
- 独自のシーン作成に挑戦

## 🔧 トラブルシューティング

### **よくある問題と1分解決法**

#### **❌ 「ping failed」**
```bash
# 原因: MCPサーバー未起動
# 解決:
cd unity-mcp-node
npm run build
# Claude Desktop 再起動
```

#### **❌ 「Unity command timed out」**
```bash
# 原因: Unity未起動またはプロジェクト未開
# 解決:
# 1. Unity Editor 起動確認
# 2. MCPLearning プロジェクト開く
# 3. Unity Editor 再起動
```

#### **❌ オブジェクトが見えない**
```bash
# 原因: カメラ位置またはビュー問題
# 解決:
# 1. Scene view でオブジェクトを確認
# 2. Hierarchy でオブジェクト存在確認
# 3. Scene view のカメラ位置調整
```

#### **❌ 「MCP server not found」**
```bash
# 原因: Claude Desktop設定問題
# 解決:
# 1. claude_desktop_config.json 確認
# 2. パス設定の確認
# 3. Claude Desktop 完全再起動
```

### **ログ確認（デバッグ用）**
```bash
# Unity側ログ
cat MCPLearning/Logs/mcp-export.log | tail -10

# 正常例:
[CommandProcessor] コマンド受信: create_cube
[CommandProcessor] コマンド実行完了: create_cube
```

## 🎯 5分完了後の次のアクション

### **🌟 成功した場合**

#### **すぐできる応用**
```bash
# シーン構築チャレンジ
create a plane         # 床
create a cube          # 建物
create a sphere        # 装飾オブジェクト
get scene info         # 状況確認
```

#### **学習の発展**
1. **Unity操作の習得** - 手動でのオブジェクト操作
2. **MCP理解の深化** - プロトコル仕様の学習
3. **プログラミング学習** - C#/TypeScriptコードの理解

### **❌ 失敗した場合**

#### **段階的なデバッグ**
1. **環境確認** - Unity, Node.js, Claude Desktop のバージョン
2. **設定確認** - 設定ファイルの内容と場所
3. **ログ確認** - エラーメッセージの詳細分析
4. **完全リセット** - 全てのアプリケーション再起動

#### **詳細ガイドへ**
- [環境設定](./01-environment-setup.md) - 詳細なセットアップ手順
- [トラブルシューティング](./04-troubleshooting.md) - 問題解決ガイド

## 🏆 まとめ

### **5分で体験できたこと**
- ✅ Claude Desktop → Unity のリアルタイム制御
- ✅ 自然言語でのGameObject作成
- ✅ Unity状態情報の自動取得
- ✅ エラーハンドリングの確認

### **達成したマイルストーン**
- 🎮 **Unity MCP Learning 基本動作確認**
- 🗣️ **自然言語によるUnity操作体験**
- 📊 **リアルタイムデータ取得体験**
- 🛡️ **エラーハンドリング確認**

**5分間でUnity MCP Learningの核心機能を体験できました！** 

これで AI による Unity 操作の可能性を実感いただけたのではないでしょうか。さらなる探索と学習をお楽しみください！🚀✨