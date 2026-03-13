import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

/**
 * Configuração Vite para o frontend NexTraceOne.
 *
 * Medidas de segurança on-premise:
 * - Source maps desativados em produção (sourcemap: false) para não expor código-fonte.
 * - Minificação ativa (padrão Vite) para dificultar engenharia reversa.
 * - Chunk splitting para vendor isolado (React/ReactDOM).
 * - Asset file names sem informação de estrutura interna.
 * - Build de produção distinto do build de desenvolvimento.
 *
 * Riscos residuais documentados:
 * - Código JavaScript minificado ainda pode ser inspecionado com ferramentas de dev.
 *   Proteção absoluta contra inspeção client-side não é viável em aplicações web.
 *   O objetivo é reduzir exposição e dificultar engenharia reversa casual.
 */
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
        // Nomes com hash de conteúdo para cache-busting e verificação de integridade.
        // Não expõem estrutura de pastas ou nomes de módulos internos.
        manualChunks: {
          vendor: ['react', 'react-dom'],
        },
        // Sanitiza nomes de assets para não expor caminhos internos do projeto.
        assetFileNames: 'assets/[hash][extname]',
        chunkFileNames: 'assets/[hash].js',
        entryFileNames: 'assets/[hash].js',
      },
    },
    // Remover console.log e debugger em produção para evitar vazamento de informação.
    minify: 'terser',
    terserOptions: {
      compress: {
        drop_console: true,
        drop_debugger: true,
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

