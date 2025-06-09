# GitHub Actions 自動同期システムガイド

## 🤖 **概要**

Unity MCP Learning では、`unity-mcp-node` ディレクトリの変更を自動的に Unity パッケージの `Server~` ディレクトリに同期する GitHub Actions ワークフローを実装しています。これにより、MCP サーバーの開発と Unity パッケージの更新が完全に自動化されています。

## 🔄 **自動同期システム**

### **トリガー条件**
- `unity-mcp-node/**` 内のファイル変更時
- `main` ブランチへのプッシュ
- プルリクエスト作成時
- 手動実行（`workflow_dispatch`）

### **同期対象**
```
unity-mcp-node/ → MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
├── dist/ (ビルド済みJavaScript)
├── src/ (TypeScriptソースコード)
├── package.json
├── mcp-config.json
├── schema/ (JSON Schema)
└── AUTO_SYNC_INFO.txt (同期情報)
```

## 📋 **ワークフローファイル**

### **.github/workflows/sync-server.yml**
```yaml
name: Sync MCP Server to Package

on:
  push:
    branches: [ main ]
    paths:
      - 'unity-mcp-node/**'
  pull_request:
    branches: [ main ]
    paths:
      - 'unity-mcp-node/**'
  workflow_dispatch:

permissions:
  contents: write

jobs:
  sync-server:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout repository
        uses: actions/checkout@v4
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '18'
          
      - name: Build MCP Server
        run: |
          cd unity-mcp-node
          npm install
          npm run build
          
      - name: Clean existing Server~ directory
        run: |
          rm -rf MCPLearning/Assets/Packages/unity-mcp-learning/Server~
          
      - name: Create and populate Server~ directory
        run: |
          # Create Server~ directory
          mkdir -p MCPLearning/Assets/Packages/unity-mcp-learning/Server~
          
          # Copy built dist files
          cp -r unity-mcp-node/dist MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
          
          # Copy essential files
          cp unity-mcp-node/package.json MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
          cp unity-mcp-node/mcp-config.json MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
          cp -r unity-mcp-node/schema MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
          
          # Copy TypeScript source for debugging (optional)
          cp -r unity-mcp-node/src MCPLearning/Assets/Packages/unity-mcp-learning/Server~/
          
          # Create manifest file
          echo "MCP Server files auto-synced by GitHub Actions on $(date)" > MCPLearning/Assets/Packages/unity-mcp-learning/Server~/AUTO_SYNC_INFO.txt
          echo "Built from unity-mcp-node commit: ${{ github.sha }}" >> MCPLearning/Assets/Packages/unity-mcp-learning/Server~/AUTO_SYNC_INFO.txt
          
      - name: Check for changes
        id: verify-changed-files
        run: |
          if [ -n "$(git status --porcelain)" ]; then
            echo "changed=true" >> $GITHUB_OUTPUT
          else
            echo "changed=false" >> $GITHUB_OUTPUT
          fi
          
      - name: Commit and push changes
        if: steps.verify-changed-files.outputs.changed == 'true'
        run: |
          git config --local user.email "action@github.com"
          git config --local user.name "GitHub Action"
          git add MCPLearning/Assets/Packages/unity-mcp-learning/Server~
          git commit -m "🤖 Auto-sync: Update Server~ from unity-mcp-node
          
          - Built MCP server from latest unity-mcp-node changes
          - Updated dist/ files and dependencies
          - Synchronized at $(date)
          
          🤖 Generated with GitHub Actions"
          git push
          
      - name: Summary
        run: |
          echo "✅ MCP Server sync completed"
          echo "📁 Server~ directory updated with latest unity-mcp-node build"
          echo "🔄 Changes automatically committed and pushed"
```

## 🛠️ **手動同期スクリプト**

開発時の手動同期には `scripts/sync-server.sh` を使用できます：

### **scripts/sync-server.sh**
```bash
#!/bin/bash

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Progress indicator
show_progress() {
    echo -e "${BLUE}🔄 $1${NC}"
}

show_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

show_warning() {
    echo -e "${YELLOW}⚠️ $1${NC}"
}

show_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Check if we're in the project root
if [ ! -d "unity-mcp-node" ] || [ ! -d "MCPLearning" ]; then
    show_error "このスクリプトはプロジェクトルートから実行してください"
    exit 1
fi

# Define paths
SOURCE_DIR="unity-mcp-node"
PACKAGE_DIR="MCPLearning/Assets/Packages/unity-mcp-learning/Server~"

show_progress "Unity MCP Server 同期開始..."

# Clean existing Server~ directory
if [ -d "$PACKAGE_DIR" ]; then
    show_progress "既存のServer~ディレクトリをクリーン中..."
    rm -rf "$PACKAGE_DIR"
fi

# Create Server~ directory
mkdir -p "$PACKAGE_DIR"
show_success "Server~ディレクトリ作成完了"

# Build MCP server
show_progress "MCPサーバーをビルド中..."
cd "$SOURCE_DIR"
npm install --silent
npm run build
cd ..
show_success "MCPサーバービルド完了"

# Copy files
show_progress "ファイルをコピー中..."

# Copy built dist files
cp -r "$SOURCE_DIR/dist" "$PACKAGE_DIR/"
show_success "dist/ ファイルコピー完了"

# Copy essential files
cp "$SOURCE_DIR/package.json" "$PACKAGE_DIR/"
cp "$SOURCE_DIR/mcp-config.json" "$PACKAGE_DIR/"
cp -r "$SOURCE_DIR/schema" "$PACKAGE_DIR/"
show_success "設定ファイルコピー完了"

# Copy TypeScript source for debugging (optional)
if [ "$1" = "--with-source" ]; then
    cp -r "$SOURCE_DIR/src" "$PACKAGE_DIR/"
    show_success "TypeScriptソースコピー完了"
fi

# Create sync info file
SYNC_TIME=$(date)
COMMIT_HASH=$(git rev-parse HEAD)
echo "MCP Server files manually synced on $SYNC_TIME" > "$PACKAGE_DIR/AUTO_SYNC_INFO.txt"
echo "Built from unity-mcp-node commit: $COMMIT_HASH" >> "$PACKAGE_DIR/AUTO_SYNC_INFO.txt"
show_success "同期情報ファイル作成完了"

# Show summary
echo ""
echo -e "${GREEN}🎉 Unity MCP Server 同期完了！${NC}"
echo -e "${BLUE}📁 同期先: $PACKAGE_DIR${NC}"
echo -e "${YELLOW}💡 Unity Editorで変更を確認してください${NC}"

# Show sync info
if [ -f "$PACKAGE_DIR/AUTO_SYNC_INFO.txt" ]; then
    echo ""
    echo -e "${BLUE}📋 同期情報:${NC}"
    cat "$PACKAGE_DIR/AUTO_SYNC_INFO.txt"
fi
```

## 📦 **package.json統合**

`unity-mcp-node/package.json` に同期スクリプトが組み込まれています：

```json
{
  "scripts": {
    "sync:server": "../scripts/sync-server.sh",
    "sync:server-source": "../scripts/sync-server.sh --with-source"
  }
}
```

### **使用方法**
```bash
# 基本同期（dist/, 設定ファイルのみ）
cd unity-mcp-node
npm run sync:server

# ソースコード含む同期（デバッグ用）
npm run sync:server-source
```

## 🔍 **同期内容詳細**

### **自動コピーされるファイル**
1. **dist/**: TypeScriptビルド済みJavaScript
2. **package.json**: Node.js依存関係情報
3. **mcp-config.json**: MCPサーバー設定
4. **schema/**: JSON Schema検証ファイル
5. **src/**: TypeScriptソースコード（デバッグ用）
6. **AUTO_SYNC_INFO.txt**: 同期情報・コミットハッシュ

### **Unity側の検知**
MCPPackageResolver.cs が Server~ ディレクトリの存在を自動検知：

```csharp
public static class MCPPackageResolver
{
    private static readonly string LOG_PREFIX = "[MCPPackageResolver]";
    
    static MCPPackageResolver()
    {
        // Force cache reset for development
        _serverPathCache = null;
    }
    
    public static string GetServerPath()
    {
        if (_serverPathCache != null)
            return _serverPathCache;
            
        string defaultServerPath = GetDefaultServerPath();
        
        // Force check directory existence with debug info
        MCPLogger.LogInfo($"{LOG_PREFIX} Checking Server~ directory at: {defaultServerPath}");
        var directoryExists = Directory.Exists(defaultServerPath);
        MCPLogger.LogInfo($"{LOG_PREFIX} Directory.Exists result: {directoryExists}");
        
        if (directoryExists)
        {
            _serverPathCache = defaultServerPath;
            MCPLogger.LogInfo($"{LOG_PREFIX} Server~ directory found: {defaultServerPath}");
            return defaultServerPath;
        }
        
        MCPLogger.LogError($"{LOG_PREFIX} Server~ directory not found in package");
        return null;
    }
}
```

## 🚀 **使用例とワークフロー**

### **開発時の典型的な流れ**
1. `unity-mcp-node/` でTypeScriptコード変更
2. Git commit & push
3. GitHub Actions が自動的に：
   - TypeScriptビルド実行
   - Server~ ディレクトリ更新
   - 変更をコミット・プッシュ
4. Unity Editor で自動的にServer~検知
5. MCPサーバー新バージョン利用可能

### **手動同期が必要な場合**
- ローカル開発中のテスト
- GitHub Actions無効時の緊急対応
- カスタムビルド設定での同期

## 🔧 **トラブルシューティング**

### **よくある問題**

#### **問題**: Server~ ディレクトリが見つからない
```bash
[MCPPackageResolver] Server~ directory not found in package
```

**解決方法**:
```bash
# 手動同期実行
cd unity-mcp-node
npm run sync:server

# または GitHub Actions 手動実行
gh workflow run sync-server.yml
```

#### **問題**: GitHub Actions権限エラー
```
Permission to repository.git denied to github-actions[bot]
```

**解決方法**: ワークフローファイルに権限設定を追加
```yaml
permissions:
  contents: write
```

#### **問題**: TypeScriptビルドエラー
```
npm run build failed
```

**解決方法**:
```bash
cd unity-mcp-node
npm install
npm run build
# エラー詳細を確認し修正
```

### **デバッグ手順**
1. **AUTO_SYNC_INFO.txt確認**: 最新の同期時刻・コミットハッシュ
2. **Unity Console確認**: MCPPackageResolver ログ
3. **GitHub Actions確認**: ワークフロー実行履歴
4. **手動同期テスト**: scripts/sync-server.sh 実行

## 🎯 **最適化とベストプラクティス**

### **GitHub Actions最適化**
- Node.js 18 固定でビルド一貫性確保
- 変更検知でコミット不要時のスキップ
- 詳細な進捗表示とエラーハンドリング

### **セキュリティ考慮**
- GITHUB_TOKEN使用で安全な認証
- `contents: write` 最小権限原則
- ビルド成果物のみ同期（ソースコードも含むがオプション）

### **パフォーマンス**
- キャッシュ利用でビルド高速化
- 差分検知で不要なコミット回避
- 並列実行可能な手動・自動同期

## 🌟 **メリット**

1. **開発効率向上**: 手動同期作業の完全自動化
2. **一貫性保証**: ビルドプロセスの標準化
3. **トレーサビリティ**: AUTO_SYNC_INFO.txtでバージョン追跡
4. **エラー削減**: 手動コピー時のヒューマンエラー防止
5. **CI/CD統合**: 継続的インテグレーションの一部として機能

この自動同期システムにより、Unity MCP Learning の開発・配布プロセスが大幅に効率化され、開発者は本質的な機能開発に集中できるようになります。