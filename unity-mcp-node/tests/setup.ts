/**
 * Jest Test Setup
 * テスト実行前の共通設定
 */

import * as fs from 'fs';
import * as path from 'path';

// テスト用のモックデータディレクトリ
const TEST_DATA_DIR = path.join(__dirname, 'mock-data');

// グローバルテスト設定
global.console = {
  ...console,
  // テスト実行時のログ出力を制御
  error: jest.fn(),
  warn: jest.fn(),
  log: jest.fn(),
};

// テスト用ディレクトリの作成
beforeAll(() => {
  if (!fs.existsSync(TEST_DATA_DIR)) {
    fs.mkdirSync(TEST_DATA_DIR, { recursive: true });
  }
});

// テスト後のクリーンアップ
afterEach(() => {
  jest.clearAllMocks();
});

// テスト終了時のクリーンアップ
afterAll(() => {
  // テスト用ファイルの削除
  if (fs.existsSync(TEST_DATA_DIR)) {
    fs.rmSync(TEST_DATA_DIR, { recursive: true, force: true });
  }
});

// タイムアウトの設定
jest.setTimeout(10000);