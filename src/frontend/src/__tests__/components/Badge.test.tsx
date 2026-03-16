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
    expect(badge).toHaveClass('bg-elevated', 'text-body');
  });

  it('aplica a variante success', () => {
    render(<Badge variant="success">OK</Badge>);
    const badge = screen.getByText('OK');
    expect(badge).toHaveClass('bg-success/15', 'text-success');
  });

  it('aplica a variante warning', () => {
    render(<Badge variant="warning">Warning</Badge>);
    const badge = screen.getByText('Warning');
    expect(badge).toHaveClass('bg-warning/15', 'text-warning');
  });

  it('aplica a variante danger', () => {
    render(<Badge variant="danger">Critical</Badge>);
    const badge = screen.getByText('Critical');
    expect(badge).toHaveClass('bg-critical/15', 'text-critical');
  });

  it('aplica a variante info', () => {
    render(<Badge variant="info">Info</Badge>);
    const badge = screen.getByText('Info');
    expect(badge).toHaveClass('bg-info/15', 'text-info');
  });

  it('renderiza como um span inline', () => {
    render(<Badge>Label</Badge>);
    const badge = screen.getByText('Label');
    expect(badge.tagName).toBe('SPAN');
  });
});
