import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { EmptyState } from '../../components/EmptyState';

describe('EmptyState', () => {
  it('renderiza título obrigatório', () => {
    render(<EmptyState title="Sem dados" />);
    expect(screen.getByText('Sem dados')).toBeInTheDocument();
  });

  it('renderiza descrição quando fornecida', () => {
    render(<EmptyState title="Vazio" description="Nenhum item encontrado" />);
    expect(screen.getByText('Nenhum item encontrado')).toBeInTheDocument();
  });

  it('não renderiza descrição quando omitida', () => {
    const { container } = render(<EmptyState title="Vazio" />);
    const paragraphs = container.querySelectorAll('p');
    expect(paragraphs).toHaveLength(0);
  });

  it('renderiza ação quando fornecida', () => {
    render(<EmptyState title="Vazio" action={<button>Criar</button>} />);
    expect(screen.getByRole('button', { name: /criar/i })).toBeInTheDocument();
  });

  it('aplica tamanho compact', () => {
    const { container } = render(<EmptyState title="Vazio" size="compact" />);
    expect(container.firstChild).toHaveClass('py-8');
  });

  it('aplica tamanho default', () => {
    const { container } = render(<EmptyState title="Vazio" />);
    expect(container.firstChild).toHaveClass('py-16');
  });

  it('renderiza ícone customizado quando fornecido', () => {
    render(<EmptyState title="Vazio" icon={<span data-testid="custom-icon">★</span>} />);
    expect(screen.getByTestId('custom-icon')).toBeInTheDocument();
  });
});
