module.exports = {
  extends: [
    'eslint:recommended',
    '@typescript-eslint/recommended',
    'plugin:security/recommended'
  ],
  plugins: [
    'security',
    'no-unsanitized'
  ],
  rules: {
    // セキュリティ関連のESLintルール
    'security/detect-buffer-noassert': 'error',
    'security/detect-child-process': 'warn',
    'security/detect-disable-mustache-escape': 'error',
    'security/detect-eval-with-expression': 'error',
    'security/detect-new-buffer': 'error',
    'security/detect-no-csrf-before-method-override': 'error',
    'security/detect-non-literal-fs-filename': 'warn',
    'security/detect-non-literal-regexp': 'warn',
    'security/detect-non-literal-require': 'error',
    'security/detect-object-injection': 'warn',
    'security/detect-possible-timing-attacks': 'warn',
    'security/detect-pseudoRandomBytes': 'error',
    'security/detect-unsafe-regex': 'error',
    
    // 追加のセキュリティルール
    'no-eval': 'error',
    'no-implied-eval': 'error',
    'no-new-func': 'error',
    'no-script-url': 'error',
    'no-caller': 'error',
    'no-extend-native': 'error',
    
    // Node.js セキュリティ
    'n/no-deprecated-api': 'error',
    
    // TypeScript セキュリティ
    '@typescript-eslint/no-explicit-any': 'warn',
    '@typescript-eslint/no-unsafe-assignment': 'warn',
    '@typescript-eslint/no-unsafe-call': 'warn',
    '@typescript-eslint/no-unsafe-member-access': 'warn',
    '@typescript-eslint/no-unsafe-return': 'warn',
    
    // unsanitized プラグイン
    'no-unsanitized/method': 'error',
    'no-unsanitized/property': 'error'
  },
  env: {
    node: true,
    es2022: true
  },
  parserOptions: {
    ecmaVersion: 2022,
    sourceType: 'module'
  },
  overrides: [
    {
      files: ['tests/**/*'],
      rules: {
        // テストファイルでは一部ルールを緩和
        'security/detect-non-literal-fs-filename': 'off',
        '@typescript-eslint/no-explicit-any': 'off'
      }
    }
  ]
};