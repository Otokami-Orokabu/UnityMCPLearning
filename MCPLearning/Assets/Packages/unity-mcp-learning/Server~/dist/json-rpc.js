"use strict";
/**
 * JSON-RPC 2.0 Protocol Implementation
 * JSON-RPCプロトコルの型定義とユーティリティ
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.JSON_RPC_ERRORS = void 0;
exports.mapMCPErrorToJsonRpc = mapMCPErrorToJsonRpc;
exports.sendResponse = sendResponse;
exports.sendErrorResponse = sendErrorResponse;
exports.sendSuccessResponse = sendSuccessResponse;
exports.validateJsonRpcRequest = validateJsonRpcRequest;
const errors_js_1 = require("./errors.js");
// JSON-RPC エラーコード
exports.JSON_RPC_ERRORS = {
    PARSE_ERROR: -32700,
    INVALID_REQUEST: -32600,
    METHOD_NOT_FOUND: -32601,
    INVALID_PARAMS: -32602,
    INTERNAL_ERROR: -32603
};
// MCPErrorCodeをJSON-RPCエラーコードにマッピング
function mapMCPErrorToJsonRpc(mcpErrorCode) {
    switch (mcpErrorCode) {
        case errors_js_1.ErrorCode.INVALID_PARAMETER:
        case errors_js_1.ErrorCode.MISSING_PARAMETER:
        case errors_js_1.ErrorCode.TYPE_MISMATCH:
        case errors_js_1.ErrorCode.VALIDATION_ERROR:
            return exports.JSON_RPC_ERRORS.INVALID_PARAMS;
        case errors_js_1.ErrorCode.EXECUTION_ERROR:
        case errors_js_1.ErrorCode.UNITY_COMMAND_FAILED:
        case errors_js_1.ErrorCode.FILE_NOT_FOUND:
        case errors_js_1.ErrorCode.FILE_READ_ERROR:
        case errors_js_1.ErrorCode.FILE_WRITE_ERROR:
        case errors_js_1.ErrorCode.CONFIG_ERROR:
        case errors_js_1.ErrorCode.INVALID_CONFIG:
        case errors_js_1.ErrorCode.MISSING_CONFIG:
        case errors_js_1.ErrorCode.SYSTEM_ERROR:
            return exports.JSON_RPC_ERRORS.INTERNAL_ERROR;
        case errors_js_1.ErrorCode.CONNECTION_ERROR:
        case errors_js_1.ErrorCode.TIMEOUT_ERROR:
            return exports.JSON_RPC_ERRORS.INTERNAL_ERROR;
        default:
            return exports.JSON_RPC_ERRORS.INTERNAL_ERROR;
    }
}
// JSON-RPCレスポンスを標準出力に送信
function sendResponse(response) {
    const message = JSON.stringify(response);
    console.log(message);
}
// エラーレスポンスを作成して送信
function sendErrorResponse(id, error) {
    let jsonRpcError;
    if (error instanceof errors_js_1.MCPError) {
        jsonRpcError = {
            code: mapMCPErrorToJsonRpc(error.code),
            message: error.message,
            data: error.context
        };
    }
    else {
        jsonRpcError = {
            code: exports.JSON_RPC_ERRORS.INTERNAL_ERROR,
            message: error.message
        };
    }
    sendResponse({
        jsonrpc: '2.0',
        id,
        error: jsonRpcError
    });
}
// 成功レスポンスを作成して送信
function sendSuccessResponse(id, result) {
    sendResponse({
        jsonrpc: '2.0',
        id,
        result
    });
}
// JSON-RPCリクエストの基本検証
function validateJsonRpcRequest(request) {
    if (!request.jsonrpc || request.jsonrpc !== '2.0') {
        throw new errors_js_1.MCPError(errors_js_1.ErrorCode.INVALID_PARAMETER, 'Invalid JSON-RPC version. Must be "2.0"');
    }
    if (!request.method || typeof request.method !== 'string') {
        throw new errors_js_1.MCPError(errors_js_1.ErrorCode.INVALID_PARAMETER, 'Invalid method. Must be a non-empty string');
    }
    if (request.id === undefined) {
        throw new errors_js_1.MCPError(errors_js_1.ErrorCode.INVALID_PARAMETER, 'Missing required id field');
    }
    return request;
}
//# sourceMappingURL=json-rpc.js.map