# Unity MCP ドキュメント

Unity MCPプロジェクトの包括的なドキュメント集です。

## 📁 ディレクトリ構造

### 🎯 [tutorial/](./tutorial/) - チュートリアル・ガイド
初心者向けのセットアップから高度な使用方法まで、ステップバイステップで学習できます。

- **[00-getting-started.md](./tutorial/00-getting-started.md)** - プロジェクト概要と前提知識
- **[01-environment-setup.md](./tutorial/01-environment-setup.md)** - 環境設定とClaude Desktop設定
- **[02-step1-basic-communication.md](./tutorial/02-step1-basic-communication.md)** - 基本通信の確立
- **[03-step2-unity-integration.md](./tutorial/03-step2-unity-integration.md)** - Unity連携システム実装
- **[04-troubleshooting.md](./tutorial/04-troubleshooting.md)** - トラブルシューティング
- **[05-advanced-configuration.md](./tutorial/05-advanced-configuration.md)** - 高度な設定・カスタマイズ
- **[06-step3-unity-control.md](./tutorial/06-step3-unity-control.md)** - Unity制御システム
- **[07-current-capabilities.md](./tutorial/07-current-capabilities.md)** - 現在の機能
- **[08-quick-start-guide.md](./tutorial/08-quick-start-guide.md)** - クイックスタートガイド
- **[09-claude-code-mcp-integration.md](./tutorial/09-claude-code-mcp-integration.md)** - Claude Code CLI統合ガイド
- **[10-complete-user-guide.md](./tutorial/10-complete-user-guide.md)** - 完全初心者ガイド（✨ 新規追加）
- **[11-mcp-server-manager-guide.md](./tutorial/11-mcp-server-manager-guide.md)** - MCP Server Manager完全ガイド（✨ 新規追加）

### 🛠️ [development/](./development/) - 開発者向けドキュメント
現在進行中の開発計画と将来のロードマップです。

- **[comprehensive-roadmap.md](./development/comprehensive-roadmap.md)** - 包括的開発ロードマップ（継続更新中）
- **[settings-system-guide.md](./development/settings-system-guide.md)** - 設定システム詳細ガイド（✨ 新規追加）

### ⚖️ [legal/](./legal/) - 法的・セキュリティ文書
ライセンス、セキュリティ対策、公開準備に関する情報です。

- **[license-guide.md](./legal/license-guide.md)** - ライセンスガイド（MIT License）
- **[security-analysis.md](./legal/security-analysis.md)** - セキュリティ分析と公開準備

### 📁 [completed/](./completed/) - 完了済みドキュメント
実装・達成が完了した計画・タスクのドキュメントです。

- **[claude-code-integration.md](./completed/claude-code-integration.md)** - Claude Code CLI統合（✅ 完了）
- **[unity-console-integration.md](./completed/unity-console-integration.md)** - Unity Console統合（✅ 完了）
- **[improvement-roadmap.md](./completed/improvement-roadmap.md)** - 改善ロードマップ（✅ 完了）
- **[code-quality-analysis.md](./completed/code-quality-analysis.md)** - コード品質分析・改善（✅ 完了）
- **[unity-test-runner-guide.md](./completed/unity-test-runner-guide.md)** - Unity Test Runner導入（✅ 完了）
- **[review-based-tasks.md](./completed/review-based-tasks.md)** - 査読ベース改善タスク（✅ 完了）

### 🚀 [future/](./future/) - 将来計画
今後の機能拡張や発展計画についてのアイデアと設計です。

- **[distribution-packages.md](./future/distribution-packages.md)** - 配布パッケージ計画
- **[future-ideas.md](./future/future-ideas.md)** - 将来のアイデア
- **[github-actions-claude-code.md](./future/github-actions-claude-code.md)** - GitHub Actions自動化
- **[unity-mcp-manager.md](./future/unity-mcp-manager.md)** - Unity Editor統合管理システム

### 🤖 [prompt/](./prompt/) - AI設定ファイル
Claude DesktopとのやりとりやAI設定に関するファイルです。

- **[prompt.yaml](./prompt/prompt.yaml)** - 基本プロンプト設定
- **[prompt-updated.yaml](./prompt/prompt-updated.yaml)** - 更新版プロンプト設定

## 🎯 目的別ドキュメント案内

### 初めて使う方
1. [getting-started.md](./tutorial/00-getting-started.md) - プロジェクト概要を理解
2. [environment-setup.md](./tutorial/01-environment-setup.md) - 環境を構築
3. [quick-start-guide.md](./tutorial/08-quick-start-guide.md) - 手早く動作確認

### 開発に参加したい方
1. [code-quality-analysis.md](./development/code-quality-analysis.md) - 現状の課題を把握
2. [improvement-roadmap.md](./development/improvement-roadmap.md) - 改善計画を確認
3. [unity-test-runner-guide.md](./development/unity-test-runner-guide.md) - テスト環境を構築

### ライセンスやセキュリティを確認したい方
1. [license-guide.md](./legal/license-guide.md) - ライセンス情報
2. [security-analysis.md](./legal/security-analysis.md) - セキュリティ対策

### 将来の計画を知りたい方
1. [future-ideas.md](./future/future-ideas.md) - アイデア一覧
2. [distribution-packages.md](./future/distribution-packages.md) - 配布計画

## 📖 プロジェクト状況

**包括的な情報**: メインの [README.md](../README.md) にプロジェクト全体の詳細情報があります。

**現在の状態**: Unity Console統合完了 - AI駆動Unity開発実現・リアルタイムエラー検知・即座フィードバック機能完備

### 🚀 最新実装機能
- **Unity Console統合** - リアルタイムエラー検知とコンパイル監視
- **AI駆動開発サイクル** - Claude Code ↔ Unity ↔ MCP Server完全統合
- **即座フィードバック** - コンパイル結果を1-3秒で取得
- **詳細エラー情報** - ファイル・行番号の正確な位置特定

### 実装済み機能
- Claude Desktop ↔ MCP Server ↔ Unity Editor の双方向通信
- リアルタイムデータ取得（6種類のエクスポーター + Console/Compile監視）
- Unity制御コマンド（GameObject作成、4ツール実装済み）
- **Unity Console統合機能**（get_console_logs、wait_for_compilation）
- 包括的エラーハンドリング（ErrorCode enum + MCPError class）
- 設定ファイル検証（JSON Schema + ajv）
- モジュール化アーキテクチャ（8専門モジュール）
- 多言語対応（英語・日本語）
- テスト環境（Jest 125テスト + Unity Test Runner）
- APIドキュメント自動生成（TypeDoc）

### 完了した品質改善
- ✅ ファイル分割（943行→211行、77%削減）
- ✅ エラーハンドリング統一化
- ✅ 設定検証システム
- ✅ テストカバレッジ大幅改善
- ✅ debounce機能によるパフォーマンス最適化
- ✅ Unity Console統合（AI駆動開発実現）
- ✅ ドキュメント整理（completed/カテゴリ追加）

### 📁 完了済み機能
詳細は [completed/](./completed/) ディレクトリを参照：
- Claude Code CLI統合（2025年6月6日完了）
- Unity Console統合（2025年6月6日完了）
- 改善ロードマップ（2025年1月6日完了）
- コード品質分析・改善（2025年1月6日完了）
- Unity Test Runner導入（2025年1月6日完了）
- 査読ベース改善タスク（2025年1月6日完了）

### 次のステップ
- セキュリティ強化（基本対策実装）
- 機能拡張（色指定、マテリアル適用、Transform操作）
- 高機能ログビューワー実装

---

*このドキュメントは定期的に更新されます。最新情報は各ドキュメントを直接確認してください。*