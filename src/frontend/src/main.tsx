import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import '@fontsource-variable/instrument-sans'
import './i18n'
import './index.css'
import App from './App.tsx'
import { ErrorBoundary } from './components/ErrorBoundary'

/**
 * Ponto de entrada da aplicação React.
 *
 * Segurança:
 * - ErrorBoundary envolve toda a árvore para capturar erros sem expor detalhes técnicos.
 * - StrictMode ativa verificações adicionais em desenvolvimento.
 * - Migração de tokens legados do localStorage é feita pelo AuthProvider internamente.
 */
/**
 * Arranca o modo stub (MSW) apenas quando VITE_STUB === 'true' (npm run stub).
 * O import é dinâmico e condicionado por uma constante substituída em build,
 * pelo que o MSW e os stubs nunca entram no bundle de dev/produção normal.
 */
async function bootstrap() {
  if (import.meta.env.VITE_STUB === 'true') {
    const { startStubs } = await import('./stubs/startStubs')
    await startStubs()
  }

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <ErrorBoundary>
        <App />
      </ErrorBoundary>
    </StrictMode>,
  )
}

void bootstrap()

