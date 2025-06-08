# 高度な自己解決システム - Shader・UI Toolkit対応

## 🎯 概要

Unity MCP Learningの自己解決型AI開発サイクルを、**Shader開発**と**UI Toolkit**に拡張することで、Unity開発の全領域での完全自動化を実現します。

## 🎨 Shader自己解決システム

### 従来のShader開発の課題
```
開発者 → HLSL書く → コンパイルエラー → 手動修正 → プロパティエラー → 手動修正...
- Shader特有の構文エラー
- Platform間の互換性問題  
- Performance警告の対処
- Material設定の不一致
```

### Shader自己解決サイクル
```
開発者: "水面シェーダーを作って"
→ AI: .shader生成 → Shader Compiler監視 → エラー自動修正 → Material自動作成 → 完成
```

## 🛠️ Shader対応の技術実装

### Unity MCP側の拡張

#### Shader専用監視機能
```typescript
// src/shader-monitor.ts (新規)
export async function wait_for_shader_compilation(shaderPath: string) {
  return new Promise((resolve) => {
    const watcher = new ShaderCompilationWatcher(shaderPath);
    
    watcher.on('compilationComplete', (result) => {
      resolve({
        success: result.success,
        errors: result.errors,
        warnings: result.warnings,
        targetPlatforms: result.supportedPlatforms,
        performance: result.performanceMetrics
      });
    });
  });
}

export async function get_shader_errors(shaderPath: string) {
  const console = await getConsoleLogsFocused('shader');
  
  return {
    syntaxErrors: console.filter(log => log.category === 'ShaderCompiler'),
    platformErrors: console.filter(log => log.category === 'Platform'),
    performanceWarnings: console.filter(log => log.category === 'Performance'),
    propertyMismatches: await getShaderPropertyErrors(shaderPath)
  };
}

export async function create_material_for_shader(shaderPath: string) {
  const materialPath = shaderPath.replace('.shader', '.mat');
  const shaderProps = await analyzeShaderProperties(shaderPath);
  
  await createMaterialWithProperties(materialPath, shaderProps);
  return materialPath;
}
```

#### Shader構文解析・自動修正
```typescript
// src/shader-analyzer.ts (新規)
export class ShaderAnalyzer {
  async analyzeShaderCode(content: string): Promise<ShaderAnalysis> {
    return {
      properties: this.extractProperties(content),
      passes: this.analyzePasses(content),
      dependencies: this.findDependencies(content),
      compatibility: this.checkPlatformCompatibility(content)
    };
  }
  
  async suggestFixes(errors: ShaderError[]): Promise<ShaderFix[]> {
    return errors.map(error => {
      switch (error.type) {
        case 'SYNTAX_ERROR':
          return this.fixSyntaxError(error);
        case 'PLATFORM_INCOMPATIBLE':
          return this.fixPlatformCompatibility(error);
        case 'PERFORMANCE_WARNING':
          return this.optimizePerformance(error);
        case 'PROPERTY_MISMATCH':
          return this.fixPropertyDefinition(error);
      }
    });
  }
}
```

### Shader自己解決の実例

#### 入力例
```
"美しい水面シェーダーを作って。波のアニメーション、反射、屈折効果付き"
```

#### 自動解決プロセス
```
[Claude Code + Unity MCP自動実行]

1回目:
├─ WaterSurface.shader生成
├─ wait_for_shader_compilation
├─ get_shader_errors: "undefined identifier '_Time'"
└─ 修正: #include "UnityCG.cginc" 追加

2回目:
├─ 修正版保存
├─ wait_for_shader_compilation
├─ get_shader_errors: "invalid swizzle 'xyz' on scalar"
└─ 修正: ベクトル型の適切な宣言

3回目:
├─ 再修正版保存
├─ wait_for_shader_compilation
├─ get_shader_errors: "Performance warning: too many texture samples"
└─ 最適化: テクスチャサンプリングの効率化

4回目:
├─ 最適化版保存
├─ wait_for_shader_compilation
├─ get_shader_errors: エラーなし
├─ create_material_for_shader: WaterSurface.mat自動作成
└─ ✅ 完成！動作する水面シェーダー + Material
```

#### 出力結果
```
✅ WaterSurface.shader を正常に作成しました。

生成された機能:
- リアルタイム波アニメーション
- 環境マッピング反射
- 屈折効果（法線マップ対応）
- フレネル効果
- 深度フェード

自動修正履歴:
- 1回目: Unity標準インクルード追加
- 2回目: ベクトル型宣言修正
- 3回目: テクスチャサンプリング最適化

パフォーマンス:
- Mobile対応: ✅
- VR対応: ✅
- インストラクション数: 42 (推奨範囲内)

付属ファイル:
- WaterSurface.mat (適切なプロパティ設定済み)
- サンプルテクスチャ (法線マップ等)
```

## 🖥️ UI Toolkit自己解決システム

### UI Toolkit開発の課題
```
従来:
- UXML/USS構文エラー
- セレクターの不一致
- Flexbox レイアウト問題
- C# Binding エラー
- アニメーション設定ミス
```

### UI Toolkit自己解決サイクル
```
開発者: "RPGのインベントリUIを作って"
→ AI: UXML/USS/C#生成 → UI Inspector監視 → Layout修正 → Binding確認 → 完成
```

## 🎛️ UI Toolkit対応の技術実装

### Unity MCP側のUI専用機能

#### UI Inspector監視
```typescript
// src/ui-toolkit-monitor.ts (新規)
export async function wait_for_ui_compilation() {
  return new Promise((resolve) => {
    const uiWatcher = new UIToolkitWatcher();
    
    uiWatcher.on('uiUpdated', (result) => {
      resolve({
        uxmlValid: result.uxmlCompilation.success,
        ussValid: result.ussCompilation.success,
        bindingErrors: result.bindingValidation.errors,
        layoutWarnings: result.layoutAnalysis.warnings
      });
    });
  });
}

export async function get_ui_inspector_errors() {
  const inspector = await getUIInspectorState();
  
  return {
    uxmlErrors: inspector.uxmlErrors,
    ussErrors: inspector.cssErrors,
    layoutIssues: inspector.layoutProblems,
    bindingErrors: inspector.bindingProblems,
    performanceWarnings: inspector.performanceIssues
  };
}

export async function validate_ui_layout(uxmlPath: string) {
  const layout = await analyzeUILayout(uxmlPath);
  
  return {
    responsiveness: layout.responsiveDesign,
    accessibility: layout.accessibilityScore,
    performance: layout.renderingCost,
    recommendations: layout.improvementSuggestions
  };
}
```

#### UI Toolkit構文解析
```typescript
// src/ui-analyzer.ts (新規)
export class UIToolkitAnalyzer {
  async analyzeUXML(content: string): Promise<UXMLAnalysis> {
    return {
      structure: this.parseElementHierarchy(content),
      classes: this.extractCSSClasses(content),
      bindings: this.findDataBindings(content),
      accessibility: this.checkAccessibility(content)
    };
  }
  
  async analyzeUSS(content: string): Promise<USSAnalysis> {
    return {
      selectors: this.validateSelectors(content),
      properties: this.checkCSSProperties(content),
      flexbox: this.analyzeFlexboxLayout(content),
      animations: this.validateAnimations(content)
    };
  }
  
  async suggestUIFixes(errors: UIError[]): Promise<UIFix[]> {
    return errors.map(error => {
      switch (error.category) {
        case 'UXML_SYNTAX':
          return this.fixUXMLSyntax(error);
        case 'USS_SELECTOR':
          return this.fixCSSSelector(error);
        case 'LAYOUT_ISSUE':
          return this.fixLayoutProblem(error);
        case 'BINDING_ERROR':
          return this.fixDataBinding(error);
        case 'ACCESSIBILITY':
          return this.improveAccessibility(error);
      }
    });
  }
}
```

### UI Toolkit自己解決の実例

#### 入力例
```
"RPGゲーム用のインベントリUIを作って。グリッド表示、ドラッグ&ドロップ、アイテム詳細表示"
```

#### 自動解決プロセス
```
[Claude Code + Unity MCP自動実行]

1回目:
├─ InventoryUI.uxml生成
├─ InventoryUI.uss生成  
├─ InventoryController.cs生成
├─ wait_for_ui_compilation
├─ get_ui_inspector_errors: "Unknown class 'inventory-grid'"
└─ 修正: USS内の.inventory-gridセレクター追加

2回目:
├─ 修正版保存
├─ wait_for_ui_compilation
├─ get_ui_inspector_errors: "Flexbox layout overflow"
└─ 修正: flex-wrap: wrapとmax-width設定

3回目:
├─ 再修正版保存
├─ wait_for_ui_compilation
├─ get_ui_inspector_errors: "Binding path 'ItemData' not found"
└─ 修正: C#のINotifyPropertyChanged実装

4回目:
├─ 最終修正版保存
├─ wait_for_ui_compilation
├─ validate_ui_layout: アクセシビリティスコア向上
├─ get_ui_inspector_errors: エラーなし
└─ ✅ 完成！完全動作するインベントリUI
```

#### 出力結果
```
✅ RPGインベントリUI を正常に作成しました。

生成されたファイル:
- InventoryUI.uxml (UI構造)
- InventoryUI.uss (スタイル)
- InventoryController.cs (ロジック)
- ItemSlot.cs (アイテムスロット)
- DragDropHandler.cs (ドラッグ&ドロップ)

実装された機能:
- 6x8グリッド表示 (レスポンシブ対応)
- ドラッグ&ドロップ移動
- アイテム詳細ポップアップ
- カテゴリフィルター
- 検索機能
- キーボードナビゲーション (アクセシビリティ)

自動修正履歴:
- 1回目: CSSセレクター不一致修正
- 2回目: Flexboxレイアウト最適化  
- 3回目: データバインディング修正

品質スコア:
- レスポンシブ対応: ✅
- アクセシビリティ: 95/100
- パフォーマンス: A級 (60fps維持)
- クロスプラットフォーム: ✅
```

## 🌐 Web技術との相互運用

### CSS/HTML知識の活用
```typescript
// Web開発者向けの自動変換
export async function convertWebToUIToolkit(htmlContent: string, cssContent: string) {
  const converter = new WebToUIToolkitConverter();
  
  return {
    uxml: await converter.htmlToUXML(htmlContent),
    uss: await converter.cssToUSS(cssContent),
    warnings: converter.getCompatibilityWarnings(),
    suggestions: converter.getUIToolkitBestPractices()
  };
}
```

### モダンWeb技術の適用
```
入力: "Bootstrap風のグリッドシステムをUI Toolkitで実装"
→ 自動生成: flexbox + grid のハイブリッドシステム
→ レスポンシブブレークポイント対応
→ Unity最適化済み
```

## 🎮 ゲーム特化UI機能

### ゲームUI自動生成
```typescript
// ゲームジャンル別テンプレート
export async function generateGameUI(genre: GameGenre, features: string[]) {
  const templates = {
    RPG: () => generateRPGUI(features),
    FPS: () => generateFPSUI(features),
    RTS: () => generateRTSUI(features),
    Puzzle: () => generatePuzzleUI(features)
  };
  
  return await templates[genre]();
}
```

### 実例：ジャンル別自動生成
```
開発者: "FPSゲーム用UIを作って。ヘルスバー、ミニマップ、武器選択、チャット"

自動生成:
├─ FPSUI.uxml (HUD レイアウト)
├─ FPSUI.uss (ゲーミングスタイル)  
├─ HUDController.cs (パフォーマンス最適化済み)
├─ HealthBar.cs (スムーズアニメーション)
├─ MiniMap.cs (リアルタイム更新)
├─ WeaponWheel.cs (コントローラー対応)
└─ ChatSystem.cs (多言語対応)
```

## 🚀 統合開発サイクル

### C# + Shader + UI の完全自動化
```
開発者: "魔法システムを作って。エフェクトシェーダー、詠唱UI、ダメージ表示"

統合自動生成:
1. MagicSystem.cs (C#ロジック)
2. MagicEffect.shader (エフェクト描画)  
3. SpellCastUI.uxml/uss (詠唱インターフェース)
4. DamageNumber.shader (ダメージ表示)
5. 全コンポーネントの連携テスト
6. パフォーマンス最適化
7. ✅ 完全動作する魔法システム
```

## 📊 開発効率の革命

### 従来 vs 自己解決型

#### UI開発の比較
```
従来のUI Toolkit開発:
- UXML作成: 30分
- USS調整: 45分  
- C# Binding: 60分
- エラー修正: 90分
- テスト・調整: 60分
合計: 4時間45分

自己解決型UI開発:
- 要求入力: 1分
- AI自動処理: 5分
- 確認・微調整: 4分  
合計: 10分
```

**開発速度: 28.5倍向上**

#### Shader開発の比較
```
従来のShader開発:
- HLSL記述: 60分
- エラー修正: 120分
- 最適化: 90分
- Material作成: 30分
合計: 5時間

自己解決型Shader開発:
- 要求入力: 1分
- AI自動処理: 8分
- 確認・調整: 6分
合計: 15分
```

**開発速度: 20倍向上**

## 🎯 応用可能性

### 教育分野
```
学生: "シェーダーの仕組みを学びたい"
→ 段階的シェーダー生成 + 詳細解説
→ インタラクティブな学習体験
```

### プロトタイピング
```
ゲームデザイナー: "この世界観のUIを試してみたい"
→ 即座にビジュアルプロトタイプ生成
→ アイデアの迅速な検証
```

### クロスプラットフォーム開発
```
"モバイル・PC・VR対応のUIシステム"
→ 各プラットフォーム最適化版を自動生成
→ 統一されたユーザー体験
```

---

**作成日**: 2025年6月8日  
**技術範囲**: C# + Shader + UI Toolkit  
**効果**: Unity開発の全領域での自動化実現

この拡張により、Unity MCP Learningは**完全なUnity開発支援AI**となり、開発者は純粋に**創造性とゲームデザイン**に集中できる革新的な環境が実現されます。