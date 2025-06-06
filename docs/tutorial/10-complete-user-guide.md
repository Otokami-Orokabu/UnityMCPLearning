# Unity MCP Learning 完全初心者ガイド

## 🎯 このガイドの目的

このドキュメントは、UnityとClaude Codeを使ってAIとコミュニケーションを取りたい初心者の方向けの完全ガイドです。プログラミング経験が少ない方でも、ステップバイステップで理解できるように書かれています。

## 📋 事前準備チェックリスト

始める前に、以下の環境が整っているか確認してください：

- [ ] Unity 2022.3 LTS以降がインストール済み
- [ ] Node.js 18.0以降がインストール済み
- [ ] Claude Desktop アプリがインストール済み
- [ ] Claude Code（CLIツール）が使用可能
- [ ] GitHubアカウント（このプロジェクトを取得するため）

## 🚀 セットアップ手順

### Step 1: プロジェクトの取得

```bash
git clone https://github.com/[your-username]/UnityMCPLearning.git
cd UnityMCPLearning
```

### Step 2: Unity プロジェクトを開く

1. Unity Hub を起動
2. 「Open」をクリック
3. `MCPLearning` フォルダを選択
4. プロジェクトが開くまで待機

### Step 3: MCP Serverの準備

```bash
cd unity-mcp-node
npm install
npm run build
```

## 🎮 MCP Server Manager の使い方

### 基本的な画面構成

Unity を開いたら、メニューバーから `Tools > MCP Server Manager` を選択してください。

#### 1. Server Control セクション
- **Start Server**: MCPサーバーを起動
- **Stop Server**: MCPサーバーを停止
- **Refresh**: 状態を手動更新
- **Test Connection**: 接続テストを実行

#### 2. Settings セクション
- **Server Path**: MCPサーバーのパス（通常は `../unity-mcp-node`）
- **Port**: 通信ポート（デフォルト: 3000）
- **Auto Start on Unity Launch**: Unity起動時の自動開始

#### 3. Data Management セクション
- **Export Data**: Unityのデータをエクスポート
- **Clear Data**: 蓄積されたデータを削除
- **Open Data Folder**: データフォルダを開く

## 🔧 基本的な操作手順

### 初回起動時

1. **Unity MCP Server Manager** を開く
2. **Server Path** が `../unity-mcp-node` になっているか確認
3. **Start Server** ボタンをクリック
4. **Server Status** が "Running" になることを確認
5. **Test Connection** ボタンで接続テスト

### 日常的な使用

1. Unity起動
2. 自動でMCPサーバーが起動（Auto Startが有効な場合）
3. Claude Code でプロジェクトに接続
4. AIとのコミュニケーション開始

## 🎨 設定のカスタマイズ

### Auto Start の設定

**推奨設定**: ✅ 有効

自動起動を有効にすると、Unity起動時に自動でMCPサーバーが開始されます。毎回手動で起動する手間が省けます。

### Server Path の変更

通常は変更不要ですが、MCPサーバーを別の場所に配置した場合：

1. **Server Path** フィールドをクリック
2. 新しいパスを入力（例: `/custom/path/to/mcp-server`）
3. 自動的に設定ファイルに保存されます

### Port番号の変更

デフォルトの3000番ポートが使用できない場合：

1. **Port** フィールドに新しい番号を入力
2. 使用可能な範囲: 1000-65535
3. 設定は自動保存されます

## 🛠️ トラブルシューティング

### よくある問題と解決方法

#### 1. サーバーが起動しない

**症状**: Start Server を押してもServer Status が "Stopped" のまま

**解決方法**:
1. Node.js がインストールされているか確認
2. `unity-mcp-node` フォルダで `npm install` を実行
3. Server Path が正しいか確認

#### 2. Connection が "Not Connected" のまま

**症状**: サーバーは動いているが接続できない

**解決方法**:
1. **Test Connection** ボタンを押して詳細確認
2. ファイアウォール設定を確認
3. ポート番号の競合を確認

#### 3. Auto Start が動作しない

**症状**: Unity起動時にサーバーが自動起動しない

**解決方法**:
1. **Auto Start on Unity Launch** がチェックされているか確認
2. 設定ファイル `UnityMCP/settings.json` の `autoStartOnLaunch` を確認

### ログの確認方法

問題が発生した場合、以下の場所でログを確認できます：

1. **Unity Console**: Unity内の Console タブ
2. **MCP Server Manager**: Logs セクション
3. **Data フォルダ**: `UnityMCP/Data/console-logs.json`

## 📊 データ管理

### データの蓄積について

Unity MCP Learning は以下のデータを自動収集します：

- **Console Logs**: Unityコンソールの出力
- **Editor State**: Unityエディタの状態
- **Scene Info**: シーン情報
- **GameObject Info**: オブジェクト情報
- **Compile Status**: コンパイル結果

### データサイズの管理

データが蓄積されすぎると、Claude Codeとの通信でトークンを多く消費します。

**Data Status Guide**:
- 🟢 **緑**: 適正サイズ（推奨）
- 🟡 **黄**: 注意が必要
- 🔴 **赤**: 削除を推奨

定期的に **Clear Data** ボタンでデータをリセットしましょう。

## 🔌 Unity Editor からの MCP サーバー起動について

### なぜ Unity Editor から起動するのか？

通常、MCPサーバーは Claude Desktop や Claude Code が自動的に起動します。しかし、Unity MCP Learning では **Unity Editor から手動起動** することで、以下のメリットがあります：

#### ✅ メリット
- **開発者主導**: いつサーバーを起動・停止するか完全制御
- **デバッグ簡単**: Unity Console で詳細ログを確認可能
- **リアルタイム監視**: サーバー状態をリアルタイムで確認
- **データ管理**: Unity内でデータのエクスポート・クリアが可能
- **設定統合**: Unity プロジェクト固有の設定を保存

#### ⚙️ 技術的な理由
```
Claude Desktop/Code → 自動起動 → 設定ファイル依存
Unity Editor → 手動起動 → プロジェクト設定連携
```

### 起動方法の比較

| 方法 | 起動場所 | 制御レベル | 適用場面 |
|------|----------|------------|----------|
| **Unity Editor** | Tools > MCP Server Manager | 🟢 高 | 開発・デバッグ |
| **Claude Desktop** | 自動起動 | 🟡 中 | 一般利用 |
| **Claude Code** | 自動起動 | 🟡 中 | CLI使用 |

## 🤖 Claude Desktop vs Claude Code の違い

### Claude Desktop との連携

#### 特徴
- **GUI アプリケーション**: デスクトップアプリとして動作
- **チャット形式**: 質問と回答の対話形式
- **設定ファイル**: `claude_desktop_config.json` で設定
- **自動起動**: アプリ起動時にMCPサーバー自動開始

#### 使用方法
1. Unity Editor で MCP Server Manager を開く
2. **Start Server** でサーバー起動
3. Claude Desktop アプリを開く
4. Unity関連の質問を入力

#### 適用場面
- 初心者向け
- 対話的な質問・回答
- UI操作が好ましい場合

### Claude Code (CLI) との連携

#### 特徴
- **コマンドライン**: ターミナル・コマンドプロンプトで動作
- **ファイル操作**: コード編集・ファイル作成が得意
- **統合開発**: IDEライクな操作が可能
- **詳細制御**: より詳細なコマンド実行

#### 使用方法
1. Unity Editor で MCP Server Manager を開く
2. **Start Server** でサーバー起動
3. ターミナルでプロジェクトディレクトリに移動
4. `claude-code` コマンドで起動
5. プログラミング作業を開始

#### 適用場面
- プログラマー向け
- コード編集・ファイル操作
- CLI環境を好む場合

### 推奨使い分け

```
📱 質問・相談 → Claude Desktop
💻 コード編集 → Claude Code
🔧 サーバー管理 → Unity Editor
```

## 🔄 統合ワークフロー

### パターン1: Claude Desktop 中心
```
Unity Editor → Start Server → Claude Desktop → 質問・相談
```

### パターン2: Claude Code 中心  
```
Unity Editor → Start Server → Claude Code → ファイル編集
```

### パターン3: 混合使用
```
Unity Editor → Start Server
     ↓
Claude Desktop (設計相談) + Claude Code (実装)
```

## 🤖 AI との連携

### 基本的な接続手順

#### Claude Desktop の場合
1. Unity Editor で MCP Server Manager を開く
2. **Start Server** でサーバー起動
3. Claude Desktop アプリを開く
4. Unity の状態について質問

#### Claude Code の場合
1. Unity Editor で MCP Server Manager を開く
2. **Start Server** でサーバー起動
3. ターミナルでプロジェクトディレクトリに移動
4. `claude-code` で Claude Code を起動
5. Unity ファイルの編集・操作を依頼

### よく使う質問例

```
- "現在のシーンにはどんなオブジェクトがありますか？"
- "最新のコンパイルエラーを教えて"
- "Unityエディタの現在の状態は？"
- "新しいスクリプトを作成してください"
```

### 効果的な使い方のコツ

1. **具体的な質問**: 「何かエラーはありますか？」より「コンパイルエラーはありますか？」
2. **段階的なリクエスト**: 複雑な機能は小さなステップに分けて依頼
3. **データクリア**: 大きな変更前にデータをクリアしてコンテキストをリセット

## 🔒 セキュリティについて

### 安全な使用のために

- **API Keys**: 設定ファイルにAPIキーを含めない
- **Personal Data**: 個人情報を含むプロジェクトでの使用は注意
- **Network**: 信頼できるネットワーク環境で使用

### データの取り扱い

- ローカルデータ: `UnityMCP/Data/` フォルダ内に保存
- 自動フィルタリング: 機密情報は自動的に除外
- 手動削除: いつでも **Clear Data** で削除可能

## 📚 さらに学ぶために

### 次のステップ

1. **Unity基礎**: Unity公式チュートリアル
2. **MCP Protocol**: Model Context Protocol の理解
3. **C# Programming**: Unityスクリプティング
4. **AI Integration**: AI開発の基本概念

### 参考リソース

- [Unity Learn](https://learn.unity.com/)
- [Claude API Documentation](https://docs.anthropic.com/)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [Node.js Documentation](https://nodejs.org/docs/)

## 🆘 サポート

### 問題が解決しない場合

1. **GitHub Issues**: バグ報告や機能要望
2. **Discord Community**: リアルタイムサポート
3. **Documentation**: より詳細な技術情報

### フィードバック

このツールをより良くするために、フィードバックをお待ちしています：

- 使いやすさの改善点
- 追加してほしい機能
- ドキュメントの改善提案

---

## ✨ おめでとうございます！

このガイドを読み終えた方は、Unity MCP Learning の基本的な使用方法を理解できました。

AIと協力してUnity開発を楽しんでください！ 🎮✨