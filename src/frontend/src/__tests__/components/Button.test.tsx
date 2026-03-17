import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Button } from '../../components/Button';

describe('Button', () => {
  it('renderiza o texto do filho', () => {
    render(<Button>Click Me</Button>);
    expect(screen.getByRole('button', { name: /click me/i })).toBeInTheDocument();
  });

  it('chama onClick ao ser clicado', async () => {
    const handleClick = vi.fn();
    render(<Button onClick={handleClick}>Submit</Button>);
    await userEvent.click(screen.getByRole('button'));
    expect(handleClick).toHaveBeenCalledOnce();
  });

  it('fica desabilitado quando loading é true', () => {
    render(<Button loading>Saving</Button>);
    expect(screen.getByRole('button')).toBeDisabled();
  });

  it('fica desabilitado quando disabled é true', () => {
    render(<Button disabled>Save</Button>);
    expect(screen.getByRole('button')).toBeDisabled();
  });

  it('não chama onClick quando desabilitado', async () => {
    const handleClick = vi.fn();
    render(<Button disabled onClick={handleClick}>Save</Button>);
    await userEvent.click(screen.getByRole('button'));
    expect(handleClick).not.toHaveBeenCalled();
  });

  it('aplica a variante primary por padrão', () => {
    render(<Button>Primary</Button>);
    const btn = screen.getByRole('button');
    expect(btn).toHaveClass('cta-gradient', 'text-on-accent');
  });

  it('aplica a variante secondary', () => {
    render(<Button variant="secondary">Secondary</Button>);
    const btn = screen.getByRole('button');
    expect(btn).toHaveClass('bg-elevated', 'text-body');
  });

  it('aplica a variante danger', () => {
    render(<Button variant="danger">Delete</Button>);
    const btn = screen.getByRole('button');
    expect(btn).toHaveClass('text-critical');
  });

  it('aplica o tamanho sm', () => {
    render(<Button size="sm">Small</Button>);
    const btn = screen.getByRole('button');
    expect(btn).toHaveClass('h-9', 'px-4');
  });

  it('aplica o tamanho lg', () => {
    render(<Button size="lg">Large</Button>);
    const btn = screen.getByRole('button');
    expect(btn).toHaveClass('h-14', 'px-6');
  });

  it('mostra o spinner de loading', () => {
    render(<Button loading>Saving</Button>);
    expect(screen.getByRole('button').querySelector('svg')).toBeInTheDocument();
  });

  it('aceita className customizada', () => {
    render(<Button className="w-full">Full Width</Button>);
    expect(screen.getByRole('button')).toHaveClass('w-full');
  });

  it('repassa atributos HTML nativos do botão', () => {
    render(<Button type="submit">Submit</Button>);
    expect(screen.getByRole('button')).toHaveAttribute('type', 'submit');
  });
});
