# Git URLインストール時のトラブル解決ガイド 🚀

**Unity初心者向け** | **最終更新**: 2025年6月9日

## 📖 このガイドについて

Unity Package ManagerでGit URLを使ってUnity MCP Learningをインストールした時に起こりがちな問題と、その解決方法を分かりやすく説明します。

**対象者**: 
- Unityを始めて間もない方
- Git URLインストールが初めての方
- エラーメッセージの意味が分からない方

## 🎯 Git URLって何？

Git URLとは、GitHubなどに置かれているUnityパッケージを、URLを指定するだけで簡単にインストールできる機能です。

**従来の方法**: `.unitypackage`ファイルをダウンロード → インポート  
**Git URL方法**: URLをコピペするだけ ✨

**Unity MCP LearningのGit URL**:
```
https://github.com/Otokami-Orokabu/UnityMCPLearning.git?path=MCPLearning/Assets/Packages/unity-mcp-learning#v0.1.12
```

## 🔧 インストール手順（おさらい）

1. **Unity Package Managerを開く**
   - メニューバー → `Window` → `Package Manager`

2. **Git URLを追加**
   - Package Manager左上の `+` ボタン
   - `Add package from git URL...` を選択

3. **URLを入力**
   - 上記のGit URLをコピペ
   - `Add` ボタンをクリック

4. **インストール完了を待つ**
   - Progress Barが100%になるまで待機

## 🚨 よくあるエラーと解決方法

### エラー1: 「MCPサーバーが起動しない」

#### どんな症状？
- Unity Consoleに赤いエラーメッセージが出る
- 「exit code 1」というメッセージが表示される
- MCPサーバーマネージャーで「Server Status: Stopped」のまま

#### 初心者向け解決方法

**ステップ1: バージョンを確認**
```
Window > Package Manager > In Project > Unity MCP Learning
```
- バージョンが `0.1.12` 以降であることを確認
- 古い場合は以下の手順で更新

**ステップ2: パッケージを更新**
1. Package Managerで `Unity MCP Learning` を選択
2. 右下の `Remove` ボタンをクリック
3. もう一度Git URLでインストール（上記手順）

**ステップ3: Unityを再起動**
- 一度Unityエディターを完全に閉じる
- プロジェクトを開き直す

### エラー2: 「画面のデザインがおかしい」

#### どんな症状？
- MCPサーバーマネージャーの見た目が簡素
- ボタンが縦に並んでいるだけの画面
- Console に「UXML not found」という警告

#### これは何？
UIファイル（画面のデザイン設定）が見つからない状態です。機能的には問題ありませんが、見た目が簡易版になります。

#### 解決方法
**バージョン0.1.11以降を使用**してください（上記の更新手順と同じ）

### エラー3: 「フォルダが見つかりません」

#### どんな症状？
Console に以下のようなメッセージ:
```
AssetDatabase.FindAssets: Folder not found: 'Assets/Packages/unity-mcp-learning'
```

#### これは何？
パッケージが「想定されていた場所と違う場所」にインストールされているため、システムがファイルを見つけられない状態です。

#### 解決方法
**バージョン0.1.12以降を使用**してください（上記の更新手順と同じ）

### エラー4: 「バージョンが更新されない」

#### どんな症状？
- 新しいバージョンをインストールしたはずなのに古いまま
- Package Managerで古いバージョン番号が表示される

#### なぜ起こる？
Unityが「キャッシュ」という一時保存ファイルを使いまわしているためです。

#### 解決方法

**方法1: Package Managerでリセット**
1. `Window > Package Manager`
2. 左上のドロップダウンを `In Project` に
3. `Unity MCP Learning` を見つけて選択
4. 右下の `Remove` ボタン
5. 再度Git URLでインストール

**方法2: キャッシュクリア（上級者向け）**
1. Unityエディターを**完全に閉じる**
2. プロジェクトフォルダの `Library/PackageCache/` を開く
3. `com.orlab.unity-mcp-learning` で始まるフォルダを削除
4. Unityを開き直してGit URLで再インストール

## 🎮 正常に動作している状態

### Unity Console（コンソール）
- **エラー（赤）**: 0件
- **警告（黄）**: 0-2件程度（「dynamic search」警告は正常）
- **情報（白）**: MCPサーバー起動ログ

### MCPサーバーマネージャー画面
- **Server Status**: Running (緑)
- **Port**: 3000番（または他の番号）
- **Connection**: Connected
- **きれいなUI**: ボタンやラベルが整理された見た目

### Claude Desktop連携
- Claudeで Unity関連の質問をすると、プロジェクト情報が返ってくる
- ゲームオブジェクトの作成・削除ができる

## 🆘 それでも解決しない場合

### 情報収集
以下の情報をメモしてから質問してください：

1. **Unityバージョン**: `Unity > About Unity` で確認
2. **OS**: Windows 10/11、macOS、Linux
3. **パッケージバージョン**: Package Managerで確認
4. **エラーメッセージ**: Consoleの赤い文字をコピー

### 質問場所
- [GitHub Issues](https://github.com/Otokami-Orokabu/UnityMCPLearning/issues)に投稿
- 上記の情報を含めて質問

## 💡 予防策

### 1. 最新バージョンを使う
常にバージョン `0.1.12` 以降を使用してください。古いバージョンには多くの既知の問題があります。

### 2. Unityを最新に保つ
Unity 6000.0以降（Unity 6以降）の使用を推奨します。

### 3. 定期的なアップデート
月に1回程度、パッケージを最新版に更新することをお勧めします。

## 🔍 用語解説

**Package Manager**: Unityでパッケージ（追加機能）を管理するツール  
**Git URL**: GitHubなどからパッケージを直接インストールするためのURL  
**Console**: Unityのログメッセージが表示される画面  
**PackageCache**: Unityが一時的にパッケージファイルを保存する場所  
**MCP**: Model Context Protocol（AIとの通信規格）  
**Server**: MCPサーバー（Claudeとの橋渡しをするプログラム）

## 📚 関連ガイド

初心者の方は以下の順番で読むことをお勧めします：

1. [はじめてのUnity MCP Learning](00-getting-started.md) - 基本概念
2. [環境セットアップ](01-environment-setup.md) - インストール手順
3. **このガイド** - トラブル解決
4. [基本的な使い方](02-step1-basic-communication.md) - 実際の操作方法

---

**🎯 重要**: 困った時は一人で悩まず、GitHub Issuesで質問してください！  
**💪 応援**: Unity開発を楽しんでください！