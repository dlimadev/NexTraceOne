import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import react from 'eslint-plugin-react'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    plugins: {
      react,
    },
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    rules: {
      // Relaxar regras para código de produção
      '@typescript-eslint/no-explicit-any': 'warn', // Mudar error para warning
      '@typescript-eslint/no-unused-vars': 'warn', // Mudar error para warning
      'react-hooks/set-state-in-effect': 'off', // Desabilitar regra problemática
      'react/no-array-index-key': 'off', // Desabilitar - muito comum em código existente
      'react/no-danger': 'warn', // Mudar para warning
      'react-refresh/only-export-components': [
        'warn',
        { allowConstantExport: true }
      ],
    },
  },
])