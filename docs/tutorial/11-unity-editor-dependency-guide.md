# Unity Editor依存機能ガイド

## 🎯 概要

Unity MCP Learningシステムは、Unity Editorが開いている時と閉じている時で利用可能な機能が異なります。このガイドでは、その機能差と最適な活用方法を説明します。

## 🔄 Unity Editor開閉状態での機能マトリックス

### ✅ **Unity Editor開いている時の機能**

#### **🎮 Unity制御系機能**
| 機能 | コマンド例 | 実行時間 | 備考 |
|------|------------|----------|------|
| GameObject作成 | `create a cube` | ~50-100ms | Unity APIが必要 |
| シーン操作 | `unity_info_realtime scene` | ~15ms | EditorApplication経由 |
| リアルタイム情報取得 | `unity_info_realtime all` | ~15ms | 6カテゴリ全情報 |

#### **🔧 Unity Console統合機能**
| 機能 | コマンド例 | 活用場面 | 備考 |
|------|------------|----------|------|
| コンパイル監視 | `wait for compilation` | AI駆動開発 | CompilationPipeline必要 |
| エラー検知 | `get console logs --filter errors` | デバッグ・問題解決 | Application.logMessageReceived |
| Test Runner実行 | Unity内でテスト実行 | 品質確認 | Editor環境でのみ動作 |

#### **📊 データエクスポート機能**
| データ種類 | 出力ファイル | 更新タイミング | Unity API依存 |
|------------|--------------|----------------|---------------|
| プロジェクト情報 | `project-info.json` | プロジェクト変更時 | ✅ |
| シーン状態 | `scene-info.json` | シーン変更時 | ✅ |
| GameObject一覧 | `gameobjects.json` | オブジェクト変更時 | ✅ |
| アセット統計 | `assets-info.json` | アセット変更時 | ✅ |
| ビルド設定 | `build-info.json` | ビルド設定変更時 | ✅ |
| エディター状態 | `editor-state.json` | エディター操作時 | ✅ |

### ✅ **Unity Editor閉じていてもできること**

#### **🖥️ MCPサーバー機能**
| 機能 | コマンド例 | 説明 | Unity依存 |
|------|------------|------|-----------|
| 接続確認 | `ping` | サーバー生存確認 | ❌ |
| ファイル監視 | 自動動作 | Node.js FileSystemWatcher | ❌ |
| JSON処理 | 自動動作 | データ解析・変換 | ❌ |
| 設定管理 | 設定ファイル編集 | MCP・Claude Desktop設定 | ❌ |

#### **🛡️ セキュリティ機能**
| 機能 | 実装クラス | テスト件数 | Unity依存 |
|------|------------|------------|-----------|
| パス検証 | `PathSecurityValidator` | 7件 | ❌ |
| 機密データ検出 | `SensitiveDataFilter` | 12件 | ❌ |
| プロセス管理 | `ProcessSecurityManager` | 準備中 | ❌ |

#### **📁 プロジェクト管理機能**
| 機能 | 例 | Unity依存 |
|------|---|-----------|
| ドキュメント更新 | README.md編集 | ❌ |
| 設定変更 | mcp-config.json更新 | ❌ |
| Git操作 | コミット・プッシュ | ❌ |
| コード品質チェック | ESLint・Jest実行 | ❌ |

#### **🧪 テスト実行**
| テスト種類 | 実行環境 | テスト数 | Unity依存 |
|------------|----------|----------|-----------|
| Jest単体テスト | Node.js | 125件 | ❌ |
| セキュリティテスト | Jest | 29件 | ❌ |
| Unity Test Runner | Unity Editor | 29件 | ✅ |

### ❌ **Unity Editor閉じている時にできないこと**

#### **Unity API依存機能**
- GameObject作成系コマンド（`create_*`）
- Unity情報取得（`unity_info_realtime`）
- Console統合（`get_console_logs`, `wait_for_compilation`）
- データエクスポート（自動JSON更新）
- Unity Test Runner実行

## 🎯 **実用的な使い分け戦略**

### 📅 **開発フェーズ別の活用**

#### **🔥 アクティブ開発時（Unity Editor開く）**
```bash
# AI駆動開発サイクル
1. コード生成・編集
2. wait for compilation      # コンパイル監視
3. get console logs         # エラー確認
4. コード修正
5. create a cube           # 動作確認
```

#### **📝 ドキュメント・管理作業時（Unity Editor閉じる）**
```bash
# 効率的なプロジェクト管理
1. ping                    # MCP動作確認
2. ドキュメント更新
3. Jest テスト実行         # Node.js側テスト
4. Git操作・コミット
5. 設定ファイル調整
```

### ⚡ **パフォーマンス最適化**

#### **メモリ・CPU使用量削減**
- **開発時以外**: Unity Editor閉じてリソース節約
- **バックグラウンド作業**: MCPサーバーのみ稼働
- **自動化処理**: Unity不要な処理は軽量で実行

#### **作業効率向上**
- **切り替え最小化**: Unity/非Unity作業を分離
- **並行作業**: Unity開発中にMCPサーバーで管理業務
- **リソース配分**: 重い処理とエディター操作の分離

## 🔍 **Unity Editor状態の判定方法**

### **自動判定システム**
```typescript
// MCPサーバー側での状態判定
async function checkUnityEditorStatus(): Promise<boolean> {
    try {
        const result = await executeUnityInfo();
        return result.success;
    } catch (error) {
        return false; // Unity Editor未起動
    }
}
```

### **エラーメッセージでの判定**
```
Unity Editor開いている時:
✅ "Unity project information retrieved successfully"

Unity Editor閉じている時:
❌ "Unity data files not found"
❌ "Cannot connect to Unity Editor"
```

## 📊 **機能可用性チャート**

| 機能カテゴリ | Unity Editor必須 | Editor不要 | 備考 |
|-------------|------------------|------------|------|
| GameObject制御 | ✅ | ❌ | Unity API必須 |
| Console統合 | ✅ | ❌ | EditorApplication必要 |
| データ監視 | ✅ | ❌ | リアルタイム更新 |
| Test Runner | ✅ | ❌ | Unity内テスト実行 |
| MCP通信 | ✅ | ✅ | Node.js独立動作 |
| セキュリティ機能 | ❌ | ✅ | 純粋なC#ロジック |
| プロジェクト管理 | ❌ | ✅ | ファイル操作中心 |
| Jest テスト | ❌ | ✅ | Node.js環境 |

## 💡 **ベストプラクティス**

### **🎯 効率的なワークフロー**

#### **朝の立ち上げルーチン**
1. Unity Editorを開く前にMCPサーバー単体でシステムチェック
2. `ping` でMCPサーバー確認
3. Jest テストでNode.js側品質確認
4. Unity Editor起動後に統合機能テスト

#### **作業切り替え時**
```bash
# Unity開発 → ドキュメント作業
1. Unity作業完了・保存
2. Unity Editor最小化（閉じても可）
3. ドキュメント・設定作業を効率的に実行
4. 必要時にUnity Editorを再アクティブ化
```

### **🔧 トラブルシューティング**

#### **Unity Editor応答なし時**
```bash
# MCP機能で診断
1. ping                    # MCPサーバー確認
2. unity_info_realtime     # Unity状態確認
3. get console logs        # エラー原因特定
```

#### **開発環境リセット**
```bash
# Unity Editor再起動不要な復旧
1. Unity Editor閉じる
2. MCPサーバー・設定確認
3. Jest テストで基盤確認
4. Unity Editor再起動・統合テスト
```

## 🎉 **まとめ**

Unity MCP Learningは、Unity Editorの状態に関係なく、常に何らかの価値のある作業が可能な柔軟なシステムです。

- **Unity開発時**: 最大限の統合機能でAI駆動開発
- **管理・ドキュメント時**: 軽量・高速なプロジェクト管理
- **状態遷移**: スムーズな切り替えで生産性最大化

この特性を理解して活用することで、Unity開発効率とプロジェクト管理効率の両方を大幅に向上できます。