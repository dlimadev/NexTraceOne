import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ProtectedRoute } from '../../components/ProtectedRoute';
import type { CurrentUserProfile } from '../../types';

// Mock dos hooks necessários
vi.mock('../../contexts/AuthContext', () => ({
  useAuth: vi.fn(),
}));

import { useAuth } from '../../contexts/AuthContext';

function mockUser(permissions: string[], roleName = 'Developer'): CurrentUserProfile {
  return {
    id: 'user-1',
    email: 'test@acme.com',
    firstName: 'Test',
    lastName: 'User',
    fullName: 'Test User',
    isActive: true,
    lastLoginAt: null,
    tenantId: 'tenant-1',
    roleName,
    permissions,
  };
}

function mockAuthValue(permissions: string[], roleName = 'Developer') {
  return {
    isAuthenticated: true,
    accessToken: 'token',
    user: mockUser(permissions, roleName),
    tenantId: 'tenant-1',
    requiresTenantSelection: false,
    availableTenants: [],
    login: vi.fn(),
    selectTenant: vi.fn(),
    logout: vi.fn(),
  };
}

function renderProtectedRoute(permission: Parameters<typeof ProtectedRoute>[0]['permission'], permissions: string[], roleName = 'Developer') {
  vi.mocked(useAuth).mockReturnValue(mockAuthValue(permissions, roleName));

  return render(
    <MemoryRouter initialEntries={['/protected']}>
      <Routes>
        <Route
          path="/protected"
          element={
            <ProtectedRoute permission={permission}>
              <div>Protected Content</div>
            </ProtectedRoute>
          }
        />
        <Route path="/" element={<div>Dashboard</div>} />
        <Route path="/unauthorized" element={<div>Unauthorized</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe('ProtectedRoute', () => {
  it('renderiza o conteúdo filho quando o usuário tem a permissão', () => {
    renderProtectedRoute('identity:users:read', ['identity:users:read', 'identity:users:write'], 'PlatformAdmin');
    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  it('redireciona para / quando o usuário não tem a permissão', () => {
    renderProtectedRoute('identity:users:read', ['contracts:read', 'contracts:write']);
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });

  it('redireciona para rota customizada quando especificada', () => {
    vi.mocked(useAuth).mockReturnValue(mockAuthValue(['contracts:read']));

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route
            path="/protected"
            element={
              <ProtectedRoute permission="identity:users:read" redirectTo="/unauthorized">
                <div>Protected Content</div>
              </ProtectedRoute>
            }
          />
          <Route path="/unauthorized" element={<div>Unauthorized</div>} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
    expect(screen.getByText('Unauthorized')).toBeInTheDocument();
  });

  it('PlatformAdmin tem acesso à rota de users', () => {
    renderProtectedRoute('identity:users:read', ['identity:users:read', 'identity:users:write'], 'PlatformAdmin');
    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  it('Auditor tem acesso à rota de audit', () => {
    renderProtectedRoute('audit:trail:read', ['audit:trail:read', 'audit:reports:read'], 'Auditor');
    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  it('Developer não tem acesso à rota de audit:reports:read', () => {
    renderProtectedRoute('audit:reports:read', ['contracts:read', 'change-intelligence:read']);
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
  });

  it('usuário sem perfil (null) não tem acesso a rotas protegidas', () => {
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: true,
      accessToken: 'token',
      user: null,
      tenantId: 'tenant-1',
      requiresTenantSelection: false,
      availableTenants: [],
      login: vi.fn(),
      selectTenant: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route
            path="/protected"
            element={
              <ProtectedRoute permission="identity:users:read">
                <div>Protected Content</div>
              </ProtectedRoute>
            }
          />
          <Route path="/" element={<div>Dashboard</div>} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });
});
