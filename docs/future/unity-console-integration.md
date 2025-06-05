# Unity Console統合計画 - Step 4 優先実装

## 🎯 背景・動機

### **現在の開発課題**
- **コード生成後の確認時間不足**: Claude CodeがC#コード生成→保存後、Unityでの自動コンパイル結果を手動確認する必要
- **エラーコピペの手間**: Unity Consoleのエラーを手動でコピー→Claude Codeに貼り付け
- **非効率なデバッグサイクル**: フィードバックループが長く、開発体験が悪化

### **解決したい理想的フロー**
```bash
Claude Code → C#コード生成・保存 → Unity自動コンパイル
     ↓
自動でコンパイル結果監視 → エラー/成功を即座返却
     ↓  
エラー時: Unity Console出力をそのまま表示 → Claude Codeが即座修正
成功時: 確認メッセージ表示 → 次のタスクへ
```

## 💡 Unity Console出力の豊富さ活用

### **Unity標準出力に含まれる情報**
```
Assets/UnityMCP/Editor/Common/MCPCommand.cs(23,5): error CS0246: The type or namespace name 'InvalidType' could not be found (are you missing a using directive or an assembly reference?)

Compilation failed: 1 error(s), 0 warnings
```

**既に完璧な情報が揃っている:**
- ✅ **完全ファイルパス**: `Assets/UnityMCP/Editor/Common/MCPCommand.cs`
- ✅ **行・列番号**: `(23,5)` - 正確な位置特定
- ✅ **エラーコード**: `CS0246` - 問題分類
- ✅ **詳細メッセージ**: 具体的な問題と解決ヒント
- ✅ **集計情報**: エラー数・警告数

**→ 新たな解析は不要、Unity出力をそのまま活用**

## 🛠️ 実装アプローチ

### **Unity側実装**

#### **1. ConsoleLogExporter.cs**
```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ConsoleLogExporter : IDataExporter
{
    private static List<LogEntry> collectedLogs = new List<LogEntry>();
    
    static ConsoleLogExporter()
    {
        // Unity Console出力をリアルタイム収集
        Application.logMessageReceived += OnLogMessageReceived;
    }
    
    private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
    {
        collectedLogs.Add(new LogEntry
        {
            message = logString,
            stackTrace = stackTrace,
            type = type,
            timestamp = System.DateTime.Now
        });
        
        // 最新N件のみ保持（メモリ効率）
        if (collectedLogs.Count > 1000)
        {
            collectedLogs.RemoveAt(0);
        }
        
        // リアルタイムJSON出力
        ExportToFile();
    }
    
    public void ExportData()
    {
        var consoleData = new
        {
            logs = collectedLogs,
            errorCount = collectedLogs.Count(l => l.type == LogType.Error || l.type == LogType.Exception),
            warningCount = collectedLogs.Count(l => l.type == LogType.Warning),
            lastUpdate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
        };
        
        // JSON出力 (他のExporterと同様)
        string json = JsonUtility.ToJson(consoleData, true);
        File.WriteAllText(GetOutputPath(), json);
    }
}

[System.Serializable]
public class LogEntry
{
    public string message;
    public string stackTrace;
    public LogType type;
    public string timestamp;
}
```

#### **2. CompileStatusMonitor.cs**
```csharp
using UnityEditor;
using UnityEditor.Compilation;

[InitializeOnLoad]
public class CompileStatusMonitor
{
    static CompileStatusMonitor()
    {
        // コンパイル完了をリアルタイム監視
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
    }
    
    static void OnCompilationStarted(object obj)
    {
        var compileStatus = new
        {
            status = "COMPILING",
            startTime = System.DateTime.Now,
            message = "Unity compilation started..."
        };
        
        ExportCompileStatus(compileStatus);
    }
    
    static void OnCompilationFinished(object obj)
    {
        // エラー・警告取得
        var messages = CompilationPipeline.GetLogEntries();
        
        var compileStatus = new
        {
            status = messages.Any(m => m.type == LogType.Error) ? "FAILED" : "SUCCESS",
            endTime = System.DateTime.Now,
            errorCount = messages.Count(m => m.type == LogType.Error),
            warningCount = messages.Count(m => m.type == LogType.Warning),
            messages = messages.Select(m => new {
                file = m.file,
                line = m.line,
                column = m.column,
                message = m.message,
                type = m.type.ToString()
            })
        };
        
        ExportCompileStatus(compileStatus);
    }
    
    static void ExportCompileStatus(object status)
    {
        string json = JsonUtility.ToJson(status, true);
        string path = Path.Combine(Application.dataPath, "../UnityMCP/Data/compile-status.json");
        File.WriteAllText(path, json);
    }
}
```

### **MCP側実装**

#### **1. get_console_logs ツール**
```typescript
{
    name: "get_console_logs",
    description: "Unity Editor Console ログを取得（エラー・警告・情報）",
    inputSchema: {
        type: "object",
        properties: {
            filter: {
                type: "string",
                enum: ["all", "errors", "warnings", "recent"],
                description: "取得するログの種別"
            },
            limit: {
                type: "number",
                description: "取得する最大件数（デフォルト: 50）"
            }
        }
    }
}

async function getConsoleLogs(args: any): Promise<any> {
    try {
        const consoleDataPath = path.join(config.unityDataPath, 'console-logs.json');
        const consoleData = JSON.parse(await fs.readFile(consoleDataPath, 'utf8'));
        
        let filteredLogs = consoleData.logs;
        
        // フィルタリング
        switch (args.filter) {
            case "errors":
                filteredLogs = filteredLogs.filter(log => 
                    log.type === "Error" || log.type === "Exception");
                break;
            case "warnings":
                filteredLogs = filteredLogs.filter(log => log.type === "Warning");
                break;
            case "recent":
                filteredLogs = filteredLogs.slice(-10); // 直近10件
                break;
        }
        
        // 件数制限
        const limit = args.limit || 50;
        if (filteredLogs.length > limit) {
            filteredLogs = filteredLogs.slice(-limit);
        }
        
        return {
            logs: filteredLogs,
            summary: {
                totalErrors: consoleData.errorCount,
                totalWarnings: consoleData.warningCount,
                lastUpdate: consoleData.lastUpdate,
                filteredCount: filteredLogs.length
            },
            rawUnityOutput: filteredLogs.map(log => log.message).join('\n')
        };
        
    } catch (error) {
        throw new Error(`Console logs取得エラー: ${error.message}`);
    }
}
```

#### **2. wait_for_compilation ツール**
```typescript
{
    name: "wait_for_compilation",
    description: "Unity コンパイル完了まで待機し、結果を返却",
    inputSchema: {
        type: "object",
        properties: {
            timeout: {
                type: "number",
                description: "タイムアウト秒数（デフォルト: 30）"
            }
        }
    }
}

async function waitForCompilation(args: any): Promise<any> {
    const timeout = (args.timeout || 30) * 1000; // ミリ秒変換
    const startTime = Date.now();
    const compileStatusPath = path.join(config.unityDataPath, 'compile-status.json');
    
    return new Promise((resolve, reject) => {
        const checkCompileStatus = async () => {
            try {
                if (Date.now() - startTime > timeout) {
                    reject(new Error(`Compilation timeout after ${timeout/1000}s`));
                    return;
                }
                
                if (await fs.access(compileStatusPath).then(() => true).catch(() => false)) {
                    const status = JSON.parse(await fs.readFile(compileStatusPath, 'utf8'));
                    
                    if (status.status === "SUCCESS" || status.status === "FAILED") {
                        // ステータスファイル削除（次回のため）
                        await fs.unlink(compileStatusPath).catch(() => {});
                        
                        resolve({
                            status: status.status,
                            duration: Date.now() - startTime,
                            errorCount: status.errorCount || 0,
                            warningCount: status.warningCount || 0,
                            messages: status.messages || [],
                            summary: status.status === "SUCCESS" 
                                ? `✅ Compilation successful! (${((Date.now() - startTime) / 1000).toFixed(1)}s)`
                                : `❌ Compilation failed (${((Date.now() - startTime) / 1000).toFixed(1)}s)\n${status.messages?.map(m => `${m.file}(${m.line},${m.column}): ${m.type} ${m.message}`).join('\n')}`
                        });
                        return;
                    }
                }
                
                // 500ms後に再チェック
                setTimeout(checkCompileStatus, 500);
                
            } catch (error) {
                reject(error);
            }
        };
        
        checkCompileStatus();
    });
}
```

## 🚀 統合ワークフローの実現

### **Claude Code使用例**

#### **1. 基本的なログ取得**
```bash
# 全ログ取得
get console logs

# エラーのみ
get console logs --filter errors

# 直近のログのみ
get console logs --filter recent --limit 10
```

#### **2. コード編集 + 自動コンパイル確認**
```bash
# 1. Claude Code がコード生成・保存
Edit MCPCommand.cs...

# 2. 自動でコンパイル結果待機
wait for compilation

# 3a. 成功の場合
✅ Compilation successful! (2.3s)

# 3b. 失敗の場合
❌ Compilation failed (1.8s)
Assets/UnityMCP/Editor/Common/MCPCommand.cs(23,5): error CS0246: The type or namespace name 'InvalidType' could not be found

# 4. Claude Code が即座に修正
Edit MCPCommand.cs line 23...
```

#### **3. 開発フロー統合**
```typescript
// MCP側で統合コマンド実装
async function editAndCompile(filePath: string, changes: string): Promise<any> {
    // 1. ファイル編集
    await editFile(filePath, changes);
    
    // 2. コンパイル結果待機
    const result = await waitForCompilation({ timeout: 30 });
    
    // 3. 結果返却
    return result;
}
```

## 📋 実装ステップ

### **Phase 1: 基本Console取得（1-2日）**
1. ✅ ConsoleLogExporter.cs 実装
2. ✅ get_console_logs MCP Tool 実装
3. ✅ 基本的なエラー・警告・ログ取得

### **Phase 2: コンパイル監視（2-3日）**
1. ✅ CompileStatusMonitor.cs 実装
2. ✅ wait_for_compilation MCP Tool 実装
3. ✅ リアルタイムコンパイル結果取得

### **Phase 3: ワークフロー統合（1-2日）**
1. ✅ edit_and_compile 統合コマンド
2. ✅ Claude Code用の便利機能
3. ✅ エラーハンドリング強化

### **Phase 4: 最適化・拡張（必要に応じて）**
1. ✅ パフォーマンス最適化
2. ✅ ログフィルタリング強化
3. ✅ Stack trace解析機能

## 🎯 期待効果

### **開発効率向上**
- ⚡ **即座のフィードバック**: コード生成→結果確認が1ステップ
- 🔄 **自動化**: 手動コピペ・確認作業の完全排除
- 🎯 **精密なデバッグ**: ファイル・行番号の正確な特定

### **Claude Code体験向上**
- 🤖 **AI開発フロー**: 人間の介入なしに編集→確認→修正
- 📊 **リアルタイム状況把握**: Unity状態の完全可視化
- 🛡️ **エラー予防**: 即座のフィードバックによる問題早期発見

### **実用性**
- 📈 **開発速度**: 2-3倍の効率化期待
- 🎓 **学習効果**: エラーパターンの理解促進
- 🔧 **実用ツール**: 日常開発での継続利用

## 💭 技術考慮事項

### **パフォーマンス**
- **メモリ管理**: ログ蓄積によるメモリ使用量増加対策
- **ファイルI/O**: 高頻度JSON出力の最適化
- **リアルタイム性**: 500ms間隔チェックの妥当性

### **信頼性**
- **タイムアウト処理**: 30秒でのタイムアウト設定
- **エラーハンドリング**: ファイル読み込み失敗・JSON解析エラー対応
- **状態管理**: コンパイル状態の正確な追跡

### **拡張性**
- **フィルタリング**: より詳細な条件指定
- **分析機能**: エラーパターン分析・統計情報
- **他IDE連携**: VSCode・Rider等との統合可能性

---

**優先度: 🔥 最高**  
**実装期間: 1週間程度**  
**効果: 開発効率2-3倍向上期待**

Unity Console統合により、Claude CodeとUnityの開発体験が革命的に向上し、真の意味でのAI駆動Unity開発が実現される。