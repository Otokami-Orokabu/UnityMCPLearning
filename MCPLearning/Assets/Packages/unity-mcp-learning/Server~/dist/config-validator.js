"use strict";
/**
 * Configuration Validation System
 * JSON Schemaを使用したmcp-config.jsonの検証
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
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.ConfigValidator = exports.DEFAULT_CONFIG = void 0;
exports.loadAndValidateConfig = loadAndValidateConfig;
const ajv_1 = __importDefault(require("ajv"));
const ajv_formats_1 = __importDefault(require("ajv-formats"));
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const errors_js_1 = require("./errors.js");
// デフォルト値
exports.DEFAULT_CONFIG = {
    logLevel: 'info',
    timeout: {
        unityCommandTimeout: 30000,
        dataWaitTimeout: 5000
    },
    server: {
        name: 'unity-mcp-server',
        version: '1.0.0',
        protocol: '2024-11-05'
    },
    unity: {
        autoDetectChanges: true,
        watchFilePattern: '*.json'
    }
};
/**
 * 設定ファイル検証クラス
 */
class ConfigValidator {
    ajv;
    schema;
    constructor() {
        this.ajv = new ajv_1.default({
            allErrors: true,
            verbose: true,
            strict: false
        });
        // フォーマット検証を追加
        (0, ajv_formats_1.default)(this.ajv);
        // スキーマを読み込み
        this.loadSchema();
    }
    /**
     * JSON Schemaを読み込み
     */
    loadSchema() {
        try {
            const schemaPath = path.join(__dirname, '../schema/mcp-config.schema.json');
            const schemaContent = fs.readFileSync(schemaPath, 'utf-8');
            this.schema = JSON.parse(schemaContent);
        }
        catch (error) {
            throw new errors_js_1.MCPError(errors_js_1.ErrorCode.CONFIG_ERROR, `Failed to load JSON schema: ${error.message}`, { schemaPath: path.join(__dirname, '../schema/mcp-config.schema.json') });
        }
    }
    /**
     * 設定ファイルを検証
     */
    validateConfig(config) {
        const validate = this.ajv.compile(this.schema);
        const isValid = validate(config);
        if (!isValid) {
            const errors = validate.errors || [];
            const errorMessages = errors.map(err => {
                const path = err.instancePath || 'root';
                return `${path}: ${err.message}`;
            });
            throw new errors_js_1.MCPError(errors_js_1.ErrorCode.INVALID_CONFIG, `Configuration validation failed: ${errorMessages.join(', ')}`, {
                validationErrors: errors,
                config: config
            });
        }
        // デフォルト値をマージ
        const validatedConfig = this.mergeDefaults(config);
        // 追加検証（スキーマでは表現しにくいもの）
        this.validateBusinessRules(validatedConfig);
        return validatedConfig;
    }
    /**
     * デフォルト値をマージ
     */
    mergeDefaults(config) {
        return {
            ...exports.DEFAULT_CONFIG,
            ...config,
            timeout: {
                ...exports.DEFAULT_CONFIG.timeout,
                ...config.timeout
            },
            server: {
                ...exports.DEFAULT_CONFIG.server,
                ...config.server
            },
            unity: {
                ...exports.DEFAULT_CONFIG.unity,
                ...config.unity
            }
        };
    }
    /**
     * ビジネスルール検証
     * JSON Schemaでは表現が困難な複雑な検証
     */
    validateBusinessRules(config) {
        // Unity データパスの存在確認
        this.validateUnityDataPath(config.unityDataPath);
        // MCPサーバー設定の検証
        this.validateMCPServers(config.mcpServers);
        // タイムアウト値の妥当性確認
        this.validateTimeouts(config.timeout);
    }
    /**
     * Unity データパスの検証
     */
    validateUnityDataPath(dataPath) {
        const resolvedPath = path.resolve(dataPath);
        // パスの存在確認（ディレクトリが存在するかは実行時にチェック）
        if (!dataPath || dataPath.trim().length === 0) {
            throw new errors_js_1.MCPError(errors_js_1.ErrorCode.INVALID_CONFIG, 'Unity data path cannot be empty', { dataPath });
        }
        // 相対パスか絶対パスかの確認
        if (!path.isAbsolute(dataPath) && !dataPath.startsWith('.')) {
            throw new errors_js_1.MCPError(errors_js_1.ErrorCode.INVALID_CONFIG, 'Unity data path must be absolute or start with "./"', { dataPath, resolvedPath });
        }
    }
    /**
     * MCPサーバー設定の検証
     */
    validateMCPServers(servers) {
        Object.entries(servers).forEach(([serverName, serverConfig]) => {
            // サーバー名の検証
            if (!/^[a-zA-Z][a-zA-Z0-9_-]*$/.test(serverName)) {
                throw new errors_js_1.MCPError(errors_js_1.ErrorCode.INVALID_CONFIG, `Invalid server name: ${serverName}. Must start with letter and contain only alphanumeric, underscore, or hyphen`, { serverName, serverConfig });
            }
            // コマンドの存在確認
            if (!serverConfig.args || serverConfig.args.length === 0) {
                throw new errors_js_1.MCPError(errors_js_1.ErrorCode.INVALID_CONFIG, `Server ${serverName} must have at least one argument`, { serverName, serverConfig });
            }
        });
    }
    /**
     * タイムアウト値の検証
     */
    validateTimeouts(timeout) {
        if (!timeout)
            return;
        const { unityCommandTimeout, dataWaitTimeout } = timeout;
        if (unityCommandTimeout && dataWaitTimeout) {
            if (unityCommandTimeout < dataWaitTimeout) {
                throw new errors_js_1.MCPError(errors_js_1.ErrorCode.INVALID_CONFIG, 'Unity command timeout should be greater than or equal to data wait timeout', { unityCommandTimeout, dataWaitTimeout });
            }
        }
    }
}
exports.ConfigValidator = ConfigValidator;
/**
 * 設定ファイルを読み込んで検証
 */
function loadAndValidateConfig(configPath) {
    const validator = new ConfigValidator();
    try {
        // 設定ファイルの存在確認
        if (!fs.existsSync(configPath)) {
            throw new errors_js_1.MCPError(errors_js_1.ErrorCode.MISSING_CONFIG, `Configuration file not found: ${configPath}`, { configPath });
        }
        // 設定ファイル読み込み
        const configContent = fs.readFileSync(configPath, 'utf-8');
        let config;
        try {
            config = JSON.parse(configContent);
        }
        catch (parseError) {
            throw new errors_js_1.MCPError(errors_js_1.ErrorCode.INVALID_CONFIG, `Failed to parse configuration file: ${parseError.message}`, { configPath, parseError: parseError.message });
        }
        // 検証実行
        return validator.validateConfig(config);
    }
    catch (error) {
        if (error instanceof errors_js_1.MCPError) {
            throw error;
        }
        throw new errors_js_1.MCPError(errors_js_1.ErrorCode.CONFIG_ERROR, `Configuration loading failed: ${error.message}`, { configPath, originalError: error.message });
    }
}
//# sourceMappingURL=config-validator.js.map