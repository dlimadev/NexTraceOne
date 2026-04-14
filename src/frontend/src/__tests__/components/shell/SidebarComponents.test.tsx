import { describe, it, expect, vi } from 'vitest';
import { screen, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '../../test-utils';
import { AppSidebarItem } from '../../../components/shell/AppSidebarItem';
import { AppSidebarGroup } from '../../../components/shell/AppSidebarGroup';
import { AppSidebarHeader } from '../../../components/shell/AppSidebarHeader';
import { AppSidebarFooter } from '../../../components/shell/AppSidebarFooter';

// Mock EnvironmentContext for AppSidebarFooter
vi.mock('../../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn(() => ({
    activeEnvironmentId: 'env-prod',
    activeEnvironment: { id: 'env-prod', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  })),
  EnvironmentProvider: ({ children }: { children: React.ReactNode }) => children,
}));

describe('AppSidebarItem', () => {
  it('renders label and icon in expanded mode', () => {
    renderWithProviders(
      <AppSidebarItem
        to="/services"
        icon={<span data-testid="icon">I</span>}
        labelKey="sidebar.serviceCatalog"
      />,
    );
    expect(screen.getByText('Service Catalog')).toBeInTheDocument();
    expect(screen.getByTestId('icon')).toBeInTheDocument();
  });

  it('renders icon-only with title tooltip in collapsed mode', () => {
    renderWithProviders(
      <AppSidebarItem
        to="/services"
        icon={<span data-testid="icon">I</span>}
        labelKey="sidebar.serviceCatalog"
        collapsed
      />,
    );
    expect(screen.queryByText('Service Catalog')).not.toBeInTheDocument();
    expect(screen.getByTitle('Service Catalog')).toBeInTheDocument();
  });
});

describe('AppSidebarGroup', () => {
  it('renders section label and children when expanded', () => {
    renderWithProviders(
      <AppSidebarGroup
        sectionKey="services"
        labelKey="sidebar.sectionServices"
        expanded={true}
        hasMultipleItems={true}
        onToggle={vi.fn()}
      >
        <li data-testid="child">Item</li>
      </AppSidebarGroup>,
    );
    expect(screen.getByText('Services')).toBeInTheDocument();
    expect(screen.getByTestId('child')).toBeInTheDocument();
  });

  it('hides children when collapsed (not expanded)', () => {
    renderWithProviders(
      <AppSidebarGroup
        sectionKey="services"
        labelKey="sidebar.sectionServices"
        expanded={false}
        hasMultipleItems={true}
        onToggle={vi.fn()}
      >
        <li data-testid="child">Item</li>
      </AppSidebarGroup>,
    );
    expect(screen.getByText('Services')).toBeInTheDocument();
    expect(screen.queryByTestId('child')).not.toBeInTheDocument();
  });

  it('calls onToggle when section header is clicked', () => {
    const onToggle = vi.fn();
    renderWithProviders(
      <AppSidebarGroup
        sectionKey="services"
        labelKey="sidebar.sectionServices"
        expanded={true}
        hasMultipleItems={true}
        onToggle={onToggle}
      >
        <li>Item</li>
      </AppSidebarGroup>,
    );
    fireEvent.click(screen.getByText('Services'));
    expect(onToggle).toHaveBeenCalledTimes(1);
  });
});

describe('AppSidebarHeader', () => {
  it('renders brand name in expanded mode', () => {
    renderWithProviders(<AppSidebarHeader collapsed={false} />);
    expect(screen.getAllByLabelText('NexTraceOne').length).toBeGreaterThanOrEqual(1);
  });

  it('renders only logo mark in collapsed mode', () => {
    renderWithProviders(<AppSidebarHeader collapsed={true} />);
    expect(screen.getByText('N')).toBeInTheDocument();
    expect(screen.queryByText('NexTraceOne')).not.toBeInTheDocument();
  });
});

describe('AppSidebarFooter', () => {
  it('renders environment indicator and quick actions in expanded mode', () => {
    renderWithProviders(<AppSidebarFooter />);
    expect(screen.getByText('Production')).toBeInTheDocument();
    expect(screen.getByText('production')).toBeInTheDocument();
    expect(screen.getByText('Search')).toBeInTheDocument();
    expect(screen.getByText('Docs')).toBeInTheDocument();
    expect(screen.getByTestId('sidebar-version')).toHaveTextContent('NexTraceOne v1.0.0');
  });

  it('renders environment dot and action buttons in collapsed mode', () => {
    renderWithProviders(<AppSidebarFooter collapsed />);
    // Environment button with aria-label
    expect(screen.getByLabelText(/Environment.*Production/i)).toBeInTheDocument();
    // Quick search button
    expect(screen.getByLabelText(/Quick Search/i)).toBeInTheDocument();
    // Settings button
    expect(screen.getByLabelText(/Platform Settings/i)).toBeInTheDocument();
  });

  it('shows help and settings buttons in expanded mode', () => {
    renderWithProviders(<AppSidebarFooter />);
    expect(screen.getByLabelText(/Help/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Platform Settings/i)).toBeInTheDocument();
  });

  it('shows knowledge hub link in expanded mode', () => {
    renderWithProviders(<AppSidebarFooter />);
    expect(screen.getByLabelText(/Knowledge Hub/i)).toBeInTheDocument();
  });
});
