# Security Policy - Unity MCP Learning

## 🛡️ セキュリティ概要

Unity MCP Learningは、ローカル開発環境での使用を前提としたオープンソースプロジェクトです。本ドキュメントでは、プロジェクトのセキュリティ側面と安全な使用方法について説明します。

## 🔍 セキュリティ分析

### アーキテクチャの安全性

#### **✅ ローカル実行限定**
- **MCPサーバー**: `localhost`のみでリッスン
- **Unity連携**: ローカルファイルシステム経由
- **外部通信**: Claude Desktopとの`stdio`通信のみ
- **ネットワーク露出**: なし（外部からアクセス不可）

#### **✅ 権限分離**
- **Unity Editor**: Unity Editorの権限内でのみ動作
- **MCPサーバー**: Node.js userプロセス権限
- **ファイルアクセス**: プロジェクトディレクトリ内に限定
- **システム変更**: 最小限（Claude Desktop設定のみ）

## 🚨 潜在的リスク分析

### 1. ファイルシステムアクセス

#### **リスク**
- UnityプロジェクトファイルのJSON出力
- MCPサーバーによるファイル監視

#### **軽減策**
```csharp
// 出力先をAssets外に限定
private static string GetOutputPath()
{
    string projectRoot = Application.dataPath.Replace("/Assets", "");
    return Path.Combine(projectRoot, "UnityMCP", "Data");
}

// パストラバーサル攻撃防止
private static bool IsPathSafe(string path)
{
    string fullPath = Path.GetFullPath(path);
    string allowedRoot = Path.GetFullPath(GetAllowedRoot());
    return fullPath.StartsWith(allowedRoot);
}
```

### 2. Claude Desktop設定変更

#### **リスク**
- Claude Desktop設定ファイルの変更
- 既存設定の上書き

#### **軽減策**
```csharp
// 自動バックアップ作成
private static void CreateConfigBackup(string configPath)
{
    string backupPath = configPath + ".backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
    File.Copy(configPath, backupPath, false);
}

// 設定変更前の確認
public static bool UpdateClaudeConfig(string mcpServerPath, bool confirmOverwrite = true)
{
    if (confirmOverwrite && File.Exists(configPath))
    {
        return EditorUtility.DisplayDialog(
            "Claude Desktop設定更新", 
            "既存の設定を変更します。バックアップを作成してから続行しますか？", 
            "続行", "キャンセル"
        );
    }
    // 実際の更新処理...
}
```

### 3. プロセス実行

#### **リスク**
- Node.jsプロセスの起動
- 外部コマンド実行の可能性

#### **軽減策**
```csharp
// 実行ファイルの検証
private static bool ValidateNodePath(string nodePath)
{
    if (!File.Exists(nodePath)) return false;
    
    FileInfo fileInfo = new FileInfo(nodePath);
    return fileInfo.Name.Equals("node") || fileInfo.Name.Equals("node.exe");
}

// 引数のサニタイズ
private static string SanitizeArgument(string arg)
{
    // 危険な文字の除去・エスケープ
    return arg.Replace("\"", "\\\"").Replace(";", "").Replace("&", "");
}
```

## 🔒 セキュリティベストプラクティス

### 開発者向け

#### **1. コードレビュー**
- すべてのPull Requestに対するセキュリティレビュー
- 外部入力のサニタイゼーション確認
- ファイルパス操作の安全性検証

#### **2. 依存関係管理**
```bash
# npm audit を定期実行
npm audit

# 脆弱性の自動修正
npm audit fix

# 依存関係の最小化
npm ls --depth=0
```

#### **3. 静的解析**
```yaml
# .github/workflows/security.yml
name: Security Analysis
on: [push, pull_request]
jobs:
  security:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Run security audit
        run: npm audit
      - name: CodeQL Analysis
        uses: github/codeql-action/analyze@v2
```

### 利用者向け

#### **1. 安全な利用環境**
- ✅ **信頼できる環境**: 個人・チーム開発環境での使用
- ✅ **最新バージョン**: 定期的なアップデート
- ✅ **バックアップ**: 重要なプロジェクトファイルのバックアップ
- ❌ **本番環境**: 本番サーバーでの使用は推奨しない

#### **2. 設定の確認**
```bash
# Claude Desktop設定の確認
cat ~/Library/Application\ Support/Claude/claude_desktop_config.json

# MCPサーバーのプロセス確認
ps aux | grep node

# ファイル権限の確認
ls -la UnityMCP/Data/
```

## 🚫 利用上の注意事項

### 禁止事項
- ❌ **機密プロジェクト**: 機密情報を含むプロジェクトでの使用
- ❌ **本番環境**: 商用・本番環境での使用
- ❌ **ネットワーク公開**: MCPサーバーの外部公開
- ❌ **設定共有**: Claude Desktop設定の他者との共有

### 推奨事項
- ✅ **学習目的**: プログラミング学習・プロトタイプ開発
- ✅ **個人プロジェクト**: 個人的な開発実験
- ✅ **チーム開発**: 信頼できるチーム内での協業
- ✅ **定期更新**: セキュリティアップデートの適用

## 🔍 セキュリティ監査

### 自動チェック項目

#### **1. ファイルアクセス監査**
```csharp
public static class SecurityAuditor
{
    public static void AuditFileAccess()
    {
        string[] sensitiveFiles = {
            "*.key", "*.pem", "*.p12", 
            "*.password", "*.secret", 
            ".env", "config.json"
        };
        
        foreach (string pattern in sensitiveFiles)
        {
            if (Directory.GetFiles(GetDataPath(), pattern).Length > 0)
            {
                MCPLogger.LogWarning($"機密ファイル検出: {pattern}");
            }
        }
    }
}
```

#### **2. 通信監査**
```typescript
// MCPサーバー側での通信ログ
function auditCommunication(request: any) {
    const sensitiveFields = ['password', 'token', 'key', 'secret'];
    
    for (const field of sensitiveFields) {
        if (JSON.stringify(request).toLowerCase().includes(field)) {
            console.warn(`Potentially sensitive data in request: ${field}`);
        }
    }
}
```

## 📋 脆弱性報告

### 報告方法
セキュリティ脆弱性を発見した場合は、以下の方法で報告してください：

1. **GitHub Security Advisory**: プライベート報告
2. **Issue作成**: 機密でない問題の場合
3. **メール連絡**: 重大な脆弱性の場合

### 報告内容
- 脆弱性の詳細な説明
- 再現手順
- 影響範囲
- 可能な場合は修正案

## 🛠️ セキュリティ更新

### 更新ポリシー
- **Critical**: 24時間以内
- **High**: 1週間以内
- **Medium**: 1ヶ月以内
- **Low**: 次回リリース時

### 通知方法
- GitHub Releases
- Security Advisory
- README.mdでの告知

## 📊 リスク評価マトリックス

| リスク項目 | 発生確率 | 影響度 | リスクレベル | 軽減策 |
|------------|----------|--------|--------------|--------|
| ファイル漏洩 | Low | Medium | **Low** | パス制限・監査 |
| 設定破損 | Medium | Low | **Low** | バックアップ・復元 |
| プロセス乗っ取り | Very Low | High | **Low** | 実行検証・権限制限 |
| 依存関係脆弱性 | Medium | Medium | **Medium** | 定期監査・更新 |

## 🏆 セキュリティ認定

### 準拠基準
- **OWASP Top 10**: Web Application Security
- **CWE**: Common Weakness Enumeration
- **NIST**: Cybersecurity Framework

### 継続的改善
- 定期的なセキュリティレビュー
- 最新の脅威情報への対応
- コミュニティからのフィードバック反映

---

## 📞 連絡先

セキュリティに関する質問・報告：
- **GitHub Issues**: 一般的なセキュリティ質問
- **Security Advisory**: 脆弱性報告
- **メール**: 重大なセキュリティ問題

**最終更新**: 2025年6月5日  
**バージョン**: 1.0.0  
**レビュー予定**: 3ヶ月後