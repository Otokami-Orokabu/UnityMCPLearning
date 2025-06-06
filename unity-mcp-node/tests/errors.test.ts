/**
 * エラーハンドリングシステムのテスト
 */

import { ErrorCode, MCPError, createErrorResponse, isValidationError, isExecutionError, isSystemError } from '../src/errors';
import { setLanguage } from '../src/i18n';

describe('errors', () => {
  beforeEach(() => {
    setLanguage('en'); // 各テスト前に英語に設定
  });

  describe('ErrorCode enum', () => {
    it('should have correct error code values', () => {
      expect(ErrorCode.CONNECTION_ERROR).toBe(1000);
      expect(ErrorCode.TIMEOUT_ERROR).toBe(1001);
      expect(ErrorCode.VALIDATION_ERROR).toBe(2000);
      expect(ErrorCode.INVALID_PARAMETER).toBe(2001);
      expect(ErrorCode.EXECUTION_ERROR).toBe(3000);
      expect(ErrorCode.UNITY_COMMAND_FAILED).toBe(3001);
      expect(ErrorCode.CONFIG_ERROR).toBe(4000);
      expect(ErrorCode.SYSTEM_ERROR).toBe(5000);
      expect(ErrorCode.UNKNOWN_ERROR).toBe(5999);
    });
  });

  describe('MCPError class', () => {
    it('should create error with correct properties', () => {
      const error = new MCPError(ErrorCode.INVALID_PARAMETER, 'Test error message');
      
      expect(error).toBeInstanceOf(Error);
      expect(error).toBeInstanceOf(MCPError);
      expect(error.name).toBe('MCPError');
      expect(error.code).toBe(ErrorCode.INVALID_PARAMETER);
      expect(error.message).toBe('Test error message');
      expect(error.timestamp).toBeInstanceOf(Date);
      expect(error.context).toBeUndefined();
    });

    it('should create error with context', () => {
      const context = { userId: 123, action: 'test' };
      const error = new MCPError(ErrorCode.VALIDATION_ERROR, 'Validation failed', context);
      
      expect(error.context).toEqual(context);
    });

    it('should serialize to JSON correctly', () => {
      const context = { param: 'value' };
      const error = new MCPError(ErrorCode.FILE_NOT_FOUND, 'File missing', context);
      const json = error.toJSON();
      
      expect(json.name).toBe('MCPError');
      expect(json.code).toBe(ErrorCode.FILE_NOT_FOUND);
      expect(json.message).toBe('File missing');
      expect(json.context).toEqual(context);
      expect(json.timestamp).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/);
    });

    it('should return correct error level', () => {
      const validationError = new MCPError(ErrorCode.INVALID_PARAMETER, 'Invalid param');
      const executionError = new MCPError(ErrorCode.UNITY_COMMAND_FAILED, 'Command failed');
      const systemError = new MCPError(ErrorCode.SYSTEM_ERROR, 'System error');
      
      expect(validationError.getLevel()).toBe('warn');
      expect(executionError.getLevel()).toBe('error');
      expect(systemError.getLevel()).toBe('error');
    });

    it('should provide error descriptions', () => {
      expect(MCPError.getErrorDescription(ErrorCode.CONNECTION_ERROR)).toBe('Connection to Unity failed');
      expect(MCPError.getErrorDescription(ErrorCode.INVALID_PARAMETER)).toBe('Invalid parameter provided');
      expect(MCPError.getErrorDescription(ErrorCode.UNITY_COMMAND_FAILED)).toBe('Unity command execution failed');
      expect(MCPError.getErrorDescription(ErrorCode.UNKNOWN_ERROR)).toBe('Unknown error occurred');
    });
  });

  describe('Error classification helpers', () => {
    it('should correctly identify validation errors', () => {
      const validationError = new MCPError(ErrorCode.INVALID_PARAMETER, 'Invalid');
      const executionError = new MCPError(ErrorCode.UNITY_COMMAND_FAILED, 'Failed');
      const regularError = new Error('Regular error');
      
      expect(isValidationError(validationError)).toBe(true);
      expect(isValidationError(executionError)).toBe(false);
      expect(isValidationError(regularError)).toBe(false);
    });

    it('should correctly identify execution errors', () => {
      const executionError = new MCPError(ErrorCode.UNITY_COMMAND_FAILED, 'Failed');
      const validationError = new MCPError(ErrorCode.INVALID_PARAMETER, 'Invalid');
      const regularError = new Error('Regular error');
      
      expect(isExecutionError(executionError)).toBe(true);
      expect(isExecutionError(validationError)).toBe(false);
      expect(isExecutionError(regularError)).toBe(false);
    });

    it('should correctly identify system errors', () => {
      const systemError = new MCPError(ErrorCode.SYSTEM_ERROR, 'System error');
      const validationError = new MCPError(ErrorCode.INVALID_PARAMETER, 'Invalid');
      const regularError = new Error('Regular error');
      
      expect(isSystemError(systemError)).toBe(true);
      expect(isSystemError(validationError)).toBe(false);
      expect(isSystemError(regularError)).toBe(false);
    });
  });

  describe('createErrorResponse function', () => {
    it('should create error response for MCPError', () => {
      const error = new MCPError(ErrorCode.INVALID_PARAMETER, 'Invalid param', { field: 'name' });
      const response = createErrorResponse(error);
      
      expect(response.content).toHaveLength(1);
      expect(response.content[0].type).toBe('text');
      expect(response.content[0].text).toBe('Error [2001]: Invalid param');
      expect(response.isError).toBe(true);
      expect(response.errorDetails).toEqual(error.toJSON());
    });

    it('should create error response for regular Error', () => {
      const error = new Error('Regular error');
      const response = createErrorResponse(error);
      
      expect(response.content).toHaveLength(1);
      expect(response.content[0].type).toBe('text');
      expect(response.content[0].text).toBe('Error: Regular error');
      expect(response.isError).toBe(true);
      expect(response.errorDetails).toBeUndefined();
    });
  });

  describe('i18n support', () => {
    it('should create error with message key and parameters', () => {
      const error = new MCPError(
        ErrorCode.INVALID_PARAMETER,
        'error.invalid_parameter',
        { parameter: 'test' },
        { parameter: 'testParam' }
      );

      expect(error.code).toBe(ErrorCode.INVALID_PARAMETER);
      expect(error.message).toBe('Invalid parameter: testParam');
      expect(error.messageKey).toBe('error.invalid_parameter');
      expect(error.messageParams).toEqual({ parameter: 'testParam' });
    });

    it('should get localized message in different languages', () => {
      const error = new MCPError(
        ErrorCode.UNKNOWN_ERROR,
        'error.unknown_method',
        { method: 'test' },
        { method: 'testMethod' }
      );

      // 英語でのメッセージ
      setLanguage('en');
      expect(error.getLocalizedMessage('en')).toBe('Unknown method: testMethod');

      // 日本語でのメッセージ
      expect(error.getLocalizedMessage('ja')).toBe('不明なメソッド: testMethod');
    });

    it('should include i18n information in JSON output', () => {
      const error = new MCPError(
        ErrorCode.VALIDATION_ERROR,
        'validation.name_empty',
        { fieldName: 'userName' },
        { fieldName: 'userNameField' }
      );

      const json = error.toJSON();
      expect(json.messageKey).toBe('validation.name_empty');
      expect(json.messageParams).toEqual({ fieldName: 'userNameField' });
    });

    it('should handle regular string messages without i18n', () => {
      const error = new MCPError(
        ErrorCode.SYSTEM_ERROR,
        'Regular error message'
      );

      expect(error.message).toBe('Regular error message');
      expect(error.messageKey).toBeUndefined();
      expect(error.messageParams).toBeUndefined();
      expect(error.getLocalizedMessage('ja')).toBe('Regular error message');
    });
  });
});