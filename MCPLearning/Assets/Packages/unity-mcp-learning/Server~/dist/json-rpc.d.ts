/**
 * JSON-RPC 2.0 Protocol Implementation
 * JSON-RPCプロトコルの型定義とユーティリティ
 */
import { ErrorCode, MCPError } from './errors.js';
export interface JsonRpcRequest {
    jsonrpc: '2.0';
    id: string | number;
    method: string;
    params?: any;
}
export interface JsonRpcNotification {
    jsonrpc: '2.0';
    method: string;
    params?: any;
}
export interface JsonRpcResponse {
    jsonrpc: '2.0';
    id: string | number;
    result?: any;
    error?: {
        code: number;
        message: string;
        data?: any;
    };
}
export declare const JSON_RPC_ERRORS: {
    readonly PARSE_ERROR: -32700;
    readonly INVALID_REQUEST: -32600;
    readonly METHOD_NOT_FOUND: -32601;
    readonly INVALID_PARAMS: -32602;
    readonly INTERNAL_ERROR: -32603;
};
export declare function mapMCPErrorToJsonRpc(mcpErrorCode: ErrorCode): number;
export declare function sendResponse(response: JsonRpcResponse): void;
export declare function sendErrorResponse(id: string | number, error: Error | MCPError): void;
export declare function sendSuccessResponse(id: string | number, result: any): void;
export declare function validateJsonRpcRequest(request: any): JsonRpcRequest;
//# sourceMappingURL=json-rpc.d.ts.map