import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:1477',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  build: {
    // Segurança on-premise: nunca gerar source maps em produção.
    // Source maps expõem o código-fonte original e facilitam engenharia reversa.
    sourcemap: false,
    // Limitar tamanho de chunk para evitar exposição excessiva em um único arquivo
    chunkSizeWarningLimit: 500,
    rollupOptions: {
      output: {
        // Nomes sem hash de conteúdo no path para dificultar mapeamento
        manualChunks: {
          vendor: ['react', 'react-dom'],
        },
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/__tests__/setup.ts'],
    css: false,
    include: ['src/__tests__/**/*.test.{ts,tsx}'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html', 'lcov'],
      exclude: [
        'node_modules/**',
        'dist/**',
        'src/__tests__/**',
        'e2e/**',
        '*.config.*',
        'src/main.tsx',
      ],
    },
  },
})
