#!/bin/bash

echo "Testing complete MCP initialization flow..."

# 完全なMCP初期化フローをテスト
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test-client","version":"1.0.0"}}}'
  echo '{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}'
  echo '{"jsonrpc":"2.0","id":2,"method":"ping","params":{}}'
) | node dist/index.js

echo -e "\n---\n"

echo "Testing invalid protocol version..."
echo '{"jsonrpc":"2.0","id":3,"method":"initialize","params":{"protocolVersion":"1.0.0","capabilities":{},"clientInfo":{"name":"test-client","version":"1.0.0"}}}' | node dist/index.js

echo -e "\n---\n"

echo "Testing unknown method..."
echo '{"jsonrpc":"2.0","id":4,"method":"unknown","params":{}}' | node dist/index.js

echo -e "\n---\n"

echo "Testing Unity dummy data..."
echo '{"jsonrpc":"2.0","id":5,"method":"unity/info","params":{}}' | node dist/index.js

echo -e "\n---\n"

echo "Testing Unity dynamic data..."
echo '{"jsonrpc":"2.0","id":6,"method":"unity/info/dynamic","params":{}}' | node dist/index.js