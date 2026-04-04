import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Card, CardHeader, CardBody } from '../../components/Card';

describe('Card', () => {
  it('renderiza o conteúdo filho', () => {
    render(<Card>Conteúdo do card</Card>);
    expect(screen.getByText('Conteúdo do card')).toBeInTheDocument();
  });

  it('aplica className customizada', () => {
    render(<Card className="extra-class" data-testid="card">Texto</Card>);
    expect(screen.getByTestId('card')).toHaveClass('extra-class');
  });

  it('possui estilo de card padrão', () => {
    render(<Card data-testid="card">Texto</Card>);
    expect(screen.getByTestId('card')).toHaveClass('bg-card', 'rounded-2xl');
  });
});

describe('CardHeader', () => {
  it('renderiza o conteúdo filho', () => {
    render(<CardHeader>Cabeçalho</CardHeader>);
    expect(screen.getByText('Cabeçalho')).toBeInTheDocument();
  });

  it('possui borda inferior', () => {
    render(<CardHeader data-testid="header">Cabeçalho</CardHeader>);
    expect(screen.getByTestId('header')).toHaveClass('border-b');
  });

  it('aplica className customizada', () => {
    render(<CardHeader className="custom" data-testid="header">Título</CardHeader>);
    expect(screen.getByTestId('header')).toHaveClass('custom');
  });
});

describe('CardBody', () => {
  it('renderiza o conteúdo filho', () => {
    render(<CardBody>Corpo do card</CardBody>);
    expect(screen.getByText('Corpo do card')).toBeInTheDocument();
  });

  it('possui padding padrão', () => {
    render(<CardBody data-testid="body">Conteúdo</CardBody>);
    expect(screen.getByTestId('body')).toHaveClass('px-5', 'py-5');
  });

  it('aplica className customizada substituindo padding', () => {
    render(<CardBody className="p-0" data-testid="body">Conteúdo</CardBody>);
    expect(screen.getByTestId('body')).toHaveClass('p-0');
  });
});
