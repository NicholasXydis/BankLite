import js from '@eslint/js';
import globals from 'globals';

const sharedAppGlobals = [
  'API_URL',
  'applyTranslations',
  'changePassword',
  'connectSignalR',
  'countUp',
  'createAccount',
  'createIdempotencyKey',
  'deleteAccount',
  'deposit',
  'forgotPassword',
  'getAccounts',
  'getTokenExpiry',
  'getTransactions',
  'getTransactionsByDateRange',
  'getUserProfile',
  'handleServerError',
  'hideElement',
  'i18n',
  'initPasswordToggle',
  'loadAccounts',
  'loadDashboard',
  'loadDeposit',
  'loadSpendingChart',
  'loadTransactions',
  'loadTransfer',
  'loadWithdraw',
  'login',
  'logout',
  'logoutApi',
  'postMoneyRequest',
  'readJsonSafe',
  'refreshToken',
  'register',
  'requireAuth',
  'resetPassword',
  'sendChatMessage',
  'setLanguage',
  'showElement',
  'startSessionTimer',
  't',
  'takeIdempotencyKey',
  'transfer',
  'transferExternal',
  'typeText',
  'withdraw'
];

export default [
  js.configs.recommended,
  {
    files: ['js/**/*.js'],
    languageOptions: {
      ecmaVersion: 2022,
      sourceType: 'script',
      globals: {
        ...globals.browser,
        Chart: 'readonly',
        signalR: 'readonly',
        confetti: 'readonly',
        THREE: 'readonly',
        VANTA: 'readonly',
        gsap: 'readonly',
        ...Object.fromEntries(sharedAppGlobals.map((name) => [name, 'writable']))
      }
    },
    rules: {
      eqeqeq: ['error', 'smart'],
      'no-var': 'error',
      'no-empty': ['error', { allowEmptyCatch: true }],
      'no-redeclare': 'off',
      'prefer-const': 'error',
      'no-unused-vars': ['error', { vars: 'local', args: 'none' }]
    }
  }
];
