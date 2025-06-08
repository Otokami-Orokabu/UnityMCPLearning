# Claude Code - 自動承認設定ガイド

## 🎯 概要

Unity MCPサーバーを使用してUnityのコード生成を行う際、Claude Codeで毎回表示されるaccept確認をスキップする方法を説明します。

## 🚫 現在の問題

### 開発時の煩わしさ
```
Claude Code: create_cubeを実行してもよろしいですか？ [Y/n]
Claude Code: get_console_logsを実行してもよろしいですか？ [Y/n] 
Claude Code: unity_info_realtimeを実行してもよろしいですか？ [Y/n]
```

毎回の確認で開発フローが中断され、効率が大幅に低下します。

## 🛠️ 解決方法

### **方法1: コマンドラインフラグ（最も簡単）**

#### 一時的にスキップ
```bash
claude --dangerously-skip-permissions
```

#### エイリアス設定（推奨）
```bash
# ~/.bashrc または ~/.zshrc に追加
alias claude-dev="claude --dangerously-skip-permissions"
alias unity-claude="claude --dangerously-skip-permissions"

# 使用方法
unity-claude  # 自動承認モードでClaude Code起動
```

### **方法2: セッション中のモード切り替え**

```bash
# Claude Code起動後
# Shift + Tab を押してAuto-accept modeに切り替え
# セッション中のみ有効（安全）
```

### **方法3: Unity MCP専用設定（プロジェクト固有）**

#### mcp-config.json設定
```json
{
  "mcpServers": {
    "unity-mcp-prod": {
      "command": "node",
      "args": ["./unity-mcp-node/dist/index.js"],
      "cwd": ".",
      "env": {
        "MCP_AUTO_APPROVE": "true"
      }
    }
  },
  "claudeCodeSettings": {
    "autoApproveTools": [
      "ping",
      "unity_info_realtime", 
      "get_console_logs",
      "create_cube",
      "create_sphere",
      "create_plane",
      "create_gameobject",
      "wait_for_compilation"
    ],
    "requireConfirmation": false,
    "skipPermissionDialogs": true
  }
}
```

#### 設定の適用
```bash
# プロジェクトディレクトリで
cd /path/to/UnityMCPLearning
claude  # 自動的にプロジェクト設定が適用される
```

## 🔧 推奨設定組み合わせ

### **開発環境（ローカル）**
```bash
# 1. エイリアス設定
echo 'alias unity-claude="claude --dangerously-skip-permissions"' >> ~/.zshrc
source ~/.zshrc

# 2. Unity MCPプロジェクトで使用
cd ~/UnityMCPLearning
unity-claude
```

### **共有・本番環境**
```bash
# 自動承認は使わず、個別確認を維持
claude  # 通常モード

# 必要時のみShift + Tabでセッション中の自動承認
```

## 🎨 実際の使用例

### **Unity GameObject作成の自動化**
```bash
unity-claude
> "プレイヤーキャラクター用のキューブを作成して、PlayerController.csスクリプトも生成して"

# 以下が自動実行（確認なし）
# 1. create_cube → キューブ作成
# 2. get_console_logs → コンパイル状況確認  
# 3. wait_for_compilation → コンパイル完了待機
# 4. ファイル書き込み → PlayerController.cs作成
```

### **AI駆動開発ワークフロー**
```bash
unity-claude
> "敵キャラクターシステムを作って。EnemyController、EnemyAI、HealthSystemを含む"

# 自動実行シーケンス（確認なし）
# 1. create_sphere → 敵キャラクター用オブジェクト
# 2. Multiple file operations → スクリプト群作成
# 3. get_console_logs → エラーチェック
# 4. unity_info_realtime → 最新状態取得
```

## ⚠️ セキュリティ注意事項

### **安全な使用方法**
- **開発環境限定**: `--dangerously-skip-permissions`はローカル開発のみ
- **信頼できるプロジェクト**: Unity MCPのような自作ツールでのみ使用
- **定期的な確認**: たまに通常モードで動作確認

### **避けるべき使用方法**
- **本番環境**: 本番サーバーでの自動承認は危険
- **未知のMCPサーバー**: 信頼できないツールでの自動承認
- **機密プロジェクト**: 企業の機密情報を扱うプロジェクト

## 📋 トラブルシューティング

### **エイリアスが効かない**
```bash
# シェル確認
echo $SHELL

# 適切な設定ファイルに追加
# bash: ~/.bashrc
# zsh: ~/.zshrc
# fish: ~/.config/fish/config.fish
```

### **自動承認が無効**
```bash
# 設定確認
claude --help | grep permission

# mcp-config.jsonの文法チェック
cat mcp-config.json | python -m json.tool
```

### **Claude Code起動エラー**
```bash
# Claude Codeのバージョン確認
claude --version

# 最新版にアップデート
# Claude Desktop経由で更新通知を確認
```

## 🚀 開発効率の改善

### **Before（自動承認なし）**
```
開発者: "キューブを作って"
Claude Code: create_cubeを実行しますか？ [Y/n] → y入力
Claude Code: get_console_logsを実行しますか？ [Y/n] → y入力
Claude Code: unity_info_realtimeを実行しますか？ [Y/n] → y入力
結果: 3回の手動確認、30秒の中断
```

### **After（自動承認）**
```
開発者: "キューブを作って"  
Claude Code: 自動実行 → 即座にキューブ作成・状態確認完了
結果: 確認なし、2秒で完了
```

### **開発速度向上**
- **確認時間削減**: 95%短縮（30秒 → 2秒）
- **フロー維持**: 思考の中断なし
- **反復開発**: プロトタイピング速度向上

## 💡 おすすめワークフロー

### **自己解決型AI開発サイクル（推奨）**

#### 基本的な流れ
```bash
# 1. 開発開始
cd ~/UnityMCPLearning
unity-claude

# 2. C#スクリプト生成依頼
"PlayerController.csを作成して。WASD移動とジャンプ機能付き"

# 3. 自動解決サイクル（Claude Codeが自動実行）
Step 1: C#コード生成・保存
Step 2: wait_for_compilation → コンパイル完了待機
Step 3: get_console_logs → エラー・警告取得
Step 4: エラーがあれば自動修正 → Step 2に戻る
Step 5: エラーなし → 完了
```

#### 実際の自動解決例
```
開発者: "EnemyAI.csを作って。プレイヤーを追跡する機能"

Claude Code:
[1回目] EnemyAI.cs生成 
→ wait_for_compilation 
→ get_console_logs: "PlayerController型が見つからない"
→ 自動修正: using文とnamespace追加

[2回目] 修正版保存
→ wait_for_compilation
→ get_console_logs: "GetComponent<Rigidbody>()がnull"
→ 自動修正: null チェックとRequireComponent追加

[3回目] 再修正版保存  
→ wait_for_compilation
→ get_console_logs: エラーなし
→ ✅ 完了！動作するEnemyAI.csが完成
```

### **MCP自動承認の重要性**
この自己解決サイクルでは、以下のMCPツールが**連続的に自動実行**されます：
- `wait_for_compilation` （コンパイル監視）
- `get_console_logs` （エラー取得）
- `unity_info_realtime` （状態確認）

**accept確認があると**：
```
Claude Code: wait_for_compilationを実行しますか？ [Y/n]
Claude Code: get_console_logsを実行しますか？ [Y/n] 
Claude Code: wait_for_compilationを実行しますか？ [Y/n] (2回目)
Claude Code: get_console_logsを実行しますか？ [Y/n] (2回目)
...無限に確認が続く
```

**自動承認により**：
```
Claude Code: スクリプト生成 → 自動コンパイル確認 → 自動エラー取得 → 自動修正 → 自動再確認
→ 開発者は結果だけ確認すればOK
```

### **学習・実験**
```bash
# 安全な実験環境
unity-claude
"Unityの物理システムを使った面白い実験をやってみて"
→ 様々なGameObjectとスクリプトを自動生成・テスト
```

---

**作成日**: 2025年6月8日  
**対象**: Claude Code + Unity MCP Learning  
**推奨**: 開発環境での使用を強く推奨