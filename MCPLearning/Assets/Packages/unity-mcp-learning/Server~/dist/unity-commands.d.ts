/**
 * Unity Command Execution System
 * Unityコマンドの実行と検証を管理
 */
import { MCPConfig } from './config-validator.js';
export declare function getUnityCommandPath(dataPath: string): string;
export declare function executeUnityCommand(commandType: string, args: any, dataPath: string, config: MCPConfig, log: (...args: any[]) => void): Promise<any>;
export declare function validateCommandParameters(commandType: string, args: any): any;
export declare function waitForCompilation(dataPath: string, timeoutMs?: number): Promise<any>;
export declare function getConsoleLogs(dataPath: string): Promise<any>;
//# sourceMappingURL=unity-commands.d.ts.map