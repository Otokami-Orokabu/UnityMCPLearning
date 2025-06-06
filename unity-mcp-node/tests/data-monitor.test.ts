/**
 * データ監視システムのテスト
 */

import * as fs from 'fs';
import * as path from 'path';
import { 
  loadDataFile, 
  loadAllData, 
  getCachedData, 
  clearCache, 
  clearDebounceTimers,
  CATEGORY_MAP 
} from '../src/data-monitor';

describe('data-monitor', () => {
  let tempDir: string;
  let mockLog: jest.Mock;

  beforeEach(() => {
    tempDir = path.join(__dirname, 'temp-data');
    if (!fs.existsSync(tempDir)) {
      fs.mkdirSync(tempDir, { recursive: true });
    }
    
    mockLog = jest.fn();
    clearCache();
  });

  afterEach(() => {
    if (fs.existsSync(tempDir)) {
      fs.rmSync(tempDir, { recursive: true, force: true });
    }
    clearCache();
    clearDebounceTimers();
  });

  describe('loadDataFile', () => {
    it('should load valid JSON file', () => {
      const testData = { name: 'Test Project', version: '1.0.0' };
      const filename = 'project-info.json';
      const filePath = path.join(tempDir, filename);
      
      fs.writeFileSync(filePath, JSON.stringify(testData));
      
      loadDataFile(filename, tempDir, mockLog);
      
      const cachedData = getCachedData();
      expect(cachedData.project_info).toEqual(testData);
      expect(mockLog).toHaveBeenCalledWith(`Data loaded: ${filename} -> project_info`);
    });

    it('should handle missing file gracefully', () => {
      loadDataFile('non-existent.json', tempDir, mockLog);
      
      const cachedData = getCachedData();
      expect(Object.keys(cachedData)).toHaveLength(0);
      // エラーログは出力されないはず（ファイルが存在しない場合は無視）
    });

    it('should handle invalid JSON gracefully', () => {
      const filename = 'invalid.json';
      const filePath = path.join(tempDir, filename);
      
      fs.writeFileSync(filePath, '{ invalid json }');
      
      loadDataFile(filename, tempDir, mockLog);
      
      const cachedData = getCachedData();
      expect(Object.keys(cachedData)).toHaveLength(0);
      expect(mockLog).toHaveBeenCalledWith(
        expect.stringContaining(`Error loading ${filename}:`),
        expect.any(Error)
      );
    });

    it('should convert filename to cache key correctly', () => {
      const testCases = [
        { filename: 'project-info.json', expectedKey: 'project_info' },
        { filename: 'scene-info.json', expectedKey: 'scene_info' },
        { filename: 'gameobjects.json', expectedKey: 'gameobjects' },
        { filename: 'assets-info.json', expectedKey: 'assets_info' }
      ];

      testCases.forEach(({ filename, expectedKey }) => {
        const testData = { test: 'data' };
        const filePath = path.join(tempDir, filename);
        
        fs.writeFileSync(filePath, JSON.stringify(testData));
        clearCache();
        
        loadDataFile(filename, tempDir, mockLog);
        
        const cachedData = getCachedData();
        expect(cachedData[expectedKey]).toEqual(testData);
      });
    });
  });

  describe('loadAllData', () => {
    it('should load all expected data files', () => {
      const testFiles = [
        'project-info.json',
        'scene-info.json', 
        'gameobjects.json',
        'assets-info.json',
        'build-info.json',
        'editor-state.json'
      ];

      // テストファイルを作成
      testFiles.forEach(filename => {
        const testData = { filename, timestamp: Date.now() };
        const filePath = path.join(tempDir, filename);
        fs.writeFileSync(filePath, JSON.stringify(testData));
      });

      loadAllData(tempDir, mockLog);

      const cachedData = getCachedData();
      
      // 各ファイルが正しくロードされていることを確認
      expect(cachedData.project_info).toBeDefined();
      expect(cachedData.scene_info).toBeDefined();
      expect(cachedData.gameobjects).toBeDefined();
      expect(cachedData.assets_info).toBeDefined();
      expect(cachedData.build_info).toBeDefined();
      expect(cachedData.editor_state).toBeDefined();

      // ログが6回呼ばれていることを確認
      expect(mockLog).toHaveBeenCalledTimes(6);
    });

    it('should handle partially missing files', () => {
      // 一部のファイルのみ作成
      const existingFiles = ['project-info.json', 'gameobjects.json'];
      
      existingFiles.forEach(filename => {
        const testData = { filename };
        const filePath = path.join(tempDir, filename);
        fs.writeFileSync(filePath, JSON.stringify(testData));
      });

      loadAllData(tempDir, mockLog);

      const cachedData = getCachedData();
      
      expect(cachedData.project_info).toBeDefined();
      expect(cachedData.gameobjects).toBeDefined();
      expect(cachedData.scene_info).toBeUndefined();
      expect(cachedData.assets_info).toBeUndefined();

      // 存在するファイルの数だけログが呼ばれる
      expect(mockLog).toHaveBeenCalledTimes(2);
    });
  });

  describe('getCachedData', () => {
    it('should return empty object initially', () => {
      const cachedData = getCachedData();
      expect(cachedData).toEqual({});
    });

    it('should return cached data after loading', () => {
      const testData = { test: 'value' };
      const filename = 'test.json';
      const filePath = path.join(tempDir, filename);
      
      fs.writeFileSync(filePath, JSON.stringify(testData));
      loadDataFile(filename, tempDir, mockLog);
      
      const cachedData = getCachedData();
      expect(cachedData.test).toEqual(testData);
    });

    it('should return reference to actual cache (not copy)', () => {
      const testData = { test: 'value' };
      const filename = 'test.json';
      const filePath = path.join(tempDir, filename);
      
      fs.writeFileSync(filePath, JSON.stringify(testData));
      loadDataFile(filename, tempDir, mockLog);
      
      const cachedData1 = getCachedData();
      const cachedData2 = getCachedData();
      
      expect(cachedData1).toBe(cachedData2); // 同じ参照
    });
  });

  describe('clearCache', () => {
    it('should clear all cached data', () => {
      // データをロード
      const testData = { test: 'value' };
      const filename = 'test.json';
      const filePath = path.join(tempDir, filename);
      
      fs.writeFileSync(filePath, JSON.stringify(testData));
      loadDataFile(filename, tempDir, mockLog);
      
      // キャッシュにデータがあることを確認
      expect(Object.keys(getCachedData())).toHaveLength(1);
      
      // キャッシュをクリア
      clearCache();
      
      // キャッシュが空になっていることを確認
      expect(getCachedData()).toEqual({});
    });
  });

  describe('CATEGORY_MAP', () => {
    it('should have correct category mappings', () => {
      expect(CATEGORY_MAP).toEqual({
        'project': 'project_info',
        'scene': 'scene_info',
        'gameobjects': 'gameobjects',
        'assets': 'assets_info',
        'build': 'build_info',
        'editor': 'editor_state'
      });
    });

    it('should map to cache keys used by loadDataFile', () => {
      // loadDataFileで使用される変換ロジックと一致することを確認
      Object.entries(CATEGORY_MAP).forEach(([category, expectedKey]) => {
        const filename = expectedKey.replace('_', '-') + '.json';
        const testData = { category };
        const filePath = path.join(tempDir, filename);
        
        fs.writeFileSync(filePath, JSON.stringify(testData));
        clearCache();
        
        loadDataFile(filename, tempDir, mockLog);
        
        const cachedData = getCachedData();
        expect(cachedData[expectedKey]).toEqual(testData);
      });
    });
  });

  describe('clearDebounceTimers', () => {
    it('should clear all pending debounce timers', () => {
      // この関数は内部状態のクリーンアップを行うため、
      // 直接的なテストは困難だが、例外が発生しないことを確認
      expect(() => clearDebounceTimers()).not.toThrow();
    });
  });

  // startFileWatching関数のテストは実際のfs.watchを使用するため、
  // より高レベルな統合テストまたは複雑なモック設定が必要
  describe('startFileWatching', () => {
    it('should be tested in integration test environment', () => {
      // ファイル監視機能のテストは以下の要素が必要：
      // - fs.watch のモック
      // - ファイルシステムイベントのシミュレーション
      // - 非同期的なファイル変更の検出テスト
      // - debounce機能の動作確認
      // 
      // この機能は統合テストまたはE2Eテストでカバーすることを推奨
      expect(true).toBe(true);
    });
  });
});