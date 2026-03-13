import { Component, type ErrorInfo, type ReactNode } from 'react';
import i18n from '../i18n';

/**
 * Propriedades do ErrorBoundary.
 *
 * `fallback` permite customizar a UI de erro.
 * Se omitido, exibe uma mensagem genérica via i18n.
 */
interface ErrorBoundaryProps {
  children: ReactNode;
  /** UI customizada para exibir quando ocorre um erro. */
  fallback?: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
}

/**
 * Componente de captura de erros em nível de árvore React.
 *
 * Segurança:
 * - Nunca exibe stack traces, mensagens técnicas ou detalhes internos ao usuário.
 * - Registra o erro no console apenas em desenvolvimento (via import.meta.env.DEV).
 * - Em produção, nenhum detalhe técnico é exposto — apenas a mensagem i18n genérica.
 * - Previne que erros em componentes filhos derrubem toda a aplicação.
 *
 * Uso recomendado: envolver o <App /> ou seções críticas da aplicação.
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    // Segurança: só registra detalhes em desenvolvimento.
    // Em produção, evita vazamento de stack trace e informações internas.
    if (import.meta.env.DEV) {
      console.error('[ErrorBoundary] Unhandled error:', error, errorInfo);
    }
  }

  render(): ReactNode {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <div className="min-h-screen bg-canvas flex items-center justify-center p-4">
          <div className="text-center max-w-md">
            <div className="inline-flex items-center justify-center w-14 h-14 rounded-xl bg-critical/15 mb-4">
              <span className="text-critical font-bold text-xl">!</span>
            </div>
            <h1 className="text-xl font-semibold text-heading mb-2">
              {i18n.t('errors.generic')}
            </h1>
            <p className="text-muted text-sm mb-6">
              {i18n.t('errors.serverError')}
            </p>
            <button
              onClick={() => {
                this.setState({ hasError: false });
                window.location.href = '/';
              }}
              className="px-4 py-2 bg-accent text-white rounded-md text-sm hover:bg-accent/90 transition-colors"
            >
              {i18n.t('common.retry')}
            </button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
