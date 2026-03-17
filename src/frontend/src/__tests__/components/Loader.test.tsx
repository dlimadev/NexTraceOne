import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Loader } from '../../components/Loader';

describe('Loader', () => {
  it('renderiza o spinner SVG', () => {
    const { container } = render(<Loader />);
    expect(container.querySelector('svg')).toBeInTheDocument();
  });

  it('é oculto de leitores de tela', () => {
    const { container } = render(<Loader />);
    expect(container.querySelector('svg')).toHaveAttribute('aria-hidden', 'true');
  });

  it('aplica tamanho sm', () => {
    const { container } = render(<Loader size="sm" />);
    expect(container.querySelector('svg')).toHaveClass('h-4', 'w-4');
  });

  it('aplica tamanho md por padrão', () => {
    const { container } = render(<Loader />);
    expect(container.querySelector('svg')).toHaveClass('h-6', 'w-6');
  });

  it('aplica tamanho lg', () => {
    const { container } = render(<Loader size="lg" />);
    expect(container.querySelector('svg')).toHaveClass('h-8', 'w-8');
  });

  it('aceita className customizada', () => {
    const { container } = render(<Loader className="text-red-500" />);
    expect(container.querySelector('svg')).toHaveClass('text-red-500');
  });

  it('possui animação de spin', () => {
    const { container } = render(<Loader />);
    expect(container.querySelector('svg')).toHaveClass('animate-spin');
  });
});
