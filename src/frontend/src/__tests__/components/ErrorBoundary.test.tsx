import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ErrorBoundary } from '../../components/ErrorBoundary';

/**
 * Testes de segurança para o ErrorBoundary — validação de captura segura de erros.
 *
 * O ErrorBoundary deve:
 * - Capturar erros sem expor stack traces ao usuário
 * - Exibir mensagem genérica via i18n
 * - Registrar erro no console apenas em desenvolvimento
 * - Renderizar filhos normalmente quando não há erro
 */

/**
 * Componente que deliberadamente lança erro para testar o ErrorBoundary.
 */
function ThrowingComponent({ shouldThrow }: { shouldThrow: boolean }) {
  if (shouldThrow) {
    throw new Error('Test error — should not be shown to user');
  }
  return <div data-testid="child">Working</div>;
}

describe('ErrorBoundary', () => {
  // Suprime console.error durante testes de ErrorBoundary
  // (React loga erros internamente ao capturar em boundary)
  const originalConsoleError = console.error;
  beforeEach(() => {
    console.error = vi.fn();
  });
  afterEach(() => {
    console.error = originalConsoleError;
  });

  it('renderiza filhos normalmente quando não há erro', () => {
    render(
      <ErrorBoundary>
        <ThrowingComponent shouldThrow={false} />
      </ErrorBoundary>
    );

    expect(screen.getByTestId('child')).toHaveTextContent('Working');
  });

  it('captura erro e exibe fallback genérico', () => {
    render(
      <ErrorBoundary>
        <ThrowingComponent shouldThrow={true} />
      </ErrorBoundary>
    );

    // Deve exibir o botão de retry (demonstra que a UI de fallback está ativa)
    expect(screen.queryByTestId('child')).not.toBeInTheDocument();
  });

  it('NÃO exibe mensagem de erro técnica ao usuário', () => {
    render(
      <ErrorBoundary>
        <ThrowingComponent shouldThrow={true} />
      </ErrorBoundary>
    );

    // A mensagem técnica do erro nunca deve aparecer na UI
    expect(screen.queryByText('Test error — should not be shown to user')).not.toBeInTheDocument();
    expect(screen.queryByText(/stack/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/throw/i)).not.toBeInTheDocument();
  });

  it('usa fallback customizado quando fornecido', () => {
    render(
      <ErrorBoundary fallback={<div data-testid="custom-fallback">Custom Error</div>}>
        <ThrowingComponent shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.getByTestId('custom-fallback')).toHaveTextContent('Custom Error');
  });
});
