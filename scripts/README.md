# Unity MCP Learning - 開発スクリプト

このディレクトリには、Unity MCP Learning開発を効率化するスクリプトが含まれています。

## 📜 利用可能なスクリプト

### `sync-server.sh` - Server~自動同期

**unity-mcp-node** から **unity-mcp-learning/Server~** に最新のビルドを自動同期します。

#### 使用方法

```bash
# 基本同期（dist、package.json、設定ファイルのみ）
./scripts/sync-server.sh

# ソースファイル込みで同期（デバッグ用）
./scripts/sync-server.sh --with-source

# npm経由で実行
cd unity-mcp-node
npm run sync:server
npm run sync:server-source
```

#### 処理内容

1. **🏗️ MCPサーバービルド**: `unity-mcp-node`で`npm install`と`npm run build`実行
2. **🗂️ ディレクトリ準備**: 既存の`Server~`を削除し新規作成
3. **📋 ファイルコピー**: 
   - `dist/` (コンパイル済みJS)
   - `package.json` (依存関係情報)
   - `mcp-config.json` (MCP設定)
   - `schema/` (JSONスキーマ)
   - `src/`, `tsconfig.json` (`--with-source`オプション時)
4. **📊 同期情報作成**: `SYNC_INFO.txt`で同期履歴記録

#### 効果

- ✅ **手動コピー不要**: Server~の手動更新が不要になります
- ✅ **ビルド自動化**: TypeScriptコンパイルからコピーまで一括実行
- ✅ **エラー削減**: Unity側での「Server~が見つからない」エラーを解決
- ✅ **開発効率向上**: MCPサーバー変更後すぐにUnityでテスト可能

## 🔄 GitHub Actions統合

`.github/workflows/sync-server.yml`により、以下の場合に自動同期されます：

- **🔧 unity-mcp-nodeの変更時**: pushやPRで自動的にServer~更新
- **🎯 手動トリガー**: GitHub Actionsから手動実行可能
- **📝 自動コミット**: 変更があれば自動的にコミット・プッシュ

## 🎯 使用タイミング

### 開発時
```bash
# MCPサーバーコード変更後
./scripts/sync-server.sh
# → Unity Editorで再コンパイル
```

### リリース準備時
```bash
# 最終テスト前の完全同期
./scripts/sync-server.sh --with-source
```

### CI/CD
- GitHub Actionsが自動実行
- リリース時にも自動同期

## 🔧 トラブルシューティング

### スクリプト実行権限エラー
```bash
chmod +x scripts/sync-server.sh
```

### ビルドエラー
```bash
cd unity-mcp-node
npm install
npm run build
```

### パス関連エラー
- プロジェクトルートから実行してください
- `unity-mcp-node/`と`MCPLearning/Assets/Packages/unity-mcp-learning/`の存在を確認

## 📈 開発ワークフロー

1. **MCPサーバー開発**: `unity-mcp-node/src/`でコード修正
2. **同期実行**: `./scripts/sync-server.sh`
3. **Unity確認**: Unity Editorで動作テスト
4. **リピート**: 必要に応じて1-3を繰り返し

この仕組みにより、**MCPサーバーの変更がUnityで即座に反映**され、開発効率が大幅に向上します！