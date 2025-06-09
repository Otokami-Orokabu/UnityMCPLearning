#!/bin/bash

# Unity MCP Learning - Server~ 同期スクリプト
# unity-mcp-nodeから unity-mcp-learning/Server~ に最新のビルドをコピー

set -e

# カラー定義
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔄 Unity MCP Learning - Server~ 同期開始${NC}"

# プロジェクトルートに移動
cd "$(dirname "$0")/.."

# unity-mcp-nodeディレクトリの存在確認
if [ ! -d "unity-mcp-node" ]; then
    echo -e "${RED}❌ unity-mcp-nodeディレクトリが見つかりません${NC}"
    exit 1
fi

# パッケージディレクトリの存在確認
PACKAGE_DIR="MCPLearning/Assets/Packages/unity-mcp-learning"
if [ ! -d "$PACKAGE_DIR" ]; then
    echo -e "${RED}❌ unity-mcp-learningパッケージディレクトリが見つかりません${NC}"
    exit 1
fi

echo -e "${YELLOW}📦 MCPサーバーをビルド中...${NC}"

# MCPサーバーのビルド
cd unity-mcp-node
npm install --silent
npm run build

# ビルド成功確認
if [ ! -d "dist" ]; then
    echo -e "${RED}❌ ビルドに失敗しました (dist/が生成されませんでした)${NC}"
    exit 1
fi

cd ..

echo -e "${YELLOW}🗂️  Server~ディレクトリを準備中...${NC}"

# 既存のServer~ディレクトリを削除
rm -rf "$PACKAGE_DIR/Server~"

# Server~ディレクトリを作成
mkdir -p "$PACKAGE_DIR/Server~"

echo -e "${YELLOW}📋 ファイルをコピー中...${NC}"

# 必須ファイルをコピー
cp -r unity-mcp-node/dist "$PACKAGE_DIR/Server~/"
cp unity-mcp-node/package.json "$PACKAGE_DIR/Server~/"
cp unity-mcp-node/tsconfig.json "$PACKAGE_DIR/Server~/"
cp -r unity-mcp-node/schema "$PACKAGE_DIR/Server~/"
cp -r unity-mcp-node/node_modules "$PACKAGE_DIR/Server~/"

# Server~専用のmcp-config.jsonを作成（正しい相対パス設定）
cat > "$PACKAGE_DIR/Server~/mcp-config.json" << 'EOF'
{
  "mcpServers": {
    "unity-mcp-server": {
      "command": "node",
      "args": ["dist/index.js"]
    }
  },
  "unityDataPath": "../../../../UnityMCP/Data"
}
EOF

# オプション: ソースファイルもコピー（デバッグ用）
if [ "$1" = "--with-source" ]; then
    echo -e "${YELLOW}📝 ソースファイルも含めてコピー中...${NC}"
    cp -r unity-mcp-node/src "$PACKAGE_DIR/Server~/"
fi

# 同期情報ファイルを作成
cat > "$PACKAGE_DIR/Server~/SYNC_INFO.txt" << EOF
MCP Server Sync Information
==========================

Sync Date: $(date)
Script: scripts/sync-server.sh
Source: unity-mcp-node/
Target: $PACKAGE_DIR/Server~/

Contents:
- dist/ (compiled JavaScript)
- package.json (dependencies info)
- mcp-config.json (MCP configuration)
- schema/ (JSON schemas)
EOF

if [ "$1" = "--with-source" ]; then
    echo "- src/ (TypeScript source)" >> "$PACKAGE_DIR/Server~/SYNC_INFO.txt"
    echo "- tsconfig.json (TypeScript config)" >> "$PACKAGE_DIR/Server~/SYNC_INFO.txt"
fi

echo -e "${GREEN}✅ Server~同期完了${NC}"
echo -e "${BLUE}📁 同期先: $PACKAGE_DIR/Server~${NC}"
echo -e "${BLUE}📋 詳細: $PACKAGE_DIR/Server~/SYNC_INFO.txt${NC}"

# ファイル数を表示
FILE_COUNT=$(find "$PACKAGE_DIR/Server~" -type f | wc -l)
echo -e "${GREEN}📊 コピーされたファイル数: $FILE_COUNT${NC}"

echo -e "${BLUE}🎯 Unityエディターで再コンパイルしてください${NC}"