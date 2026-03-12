import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Badge } from '../../components/Badge';

describe('Badge', () => {
  it('renderiza o texto do filho', () => {
    render(<Badge>Breaking</Badge>);
    expect(screen.getByText('Breaking')).toBeInTheDocument();
  });

  it('aplica a variante default quando nenhuma variante é fornecida', () => {
    render(<Badge>Default</Badge>);
    const badge = screen.getByText('Default');
    expect(badge).toHaveClass('bg-gray-100', 'text-gray-700');
  });

  it('aplica a variante success', () => {
    render(<Badge variant="success">OK</Badge>);
    const badge = screen.getByText('OK');
    expect(badge).toHaveClass('bg-green-100', 'text-green-700');
  });

  it('aplica a variante warning', () => {
    render(<Badge variant="warning">Warning</Badge>);
    const badge = screen.getByText('Warning');
    expect(badge).toHaveClass('bg-yellow-100', 'text-yellow-700');
  });

  it('aplica a variante danger', () => {
    render(<Badge variant="danger">Critical</Badge>);
    const badge = screen.getByText('Critical');
    expect(badge).toHaveClass('bg-red-100', 'text-red-700');
  });

  it('aplica a variante info', () => {
    render(<Badge variant="info">Info</Badge>);
    const badge = screen.getByText('Info');
    expect(badge).toHaveClass('bg-blue-100', 'text-blue-700');
  });

  it('renderiza como um span inline', () => {
    render(<Badge>Label</Badge>);
    const badge = screen.getByText('Label');
    expect(badge.tagName).toBe('SPAN');
  });
});
