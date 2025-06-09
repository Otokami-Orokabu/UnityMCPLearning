/**
 * JSON-RPC 2.0 Protocol Implementation
 * JSON-RPCプロトコルの型定義とユーティリティ
 */

import { ErrorCode, MCPError } from './errors.js';

// JSON-RPC 2.0のメッセージタイプ
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

// JSON-RPC エラーコード
export const JSON_RPC_ERRORS = {
  PARSE_ERROR: -32700,
  INVALID_REQUEST: -32600,
  METHOD_NOT_FOUND: -32601,
  INVALID_PARAMS: -32602,
  INTERNAL_ERROR: -32603
} as const;

// MCPErrorCodeをJSON-RPCエラーコードにマッピング
export function mapMCPErrorToJsonRpc(mcpErrorCode: ErrorCode): number {
  switch (mcpErrorCode) {
    case ErrorCode.INVALID_PARAMETER:
    case ErrorCode.MISSING_PARAMETER:
    case ErrorCode.TYPE_MISMATCH:
    case ErrorCode.VALIDATION_ERROR:
      return JSON_RPC_ERRORS.INVALID_PARAMS;
    
    case ErrorCode.EXECUTION_ERROR:
    case ErrorCode.UNITY_COMMAND_FAILED:
    case ErrorCode.FILE_NOT_FOUND:
    case ErrorCode.FILE_READ_ERROR:
    case ErrorCode.FILE_WRITE_ERROR:
    case ErrorCode.CONFIG_ERROR:
    case ErrorCode.INVALID_CONFIG:
    case ErrorCode.MISSING_CONFIG:
    case ErrorCode.SYSTEM_ERROR:
      return JSON_RPC_ERRORS.INTERNAL_ERROR;
    
    case ErrorCode.CONNECTION_ERROR:
    case ErrorCode.TIMEOUT_ERROR:
      return JSON_RPC_ERRORS.INTERNAL_ERROR;
    
    default:
      return JSON_RPC_ERRORS.INTERNAL_ERROR;
  }
}

// JSON-RPCレスポンスを標準出力に送信
export function sendResponse(response: JsonRpcResponse): void {
  const message = JSON.stringify(response);
  console.log(message);
}

// エラーレスポンスを作成して送信
export function sendErrorResponse(id: string | number, error: Error | MCPError): void {
  let jsonRpcError;
  
  if (error instanceof MCPError) {
    jsonRpcError = {
      code: mapMCPErrorToJsonRpc(error.code),
      message: error.message,
      data: error.context
    };
  } else {
    jsonRpcError = {
      code: JSON_RPC_ERRORS.INTERNAL_ERROR,
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
export function sendSuccessResponse(id: string | number, result: any): void {
  sendResponse({
    jsonrpc: '2.0',
    id,
    result
  });
}

// JSON-RPCリクエストの基本検証
export function validateJsonRpcRequest(request: any): JsonRpcRequest {
  if (!request.jsonrpc || request.jsonrpc !== '2.0') {
    throw new MCPError(
      ErrorCode.INVALID_PARAMETER,
      'Invalid JSON-RPC version. Must be "2.0"'
    );
  }
  
  if (!request.method || typeof request.method !== 'string') {
    throw new MCPError(
      ErrorCode.INVALID_PARAMETER,
      'Invalid method. Must be a non-empty string'
    );
  }
  
  if (request.id === undefined) {
    throw new MCPError(
      ErrorCode.INVALID_PARAMETER,
      'Missing required id field'
    );
  }
  
  return request as JsonRpcRequest;
}