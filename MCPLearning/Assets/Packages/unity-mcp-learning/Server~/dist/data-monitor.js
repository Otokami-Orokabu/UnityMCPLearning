"use strict";
/**
 * Unity Data Monitoring System
 * Unityプロジェクトデータの監視と読み込みを管理
 */
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.CATEGORY_MAP = void 0;
exports.loadDataFile = loadDataFile;
exports.loadAllData = loadAllData;
exports.getCachedData = getCachedData;
exports.clearCache = clearCache;
exports.clearDebounceTimers = clearDebounceTimers;
exports.startFileWatching = startFileWatching;
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
// データキャッシュ
let cachedData = {};
// Debounce用のタイマー管理
const debounceTimers = {};
const DEBOUNCE_DELAY = 300; // 300ms
// データファイルの読み込み
function loadDataFile(filename, dataPath, log) {
    try {
        const fullPath = path.join(dataPath, filename);
        if (fs.existsSync(fullPath)) {
            const data = JSON.parse(fs.readFileSync(fullPath, 'utf-8'));
            const key = filename.replace('.json', '').replace('-', '_');
            cachedData[key] = data;
            log(`Data loaded: ${filename} -> ${key}`);
        }
    }
    catch (error) {
        log(`Error loading ${filename}:`, error);
    }
}
// 全データ読み込み
function loadAllData(dataPath, log) {
    const files = [
        'project-info.json',
        'scene-info.json',
        'gameobjects.json',
        'assets-info.json',
        'build-info.json',
        'editor-state.json',
        'console-logs.json',
        'compile-status.json'
    ];
    files.forEach(file => loadDataFile(file, dataPath, log));
}
// キャッシュされたデータを取得
function getCachedData() {
    return cachedData;
}
// データキャッシュをクリア
function clearCache() {
    cachedData = {};
}
// Debounceタイマーをクリア（シャットダウン時に使用）
function clearDebounceTimers() {
    Object.values(debounceTimers).forEach(timer => clearTimeout(timer));
    Object.keys(debounceTimers).forEach(filename => delete debounceTimers[filename]);
}
// Debounced ファイル読み込み
function debouncedLoadDataFile(filename, dataPath, log) {
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
function startFileWatching(dataPath, log) {
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
    }
    else {
        log(`Unity data directory not found: ${fullPath}`);
    }
}
// カテゴリ別データ取得のためのマッピング
exports.CATEGORY_MAP = {
    'project': 'project_info',
    'scene': 'scene_info',
    'gameobjects': 'gameobjects',
    'assets': 'assets_info',
    'build': 'build_info',
    'editor': 'editor_state',
    'console': 'console_logs'
};
//# sourceMappingURL=data-monitor.js.map