# MCP Server Manager 完全ガイド

## 🎯 MCP Server Manager とは

MCP Server Manager は、Unity エディタ内でMCPサーバーを簡単に管理するための専用ツールです。Claude Desktop や Claude Code との連携を簡単にし、AI支援Unity開発を実現します。

### 🔌 Unity Editor 起動の特徴

**一般的なMCPサーバー**: Claude Desktop/Code が自動起動
**Unity MCP Learning**: Unity Editor から手動起動（開発者制御）

#### ✅ Unity Editor 起動のメリット
- **完全制御**: 開発者がサーバーの起動・停止を管理
- **統合デバッグ**: Unity Console で詳細ログ確認
- **リアルタイム監視**: 接続状態・データサイズを即座確認
- **プロジェクト固有設定**: Unity プロジェクトと連携した設定管理

## 🚀 起動方法

Unity エディタのメニューバーから：
```
Tools > MCP Server Manager
```

## 📱 画面構成詳細

### 1. Server Status エリア

#### Server Status
- **Stopped** 🔴: サーバーが停止中
- **Running** 🟢: サーバーが動作中（PID表示）
- **Starting** 🟡: サーバー起動処理中

#### Connection Status  
- **Connected** 🟢: MCPサーバーとの通信が正常
- **Not Connected** 🔴: 通信に問題がある
- **Testing** 🟡: 接続テスト実行中

### 2. Control Buttons

#### Start Server ボタン
**機能**: MCPサーバーを起動します
**実行内容**:
- Node.js の自動検出
- `unity-mcp-node` ディレクトリでサーバー実行
- Claude Desktop設定の自動更新

#### Stop Server ボタン
**機能**: MCPサーバーを安全に停止します
**実行内容**:
- プロセスの正常終了
- リソースのクリーンアップ

#### Refresh ボタン
**機能**: 状態表示を手動更新します
**使用場面**:
- サーバー状態の確認
- 接続状況の再確認
- 自動更新が遅い場合

#### Test Connection ボタン
**機能**: MCPサーバーとの接続テストを実行
**テスト内容**:
- コマンドディレクトリの存在確認
- ファイルベース通信のテスト
- 接続結果の詳細ログ出力

### 3. Settings セクション

#### Server Path 設定
**デフォルト**: `../unity-mcp-node`
**説明**: MCPサーバーの配置ディレクトリパス

**変更が必要な場合**:
- サーバーを別の場所に配置した時
- カスタムサーバー実装を使用する時

**設定例**:
```
../unity-mcp-node          # 相対パス（推奨）
/Users/name/mcp-server     # 絶対パス
./custom-server            # カレントディレクトリ基準
```

#### Port 設定
**デフォルト**: `3000`
**範囲**: 1000-65535
**説明**: MCPサーバーが使用する通信ポート

**変更が必要な場合**:
- 3000番ポートが他のアプリケーションで使用中
- ファイアウォール設定で特定ポートのみ許可
- 複数のMCPサーバーを同時実行

#### Auto Start on Unity Launch
**デフォルト**: ✅ 有効
**説明**: Unity起動時に自動でMCPサーバーを開始

**メリット**:
- 手動操作不要
- 開発開始までの時間短縮
- 忘れによる接続エラー防止

### 4. Data Management セクション

#### Data Status インジケーター
データサイズの視覚的表示：

- 🟢 **緑 (Green)**: 0-50KB - 適正サイズ、トークン消費も最小
- 🟡 **黄 (Yellow)**: 50-200KB - 注意レベル、定期的なクリア推奨
- 🔴 **赤 (Red)**: 200KB以上 - クリア必須、トークン消費大

#### Export Data ボタン
**機能**: 現在のUnityデータをJSON形式でエクスポート
**出力内容**:
- Console logs
- Editor state
- Scene information
- GameObject details
- Compile status

#### Force Export ボタン
**機能**: 強制的に全データを再収集・エクスポート
**使用場面**:
- データが更新されない場合
- 完全な状態情報が必要な場合

#### Clear Data ボタン
**機能**: 蓄積されたデータを完全削除
**対象ファイル**:
- `console-logs.json`
- `editor-state.json` 
- `scene-info.json`
- `gameobjects.json`

**実行タイミング**:
- プロジェクト変更時
- トークン消費を抑えたい時
- 古い情報をリセットしたい時

### 5. Logs セクション

#### ログ表示エリア
**内容**: リアルタイムのシステムログ
**種類**:
- Server operations
- Connection events
- Error messages
- Status changes

#### Copy Logs ボタン
**機能**: 表示されているログをクリップボードにコピー
**用途**: トラブルシューティング時の情報共有

#### Clear Logs ボタン
**機能**: 表示ログをクリア（ファイルは保持）

## ⚙️ 設定ファイルシステム

### settings.json の場所
```
UnityMCP/settings.json
```

### 設定内容
```json
{
    "serverPath": "../unity-mcp-node",
    "autoStartOnLaunch": true,
    "defaultPort": 3000,
    "lastModified": "2025-06-06T14:42:47Z"
}
```

### 自動保存機能
- UI での設定変更時に即座に保存
- Unity 起動時に自動読み込み
- 設定エラー時はデフォルト値を使用

## 🔄 ワークフロー例

### 日常的な開発フロー

1. **Unity 起動**
   - Auto Start 有効の場合、自動でMCPサーバー起動
   - Server Manager で状態確認

2. **AI接続 (選択)**
   
   **Claude Desktop の場合**:
   - Claude Desktop アプリを起動
   - Unity について質問・相談
   
   **Claude Code の場合**:
   - ターミナルでプロジェクトディレクトリに移動
   - `claude-code` コマンドでCLI起動
   - ファイル編集・コード作成を依頼

3. **開発作業**
   - Unity でゲーム開発
   - Claude に質問や依頼
   - リアルタイムでデータ同期

4. **作業終了**
   - Stop Server でサーバー停止
   - または Unity 終了時に自動停止

### Claude Desktop vs Claude Code 使い分け

| 用途 | Claude Desktop | Claude Code |
|------|----------------|-------------|
| **質問・相談** | ✅ 最適 | 🔵 可能 |
| **コード編集** | 🔵 可能 | ✅ 最適 |
| **ファイル操作** | ❌ 不可 | ✅ 得意 |
| **学習・理解** | ✅ 最適 | 🔵 可能 |
| **初心者向け** | ✅ 推奨 | 🔵 中級者向け |

### トラブル時のフロー

1. **接続問題発生**
   - Test Connection で詳細確認
   - Refresh で状態更新

2. **サーバー問題**
   - Stop → Start でサーバー再起動
   - ログで詳細エラー確認

3. **データ問題**
   - Clear Data でリセット
   - Force Export で再収集

## 🚨 トラブルシューティング

### パフォーマンス問題

**症状**: レスポンスが遅い
**対処法**:
1. Data Status を確認
2. 🔴赤の場合は Clear Data
3. 不要なログファイルを削除

### 接続エラー

**症状**: Connection Status が "Not Connected"
**対処法**:
1. Test Connection 実行
2. Port 番号の重複確認  
3. ファイアウォール設定確認
4. Server Path の正確性確認

### 起動エラー

**症状**: Start Server が失敗
**対処法**:
1. Node.js インストール確認
2. Server Path の存在確認
3. `npm install` の実行
4. ポート使用状況確認

## 💡 ベストプラクティス

### 効率的な使用方法

1. **Auto Start を有効に**: 手動操作を最小化
2. **定期的な Data Clear**: トークン消費を抑制
3. **Test Connection 活用**: 問題の早期発見
4. **ログ監視**: 異常の早期察知

### 設定の最適化

1. **適切なポート選択**: 他のアプリと競合しない番号
2. **Server Path の管理**: 相対パスで可搬性を確保
3. **データサイズ監視**: 🟢緑を維持するよう管理

## 📊 監視とメンテナンス

### 日次チェック項目

- [ ] Server Status が "Running"
- [ ] Connection Status が "Connected" 
- [ ] Data Status が 🟢緑または🟡黄
- [ ] エラーログの有無

### 週次メンテナンス

- [ ] Clear Data でデータリセット
- [ ] ログファイルのサイズ確認
- [ ] 設定ファイルのバックアップ

### 月次レビュー

- [ ] Server Path の最適化検討
- [ ] Port 設定の見直し
- [ ] パフォーマンス分析

---

このガイドを参考に、MCP Server Manager を効率的に活用して、AI支援Unity開発を楽しんでください！ 🚀