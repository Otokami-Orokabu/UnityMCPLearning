/**
 * ProcessSecurityManager テストスイート
 */

import { ProcessSecurityManager, AllowedCommandType, executeSecurely } from '../src/process-security';
import * as path from 'path';

describe('ProcessSecurityManager', () => {
    let securityManager: ProcessSecurityManager;

    beforeEach(() => {
        securityManager = new ProcessSecurityManager({
            dryRun: true, // テスト時はドライランモード
            logExecutions: false // テスト時はログ無効
        });
    });

    describe('コマンド検証', () => {
        test('有効なNodeコマンドを許可する', async () => {
            const result = await securityManager.executeSecureCommand(
                'node --version',
                undefined,
                AllowedCommandType.NODE
            );

            expect(result.success).toBe(true);
            expect(result.commandType).toBe(AllowedCommandType.NODE);
            expect(result.output).toContain('[DRY RUN]');
        });

        test('有効なnpmコマンドを許可する', async () => {
            const result = await securityManager.executeSecureCommand(
                'npm --version',
                undefined,
                AllowedCommandType.NPM
            );

            expect(result.success).toBe(true);
            expect(result.commandType).toBe(AllowedCommandType.NPM);
        });

        test('有効なGitコマンドを許可する', async () => {
            const result = await securityManager.executeSecureCommand(
                'git --version',
                undefined,
                AllowedCommandType.GIT
            );

            expect(result.success).toBe(true);
            expect(result.commandType).toBe(AllowedCommandType.GIT);
        });

        test('システム情報コマンドを許可する', async () => {
            const result = await securityManager.executeSecureCommand(
                'pwd',
                undefined,
                AllowedCommandType.SYSTEM_INFO
            );

            expect(result.success).toBe(true);
            expect(result.commandType).toBe(AllowedCommandType.SYSTEM_INFO);
        });
    });

    describe('危険なコマンドの拒否', () => {
        test('rm -rf コマンドを拒否する', async () => {
            const result = await securityManager.executeSecureCommand('rm -rf /');

            expect(result.success).toBe(false);
            expect(result.error).toContain('Blocked pattern detected');
        });

        test('sudo コマンドを拒否する', async () => {
            const result = await securityManager.executeSecureCommand('sudo apt-get install malware');

            expect(result.success).toBe(false);
            expect(result.error).toContain('Blocked pattern detected');
        });

        test('curl コマンドを拒否する', async () => {
            const result = await securityManager.executeSecureCommand('curl https://malicious-site.com');

            expect(result.success).toBe(false);
            expect(result.error).toContain('Blocked pattern detected');
        });

        test('パイプ演算子を含むコマンドを拒否する', async () => {
            const result = await securityManager.executeSecureCommand('cat /etc/passwd | grep root');

            expect(result.success).toBe(false);
            expect(result.error).toContain('Blocked pattern detected');
        });

        test('コマンド実行演算子を拒否する', async () => {
            const result = await securityManager.executeSecureCommand('ls && rm -rf *');

            expect(result.success).toBe(false);
            expect(result.error).toContain('Blocked pattern detected');
        });
    });

    describe('コマンドタイプ自動検出', () => {
        test('nodeコマンドを正しく検出する', async () => {
            const result = await securityManager.executeSecureCommand('node test.js');

            expect(result.success).toBe(true);
            expect(result.commandType).toBe(AllowedCommandType.NODE);
        });

        test('npmコマンドを正しく検出する', async () => {
            const result = await securityManager.executeSecureCommand('npm test');

            expect(result.success).toBe(true);
            expect(result.commandType).toBe(AllowedCommandType.NPM);
        });

        test('gitコマンドを正しく検出する', async () => {
            const result = await securityManager.executeSecureCommand('git status');

            expect(result.success).toBe(true);
            expect(result.commandType).toBe(AllowedCommandType.GIT);
        });

        test('不明なコマンドを拒否する', async () => {
            const result = await securityManager.executeSecureCommand('unknown-command');

            expect(result.success).toBe(false);
            expect(result.error).toContain('Command type not allowed');
        });
    });

    describe('作業ディレクトリ検証', () => {
        test('プロジェクトルート内のディレクトリを許可する', async () => {
            const result = await securityManager.executeSecureCommand(
                'pwd',
                process.cwd(), // 現在のディレクトリを使用
                AllowedCommandType.SYSTEM_INFO
            );

            expect(result.success).toBe(true);
        });

        test('プロジェクトルート外のディレクトリを拒否する', async () => {
            const result = await securityManager.executeSecureCommand(
                'pwd',
                '/etc',
                AllowedCommandType.SYSTEM_INFO
            );

            expect(result.success).toBe(false);
            expect(result.error).toContain('Working directory not allowed');
        });

        test('存在しないディレクトリを拒否する', async () => {
            const result = await securityManager.executeSecureCommand(
                'pwd',
                path.join(process.cwd(), 'non-existent-directory'),
                AllowedCommandType.SYSTEM_INFO
            );

            expect(result.success).toBe(false);
            expect(result.error).toContain('Working directory not allowed');
        });
    });

    describe('入力サニタイズ', () => {
        test('空のコマンドを拒否する', async () => {
            const result = await securityManager.executeSecureCommand('');

            expect(result.success).toBe(false);
            expect(result.error).toContain('Empty command');
        });

        test('長すぎるコマンドを拒否する', async () => {
            const longCommand = 'node ' + 'a'.repeat(1000);
            const result = await securityManager.executeSecureCommand(longCommand);

            expect(result.success).toBe(false);
            expect(result.error).toContain('Command too long');
        });

        test('無効な文字を含むコマンドを拒否する', async () => {
            const result = await securityManager.executeSecureCommand('node test.js; rm -rf *');

            expect(result.success).toBe(false);
            expect(result.error).toContain('Blocked pattern detected');
        });

        test('連続スペースを正規化する', async () => {
            const result = await securityManager.executeSecureCommand(
                'node    --version',
                undefined,
                AllowedCommandType.NODE
            );

            expect(result.success).toBe(true);
            expect(result.sanitizedCommand).toBe('node --version');
        });
    });

    describe('専用メソッド', () => {
        test('executeNodeScript が正しく動作する', async () => {
            const result = await securityManager.executeNodeScript('test.js', ['--verbose']);

            expect(result.success).toBe(true);
            expect(result.commandType).toBe(AllowedCommandType.NODE);
            expect(result.sanitizedCommand).toContain('node "test.js" "--verbose"');
        });

        test('executeNpmCommand が正しく動作する', async () => {
            const result = await securityManager.executeNpmCommand('test');

            expect(result.success).toBe(true);
            expect(result.commandType).toBe(AllowedCommandType.NPM);
            expect(result.sanitizedCommand).toBe('npm test');
        });

        test('executeGitCommand が正しく動作する', async () => {
            const result = await securityManager.executeGitCommand('status');

            expect(result.success).toBe(true);
            expect(result.commandType).toBe(AllowedCommandType.GIT);
            expect(result.sanitizedCommand).toBe('git status');
        });

        test('getSystemInfo が正しく動作する', async () => {
            const result = await securityManager.getSystemInfo('directory');

            expect(result.success).toBe(true);
            expect(result.commandType).toBe(AllowedCommandType.SYSTEM_INFO);
            expect(result.sanitizedCommand).toBe('pwd');
        });
    });

    describe('設定管理', () => {
        test('設定を更新できる', () => {
            const originalConfig = securityManager.getConfig();
            
            securityManager.updateConfig({
                maxExecutionTime: 60000,
                dryRun: false
            });

            const updatedConfig = securityManager.getConfig();
            expect(updatedConfig.maxExecutionTime).toBe(60000);
            expect(updatedConfig.dryRun).toBe(false);
            expect(updatedConfig.logExecutions).toBe(originalConfig.logExecutions);
        });

        test('セキュリティ統計を取得できる', () => {
            const stats = securityManager.getSecurityStats();

            expect(stats.allowedCommandTypes).toContain(AllowedCommandType.NODE);
            expect(stats.allowedCommandTypes).toContain(AllowedCommandType.NPM);
            expect(stats.allowedCommandTypes).toContain(AllowedCommandType.GIT);
            expect(stats.blockedPatterns).toContain('rm -rf');
            expect(stats.allowedDirectories).toContain('unity-mcp-node');
            expect(typeof stats.maxExecutionTime).toBe('number');
        });
    });

    describe('パフォーマンステスト', () => {
        test('実行時間が測定される', async () => {
            const result = await securityManager.executeSecureCommand(
                'node --version',
                undefined,
                AllowedCommandType.NODE
            );

            expect(result.executionTime).toBeGreaterThanOrEqual(0);
            expect(result.executionTime).toBeLessThan(1000); // ドライランなので1秒未満
        });

        test('複数のコマンドを並行実行できる', async () => {
            const promises = [
                securityManager.executeSecureCommand('node --version'),
                securityManager.executeSecureCommand('npm --version'),
                securityManager.executeSecureCommand('git --version')
            ];

            const results = await Promise.all(promises);
            
            expect(results).toHaveLength(3);
            results.forEach(result => {
                expect(result.success).toBe(true);
            });
        });
    });

    describe('ヘルパー関数', () => {
        test('executeSecurely ヘルパー関数が動作する', async () => {
            const result = await executeSecurely('node --version', {
                commandType: AllowedCommandType.NODE,
                config: { dryRun: true, logExecutions: false }
            });

            expect(result.success).toBe(true);
            expect(result.commandType).toBe(AllowedCommandType.NODE);
        });
    });

    describe('エッジケース', () => {
        test('nullまたはundefinedコマンドを適切に処理する', async () => {
            // @ts-ignore - 意図的にnullを渡してテスト
            const result = await securityManager.executeSecureCommand(null);

            expect(result.success).toBe(false);
            expect(result.error).toContain('Empty command');
        });

        test('空白のみのコマンドを拒否する', async () => {
            const result = await securityManager.executeSecureCommand('   ');

            expect(result.success).toBe(false);
            expect(result.error).toContain('Empty command');
        });

        test('非ASCII文字を含むコマンドを拒否する', async () => {
            const result = await securityManager.executeSecureCommand('node テスト.js');

            expect(result.success).toBe(false);
            expect(result.error).toContain('Invalid characters');
        });
    });
});