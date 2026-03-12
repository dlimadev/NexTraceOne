import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ProtectedRoute } from '../../components/ProtectedRoute';
import type { UserProfile } from '../../types';

// Mock dos hooks necessários
vi.mock('../../contexts/AuthContext', () => ({
  useAuth: vi.fn(),
}));

import { useAuth } from '../../contexts/AuthContext';

function mockUser(roles: string[]): UserProfile {
  return {
    id: 'user-1',
    email: 'test@acme.com',
    fullName: 'Test User',
    roles,
    tenantId: 'tenant-1',
  };
}

function renderProtectedRoute(permission: Parameters<typeof ProtectedRoute>[0]['permission'], roles: string[]) {
  vi.mocked(useAuth).mockReturnValue({
    isAuthenticated: true,
    accessToken: 'token',
    user: mockUser(roles),
    tenantId: 'tenant-1',
    login: vi.fn(),
    logout: vi.fn(),
  });

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
    renderProtectedRoute('users:read', ['Admin']);
    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  it('redireciona para / quando o usuário não tem a permissão', () => {
    renderProtectedRoute('users:read', ['Developer']);
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });

  it('redireciona para rota customizada quando especificada', () => {
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: true,
      accessToken: 'token',
      user: mockUser(['Developer']),
      tenantId: 'tenant-1',
      login: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route
            path="/protected"
            element={
              <ProtectedRoute permission="users:read" redirectTo="/unauthorized">
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

  it('Admin tem acesso à rota de users', () => {
    renderProtectedRoute('users:read', ['Admin']);
    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  it('Manager tem acesso à rota de users', () => {
    renderProtectedRoute('users:read', ['Manager']);
    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  it('Auditor tem acesso à rota de audit', () => {
    renderProtectedRoute('audit:read', ['Auditor']);
    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  it('Developer não tem acesso à rota de audit:export', () => {
    renderProtectedRoute('audit:export', ['Developer']);
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
  });

  it('usuário sem perfil (null) não tem acesso a rotas protegidas', () => {
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: true,
      accessToken: 'token',
      user: null,
      tenantId: 'tenant-1',
      login: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route
            path="/protected"
            element={
              <ProtectedRoute permission="users:read">
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
