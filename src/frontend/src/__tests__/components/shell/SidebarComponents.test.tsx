import { describe, it, expect, vi } from 'vitest';
import { screen, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '../../test-utils';
import { AppSidebarItem } from '../../../components/shell/AppSidebarItem';
import { AppSidebarGroup } from '../../../components/shell/AppSidebarGroup';
import { AppSidebarHeader } from '../../../components/shell/AppSidebarHeader';
import { AppSidebarFooter } from '../../../components/shell/AppSidebarFooter';

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
  it('renders user display name and persona in expanded mode', () => {
    renderWithProviders(
      <AppSidebarFooter
        collapsed={false}
        email="test@example.com"
        persona="Engineer"
        roleName="Developer"
        onLogout={vi.fn()}
      />,
    );
    expect(screen.getByText('test')).toBeInTheDocument();
  });

  it('renders initial letter in collapsed mode', () => {
    renderWithProviders(
      <AppSidebarFooter
        collapsed={true}
        email="test@example.com"
        persona="Engineer"
        roleName="Developer"
        onLogout={vi.fn()}
      />,
    );
    expect(screen.getByText('T')).toBeInTheDocument();
  });

  it('calls onLogout when logout button is clicked', () => {
    const onLogout = vi.fn();
    renderWithProviders(
      <AppSidebarFooter
        collapsed={false}
        email="test@example.com"
        persona="Engineer"
        roleName="Developer"
        onLogout={onLogout}
      />,
    );
    fireEvent.click(screen.getByLabelText('Sign out'));
    expect(onLogout).toHaveBeenCalledTimes(1);
  });
});
