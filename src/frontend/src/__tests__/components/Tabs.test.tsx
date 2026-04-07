import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Tabs, TabPanel } from '../../components/Tabs';

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

  // ─── Keyboard navigation tests ──────────────────────────────

  it('navigates to next tab with ArrowRight', async () => {
    const onChange = vi.fn();
    render(<Tabs items={items} activeId="overview" onChange={onChange} />);
    const tablist = screen.getByRole('tablist');
    const firstTab = screen.getByRole('tab', { name: 'Overview' });
    firstTab.focus();
    await userEvent.keyboard('{ArrowRight}');
    expect(onChange).toHaveBeenCalledWith('details');
  });

  it('navigates to previous tab with ArrowLeft', async () => {
    const onChange = vi.fn();
    render(<Tabs items={items} activeId="details" onChange={onChange} />);
    const secondTab = screen.getByRole('tab', { name: 'Details' });
    secondTab.focus();
    await userEvent.keyboard('{ArrowLeft}');
    expect(onChange).toHaveBeenCalledWith('overview');
  });

  it('navigates to first tab with Home', async () => {
    const onChange = vi.fn();
    render(<Tabs items={items} activeId="details" onChange={onChange} />);
    const tab = screen.getByRole('tab', { name: 'Details' });
    tab.focus();
    await userEvent.keyboard('{Home}');
    expect(onChange).toHaveBeenCalledWith('overview');
  });

  it('navigates to last tab with End', async () => {
    const onChange = vi.fn();
    render(<Tabs items={items} activeId="overview" onChange={onChange} />);
    const tab = screen.getByRole('tab', { name: 'Overview' });
    tab.focus();
    await userEvent.keyboard('{End}');
    // Last enabled tab is 'details' (history is disabled)
    expect(onChange).toHaveBeenCalledWith('details');
  });

  it('uses roving tabindex - active tab has tabindex=0', () => {
    render(<Tabs items={items} activeId="details" onChange={() => {}} />);
    const activeTab = screen.getByRole('tab', { name: 'Details' });
    const inactiveTab = screen.getByRole('tab', { name: 'Overview' });
    expect(activeTab).toHaveAttribute('tabindex', '0');
    expect(inactiveTab).toHaveAttribute('tabindex', '-1');
  });

  it('sets aria-controls when id is provided', () => {
    render(<Tabs items={items} activeId="overview" onChange={() => {}} id="my-tabs" />);
    const tab = screen.getByRole('tab', { name: 'Overview' });
    expect(tab).toHaveAttribute('aria-controls', 'my-tabs-panel-overview');
  });
});

describe('TabPanel', () => {
  it('renders content when active', () => {
    render(
      <TabPanel tabId="overview" tabsId="my-tabs" active={true}>
        Panel Content
      </TabPanel>,
    );
    expect(screen.getByText('Panel Content')).toBeInTheDocument();
  });

  it('does not render when inactive', () => {
    render(
      <TabPanel tabId="overview" tabsId="my-tabs" active={false}>
        Hidden Content
      </TabPanel>,
    );
    expect(screen.queryByText('Hidden Content')).not.toBeInTheDocument();
  });

  it('has role="tabpanel"', () => {
    render(
      <TabPanel tabId="overview" tabsId="my-tabs" active={true}>
        Content
      </TabPanel>,
    );
    expect(screen.getByRole('tabpanel')).toBeInTheDocument();
  });

  it('has correct aria-labelledby', () => {
    render(
      <TabPanel tabId="overview" tabsId="my-tabs" active={true}>
        Content
      </TabPanel>,
    );
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', 'my-tabs-tab-overview');
  });

  it('has matching id for aria-controls linkage', () => {
    render(
      <TabPanel tabId="overview" tabsId="my-tabs" active={true}>
        Content
      </TabPanel>,
    );
    expect(screen.getByRole('tabpanel')).toHaveAttribute('id', 'my-tabs-panel-overview');
  });
});
