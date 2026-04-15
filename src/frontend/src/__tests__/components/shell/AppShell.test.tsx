import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent } from '@testing-library/react';
import { Route, Routes } from 'react-router-dom';
import type { ReactNode } from 'react';
import { renderWithProviders } from '../../test-utils';
import { AppShell } from '../../../components/shell/AppShell';

// Mock incidents API used by useNavCounters
vi.mock('../../../features/operations/api/incidents', () => ({
  incidentsApi: {
    getIncidentsSummary: vi.fn().mockResolvedValue({ totalOpen: 0, criticalIncidents: 0 }),
  },
}));

// Mock auth context
vi.mock('../../../contexts/AuthContext', () => ({
  useAuth: vi.fn(() => ({
    isAuthenticated: true,
    user: {
      id: '1',
      email: 'admin@nextraceone.io',
      firstName: 'Admin',
      lastName: 'User',
      fullName: 'Admin User',
      isActive: true,
      lastLoginAt: null,
      tenantId: 'tenant-1',
      roleName: 'Admin',
      permissions: ['catalog:assets:read', 'contracts:read', 'identity:users:read'],
    },
    logout: vi.fn(),
    login: vi.fn(),
    selectTenant: vi.fn(),
    requiresTenantSelection: false,
    availableTenants: [],
  })),
}));

// Mock environment context
vi.mock('../../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn(() => ({
    activeEnvironmentId: 'tenant-1-prod',
    activeEnvironment: { id: 'tenant-1-prod', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  })),
  EnvironmentProvider: ({ children }: { children: ReactNode }) => children,
}));

// Mock persona context
vi.mock('../../../contexts/PersonaContext', () => ({
  usePersona: vi.fn(() => ({
    persona: 'Engineer',
    config: {
      sectionOrder: ['home', 'services', 'contracts', 'changes', 'operations', 'knowledge', 'aiHub', 'governance', 'organization', 'analytics', 'integrations', 'admin'],
      highlightedSections: ['services', 'operations'],
      homeSubtitleKey: 'persona.Engineer.homeSubtitle',
      homeWidgets: [],
      quickActions: [],
      aiContextScopes: [],
      aiSuggestedPromptKeys: [],
    },
  })),
}));

// Mock permissions hook
vi.mock('../../../hooks/usePermissions', () => ({
  usePermissions: vi.fn(() => ({
    can: (p: string) => ['catalog:assets:read', 'contracts:read', 'identity:users:read'].includes(p),
    roleName: 'Admin',
    permissions: ['catalog:assets:read', 'contracts:read', 'identity:users:read'],
  })),
}));

// Mock command palette
vi.mock('../../../components/CommandPalette', () => ({
  CommandPalette: ({ open }: { open: boolean }) =>
    open ? <div data-testid="command-palette">Command Palette</div> : null,
}));

function renderShell(route = '/') {
  return renderWithProviders(
    <Routes>
      <Route element={<AppShell />}>
        <Route path="/" element={<div data-testid="dashboard">Dashboard</div>} />
        <Route path="/services" element={<div data-testid="services">Services</div>} />
        <Route path="/contracts" element={<div data-testid="contracts">Contracts</div>} />
      </Route>
    </Routes>,
    { routerProps: { initialEntries: [route] } },
  );
}

describe('AppShell', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the shell structure with sidebar and topbar', () => {
    renderShell();
    expect(screen.getByTestId('app-shell')).toBeInTheDocument();
    expect(screen.getByTestId('app-shell-main')).toBeInTheDocument();
  });

  it('renders the outlet content for the home route', () => {
    renderShell('/');
    expect(screen.getByTestId('dashboard')).toBeInTheDocument();
  });

  it('renders the outlet content for a nested route', () => {
    renderShell('/services');
    expect(screen.getByTestId('services')).toBeInTheDocument();
  });

  it('renders the brand logo in sidebar', () => {
    renderShell();
    expect(screen.getByText('NexTraceOne')).toBeInTheDocument();
  });

  it('renders navigation items for permitted sections', () => {
    renderShell();
    expect(screen.getAllByText('Dashboard').length).toBeGreaterThan(0);
    // The sidebar icon rail renders items with title attributes
    const sidebarNav = screen.getByRole('navigation', { name: /sidebar/i });
    expect(sidebarNav).toBeInTheDocument();
  });

  it('renders the user display name in topbar', () => {
    renderShell();
    // 'Admin User' is the mock user's fullName — rendered by AppUserMenu in the topbar
    expect(screen.getAllByText('Admin User').length).toBeGreaterThan(0);
  });

  it('opens command palette with Ctrl+K', () => {
    renderShell();
    expect(screen.queryByTestId('command-palette')).not.toBeInTheDocument();

    fireEvent.keyDown(window, { key: 'k', ctrlKey: true });
    expect(screen.getByTestId('command-palette')).toBeInTheDocument();
  });

  it('renders the search trigger in topbar', () => {
    renderShell();
    const searchButton = screen.getByLabelText('Command Palette');
    expect(searchButton).toBeInTheDocument();
  });

  it('renders the user menu trigger in topbar', () => {
    renderShell();
    expect(screen.getByTestId('user-menu-trigger')).toBeInTheDocument();
  });
});
