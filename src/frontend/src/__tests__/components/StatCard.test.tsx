import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Activity } from 'lucide-react';
import { StatCard } from '../../components/StatCard';

describe('StatCard', () => {
  it('renderiza o título', () => {
    render(
      <StatCard
        title="Active Services"
        value={42}
        icon={<Activity size={28} />}
      />
    );
    expect(screen.getByText('Active Services')).toBeInTheDocument();
  });

  it('renderiza o valor numérico', () => {
    render(
      <StatCard
        title="Total APIs"
        value={100}
        icon={<Activity size={28} />}
      />
    );
    expect(screen.getByText('100')).toBeInTheDocument();
  });

  it('renderiza o valor como string (placeholder)', () => {
    render(
      <StatCard
        title="Pending"
        value="—"
        icon={<Activity size={28} />}
      />
    );
    expect(screen.getByText('—')).toBeInTheDocument();
  });

  it('aplica a cor padrão quando nenhuma cor é fornecida', () => {
    render(
      <StatCard
        title="Test"
        value={0}
        icon={<Activity data-testid="icon" size={28} />}
      />
    );
    // O contêiner do ícone deve ter a classe de cor padrão
    const iconWrapper = screen.getByTestId('icon').parentElement;
    expect(iconWrapper).toHaveClass('text-accent');
  });

  it('aplica a cor customizada', () => {
    render(
      <StatCard
        title="Test"
        value={5}
        icon={<Activity data-testid="icon" size={28} />}
        color="text-blue-600"
      />
    );
    const iconWrapper = screen.getByTestId('icon').parentElement;
    expect(iconWrapper).toHaveClass('text-blue-600');
  });

  it('renderiza o ícone fornecido', () => {
    render(
      <StatCard
        title="Test"
        value={1}
        icon={<Activity data-testid="activity-icon" size={28} />}
      />
    );
    expect(screen.getByTestId('activity-icon')).toBeInTheDocument();
  });
});
