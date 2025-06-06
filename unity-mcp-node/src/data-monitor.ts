/**
 * Unity Data Monitoring System
 * Unityプロジェクトデータの監視と読み込みを管理
 */

import * as fs from 'fs';
import * as path from 'path';

// データキャッシュ
let cachedData: { [key: string]: any } = {};

// Debounce用のタイマー管理
const debounceTimers: { [filename: string]: NodeJS.Timeout } = {};
const DEBOUNCE_DELAY = 300; // 300ms

// データファイルの読み込み
export function loadDataFile(filename: string, dataPath: string, log: (...args: any[]) => void): void {
  try {
    const fullPath = path.join(dataPath, filename);
    if (fs.existsSync(fullPath)) {
      const data = JSON.parse(fs.readFileSync(fullPath, 'utf-8'));
      const key = filename.replace('.json', '').replace('-', '_');
      cachedData[key] = data;
      log(`Data loaded: ${filename} -> ${key}`);
    }
  } catch (error) {
    log(`Error loading ${filename}:`, error);
  }
}

// 全データ読み込み
export function loadAllData(dataPath: string, log: (...args: any[]) => void): void {
  const files = [
    'project-info.json', 
    'scene-info.json', 
    'gameobjects.json',
    'assets-info.json', 
    'build-info.json', 
    'editor-state.json'
  ];
  
  files.forEach(file => loadDataFile(file, dataPath, log));
}

// キャッシュされたデータを取得
export function getCachedData(): { [key: string]: any } {
  return cachedData;
}

// データキャッシュをクリア
export function clearCache(): void {
  cachedData = {};
}

// Debounceタイマーをクリア（シャットダウン時に使用）
export function clearDebounceTimers(): void {
  Object.values(debounceTimers).forEach(timer => clearTimeout(timer));
  Object.keys(debounceTimers).forEach(filename => delete debounceTimers[filename]);
}

// Debounced ファイル読み込み
function debouncedLoadDataFile(filename: string, dataPath: string, log: (...args: any[]) => void): void {
  // 既存のタイマーをクリア
  if (debounceTimers[filename]) {
    clearTimeout(debounceTimers[filename]);
  }
  
  // 新しいタイマーを設定
  debounceTimers[filename] = setTimeout(() => {
    loadDataFile(filename, dataPath, log);
    delete debounceTimers[filename];
  }, DEBOUNCE_DELAY);
}

// ファイル監視を開始
export function startFileWatching(dataPath: string, log: (...args: any[]) => void): void {
  const fullPath = path.resolve(dataPath);
  if (fs.existsSync(fullPath)) {
    log(`Watching Unity data directory: ${fullPath} (debounce: ${DEBOUNCE_DELAY}ms)`);
    
    fs.watch(fullPath, { recursive: false }, (_eventType, filename) => {
      if (filename && filename.endsWith('.json')) {
        log(`Unity data file changed: ${filename} (debounced)`);
        debouncedLoadDataFile(filename, dataPath, log);
      }
    });
    
    // 初期データ読み込み
    loadAllData(dataPath, log);
  } else {
    log(`Unity data directory not found: ${fullPath}`);
  }
}

// カテゴリ別データ取得のためのマッピング
export const CATEGORY_MAP: { [key: string]: string } = {
  'project': 'project_info',
  'scene': 'scene_info',
  'gameobjects': 'gameobjects',
  'assets': 'assets_info',
  'build': 'build_info',
  'editor': 'editor_state'
};