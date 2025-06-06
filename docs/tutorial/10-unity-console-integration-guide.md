# Unity Console統合完全ガイド 🔧

## 🎯 概要

Unity Console統合により、**AI駆動Unity開発**が実現されました。このガイドでは、リアルタイムエラー検知と即座フィードバック機能の使い方を詳しく説明します。

## ✅ 実装済み機能

### Console Log収集システム
- **ConsoleLogExporter.cs**: Unity Consoleの全ログをリアルタイム収集
- **get_console_logs**: フィルター付きログ取得MCPツール
- **詳細情報**: エラー・警告・ログ・アサートを分類・集計

### コンパイル監視システム  
- **CompileStatusMonitor.cs**: コンパイル状態の監視・測定
- **wait_for_compilation**: コンパイル完了待機MCPツール
- **パフォーマンス**: コンパイル時間の正確な測定

## 🚀 AI駆動開発フローの実現

### 従来の開発フロー
```
Claude Code → C#コード生成
    ↓
Unity Editorで手動確認
    ↓  
エラーがあれば手動コピペ
    ↓
Claude Codeで修正
```

### Unity Console統合後の開発フロー
```
Claude Code → C#コード生成・保存
    ↓
Unity自動コンパイル (1-3秒)
    ↓
wait_for_compilation → 結果即座取得
    ↓
エラー時: get_console_logs → 詳細位置情報
    ↓
Claude Codeが即座修正 → 高速開発サイクル
```

## 📋 実践的な使用方法

### 1. コンソールログの確認

#### 基本的なログ取得
```bash
# 全ログを取得
get console logs

# エラーのみ取得  
get console logs --filter errors

# 警告のみ取得
get console logs --filter warnings

# 直近10件のログ
get console logs --filter recent --limit 10
```

#### 取得できる情報
- **タイムスタンプ**: `2025-06-06 17:48:55.321`
- **ログタイプ**: Log, Warning, Error, Assert, Exception
- **メッセージ**: 実際のログ内容
- **スタックトレース**: ファイル・行番号の詳細位置
- **統計情報**: 各種類のログ数

#### 出力例
```
# Unity Console Logs

## Summary
- Total Logs: 10
- Errors: 0  
- Warnings: 0
- Info: 10
- Last Update: 2025-06-06 17:49:01.082

## Logs (Filter: all, Showing: 10)

[2025-06-06 17:48:55.321] [Log] Hello, World!
  Stack: UnityEngine.Debug:Log (object)
HelloWorld:Start () (at Assets/HelloWorld.cs:7)
```

### 2. コンパイル監視機能

#### コンパイル完了待機
```bash
# デフォルト30秒でタイムアウト
wait for compilation

# 45秒でタイムアウト設定
wait for compilation --timeout 45
```

#### 成功時の出力
```
✅ **Compilation Successful!**

- **Duration**: 1.8s
- **Warnings**: 0
- **Wait Time**: 0.0s

Unity compilation completed successfully.
```

#### 失敗時の出力
```
❌ **Compilation Failed!**

- **Duration**: 1.3s
- **Errors**: 2
- **Warnings**: 0
- **Wait Time**: 0.1s

**Error Details:**
- `Assets/Scripts/MyScript.cs(25,13)`: Error - The type 'InvalidType' could not be found
- `Assets/Scripts/MyScript.cs(30,5)`: Error - Syntax error, unexpected token
```

## 🛠️ 実装詳細（開発者向け）

### Unity側コンポーネント

#### ConsoleLogExporter.cs
```csharp
// Application.logMessageReceived イベントで全ログを収集
static ConsoleLogExporter()
{
    Application.logMessageReceived += OnLogMessageReceived;
    Application.logMessageReceivedThreaded += OnLogMessageReceived;
}

private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
{
    // ログエントリー作成・JSON出力
    collectedLogs.Add(new LogEntry {
        message = logString,
        stackTrace = stackTrace,
        type = type.ToString(),
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
    });
}
```

#### CompileStatusMonitor.cs
```csharp
// CompilationPipeline イベントでコンパイル状態を監視
static CompileStatusMonitor()
{
    CompilationPipeline.compilationStarted += OnCompilationStarted;
    CompilationPipeline.compilationFinished += OnCompilationFinished;
}

static void OnCompilationFinished(object obj)
{
    var hasErrors = HasCompilationErrors();
    var compileStatus = new CompileStatus {
        status = hasErrors ? "FAILED" : "SUCCESS",
        duration = (long)(DateTime.Now - startTime).TotalMilliseconds,
        errorCount = GetCompilationErrorCount()
    };
    ExportCompileStatus(compileStatus);
}
```

### MCP側ツール

#### get_console_logs実装
```typescript
async function handleGetConsoleLogs(params, dataPath, log) {
    const filter = params?.filter || 'all';
    const limit = params?.limit || 50;
    
    // console-logs.json読み込み
    const consoleData = JSON.parse(await fs.readFile(consoleLogsPath, 'utf8'));
    let logs = consoleData.logs || [];
    
    // フィルタリング
    switch (filter) {
        case 'errors':
            logs = logs.filter(log => log.type === 'Error' || log.type === 'Exception');
            break;
        case 'warnings':
            logs = logs.filter(log => log.type === 'Warning');
            break;
        // ...
    }
    
    return formatLogsResponse(logs, consoleData.summary);
}
```

#### wait_for_compilation実装
```typescript
async function handleWaitForCompilation(params, dataPath, log) {
    const timeout = (params?.timeout || 30) * 1000;
    const compileStatusPath = path.join(dataPath, 'compile-status.json');
    
    return new Promise((resolve, reject) => {
        const checkStatus = async () => {
            const compileData = JSON.parse(await fs.readFile(compileStatusPath, 'utf8'));
            
            if (compileData.status === "SUCCESS" || compileData.status === "FAILED") {
                // 結果をフォーマットして返却
                resolve(formatCompilationResult(compileData));
            } else {
                // 500ms後に再チェック
                setTimeout(checkStatus, 500);
            }
        };
        checkStatus();
    });
}
```

## 🎯 活用シナリオ

### 1. 高速プロトタイピング
```bash
# Claude Codeで新機能のアイデアを実装
create new script "PlayerController" with movement and jump

# 自動コンパイル→結果確認
wait for compilation

# エラーがあれば詳細確認
get console logs --filter errors

# Claude Codeが即座修正
fix compilation errors in PlayerController script
```

### 2. デバッグ・トラブルシューティング
```bash
# 実行時ログの確認
get console logs --filter all --limit 20

# 特定のエラーに焦点
get console logs --filter errors

# 警告の分析
get console logs --filter warnings
```

### 3. 学習・実験
```bash
# 新しいUnity機能の実験
create simple shader with color animation

# リアルタイム結果確認
wait for compilation

# ログで動作状況確認
get console logs --filter recent
```

## 🔧 設定・カスタマイズ

### ログ保持設定
`ConsoleLogExporter.cs`の設定:
```csharp
private static int _maxLogsToKeep = 1000;  // 最大保持ログ数
```

### タイムアウト設定
`wait_for_compilation`のデフォルトタイムアウト:
```typescript
const timeout = (params?.timeout || 30) * 1000; // 30秒
```

### 出力フォーマット
JSON出力先: `UnityMCP/Data/`
- `console-logs.json`: ログデータ
- `compile-status.json`: コンパイル状態

## 🎓 学習のポイント

### 初学者向け
1. **基本操作の習得**: `get console logs` の使い方
2. **エラー読解**: スタックトレースの見方
3. **開発フロー**: コンパイル→確認→修正の流れ

### 中級者向け
1. **フィルター活用**: 効率的なログ分析
2. **パフォーマンス把握**: コンパイル時間の最適化
3. **デバッグ戦略**: ログ情報を活用した問題解決

### 上級者向け
1. **システム拡張**: 新しいログ収集機能の追加
2. **最適化**: メモリ使用量とパフォーマンスのバランス
3. **統合開発**: より高度なAI開発フローの構築

## 🛡️ トラブルシューティング

### よくある問題

#### Q: console-logs.jsonが生成されない
```bash
# Unity Editorでスクリプト再コンパイル
Assets > Refresh (Cmd+R / Ctrl+R)

# MCP exporterの動作確認
UnityMCP > Export All Data
```

#### Q: wait_for_compilationがタイムアウトする
```bash
# タイムアウト時間を延長
wait for compilation --timeout 60

# Unity Editorの状態確認
get unity info editor
```

#### Q: ログが重複して表示される
Unity側のログ収集で重複が発生している場合があります。これは正常な動作です。

### パフォーマンス最適化
- ログ保持数の調整 (`_maxLogsToKeep`)
- 不要なスタックトレース情報の削減
- デバウンス機能による更新頻度制御

## 🌟 今後の拡張予定

### 予定機能
- **ログフィルタリング強化**: 正規表現対応
- **コンパイル警告詳細**: より詳細な警告情報
- **リアルタイム通知**: エラー発生時の即座通知
- **統計分析**: ログパターンの分析機能

### コミュニティ貢献
Unity Console統合機能の改善案やバグ報告は、GitHub Issuesでお寄せください。

## 🏆 まとめ

Unity Console統合により、AI駆動Unity開発の基盤が完成しました：

✅ **即座のフィードバック**: コンパイル結果を1-3秒で取得  
✅ **詳細なエラー情報**: ファイル・行番号の正確な位置特定  
✅ **シームレスな開発**: 手動操作の完全排除  
✅ **学習支援**: エラーパターンの理解促進  

**この統合機能を活用して、効率的なUnity開発をお楽しみください！** 🚀