import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { AppSidebarItem } from '../../../components/shell/AppSidebarItem';
import { AppSidebarGroup } from '../../../components/shell/AppSidebarGroup';
import { AppSidebarHeader } from '../../../components/shell/AppSidebarHeader';
import { AppSidebarFooter } from '../../../components/shell/AppSidebarFooter';

describe('AppSidebarItem', () => {
  it('renders label and icon in expanded mode', () => {
    render(
      <MemoryRouter>
        <AppSidebarItem
          to="/services"
          icon={<span data-testid="icon">I</span>}
          labelKey="sidebar.serviceCatalog"
        />
      </MemoryRouter>,
    );
    expect(screen.getByText('Service Catalog')).toBeInTheDocument();
    expect(screen.getByTestId('icon')).toBeInTheDocument();
  });

  it('renders icon-only with title tooltip in collapsed mode', () => {
    render(
      <MemoryRouter>
        <AppSidebarItem
          to="/services"
          icon={<span data-testid="icon">I</span>}
          labelKey="sidebar.serviceCatalog"
          collapsed
        />
      </MemoryRouter>,
    );
    expect(screen.queryByText('Service Catalog')).not.toBeInTheDocument();
    expect(screen.getByTitle('Service Catalog')).toBeInTheDocument();
  });
});

describe('AppSidebarGroup', () => {
  it('renders section label and children when expanded', () => {
    render(
      <MemoryRouter>
        <AppSidebarGroup
          sectionKey="services"
          labelKey="sidebar.sectionServices"
          expanded={true}
          hasMultipleItems={true}
          onToggle={vi.fn()}
        >
          <li data-testid="child">Item</li>
        </AppSidebarGroup>
      </MemoryRouter>,
    );
    expect(screen.getByText('Services')).toBeInTheDocument();
    expect(screen.getByTestId('child')).toBeInTheDocument();
  });

  it('hides children when collapsed (not expanded)', () => {
    render(
      <MemoryRouter>
        <AppSidebarGroup
          sectionKey="services"
          labelKey="sidebar.sectionServices"
          expanded={false}
          hasMultipleItems={true}
          onToggle={vi.fn()}
        >
          <li data-testid="child">Item</li>
        </AppSidebarGroup>
      </MemoryRouter>,
    );
    expect(screen.getByText('Services')).toBeInTheDocument();
    expect(screen.queryByTestId('child')).not.toBeInTheDocument();
  });

  it('calls onToggle when section header is clicked', () => {
    const onToggle = vi.fn();
    render(
      <MemoryRouter>
        <AppSidebarGroup
          sectionKey="services"
          labelKey="sidebar.sectionServices"
          expanded={true}
          hasMultipleItems={true}
          onToggle={onToggle}
        >
          <li>Item</li>
        </AppSidebarGroup>
      </MemoryRouter>,
    );
    fireEvent.click(screen.getByText('Services'));
    expect(onToggle).toHaveBeenCalledTimes(1);
  });
});

describe('AppSidebarHeader', () => {
  it('renders brand name in expanded mode', () => {
    render(<AppSidebarHeader collapsed={false} />);
    expect(screen.getByText('NexTraceOne')).toBeInTheDocument();
  });

  it('renders only logo mark in collapsed mode', () => {
    render(<AppSidebarHeader collapsed={true} />);
    expect(screen.getByText('N')).toBeInTheDocument();
    expect(screen.queryByText('NexTraceOne')).not.toBeInTheDocument();
  });
});

describe('AppSidebarFooter', () => {
  it('renders user email and persona in expanded mode', () => {
    render(
      <AppSidebarFooter
        collapsed={false}
        email="test@example.com"
        persona="Engineer"
        roleName="Developer"
        onLogout={vi.fn()}
      />,
    );
    expect(screen.getByText('test@example.com')).toBeInTheDocument();
  });

  it('renders initial letter in collapsed mode', () => {
    render(
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
    render(
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
