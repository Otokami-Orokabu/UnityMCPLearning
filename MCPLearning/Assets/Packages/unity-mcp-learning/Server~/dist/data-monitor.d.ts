/**
 * Unity Data Monitoring System
 * Unityプロジェクトデータの監視と読み込みを管理
 */
export declare function loadDataFile(filename: string, dataPath: string, log: (...args: any[]) => void): void;
export declare function loadAllData(dataPath: string, log: (...args: any[]) => void): void;
export declare function getCachedData(): {
    [key: string]: any;
};
export declare function clearCache(): void;
export declare function clearDebounceTimers(): void;
export declare function startFileWatching(dataPath: string, log: (...args: any[]) => void): void;
export declare const CATEGORY_MAP: {
    [key: string]: string;
};
//# sourceMappingURL=data-monitor.d.ts.map