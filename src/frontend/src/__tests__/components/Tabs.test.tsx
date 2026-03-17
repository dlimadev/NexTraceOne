import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Tabs } from '../../components/Tabs';

const items = [
  { id: 'overview', label: 'Overview' },
  { id: 'details', label: 'Details' },
  { id: 'history', label: 'History', disabled: true },
];

describe('Tabs', () => {
  it('renderiza todas as abas', () => {
    render(<Tabs items={items} activeId="overview" onChange={() => {}} />);
    expect(screen.getAllByRole('tab')).toHaveLength(3);
  });

  it('marca a aba ativa com aria-selected', () => {
    render(<Tabs items={items} activeId="overview" onChange={() => {}} />);
    const activeTab = screen.getByRole('tab', { name: 'Overview' });
    expect(activeTab).toHaveAttribute('aria-selected', 'true');
  });

  it('marca abas inativas com aria-selected false', () => {
    render(<Tabs items={items} activeId="overview" onChange={() => {}} />);
    const inactiveTab = screen.getByRole('tab', { name: 'Details' });
    expect(inactiveTab).toHaveAttribute('aria-selected', 'false');
  });

  it('chama onChange ao clicar em aba', async () => {
    const onChange = vi.fn();
    render(<Tabs items={items} activeId="overview" onChange={onChange} />);
    await userEvent.click(screen.getByRole('tab', { name: 'Details' }));
    expect(onChange).toHaveBeenCalledWith('details');
  });

  it('desabilita abas com disabled=true', () => {
    render(<Tabs items={items} activeId="overview" onChange={() => {}} />);
    expect(screen.getByRole('tab', { name: 'History' })).toBeDisabled();
  });

  it('renderiza variante underline por padrão', () => {
    render(<Tabs items={items} activeId="overview" onChange={() => {}} />);
    const tablist = screen.getByRole('tablist');
    expect(tablist).toHaveClass('border-b');
  });

  it('renderiza variante pill', () => {
    render(<Tabs items={items} activeId="overview" onChange={() => {}} variant="pill" />);
    const tablist = screen.getByRole('tablist');
    expect(tablist).toHaveClass('bg-elevated');
  });

  it('renderiza ícone na aba quando fornecido', () => {
    const itemsWithIcon = [
      { id: 'a', label: 'Tab A', icon: <span data-testid="tab-icon">🔵</span> },
    ];
    render(<Tabs items={itemsWithIcon} activeId="a" onChange={() => {}} />);
    expect(screen.getByTestId('tab-icon')).toBeInTheDocument();
  });
});
