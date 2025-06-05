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

### 🛠️ [development/](./development/) - 開発者向けドキュメント
コード品質、テスト、改善計画など開発に関する技術情報です。

- **[code-quality-analysis.md](./development/code-quality-analysis.md)** - コード品質分析レポート
- **[improvement-roadmap.md](./development/improvement-roadmap.md)** - 改善ロードマップ
- **[unity-test-runner-guide.md](./development/unity-test-runner-guide.md)** - Unity Test Runner導入ガイド

### ⚖️ [legal/](./legal/) - 法的・セキュリティ文書
ライセンス、セキュリティ対策、公開準備に関する情報です。

- **[license-guide.md](./legal/license-guide.md)** - ライセンスガイド（MIT License）
- **[security-analysis.md](./legal/security-analysis.md)** - セキュリティ分析と公開準備

### 🚀 [future/](./future/) - 将来計画
今後の機能拡張や発展計画についてのアイデアと設計です。

- **[distribution-packages.md](./future/distribution-packages.md)** - 配布パッケージ計画
- **[future-ideas.md](./future/future-ideas.md)** - 将来のアイデア
- **[unity-console-integration.md](./future/unity-console-integration.md)** - Unity Console統合
- **[unity-mcp-manager.md](./future/unity-mcp-manager.md)** - Unity MCP管理システム

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

**現在の状態**: Step 3 完了 - Claude Desktop ↔ Unity 完全双方向制御システム確立済み

### 実装済み機能
- Claude Desktop ↔ MCP Server ↔ Unity Editor の双方向通信
- リアルタイムデータ取得（6種類のエクスポーター）
- Unity制御コマンド（GameObject作成）
- 包括的エラーハンドリング

### 次のステップ
- コード品質改善（テストカバレッジ向上）
- パフォーマンス最適化
- 機能拡張（色指定、マテリアル適用など）

---

*このドキュメントは定期的に更新されます。最新情報は各ドキュメントを直接確認してください。*