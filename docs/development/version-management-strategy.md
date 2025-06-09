# バージョン管理戦略

**最終更新**: 2025年6月9日  
**現在のバージョン**: v0.1.12

## 🎯 バージョニング方針

### セマンティックバージョニング準拠
```
MAJOR.MINOR.PATCH (例: 1.0.0)
```

- **MAJOR**: 破壊的変更・大幅なAPI変更
- **MINOR**: 新機能追加・後方互換性維持
- **PATCH**: バグ修正・小さな改善

### 現在の0.xシリーズ
```
0.MINOR.PATCH (例: 0.1.12)
```
- **0.x**: 開発版・実験的機能
- **MINOR**: 機能追加・改善
- **PATCH**: バグ修正・微調整

## 📚 リリース履歴

### v0.1.x シリーズ（Git URL配布対応）

#### 🚀 メジャーマイルストーン

**v0.1.0** (2025-06-07)
- Git URL配布機能の初回実装
- GitHub Actionsワークフロー基盤構築
- Unity Package Manager対応開始

**v0.1.8** (2025-06-08)
- mcp-config.json配布対応
- MCPサーバー設定ファイル問題解決

**v0.1.10** (2025-06-09)
- schema/node_modules完全配布対応
- PackageCache環境での安定動作実現

**v0.1.11** (2025-06-09)
- UIファイル（UXML/USS）配布対応
- MCPServerManagerWindow警告解消

**v0.1.12** (2025-06-09) ⭐ **現在の安定版**
- 動的パス解決システム実装
- PackageCache完全対応完了
- ハードコードパス問題完全解決

#### 🔧 中間バージョン

| バージョン | 日付 | 主な変更内容 |
|-----------|------|-------------|
| v0.1.1 | 2025-06-07 | TypeScript設定ファイル追加 |
| v0.1.2 | 2025-06-07 | package.jsonバージョン同期 |
| v0.1.3 | 2025-06-07 | distディレクトリコピー修正 |
| v0.1.4 | 2025-06-07 | 起動メッセージクリーンアップ |
| v0.1.5 | 2025-06-08 | UnityDataPath堅牢性改善 |
| v0.1.6 | 2025-06-08 | プロジェクト固有データパス |
| ~~v0.1.7~~ | - | 欠番 |
| v0.1.9 | 2025-06-09 | Server~ディレクトリ包含確実化 |

## 🔄 今後のバージョニング計画

### Phase 1: v0.2.x シリーズ（機能拡張）
- **v0.2.0**: 複数Unity同時起動サポート（Issue #9）
- **v0.2.1-v0.2.x**: マルチプロジェクト機能改善

### Phase 2: v0.3.x シリーズ（高度機能）
- **v0.3.0**: 高機能ログビューワー（Issue #2）
- **v0.3.1**: MCP API拡張（Issue #3）

### Phase 3: v1.0.0（安定版リリース）
```
v1.0.0 リリース条件:
✅ Git URL配布完全対応
✅ PackageCache安定動作
✅ マルチプロジェクト対応
✅ 包括的ドキュメント
✅ セキュリティ監査完了
❌ 高機能ログビューワー
❌ 商用利用実績
❌ コミュニティフィードバック反映
```

## 🏷️ タグ管理ポリシー

### 保持するタグ
- ✅ **v0.1.0以降**: すべて保持（重要なマイルストーン）
- ✅ **安定版タグ**: v0.1.12, 将来のv1.0.0等

### 削除対象タグ
- ❌ **v0.0.x**: 存在しない（適切）
- ❌ **テスト用タグ**: alpha, beta, rc等は適宜削除
- ❌ **重複タグ**: 同一機能の複数タグ

### GitHub Releases対応
```bash
# メジャーバージョンのみGitHub Releasesに登録
v0.1.0, v0.1.8, v0.1.10, v0.1.11, v0.1.12, v1.0.0
```

## 📋 リリースプロセス

### 1. バージョン決定
```bash
# パッケージバージョン更新
# MCPLearning/Assets/Packages/unity-mcp-learning/package.json
{
  "version": "0.1.13"
}
```

### 2. 変更履歴更新
```bash
# CHANGELOG.md または リリースノート更新
## v0.1.13
- 新機能: ...
- バグ修正: ...
- 改善: ...
```

### 3. コミット・タグ・プッシュ
```bash
git add .
git commit -m "release: v0.1.13 - 機能概要"
git tag v0.1.13
git push origin v0.1.13
```

### 4. GitHub Actions自動実行
- Server~ディレクトリ自動構築
- パッケージファイル更新
- Git URLで即座利用可能

## 🔍 バージョン確認方法

### Package Manager
```
Window > Package Manager > In Project > Unity MCP Learning
Version: 0.1.12
```

### Git URL確認
```bash
git ls-remote --tags https://github.com/Otokami-Orokabu/UnityMCPLearning.git
```

### 実行時確認
```csharp
// MCPPackageResolver.cs内で確認可能
var packageInfo = PackageInfo.FindForAssetPath(packagePath);
Debug.Log($"Package Version: {packageInfo.version}");
```

## 📊 互換性マトリックス

| Unity MCP Learning | Unity Editor | Node.js | Claude Desktop |
|-------------------|--------------|---------|----------------|
| v0.1.0-v0.1.12 | 6000.0+ | 18.0+ | MCP対応版 |
| v0.2.x (予定) | 6000.0+ | 18.0+ | MCP対応版 |
| v1.0.0 (予定) | 6000.0+ | 20.0+ | MCP対応版 |

## 🎯 品質保証

### リリース前チェックリスト
- [ ] MCPLearning開発環境での動作確認
- [ ] 外部プロジェクトでのGit URLインストール確認
- [ ] 基本機能テスト（ping, create_cube, unity_info_realtime）
- [ ] Console ログでエラー0件確認
- [ ] ドキュメント更新

### 自動化テスト
- ✅ **GitHub Actions**: 自動ビルド・パッケージング
- ✅ **Jest**: 125テスト自動実行
- ✅ **Unity Test Runner**: 29セキュリティテスト
- ✅ **ESLint**: セキュリティ・品質チェック

## 📞 バージョン関連サポート

### 問題報告時の情報
```
- Unity MCP Learning バージョン: v0.1.12
- Unity Editor バージョン: 6000.1.5f1
- インストール方法: Git URL / Local Package
- エラー内容: [具体的なエラーメッセージ]
```

### アップグレードパス
```bash
# Package Manager経由
1. Remove existing package
2. Add package from git URL (最新タグ)

# Git URL更新
https://github.com/Otokami-Orokabu/UnityMCPLearning.git?path=MCPLearning/Assets/Packages/unity-mcp-learning#v0.1.12
```

---

**現在の推奨バージョン**: v0.1.12  
**次期メジャーバージョン**: v0.2.0（マルチプロジェクト対応）  
**長期目標**: v1.0.0（2025年夏予定）