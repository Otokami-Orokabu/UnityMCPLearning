# Unity MCP Learning - ローカルパッケージテストガイド

## 概要
Unity MCP Learning パッケージをローカルパッケージとして別のUnityプロジェクトで使用する方法とテストガイドです。

## 📋 テスト前チェックリスト

### ✅ 現在の状態確認
- [x] Phase 4.4実装完了（マルチプロジェクト設定生成UI）
- [x] コンパイル成功（エラー・警告なし）
- [x] MCPサーバー正常起動（PID: 61495）
- [x] パッケージ構造完成
  - [x] package.json設定
  - [x] Scripts/Editor/アセンブリ
  - [x] Server~/Node.js実装
  - [x] Tests/Editor/テストファイル
  - [x] Documentation~/ドキュメント

### 📦 パッケージ構成
```
Assets/Packages/unity-mcp-learning/
├── package.json                    # パッケージ定義
├── README.md                       # 基本説明
├── CHANGELOG.md                    # 変更履歴
├── Scripts/
│   ├── Editor/                     # エディタースクリプト
│   │   ├── UnityMCP.Editor.asmdef  # アセンブリ定義
│   │   ├── Common/                 # 共通クラス群
│   │   ├── Windows/                # UIウィンドウ
│   │   ├── Setup/                  # セットアップツール
│   │   ├── Exporters/              # データエクスポーター
│   │   └── ...
│   └── Runtime/                    # ランタイムスクリプト
│       ├── UnityMCP.Runtime.asmdef # アセンブリ定義
│       └── UnityMCPRuntime.cs      # ランタイム機能
├── Server~/                        # MCPサーバー（Node.js）
│   ├── package.json               # Node.js依存関係
│   ├── dist/                      # ビルド済みJS
│   ├── src/                       # TypeScriptソース
│   └── ...
├── Tests/
│   └── Editor/                     # エディターテスト
└── Documentation~/                 # ドキュメント
```

## 🧪 ローカルパッケージテスト手順

### Step 1: 新しいUnityプロジェクトの作成
1. Unity Hub で新しい3Dプロジェクトを作成
2. Unity 6000.0.0f1 以上を使用
3. プロジェクト名: `MCPTest` （任意）

### Step 2: Package Manager でローカルパッケージを追加
1. Unity Editor で Window > Package Manager を開く
2. 左上の「+」ボタンをクリック
3. 「Add package from disk...」を選択
4. 以下のパスに移動してpackage.jsonを選択：
   ```
   /path/to/UnityMCPLearning/MCPLearning/Assets/Packages/unity-mcp-learning/package.json
   ```

### Step 3: インポート確認
パッケージが正常にインポートされると：
- Package Manager の「In Project」にUnity MCP Learning が表示される
- プロジェクトウィンドウの Packages/Unity MCP Learning フォルダが表示される
- メニューに「UnityMCP」が追加される

### Step 4: 基本機能テスト

#### 4.1 MCP Server Manager の起動
1. メニューから `UnityMCP > MCP Server Manager` を選択
2. Server Manager Windowが開くことを確認
3. UI要素が正常に表示されることを確認：
   - Server Status表示
   - Multi-Project Support (Phase 3)
   - Auto-Accept Configuration (Phase 4.3)
   - Multi-Project Configuration Generator (Phase 4.4)

#### 4.2 MCPサーバーのテスト
1. Server Manager Window で「Start Server」をクリック
2. ステータスが「Running」になることを確認
3. ポート（デフォルト3000）でサーバーが起動することを確認
4. 「Test Connection」で接続テストが成功することを確認

#### 4.3 マルチプロジェクト機能のテスト
1. Multi-Project Support セクションで：
   - Project ID が表示されることを確認
   - 別のプロジェクトIDが生成されることを確認
   - Port Management が機能することを確認

#### 4.4 Auto-Accept機能のテスト
1. Auto-Accept Configuration セクションで：
   - 「Enable Auto-Accept」をクリック
   - Claude Desktop設定が更新されることを確認
   - Status表示が「Enabled」になることを確認

#### 4.5 Configuration Generator のテスト
1. Multi-Project Configuration Generator セクションで：
   - 「Preview Config」で設定プレビューが表示されることを確認
   - 「Validate Setup」で検証が実行されることを確認
   - 「Generate All Configs」で設定生成が成功することを確認

### Step 5: 高度な機能テスト

#### 5.1 Claude Code CLI との連携テスト
1. ターミナルで以下を実行：
   ```bash
   # Claude Code CLIでプロジェクトに接続
   cd /path/to/MCPTest
   claude-code
   ```
2. Claude Code内で以下をテスト：
   - `ping` - サーバー応答確認
   - `unity_info_realtime` - Unity情報取得
   - `get_console_logs` - コンソールログ取得
   - `create_cube` - GameObject作成

#### 5.2 データエクスポート機能のテスト
1. Data Management セクションで：
   - 「Export Data」でJSONデータが生成されることを確認
   - UnityMCP/Data フォルダにファイルが作成されることを確認
   - データサイズが適切に表示されることを確認

## 🔧 トラブルシューティング

### パッケージがインポートできない場合
- package.json の内容を確認
- Unity バージョンが 6000.0.0f1 以上か確認
- パッケージパスが正しいか確認

### MCPサーバーが起動しない場合
- Node.js がインストールされているか確認
- Server~/dist/index.js が存在するか確認
- ポート3000が他のプロセスで使用されていないか確認

### Claude Desktop連携ができない場合
- Claude Desktop がインストールされているか確認
- ~/Library/Application Support/Claude/claude_desktop_config.json が存在するか確認
- Auto-Accept設定が正しく適用されているか確認

## 📊 期待される結果

### 成功基準
- [ ] パッケージが正常にインポートされる
- [ ] MCP Server Manager が起動する
- [ ] MCPサーバーが正常に動作する
- [ ] 別プロジェクトで異なるProject IDが生成される
- [ ] Auto-Accept機能が動作する
- [ ] Claude Code CLI から Unity を制御できる
- [ ] マルチプロジェクト環境で競合しない

### パフォーマンス基準
- サーバー起動時間: 3秒以内
- UI応答性: 快適
- メモリ使用量: 適正
- Claude Code応答時間: 1秒以内

## 📝 テスト結果記録

以下の項目をテストして結果を記録してください：

```
[ ] パッケージインポート: ⭕/❌
[ ] UI表示: ⭕/❌
[ ] サーバー起動: ⭕/❌
[ ] 接続テスト: ⭕/❌
[ ] プロジェクトID生成: ⭕/❌
[ ] Auto-Accept設定: ⭕/❌
[ ] 設定ファイル生成: ⭕/❌
[ ] Claude Code連携: ⭕/❌
[ ] データエクスポート: ⭕/❌
[ ] マルチプロジェクト動作: ⭕/❌
```

## 🚀 次のステップ

ローカルパッケージテストが成功したら：
1. GitHub Release の準備
2. Git URL Distribution の設定
3. Documentation の最終確認
4. パブリック公開

---

このガイドに従ってテストを実行し、問題があれば修正してから公開準備を進めてください。