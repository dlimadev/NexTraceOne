import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
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
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ErrorBoundary>
      <App />
    </ErrorBoundary>
  </StrictMode>,
)

