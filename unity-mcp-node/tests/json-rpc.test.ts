/**
 * JSON-RPCプロトコル処理のテスト
 */

import { 
  JsonRpcRequest, 
  JsonRpcResponse, 
  JSON_RPC_ERRORS,
  mapMCPErrorToJsonRpc,
  validateJsonRpcRequest,
  sendResponse,
  sendErrorResponse,
  sendSuccessResponse
} from '../src/json-rpc';
import { ErrorCode, MCPError } from '../src/errors';

// console.logのモック
const mockConsoleLog = jest.spyOn(console, 'log').mockImplementation();

beforeEach(() => {
  mockConsoleLog.mockClear();
});

describe('JSON-RPC Types', () => {
  it('should define correct JSON-RPC error codes', () => {
    expect(JSON_RPC_ERRORS.PARSE_ERROR).toBe(-32700);
    expect(JSON_RPC_ERRORS.INVALID_REQUEST).toBe(-32600);
    expect(JSON_RPC_ERRORS.METHOD_NOT_FOUND).toBe(-32601);
    expect(JSON_RPC_ERRORS.INVALID_PARAMS).toBe(-32602);
    expect(JSON_RPC_ERRORS.INTERNAL_ERROR).toBe(-32603);
  });
});

describe('mapMCPErrorToJsonRpc', () => {
  it('should map validation errors to INVALID_PARAMS', () => {
    expect(mapMCPErrorToJsonRpc(ErrorCode.INVALID_PARAMETER)).toBe(JSON_RPC_ERRORS.INVALID_PARAMS);
    expect(mapMCPErrorToJsonRpc(ErrorCode.MISSING_PARAMETER)).toBe(JSON_RPC_ERRORS.INVALID_PARAMS);
    expect(mapMCPErrorToJsonRpc(ErrorCode.TYPE_MISMATCH)).toBe(JSON_RPC_ERRORS.INVALID_PARAMS);
    expect(mapMCPErrorToJsonRpc(ErrorCode.VALIDATION_ERROR)).toBe(JSON_RPC_ERRORS.INVALID_PARAMS);
  });

  it('should map execution errors to INTERNAL_ERROR', () => {
    expect(mapMCPErrorToJsonRpc(ErrorCode.EXECUTION_ERROR)).toBe(JSON_RPC_ERRORS.INTERNAL_ERROR);
    expect(mapMCPErrorToJsonRpc(ErrorCode.UNITY_COMMAND_FAILED)).toBe(JSON_RPC_ERRORS.INTERNAL_ERROR);
    expect(mapMCPErrorToJsonRpc(ErrorCode.FILE_NOT_FOUND)).toBe(JSON_RPC_ERRORS.INTERNAL_ERROR);
    expect(mapMCPErrorToJsonRpc(ErrorCode.SYSTEM_ERROR)).toBe(JSON_RPC_ERRORS.INTERNAL_ERROR);
  });

  it('should map unknown errors to INTERNAL_ERROR', () => {
    expect(mapMCPErrorToJsonRpc(ErrorCode.UNKNOWN_ERROR)).toBe(JSON_RPC_ERRORS.INTERNAL_ERROR);
    expect(mapMCPErrorToJsonRpc(999 as ErrorCode)).toBe(JSON_RPC_ERRORS.INTERNAL_ERROR);
  });
});

describe('validateJsonRpcRequest', () => {
  it('should validate correct JSON-RPC request', () => {
    const request = {
      jsonrpc: '2.0' as const,
      id: 1,
      method: 'test_method',
      params: { test: 'value' }
    };

    const result = validateJsonRpcRequest(request);
    expect(result).toEqual(request);
  });

  it('should throw error for missing jsonrpc field', () => {
    const request = {
      id: 1,
      method: 'test_method'
    };

    expect(() => validateJsonRpcRequest(request)).toThrow(MCPError);
    expect(() => validateJsonRpcRequest(request)).toThrow(/Invalid JSON-RPC version/);
  });

  it('should throw error for wrong jsonrpc version', () => {
    const request = {
      jsonrpc: '1.0',
      id: 1,
      method: 'test_method'
    };

    expect(() => validateJsonRpcRequest(request)).toThrow(MCPError);
    expect(() => validateJsonRpcRequest(request)).toThrow(/Invalid JSON-RPC version. Must be "2.0"/);
  });

  it('should throw error for missing method', () => {
    const request = {
      jsonrpc: '2.0',
      id: 1
    };

    expect(() => validateJsonRpcRequest(request)).toThrow(MCPError);
    expect(() => validateJsonRpcRequest(request)).toThrow(/Invalid method/);
  });

  it('should throw error for empty method', () => {
    const request = {
      jsonrpc: '2.0',
      id: 1,
      method: ''
    };

    expect(() => validateJsonRpcRequest(request)).toThrow(MCPError);
    expect(() => validateJsonRpcRequest(request)).toThrow(/Invalid method/);
  });

  it('should throw error for missing id', () => {
    const request = {
      jsonrpc: '2.0',
      method: 'test_method'
    };

    expect(() => validateJsonRpcRequest(request)).toThrow(MCPError);
    expect(() => validateJsonRpcRequest(request)).toThrow(/Missing required id field/);
  });

  it('should accept id as string or number', () => {
    const requestWithStringId = {
      jsonrpc: '2.0' as const,
      id: 'test-id',
      method: 'test_method'
    };

    const requestWithNumberId = {
      jsonrpc: '2.0' as const,
      id: 123,
      method: 'test_method'
    };

    expect(() => validateJsonRpcRequest(requestWithStringId)).not.toThrow();
    expect(() => validateJsonRpcRequest(requestWithNumberId)).not.toThrow();
  });
});

describe('sendResponse', () => {
  it('should send JSON response to console', () => {
    const response: JsonRpcResponse = {
      jsonrpc: '2.0',
      id: 1,
      result: { success: true }
    };

    sendResponse(response);

    expect(mockConsoleLog).toHaveBeenCalledWith(JSON.stringify(response));
  });
});

describe('sendSuccessResponse', () => {
  it('should send success response with result', () => {
    const result = { message: 'Success', data: [1, 2, 3] };
    
    sendSuccessResponse(1, result);

    expect(mockConsoleLog).toHaveBeenCalledWith(JSON.stringify({
      jsonrpc: '2.0',
      id: 1,
      result: result
    }));
  });

  it('should handle string id', () => {
    const result = { message: 'Success' };
    
    sendSuccessResponse('test-id', result);

    expect(mockConsoleLog).toHaveBeenCalledWith(JSON.stringify({
      jsonrpc: '2.0',
      id: 'test-id',
      result: result
    }));
  });
});

describe('sendErrorResponse', () => {
  it('should send error response for MCPError', () => {
    const error = new MCPError(ErrorCode.INVALID_PARAMETER, 'Test error', { field: 'name' });
    
    sendErrorResponse(1, error);

    const expectedResponse = {
      jsonrpc: '2.0',
      id: 1,
      error: {
        code: JSON_RPC_ERRORS.INVALID_PARAMS,
        message: 'Test error',
        data: { field: 'name' }
      }
    };

    expect(mockConsoleLog).toHaveBeenCalledWith(JSON.stringify(expectedResponse));
  });

  it('should send error response for regular Error', () => {
    const error = new Error('Regular error message');
    
    sendErrorResponse('test-id', error);

    const expectedResponse = {
      jsonrpc: '2.0',
      id: 'test-id',
      error: {
        code: JSON_RPC_ERRORS.INTERNAL_ERROR,
        message: 'Regular error message'
      }
    };

    expect(mockConsoleLog).toHaveBeenCalledWith(JSON.stringify(expectedResponse));
  });

  it('should map different MCP error types correctly', () => {
    const executionError = new MCPError(ErrorCode.UNITY_COMMAND_FAILED, 'Command failed');
    
    sendErrorResponse(2, executionError);

    const expectedResponse = {
      jsonrpc: '2.0',
      id: 2,
      error: {
        code: JSON_RPC_ERRORS.INTERNAL_ERROR,
        message: 'Command failed',
        data: undefined
      }
    };

    expect(mockConsoleLog).toHaveBeenCalledWith(JSON.stringify(expectedResponse));
  });
});